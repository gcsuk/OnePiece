namespace OnePiece.Configuration;

public class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";
    
    public string ConnectionString { get; set; } = string.Empty;
    public string BlobContainerName { get; set; } = "onepiece-cards";
    public string TableName { get; set; } = "CardMetadata";
    public string StorageAccountName { get; set; } = string.Empty;
}
