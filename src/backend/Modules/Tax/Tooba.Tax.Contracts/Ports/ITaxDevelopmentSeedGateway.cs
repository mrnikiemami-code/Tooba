using Tooba.BuildingBlocks.Results;

namespace Tooba.Tax.Contracts.Ports;

/// <summary>Tax-owned Development-support request for demo offer classification.</summary>
public sealed record EnsureDevelopmentOfferCategory(
    Guid OfferId,
    string CategoryCode,
    string DisplayName);

/// <summary>
/// Development-support tax rule kinds exposed at the module boundary so development seeds
/// never reference Tax domain types. Mirrors only what the development seam supports.
/// </summary>
public enum DevelopmentTaxRuleKind
{
    /// <summary>Percentage rule; the only development shape currently used.</summary>
    Percentage = 0,
}

/// <summary>
/// Development-support override policies exposed at the module boundary.
/// </summary>
public enum DevelopmentTaxOverridePolicy
{
    /// <summary>Overrides disabled; the only development shape currently used.</summary>
    Disabled = 0,
}

/// <summary>Tax-owned Development-support request to create and activate the demo tax rule.</summary>
public sealed record EnsureDevelopmentTaxRule(
    string Jurisdiction,
    string Market,
    string CategoryCode,
    DevelopmentTaxRuleKind Kind,
    decimal Rate,
    DateTimeOffset EffectiveFrom,
    int Specificity,
    DevelopmentTaxOverridePolicy OverridePolicy);

/// <summary>
/// Tax-owned Development-support capability used by module-owned Development seeds
/// so they can classify demo offers and configure the demo rule without touching Tax persistence.
/// </summary>
public interface ITaxDevelopmentSeedGateway
{
    /// <summary>Creates the category when absent and assigns the offer to it.</summary>
    Task<Result> EnsureDevelopmentOfferCategoryAsync(
        EnsureDevelopmentOfferCategory request,
        CancellationToken cancellationToken);

    /// <summary>Creates and activates the demo rule when no active rule exists yet.</summary>
    Task<Result> EnsureDevelopmentRuleAsync(
        EnsureDevelopmentTaxRule request,
        CancellationToken cancellationToken);
}
