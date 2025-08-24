using Microsoft.Extensions.Options;
using OnePiece.Configuration;
using OnePiece.Models;
using OpenAI;
using OpenAI.Chat;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Net.Http.Headers;
using System.Text.Json;
using Size = SixLabors.ImageSharp.Size;

namespace OnePiece.Services;

public class OpenAIVisionService : IOpenAIVisionService
{
    private readonly OpenAIClient _openAIClient;
    private readonly OpenAIOptions _options;

    public OpenAIVisionService(IOptions<OpenAIOptions> options)
    {
        _options = options.Value;
        _openAIClient = new OpenAIClient(_options.ApiKey);
    }

    public async Task<OnePieceCard> AnalyzeOnePieceCardAsync(Stream imageStream)
    {
        try
        {
            // Ensure the stream is at the beginning
            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
            }
            
            // 1. Downscale image to minimize costs (1024px max side, 85% quality)
            using var downsizedJpeg = await DownscaleToJpegAsync(imageStream, 1024, 85);
            
            // 2. Create optimized messages with strict JSON schema
            var messages = new List<Message>
            {
                new(Role.System, CreateSystemPrompt()),
                new(Role.User, new List<Content>
                {
                    new(ContentType.Text, CreateUserPrompt()),
                    new(ContentType.ImageUrl, $"data:image/jpeg;base64,{Convert.ToBase64String(downsizedJpeg.ToArray())}")
                })
            };

            // 3. Create chat completion options with strict JSON schema
            var chatRequest = new ChatRequest(
                messages: messages,
                model: _options.Model,
                maxTokens: _options.MaxTokens,
                temperature: 0.2f,
                responseFormat: TextResponseFormat.JsonSchema
            );

            // Debug: Log the request (remove in production)
            Console.WriteLine($"OpenAI API Request - Model: {_options.Model}, MaxTokens: {_options.MaxTokens}");

            // 4. Get completion directly
            var completion = await _openAIClient.ChatEndpoint.GetCompletionAsync(chatRequest);
            var content = completion.Choices[0].Message.Content;
            
            Console.WriteLine($"OpenAI Response: {content}");
            
            // Deserialize directly from the content
            OnePieceCard cardData = null!;

            if (content.ValueKind == JsonValueKind.String)
            {
                // unwrap the string
                var innerJson = content.GetString();
                cardData = JsonSerializer.Deserialize<OnePieceCard>(innerJson!);
            }
            else if (content.ValueKind == JsonValueKind.Object)
            {
                // already a JSON object
                cardData = JsonSerializer.Deserialize<OnePieceCard>(content.ToString());
            }
            
            cardData.Timestamp = DateTime.UtcNow;
            Console.WriteLine("Deserialization successful!");
            return cardData;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error analyzing OnePiece card with OpenAI: {ex.Message}", ex);
        }
    }

    public async Task<(OnePieceCard CardData, byte[] TranslatedImage)> AnalyzeAndTranslateCardAsync(Stream imageStream)
    {
        try
        {
            // Copy the stream to a MemoryStream so we can seek and reuse it
            var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            
            // First analyze the card
            var cardData = await AnalyzeOnePieceCardAsync(memoryStream);
            
            // Reset stream position for image generation
            memoryStream.Position = 0;
            
            // Generate the translated image
            var translatedImage = await CreateEnglishOverlayImageAsync(memoryStream, cardData);
            
            return (cardData, translatedImage);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error analyzing and translating OnePiece card: {ex.Message}", ex);
        }
    }

    public async Task<byte[]> CreateEnglishOverlayImageAsync(Stream originalImageStream, OnePieceCard cardData)
    {
        // Ensure the stream is at the beginning
        if (originalImageStream.CanSeek)
        {
            originalImageStream.Position = 0;
        }
        
        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            await originalImageStream.CopyToAsync(ms);
            bytes = ms.ToArray();
        }

        return await TranslateCardToEnglishAsync(bytes, "image/jpeg", _options.ApiKey, size: "auto");
    }

    private static async Task<MemoryStream> DownscaleToJpegAsync(Stream imageStream, int maxSide, int quality)
    {
        // Ensure the stream is at the beginning
        if (imageStream.CanSeek)
        {
            imageStream.Position = 0;
        }
        
        using var image = await Image.LoadAsync(imageStream);
        var scale = Math.Min(1.0, (double)maxSide / Math.Max(image.Width, image.Height));
        var targetW = (int)Math.Round(image.Width * scale);
        var targetH = (int)Math.Round(image.Height * scale);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(targetW, targetH),
            Mode = ResizeMode.Max
        }));

        var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = quality });
        ms.Position = 0;
        return ms;
    }

    private static string CreateSystemPrompt() =>
        """
        You are an expert OnePiece card collector and analyst. Analyze this image of a OnePiece trading card and extract all relevant information.

        Please provide a detailed analysis in the following JSON format, filling in as many fields as you can identify from the image:

        {
          "cardName": "The official name of the card",
          "characterName": "The name of the OnePiece character featured on the card",
          "cardType": "Type of card (e.g., Character, Event, Stage, Leader, etc.)",
          "rarity": "Card rarity (e.g., Common, Uncommon, Rare, Super Rare, Secret Rare, etc.)",
          "power": "Power/attack value if shown",
          "cost": "Cost to play the card if shown",
          "attribute": "Card attribute (e.g., Red, Blue, Green, Purple, Black, etc.)",
          "cardNumber": "Card number in the set",
          "setName": "Name of the card set/expansion",
          "cardText": "Full card text/effect in Japanese",
          "cardTextEnglish": "English translation of the card text if you can provide it",
          "artworkDescription": "Description of the card artwork and character pose",
          "series": "OnePiece series/arc this card represents",
          "releaseDate": "Release date if visible",
          "collectorNumber": "Collector number or other identifying numbers",
          "condition": "Card condition if visible (e.g., Mint, Near Mint, etc.)",
          "estimatedValue": "Estimated market value if you can determine it",
          "notes": "Any additional observations or notes about the card",
          "confidence": 0.95
        }

        Important guidelines:
        1. If you cannot read certain text clearly, mark it as "Not visible" or "Unclear"
        2. For Japanese text you can read, provide the exact characters
        3. For card text, try to capture the complete effect text
        4. Be specific about card types and attributes
        5. If this appears to be a specific OnePiece card game (like OnePiece TCG), note that in your analysis
        6. Set confidence to 0.95 if you're very certain, lower if you're less certain about some elements

        Analyze the image carefully and provide the most accurate and complete information possible.
        """;

    private static string CreateUserPrompt() =>
        """
        Extract all visible details from the attached image and output ONLY a single valid JSON object conforming to the SCHEMA.
        Keep line breaks in rules text as \n; normalize whitespace; no extra keys, no comments, no markdown.
        IMPORTANT: Return ONLY the JSON object, no additional text, no explanations.

        SCHEMA:
        {
          "name_jp": "string or null",
          "name_en": "string or null",
          "type": "Event or Character or Leader or Stage or null",
          "color": "Red or Green or Blue or Purple or Black or Yellow or Dual or Unknown or null",
          "cost": "number or null",
          "power": "number or null",
          "attribute": "Slash or Strike or Special or Ranged or Wisdom or Unknown or null",
          "traits": ["string"] or null,
          "effect_main_jp": "string or null",
          "effect_main_en": "string or null",
          "effect_counter_jp": "string or null",
          "effect_counter_en": "string or null",
          "effect_trigger_jp": "string or null",
          "effect_trigger_en": "string or null",
          "set_code": "string or null",
          "collector_number": "string or null",
          "rarity": "C or U or R or SR or L or SEC or P or SP or Unknown or null",
          "artist": "string or null",
          "copyright_footer": "string or null",
          "notes": "string or null",
          "bbox_text_regions": [
             {"label":"name","x":0,"y":0,"w":0,"h":0},
             {"label":"main_text","x":0,"y":0,"w":0,"h":0}
          ] or null,
          "confidences": {
            "name": "number or null",
            "type": "number or null",
            "cost": "number or null",
            "color": "number or null",
            "effects": "number or null",
            "set_code": "number or null",
            "collector_number": "number or null",
            "rarity": "number or null"
          }
        }
        """;

    public static async Task<byte[]> TranslateCardToEnglishAsync(
        byte[] imageBytes,
        string imageMime,                   // "image/png" or "image/jpeg"
        string apiKey,
        string? promptOverride = null,
        byte[]? maskPng = null,             // PNG with transparent regions where text should be replaced
        string size = "1024x1024",          // 256x256 | 512x512 | 1024x1024
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("Missing OpenAI API key.", nameof(apiKey));

        using var http = new HttpClient();
        http.BaseAddress = new Uri("https://api.openai.com/v1/");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        // Use a focused, deterministic prompt
        var prompt = promptOverride ??
            "Replace all Japanese text in this trading card with accurate English equivalents. " +
            "Preserve original layout, borders, art, icons, symbols, and costs. " +
            "Use clean, readable typography and align text to existing boxes.";

        using var form = new MultipartFormDataContent
        {
            { new StringContent("gpt-image-1"), "model" },
            { new StringContent(prompt), "prompt" },
            { new StringContent(size), "size" }
        };

        // Image part (honor the actual MIME)
        var img = new ByteArrayContent(imageBytes);
        img.Headers.ContentType = new MediaTypeHeaderValue(imageMime);
        var imgFileName = imageMime == "image/png" ? "card.png" : "card.jpg";
        form.Add(img, "image", imgFileName);

        // Optional mask (must be PNG; transparent where text should be replaced)
        if (maskPng is not null)
        {
            var mask = new ByteArrayContent(maskPng);
            mask.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(mask, "mask", "mask.png");
        }

        using var req = new HttpRequestMessage(HttpMethod.Post, "images/edits") { Content = form };
        using var resp = await http.SendAsync(req, ct).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
        {
            var errorMsg = TryExtractOpenAiError(body) ?? body;
            throw new HttpRequestException($"OpenAI Images API error ({(int)resp.StatusCode} {resp.ReasonPhrase}): {errorMsg}");
        }

        using var doc = JsonDocument.Parse(body);
        var b64 = doc.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetString();
        if (string.IsNullOrWhiteSpace(b64))
            throw new InvalidOperationException("No image data returned from OpenAI.");

        return Convert.FromBase64String(b64);
    }

    private static string? TryExtractOpenAiError(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out var err))
            {
                var msg = err.TryGetProperty("message", out var m) ? m.GetString() : null;
                var t = err.TryGetProperty("type", out var tp) ? tp.GetString() : null;
                return string.IsNullOrWhiteSpace(t) ? msg : $"{t}: {msg}";
            }
        }
        catch { /* ignore parse issues */ }
        return null;
    }
}
