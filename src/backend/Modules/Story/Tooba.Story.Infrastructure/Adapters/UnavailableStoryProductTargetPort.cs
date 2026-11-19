using Tooba.BuildingBlocks;
using Tooba.Story.Application.Stories.Ports;
using Tooba.Story.Contracts.Errors;

namespace Tooba.Story.Infrastructure.Adapters;

/// <summary>درگاه CTA محصول با شکست بسته وقتی جستجوی Catalog ثبت نشده است.</summary>
public sealed class UnavailableStoryProductTargetPort : IStoryProductTargetPort
{
    /// <inheritdoc />
    public Task EnsureOwnedBySellerAsync(Guid productId, Guid sellerPartyId, CancellationToken cancellationToken) =>
        throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));

    /// <inheritdoc />
    public Task EnsurePublishedExistsAsync(Guid productId, CancellationToken cancellationToken) =>
        throw new SemanticException(new SemanticError(StoryErrorCodes.CtaRejected));
}
