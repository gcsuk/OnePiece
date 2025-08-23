using OnePiece.Models;

namespace OnePiece.Services;

public interface IOpenAIVisionService
{
    Task<OnePieceCard> AnalyzeOnePieceCardAsync(Stream imageStream);
}
