using OnePiece.Models;

namespace OnePiece.Services;

public interface IAzureStorageService
{
    Task<string> UploadImageAsync(byte[] imageBytes, string fileName, string contentType);
    Task<CardMetadata> StoreCardMetadataAsync(OnePieceCard cardData, string originalImageUrl, string translatedImageUrl);
    Task<List<CardMetadata>> GetAllCardMetadataAsync();
    Task<CardMetadata?> GetCardMetadataAsync(string cardId);
}
