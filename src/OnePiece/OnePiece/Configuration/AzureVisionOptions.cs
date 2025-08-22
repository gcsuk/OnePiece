namespace OnePiece.Configuration;

public class AzureVisionOptions
{
    public const string SectionName = "AzureVision";
    
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
