using Tooba.BuildingBlocks.Results;
using Tooba.Tax.Application.Ports;
using Tooba.Tax.Contracts.Ports;
using Tooba.Tax.Domain.Enums;

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

    /// <inheritdoc />
    public async Task<Result> EnsureDevelopmentRuleAsync(
        EnsureDevelopmentTaxRule request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var existing = await queries.FindCategoryByCodesAsync([request.CategoryCode], cancellationToken);
        var categoryId = existing?.CategoryId
            ?? (await tax.CreateCategoryAsync(request.CategoryCode, request.CategoryCode, cancellationToken)).CategoryId;
        if (await queries.HasActiveRuleAsync(
                categoryId,
                request.Jurisdiction,
                request.Market,
                cancellationToken))
        {
            return Result.Success();
        }

        var rule = await tax.CreateRuleAsync(
            request.Jurisdiction,
            request.Market,
            categoryId,
            MapKind(request.Kind),
            request.Rate,
            request.EffectiveFrom,
            null,
            request.Specificity,
            MapOverridePolicy(request.OverridePolicy),
            cancellationToken);
        await tax.ActivateRuleAsync(rule.RuleId, cancellationToken);
        return Result.Success();
    }

    private static TaxRuleKind MapKind(DevelopmentTaxRuleKind kind) => kind switch
    {
        DevelopmentTaxRuleKind.Percentage => TaxRuleKind.Percentage,
        _ => TaxRuleKind.Percentage,
    };

    private static TaxOverridePolicy MapOverridePolicy(DevelopmentTaxOverridePolicy policy) => policy switch
    {
        DevelopmentTaxOverridePolicy.Disabled => TaxOverridePolicy.Disabled,
        _ => TaxOverridePolicy.Disabled,
    };
}
