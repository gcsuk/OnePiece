using OnePiece.Models;

namespace OnePiece.Services;

public interface ICardProcessingService
{
    Task<(OnePieceCard CardData, byte[] TranslatedImage, CardMetadata Metadata)> ProcessCardAsync(Stream imageStream, byte[] originalImageBytes);
    Task<(OnePieceCard CardData, byte[] TranslatedImage, CardMetadata Metadata)> ProcessCardWithStorageAsync(Stream imageStream, byte[] originalImageBytes);
}
