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
            
            // 1. Downscale image to minimize costs (600px max side, 70% quality for cost optimization)
            using var downsizedJpeg = await DownscaleToJpegAsync(imageStream, 600, 70);
            
            // 2. Create optimized messages with minimal token usage
            var messages = new List<Message>
            {
                new(Role.System, CreateOptimizedSystemPrompt()),
                new(Role.User, new List<Content>
                {
                    new(ContentType.Text, CreateOptimizedUserPrompt()),
                    new(ContentType.ImageUrl, $"data:image/jpeg;base64,{Convert.ToBase64String(downsizedJpeg.ToArray())}")
                })
            };

            // 3. Create chat completion options with strict JSON schema and lower token limits
            var chatRequest = new ChatRequest(
                messages: messages,
                model: _options.Model,
                maxTokens: 300, // Reduced from 500
                temperature: 0.1f, // Lower temperature for more consistent results
                responseFormat: TextResponseFormat.JsonSchema
            );

            // Debug: Log the request (remove in production)
            Console.WriteLine($"OpenAI API Request - Model: {_options.Model}, MaxTokens: 300, ImageSize: 600px");

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
            
            // Generate the translated image with optimized settings
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

        // Use smaller image size for cost optimization
        return await TranslateCardToEnglishAsync(bytes, "image/jpeg", _options.ApiKey, size: "512x512");
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

    // Optimized system prompt - reduced from ~200 tokens to ~80 tokens
    private static string CreateOptimizedSystemPrompt() =>
        """
        Analyze this OnePiece trading card image. Extract card details in JSON format.
        
        Guidelines:
        - Read Japanese text accurately
        - Identify card type, color, cost, power, rarity
        - Capture effect text completely
        - Set confidence scores (0.0-1.0)
        - Return valid JSON only
        """;

    // Optimized user prompt - reduced from ~150 tokens to ~60 tokens
    private static string CreateOptimizedUserPrompt() =>
        """
        Extract card details. Return ONLY valid JSON matching this schema:
        
        {
          "name_jp": "string|null",
          "name_en": "string|null", 
          "type": "Event|Character|Leader|Stage|null",
          "color": "Red|Green|Blue|Purple|Black|Yellow|null",
          "cost": "number|null",
          "power": "number|null",
          "attribute": "Slash|Strike|Special|Ranged|Wisdom|null",
          "traits": ["string"]|null,
          "effect_main_jp": "string|null",
          "effect_main_en": "string|null",
          "effect_counter_jp": "string|null", 
          "effect_counter_en": "string|null",
          "effect_trigger_jp": "string|null",
          "effect_trigger_en": "string|null",
          "set_code": "string|null",
          "collector_number": "string|null",
          "rarity": "C|U|R|SR|L|SEC|P|SP|null",
          "artist": "string|null",
          "notes": "string|null",
          "confidences": {
            "name": "number|null",
            "type": "number|null", 
            "cost": "number|null",
            "color": "number|null",
            "effects": "number|null"
          }
        }
        """;

    public static async Task<byte[]> TranslateCardToEnglishAsync(
        byte[] imageBytes,
        string imageMime,
        string apiKey,
        string? promptOverride = null,
        byte[]? maskPng = null,
        string size = "512x512", // Default to 512x512 for cost optimization
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("Missing OpenAI API key.", nameof(apiKey));

        using var http = new HttpClient();
        http.BaseAddress = new Uri("https://api.openai.com/v1/");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        // Optimized, concise prompt
        var prompt = promptOverride ??
            "Replace Japanese text with English. Preserve layout, art, icons, costs.";

        using var form = new MultipartFormDataContent
        {
            { new StringContent("gpt-image-1"), "model" },
            { new StringContent(prompt), "prompt" },
            { new StringContent(size), "size" }
        };

        // Image part
        var img = new ByteArrayContent(imageBytes);
        img.Headers.ContentType = new MediaTypeHeaderValue(imageMime);
        var imgFileName = imageMime == "image/png" ? "card.png" : "card.jpg";
        form.Add(img, "image", imgFileName);

        // Optional mask
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
