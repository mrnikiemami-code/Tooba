using Tooba.BuildingBlocks.Results;

namespace Tooba.Tax.Contracts;

/// <summary>Tax-owned Development-support request for demo offer classification.</summary>
public sealed record EnsureDevelopmentOfferCategory(
    Guid OfferId,
    string CategoryCode,
    string DisplayName);

/// <summary>
/// Tax-owned Development-support capability used by the Catalog attribute-schema seed
/// so Catalog can classify the demo offer without touching Tax persistence.
/// </summary>
public interface ITaxDevelopmentSeedGateway
{
    /// <summary>Creates the category when absent and assigns the offer to it.</summary>
    Task<Result> EnsureDevelopmentOfferCategoryAsync(
        EnsureDevelopmentOfferCategory request,
        CancellationToken cancellationToken);
}
