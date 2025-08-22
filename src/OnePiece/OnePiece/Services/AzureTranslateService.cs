using Azure;
using Azure.AI.Translation.Text;
using Microsoft.Extensions.Options;
using OnePiece.Configuration;

namespace OnePiece.Services;

public interface IAzureTranslateService
{
    Task<string> TranslateJapaneseToEnglishAsync(string japaneseText);
}

public class AzureTranslateService : IAzureTranslateService
{
    private readonly string _endpoint;
    private readonly string _apiKey;

    public AzureTranslateService(IOptions<AzureTranslateOptions> options)
    {
        var config = options.Value;
        _endpoint = config.Endpoint ?? throw new InvalidOperationException("Azure Translator endpoint not configured");
        _apiKey = config.ApiKey ?? throw new InvalidOperationException("Azure Translator API key not configured");
    }

    public async Task<string> TranslateJapaneseToEnglishAsync(string japaneseText)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(japaneseText))
            {
                return "No text to translate.";
            }

            // Create the Azure Translator client using the SDK
            var credential = new AzureKeyCredential(_apiKey);
            var client = new TextTranslationClient(credential, new Uri(_endpoint), region: "uksouth");

            // Perform the translation
            var response = await client.TranslateAsync(
                targetLanguages: ["en"],
                content: [japaneseText],
                sourceLanguage: "ja"
            );

            if (response?.Value is { Count: > 0 })
            {
                var firstResult = response.Value[0];
                if (firstResult?.Translations is { Count: > 0 })
                {
                    var translation = firstResult.Translations[0];
                    return translation?.Text ?? "Translation failed.";
                }
            }

            return "Translation failed - no response received.";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error translating text with Azure Translator: {ex.Message}", ex);
        }
    }
}
