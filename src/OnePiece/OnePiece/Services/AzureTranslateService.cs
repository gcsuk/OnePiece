using Azure;
using Azure.AI.Translation.Text;
using Microsoft.Extensions.Configuration;

namespace OnePiece.Services;

public interface IAzureTranslateService
{
    Task<string> TranslateJapaneseToEnglishAsync(string japaneseText);
}

public class AzureTranslateService : IAzureTranslateService
{
    private readonly string _endpoint;
    private readonly string _apiKey;

    public AzureTranslateService(IConfiguration configuration)
    {
        _endpoint = configuration["AzureTranslate:Endpoint"] ?? throw new InvalidOperationException("Azure Translator endpoint not configured");
        _apiKey = configuration["AzureTranslate:ApiKey"] ?? throw new InvalidOperationException("Azure Translator API key not configured");
        
        // Debug logging (remove in production)
        Console.WriteLine($"Azure Translator Endpoint: {_endpoint}");
        Console.WriteLine($"Azure Translator API Key: {_apiKey.Substring(0, Math.Min(8, _apiKey.Length))}...");
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
                targetLanguages: new[] { "en" },  // English
                content: new[] { japaneseText },
                sourceLanguage: "ja"  // Japanese
            );

            if (response?.Value != null && response.Value.Count > 0)
            {
                var firstResult = response.Value[0];
                if (firstResult?.Translations != null && firstResult.Translations.Count > 0)
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
