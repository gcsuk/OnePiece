using OnePiece.Models;

namespace OnePiece.Services;

public interface IOpenAIVisionService
{
    Task<OnePieceCard> AnalyzeOnePieceCardAsync(Stream imageStream);
    Task<byte[]> CreateEnglishOverlayImageAsync(Stream originalImageStream, OnePieceCard cardData);
    Task<(OnePieceCard CardData, byte[] TranslatedImage)> AnalyzeAndTranslateCardAsync(Stream imageStream);
}
