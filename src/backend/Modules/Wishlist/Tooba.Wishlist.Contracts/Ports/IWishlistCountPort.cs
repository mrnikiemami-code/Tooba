namespace Tooba.Wishlist.Contracts.Ports;

/// <summary>Narrow read port: actor wishlist row count for customer-account dashboard.</summary>
public interface IWishlistCountPort
{
    /// <summary>Returns the number of private wishlist rows for the actor.</summary>
    Task<long> CountAsync(Guid actorUserId, CancellationToken cancellationToken);
}
