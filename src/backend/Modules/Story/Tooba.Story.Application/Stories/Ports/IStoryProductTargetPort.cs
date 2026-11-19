namespace Tooba.Story.Application.Stories.Ports;

/// <summary>Owning product/offer authority for shop-scoped Story CTA targets.</summary>
public interface IStoryProductTargetPort
{
    /// <summary>
    /// Confirms the product is a positively established target owned by <paramref name="sellerPartyId"/>.
    /// Missing/foreign products refuse; never invents ownership.
    /// </summary>
    Task EnsureOwnedBySellerAsync(Guid productId, Guid sellerPartyId, CancellationToken cancellationToken);
}
