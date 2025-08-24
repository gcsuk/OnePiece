using Azure.Data.Tables;

namespace OnePiece.Models;

public class CardMetadata : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public Azure.ETag ETag { get; set; }

    // Card identification
    public string CardName { get; set; } = string.Empty;
    public string CardNameJapanese { get; set; } = string.Empty;
    public string CardNameEnglish { get; set; } = string.Empty;
    
    // Image URLs
    public string OriginalImageUrl { get; set; } = string.Empty;
    public string TranslatedImageUrl { get; set; } = string.Empty;
    
    // Card details
    public string CardType { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int? Cost { get; set; }
    public int? Power { get; set; }
    public string Rarity { get; set; } = string.Empty;
    public string SetCode { get; set; } = string.Empty;
    public string CollectorNumber { get; set; } = string.Empty;
    
    // Analysis metadata
    public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
    public string AnalysisMethod { get; set; } = string.Empty;
    public double? Confidence { get; set; }
    
    // Storage metadata
    public string StorageAccount { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
}
