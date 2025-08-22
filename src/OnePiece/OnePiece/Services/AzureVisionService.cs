using Azure;
using Azure.AI.Vision.ImageAnalysis;
using Microsoft.Extensions.Configuration;

namespace OnePiece.Services;

public interface IAzureVisionService
{
    Task<string> AnalyzeImageForJapaneseTextAsync(Stream imageStream);
}

public class AzureVisionService : IAzureVisionService
{
    private readonly string _endpoint;
    private readonly string _apiKey;

    public AzureVisionService(IConfiguration configuration)
    {
        _endpoint = configuration["AzureVision:Endpoint"] ?? throw new InvalidOperationException("Azure Vision endpoint not configured");
        _apiKey = configuration["AzureVision:ApiKey"] ?? throw new InvalidOperationException("Azure Vision API key not configured");
        
        // Debug logging (remove in production)
        Console.WriteLine($"Azure Vision Endpoint: {_endpoint}");
        Console.WriteLine($"Azure Vision API Key: {_apiKey.Substring(0, Math.Min(8, _apiKey.Length))}...");
    }

    public async Task<string> AnalyzeImageForJapaneseTextAsync(Stream imageStream)
    {
        try
        {
            // Read the image stream into a byte array
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();

            // Create the Azure Vision client using the SDK
            var credential = new AzureKeyCredential(_apiKey);
            var client = new ImageAnalysisClient(new Uri(_endpoint), credential);

            // Analyze the image for text (OCR)
            var result = await client.AnalyzeAsync(BinaryData.FromBytes(imageBytes), VisualFeatures.Read);

            // Debug: Let's see what properties are available
            Console.WriteLine($"Result type: {result?.GetType()}");
            Console.WriteLine($"Result.Value type: {result?.Value?.GetType()}");
            
            if (result?.Value != null)
            {
                var analysisResult = result.Value;
                Console.WriteLine($"Available properties: {string.Join(", ", analysisResult.GetType().GetProperties().Select(p => p.Name))}");
                
                // Try to access Read/OCR results
                var readProperty = analysisResult.GetType().GetProperty("Read");
                if (readProperty != null)
                {
                    var readResult = readProperty.GetValue(analysisResult);
                    Console.WriteLine($"Read result type: {readResult?.GetType()}");
                    return ExtractTextFromReadResult(readResult);
                }
            }

            return "No text content detected in the image.";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error analyzing image with Azure Vision: {ex.Message}", ex);
        }
    }

    private string ExtractTextFromReadResult(object? readResult)
    {
        if (readResult == null)
        {
            return "No text content detected in the image.";
        }

        try
        {
            var extractedText = new List<string>();
            
            // Use reflection to navigate the SDK structure
            var blocksProperty = readResult.GetType().GetProperty("Blocks");
            if (blocksProperty != null)
            {
                var blocks = blocksProperty.GetValue(readResult);
                if (blocks is System.Collections.IEnumerable enumerable)
                {
                    foreach (var block in enumerable)
                    {
                        var linesProperty = block?.GetType().GetProperty("Lines");
                        if (linesProperty != null)
                        {
                            var lines = linesProperty.GetValue(block);
                            if (lines is System.Collections.IEnumerable linesEnumerable)
                            {
                                foreach (var line in linesEnumerable)
                                {
                                    var textProperty = line?.GetType().GetProperty("Text");
                                    if (textProperty != null)
                                    {
                                        var lineText = textProperty.GetValue(line) as string;
                                        if (!string.IsNullOrWhiteSpace(lineText))
                                        {
                                            extractedText.Add(lineText);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            return extractedText.Any() ? string.Join("\n", extractedText) : "No text content detected in the image.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting text: {ex.Message}");
            return "Error extracting text from image.";
        }
    }
}
