using OnePiece.Models;

namespace OnePiece.Services;

public interface IBatchProcessingService
{
    /// <summary>
    /// Process multiple cards in a single API call to reduce costs
    /// </summary>
    /// <param name="imageStreams">Collection of card image streams</param>
    /// <returns>Collection of analyzed card data</returns>
    Task<List<OnePieceCard>> ProcessBatchAsync(IEnumerable<Stream> imageStreams);
    
    /// <summary>
    /// Process multiple cards with translation in batch
    /// </summary>
    /// <param name="imageStreams">Collection of card image streams</param>
    /// <returns>Collection of analyzed and translated cards</returns>
    Task<List<(OnePieceCard CardData, byte[] TranslatedImage)>> ProcessBatchWithTranslationAsync(IEnumerable<Stream> imageStreams);
    
    /// <summary>
    /// Get estimated cost savings for batch processing
    /// </summary>
    /// <param name="cardCount">Number of cards to process</param>
    /// <returns>Estimated cost savings compared to individual processing</returns>
    decimal GetEstimatedCostSavings(int cardCount);
}

