namespace OnePiece.Configuration;

public class OpenAIOptions
{
    public const string SectionName = "OpenAI";
    
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini"; // Most cost-effective model for vision tasks
    public int MaxTokens { get; set; } = 300; // Reduced from 500 for cost optimization
    public float Temperature { get; set; } = 0.1f; // Lower temperature for more consistent results
}
