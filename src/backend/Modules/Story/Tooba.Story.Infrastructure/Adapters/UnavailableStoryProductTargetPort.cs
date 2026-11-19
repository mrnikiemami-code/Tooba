using Tooba.BuildingBlocks;
using Tooba.Story.Application.Stories.Ports;
using Tooba.Story.Contracts.Errors;

namespace Tooba.Story.Infrastructure.Adapters;

/// <summary>Fail-closed product CTA port when Catalog lookup is not registered.</summary>
public sealed class UnavailableStoryProductTargetPort : IStoryProductTargetPort
{
    /// <inheritdoc />
    public Task EnsureOwnedBySellerAsync(Guid productId, Guid sellerPartyId, CancellationToken cancellationToken) =>
        throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));
}
