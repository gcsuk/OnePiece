namespace OnePiece.Configuration;

public class AzureTranslateOptions
{
    public const string SectionName = "AzureTranslate";
    
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
