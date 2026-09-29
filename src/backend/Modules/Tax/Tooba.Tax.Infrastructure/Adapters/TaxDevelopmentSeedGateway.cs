using Tooba.BuildingBlocks.Results;
using Tooba.Tax.Application;
using Tooba.Tax.Contracts;

namespace Tooba.Tax.Infrastructure.Adapters;

/// <summary>Development-only Tax seed capability owned by Tax.Infrastructure.</summary>
public sealed class TaxDevelopmentSeedGateway(
    ITaxDirectory tax,
    ITaxQueryGateway queries) : ITaxDevelopmentSeedGateway
{
    /// <inheritdoc />
    public async Task<Result> EnsureDevelopmentOfferCategoryAsync(
        EnsureDevelopmentOfferCategory request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var existing = await queries.FindCategoryByCodesAsync([request.CategoryCode], cancellationToken);
        var categoryId = existing?.CategoryId
            ?? (await tax.CreateCategoryAsync(request.CategoryCode, request.DisplayName, cancellationToken)).CategoryId;
        await tax.AssignOfferCategoryAsync(request.OfferId, categoryId, cancellationToken);
        return Result.Success();
    }
}
