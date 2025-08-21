using System.Text.Json;
using System.Text.Json.Serialization;

namespace OnePiece.Services;

public interface IAzureVisionService
{
    Task<string> AnalyzeImageForJapaneseTextAsync(Stream imageStream);
}

public class AzureVisionService : IAzureVisionService
{
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;

    public AzureVisionService(IConfiguration configuration, HttpClient httpClient)
    {
        _endpoint = configuration["AzureVision:Endpoint"] ?? throw new InvalidOperationException("Azure Vision endpoint not configured");
        _apiKey = configuration["AzureVision:ApiKey"] ?? throw new InvalidOperationException("Azure Vision API key not configured");
        _httpClient = httpClient;
    }

    public async Task<string> AnalyzeImageForJapaneseTextAsync(Stream imageStream)
    {
        try
        {
            // Read the image stream into a byte array
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();

            // Create the request URL for OCR (Optical Character Recognition)
            var requestUrl = $"{_endpoint.TrimEnd('/')}/vision/v3.2/read/analyze";

            // Create the HTTP request
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
            request.Content = new ByteArrayContent(imageBytes);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            // Send the initial request
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Azure Vision API error: {response.StatusCode} - {errorContent}");
            }

            // Get the operation location from headers
            var operationLocation = response.Headers.GetValues("Operation-Location").FirstOrDefault();
            if (string.IsNullOrEmpty(operationLocation))
            {
                throw new InvalidOperationException("No operation location received from Azure Vision API");
            }

            // Poll for results
            var maxRetries = 10;
            var retryCount = 0;
            
            while (retryCount < maxRetries)
            {
                await Task.Delay(1000); // Wait 1 second between retries
                
                using var getRequest = new HttpRequestMessage(HttpMethod.Get, operationLocation);
                getRequest.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
                
                var getResponse = await _httpClient.SendAsync(getRequest);
                
                if (getResponse.IsSuccessStatusCode)
                {
                    var resultContent = await getResponse.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<AzureVisionResult>(resultContent);
                    
                    if (result?.Status == "succeeded")
                    {
                        return ExtractTextFromResult(result);
                    }

                    if (result?.Status == "failed")
                    {
                        throw new InvalidOperationException($"Azure Vision analysis failed: {result?.Error?.Message ?? "Unknown error"}");
                    }
                    // If still running, continue polling
                }
                
                retryCount++;
            }
            
            throw new InvalidOperationException("Azure Vision analysis timed out");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error analyzing image with Azure Vision: {ex.Message}", ex);
        }
    }

    private string ExtractTextFromResult(AzureVisionResult result)
    {
        if (result?.AnalyzeResult?.ReadResults == null || !result.AnalyzeResult.ReadResults.Any())
        {
            return "No text content detected in the image.";
        }

        var extractedText = new List<string>();
        
        foreach (var readResult in result.AnalyzeResult.ReadResults)
        {
            foreach (var line in readResult.Lines)
            {
                var lineText = string.Join("", line.Words.Select(w => w.Text));
                if (!string.IsNullOrWhiteSpace(lineText))
                {
                    extractedText.Add(lineText);
                }
            }
        }

        return extractedText.Any() ? string.Join("\n", extractedText) : "No text content detected in the image.";
    }
}

// Response models for Azure Vision API
public class AzureVisionResult
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    
    [JsonPropertyName("createdDateTime")]
    public DateTime? CreatedDateTime { get; set; }
    
    [JsonPropertyName("lastUpdatedDateTime")]
    public DateTime? LastUpdatedDateTime { get; set; }
    
    [JsonPropertyName("analyzeResult")]
    public AnalyzeResult? AnalyzeResult { get; set; }
    
    [JsonPropertyName("error")]
    public Error? Error { get; set; }
}

public class AnalyzeResult
{
    [JsonPropertyName("readResults")]
    public List<ReadResult>? ReadResults { get; set; }
}

public class ReadResult
{
    [JsonPropertyName("page")]
    public int Page { get; set; }
    
    [JsonPropertyName("angle")]
    public double Angle { get; set; }
    
    [JsonPropertyName("width")]
    public int Width { get; set; }
    
    [JsonPropertyName("height")]
    public int Height { get; set; }
    
    [JsonPropertyName("unit")]
    public string? Unit { get; set; }
    
    [JsonPropertyName("lines")]
    public List<Line>? Lines { get; set; }
}

public class Line
{
    [JsonPropertyName("boundingBox")]
    public List<int>? BoundingBox { get; set; }
    
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [JsonPropertyName("words")]
    public List<Word>? Words { get; set; }
}

public class Word
{
    [JsonPropertyName("boundingBox")]
    public List<int>? BoundingBox { get; set; }
    
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}

public class Error
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
