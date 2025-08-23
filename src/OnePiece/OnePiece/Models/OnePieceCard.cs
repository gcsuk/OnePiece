using System.Text.Json.Serialization;

namespace OnePiece.Models;

public class OnePieceCard
{
    [JsonPropertyName("name_jp")]
    public string? NameJapanese { get; set; }
    
    [JsonPropertyName("name_en")]
    public string? NameEnglish { get; set; }
    
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("color")]
    public string? Color { get; set; }
    
    [JsonPropertyName("cost")]
    public int? Cost { get; set; }
    
    [JsonPropertyName("power")]
    public int? Power { get; set; }
    
    [JsonPropertyName("attribute")]
    public string? Attribute { get; set; }
    
    [JsonPropertyName("traits")]
    public List<string>? Traits { get; set; }
    
    [JsonPropertyName("effect_main_jp")]
    public string? EffectMainJapanese { get; set; }
    
    [JsonPropertyName("effect_main_en")]
    public string? EffectMainEnglish { get; set; }
    
    [JsonPropertyName("effect_counter_jp")]
    public string? EffectCounterJapanese { get; set; }
    
    [JsonPropertyName("effect_counter_en")]
    public string? EffectCounterEnglish { get; set; }
    
    [JsonPropertyName("effect_trigger_jp")]
    public string? EffectTriggerJapanese { get; set; }
    
    [JsonPropertyName("effect_trigger_en")]
    public string? EffectTriggerEnglish { get; set; }
    
    [JsonPropertyName("set_code")]
    public string? SetCode { get; set; }
    
    [JsonPropertyName("collector_number")]
    public string? CollectorNumber { get; set; }
    
    [JsonPropertyName("rarity")]
    public string? Rarity { get; set; }
    
    [JsonPropertyName("artist")]
    public string? Artist { get; set; }
    
    [JsonPropertyName("copyright_footer")]
    public string? CopyrightFooter { get; set; }
    
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
    
    [JsonPropertyName("bbox_text_regions")]
    public List<BoundingBox>? BoundingBoxes { get; set; }
    
    [JsonPropertyName("confidences")]
    public ConfidenceScores Confidences { get; set; } = new();
    
    [JsonPropertyName("extractionMethod")]
    public string ExtractionMethod { get; set; } = "OpenAI Vision API (Optimized)";
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class BoundingBox
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;
    
    [JsonPropertyName("x")]
    public float X { get; set; }
    
    [JsonPropertyName("y")]
    public float Y { get; set; }
    
    [JsonPropertyName("w")]
    public float Width { get; set; }
    
    [JsonPropertyName("h")]
    public float Height { get; set; }
}

public class ConfidenceScores
{
    [JsonPropertyName("name")]
    public float? Name { get; set; }
    
    [JsonPropertyName("type")]
    public float? Type { get; set; }
    
    [JsonPropertyName("cost")]
    public float? Cost { get; set; }
    
    [JsonPropertyName("color")]
    public float? Color { get; set; }
    
    [JsonPropertyName("effects")]
    public float? Effects { get; set; }
    
    [JsonPropertyName("set_code")]
    public float? SetCode { get; set; }
    
    [JsonPropertyName("collector_number")]
    public float? CollectorNumber { get; set; }
    
    [JsonPropertyName("rarity")]
    public float? Rarity { get; set; }
}
