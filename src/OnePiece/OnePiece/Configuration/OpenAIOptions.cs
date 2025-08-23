namespace OnePiece.Configuration;

public class OpenAIOptions
{
    public const string SectionName = "OpenAI";
    
    public string ApiKey { get; set; } = string.Empty;
            public string Model { get; set; } = "gpt-4o-mini"; // Cost-optimized model
            public int MaxTokens { get; set; } = 500;
        public float Temperature { get; set; } = 0.2f;
}
