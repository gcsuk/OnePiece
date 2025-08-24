using Azure.Storage.Blobs;
using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using OnePiece.Configuration;
using OnePiece.Models;

namespace OnePiece.Services;

public class AzureStorageService : IAzureStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly TableServiceClient _tableServiceClient;
    private readonly AzureStorageOptions _options;
    private readonly BlobContainerClient _containerClient;
    private readonly TableClient _tableClient;

    public AzureStorageService(IOptions<AzureStorageOptions> options)
    {
        _options = options.Value;
        
        // Initialize Blob Storage
        _blobServiceClient = new BlobServiceClient(_options.ConnectionString);
        _containerClient = _blobServiceClient.GetBlobContainerClient(_options.BlobContainerName);
        
        // Initialize Table Storage
        _tableServiceClient = new TableServiceClient(_options.ConnectionString);
        _tableClient = _tableServiceClient.GetTableClient(_options.TableName);
        
        // Ensure container and table exist
        _ = Task.Run(async () => await InitializeStorageAsync());
    }

    private async Task InitializeStorageAsync()
    {
        try
        {
            // Create blob container if it doesn't exist
            await _containerClient.CreateIfNotExistsAsync();
            
            // Create table if it doesn't exist
            await _tableClient.CreateIfNotExistsAsync();
        }
        catch (Exception ex)
        {
            // Log error but don't throw - service can still work if storage is already set up
            Console.WriteLine($"Warning: Could not initialize Azure Storage: {ex.Message}");
        }
    }

    public async Task<string> UploadImageAsync(byte[] imageBytes, string fileName, string contentType)
    {
        try
        {
            // Generate unique blob name with timestamp
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var uniqueFileName = $"{timestamp}_{Guid.NewGuid()}_{fileName}";
            
            var blobClient = _containerClient.GetBlobClient(uniqueFileName);
            
            // Upload the image
            using var stream = new MemoryStream(imageBytes);
            await blobClient.UploadAsync(stream, overwrite: true);
            
            // Set content type
            await blobClient.SetHttpHeadersAsync(new Azure.Storage.Blobs.Models.BlobHttpHeaders
            {
                ContentType = contentType
            });
            
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to upload image to Azure Blob Storage: {ex.Message}", ex);
        }
    }

    public async Task<CardMetadata> StoreCardMetadataAsync(OnePieceCard cardData, string originalImageUrl, string translatedImageUrl)
    {
        try
        {
            var metadata = new CardMetadata
            {
                PartitionKey = "OnePiece",
                RowKey = Guid.NewGuid().ToString(),
                Timestamp = DateTimeOffset.UtcNow,
                
                // Card identification
                CardName = cardData.NameEnglish ?? cardData.NameJapanese ?? "Unknown",
                CardNameJapanese = cardData.NameJapanese ?? string.Empty,
                CardNameEnglish = cardData.NameEnglish ?? string.Empty,
                
                // Image URLs
                OriginalImageUrl = originalImageUrl,
                TranslatedImageUrl = translatedImageUrl,
                
                // Card details
                CardType = cardData.Type ?? string.Empty,
                Color = cardData.Color ?? string.Empty,
                Cost = cardData.Cost,
                Power = cardData.Power,
                Rarity = cardData.Rarity ?? string.Empty,
                SetCode = cardData.SetCode ?? string.Empty,
                CollectorNumber = cardData.CollectorNumber ?? string.Empty,
                
                // Analysis metadata
                AnalysisDate = cardData.Timestamp,
                AnalysisMethod = cardData.ExtractionMethod,
                Confidence = cardData.Confidences?.Name ?? 0.0,
                
                // Storage metadata
                StorageAccount = _options.StorageAccountName,
                ContainerName = _options.BlobContainerName
            };
            
            await _tableClient.AddEntityAsync(metadata);
            return metadata;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to store card metadata in Azure Table Storage: {ex.Message}", ex);
        }
    }

    public async Task<List<CardMetadata>> GetAllCardMetadataAsync()
    {
        try
        {
            var metadataList = new List<CardMetadata>();
            
            await foreach (var entity in _tableClient.QueryAsync<CardMetadata>())
            {
                metadataList.Add(entity);
            }
            
            return metadataList.OrderByDescending(x => x.AnalysisDate).ToList();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve card metadata from Azure Table Storage: {ex.Message}", ex);
        }
    }

    public async Task<CardMetadata?> GetCardMetadataAsync(string cardId)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<CardMetadata>("OnePiece", cardId);
            return response.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            // Card not found
            return null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve card metadata from Azure Table Storage: {ex.Message}", ex);
        }
    }
}
