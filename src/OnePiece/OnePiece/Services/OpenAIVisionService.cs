using System.Text.Json;
using Microsoft.Extensions.Options;
using OnePiece.Configuration;
using OnePiece.Models;
using OpenAI;
using OpenAI.Chat;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace OnePiece.Services;

public class OpenAIVisionService : IOpenAIVisionService
{
    private readonly OpenAIClient _openAIClient;
    private readonly OpenAIOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public OpenAIVisionService(IOptions<OpenAIOptions> options)
    {
        _options = options.Value;
        _openAIClient = new OpenAIClient(_options.ApiKey);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public async Task<OnePieceCard> AnalyzeOnePieceCardAsync(Stream imageStream)
    {
        try
        {
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
                cardData = content.Deserialize<OnePieceCard>();
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

    private static async Task<MemoryStream> DownscaleToJpegAsync(Stream imageStream, int maxSide, int quality)
    {
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



    private string CreateSystemPrompt()
    {
        return @"You are an expert OnePiece card collector and analyst. Analyze this image of a OnePiece trading card and extract all relevant information.

Please provide a detailed analysis in the following JSON format, filling in as many fields as you can identify from the image:

{
  ""cardName"": ""The official name of the card"",
  ""characterName"": ""The name of the OnePiece character featured on the card"",
  ""cardType"": ""Type of card (e.g., Character, Event, Stage, Leader, etc.)"",
  ""rarity"": ""Card rarity (e.g., Common, Uncommon, Rare, Super Rare, Secret Rare, etc.)"",
  ""power"": ""Power/attack value if shown"",
  ""cost"": ""Cost to play the card if shown"",
  ""attribute"": ""Card attribute (e.g., Red, Blue, Green, Purple, Black, etc.)"",
  ""cardNumber"": ""Card number in the set"",
  ""setName"": ""Name of the card set/expansion"",
  ""cardText"": ""Full card text/effect in Japanese"",
  ""cardTextEnglish"": ""English translation of the card text if you can provide it"",
  ""artworkDescription"": ""Description of the card artwork and character pose"",
  ""series"": ""OnePiece series/arc this card represents"",
  ""releaseDate"": ""Release date if visible"",
  ""collectorNumber"": ""Collector number or other identifying numbers"",
  ""condition"": ""Card condition if visible (e.g., Mint, Near Mint, etc.)"",
  ""estimatedValue"": ""Estimated market value if you can determine it"",
  ""notes"": ""Any additional observations or notes about the card"",
  ""confidence"": 0.95
}

Important guidelines:
1. If you cannot read certain text clearly, mark it as ""Not visible"" or ""Unclear""
2. For Japanese text you can read, provide the exact characters
3. For card text, try to capture the complete effect text
4. Be specific about card types and attributes
5. If this appears to be a specific OnePiece card game (like OnePiece TCG), note that in your analysis
6. Set confidence to 0.95 if you're very certain, lower if you're less certain about some elements

Analyze the image carefully and provide the most accurate and complete information possible.";
    }

    private string CreateUserPrompt()
    {
        return @"Extract all visible details from the attached image and output ONLY a single valid JSON object conforming to the SCHEMA.
Keep line breaks in rules text as \n; normalize whitespace; no extra keys, no comments, no markdown.
IMPORTANT: Return ONLY the JSON object, no additional text, no explanations.

SCHEMA:
{
  ""name_jp"": ""string or null"",
  ""name_en"": ""string or null"",
  ""type"": ""Event or Character or Leader or Stage or null"",
  ""color"": ""Red or Green or Blue or Purple or Black or Yellow or Dual or Unknown or null"",
  ""cost"": ""number or null"",
  ""power"": ""number or null"",
  ""attribute"": ""Slash or Strike or Special or Ranged or Wisdom or Unknown or null"",
  ""traits"": [""string""] or null,
  ""effect_main_jp"": ""string or null"",
  ""effect_main_en"": ""string or null"",
  ""effect_counter_jp"": ""string or null"",
  ""effect_counter_en"": ""string or null"",
  ""effect_trigger_jp"": ""string or null"",
  ""effect_trigger_en"": ""string or null"",
  ""set_code"": ""string or null"",
  ""collector_number"": ""string or null"",
  ""rarity"": ""C or U or R or SR or L or SEC or P or SP or Unknown or null"",
  ""artist"": ""string or null"",
  ""copyright_footer"": ""string or null"",
  ""notes"": ""string or null"",
  ""bbox_text_regions"": [
     {""label"":""name"",""x"":0,""y"":0,""w"":0,""h"":0},
     {""label"":""main_text"",""x"":0,""y"":0,""w"":0,""h"":0}
  ] or null,
  ""confidences"": {
    ""name"": ""number or null"",
    ""type"": ""number or null"",
    ""cost"": ""number or null"",
    ""color"": ""number or null"",
    ""effects"": ""number or null"",
    ""set_code"": ""number or null"",
    ""collector_number"": ""number or null"",
    ""rarity"": ""number or null""
  }
}";
    }
    

}
