using OnePiece.Models;

namespace OnePiece.Services;

public class CardProcessingService : ICardProcessingService
{
    private readonly IOpenAIVisionService _visionService;
    private readonly IAzureStorageService _storageService;

    public CardProcessingService(IOpenAIVisionService visionService, IAzureStorageService storageService)
    {
        _visionService = visionService;
        _storageService = storageService;
    }

    public async Task<(OnePieceCard CardData, byte[] TranslatedImage, CardMetadata Metadata)> ProcessCardAsync(Stream imageStream, byte[] originalImageBytes)
    {
        try
        {
            // Copy the stream to a MemoryStream so we can seek and reuse it
            var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            
            // 1. Analyze the card using the vision service
            var cardData = await _visionService.AnalyzeOnePieceCardAsync(memoryStream);
            
            // Reset stream position for image generation
            memoryStream.Position = 0;
            
            // 2. Generate the translated image using the vision service
            var translatedImage = await _visionService.CreateEnglishOverlayImageAsync(memoryStream, cardData);
            
            // 3. Upload both images to Azure Blob Storage
            var originalImageUrl = await _storageService.UploadImageAsync(originalImageBytes, "original.jpg", "image/jpeg");
            var translatedImageUrl = await _storageService.UploadImageAsync(translatedImage, "translated.png", "image/png");
            
            // 4. Store metadata in Azure Table Storage
            var metadata = await _storageService.StoreCardMetadataAsync(cardData, originalImageUrl, translatedImageUrl);
            
            return (cardData, translatedImage, metadata);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error processing OnePiece card: {ex.Message}", ex);
        }
    }

    public async Task<(OnePieceCard CardData, byte[] TranslatedImage, CardMetadata Metadata)> ProcessCardWithStorageAsync(Stream imageStream, byte[] originalImageBytes)
    {
        try
        {
            // Copy the stream to a MemoryStream so we can seek and reuse it
            var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            
            // 1. Analyze the card using the vision service
            var cardData = await _visionService.AnalyzeOnePieceCardAsync(memoryStream);
            
            // Reset stream position for image generation
            memoryStream.Position = 0;
            
            // 2. Generate the translated image using the vision service
            var translatedImage = await _visionService.CreateEnglishOverlayImageAsync(memoryStream, cardData);
            
            // 3. Upload both images to Azure Blob Storage
            var originalImageUrl = await _storageService.UploadImageAsync(originalImageBytes, "original.jpg", "image/jpeg");
            var translatedImageUrl = await _storageService.UploadImageAsync(translatedImage, "translated.png", "image/png");
            
            // 4. Store metadata in Azure Table Storage
            var metadata = await _storageService.StoreCardMetadataAsync(cardData, originalImageUrl, translatedImageUrl);
            
            return (cardData, translatedImage, metadata);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error processing OnePiece card with storage: {ex.Message}", ex);
        }
    }
}
