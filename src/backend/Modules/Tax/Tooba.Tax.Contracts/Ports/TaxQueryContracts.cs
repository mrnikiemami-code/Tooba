namespace Tooba.Tax.Contracts;

/// <summary>Opaque tax category row for Host labels and development seeds.</summary>
public sealed record TaxCategorySnapshot(Guid CategoryId, string Code, string DisplayName);

/// <summary>Offer-to-category assignment without a tax amount.</summary>
public sealed record TaxClassificationSnapshot(Guid OfferId, Guid CategoryId);

/// <summary>Tax-owned read port so Host never opens TaxDbContext.</summary>
public interface ITaxQueryGateway
{
    /// <summary>First category whose code is in the given set, or null.</summary>
    Task<TaxCategorySnapshot?> FindCategoryByCodesAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken cancellationToken);

    /// <summary>True when an active rule exists for the category, jurisdiction, and market.</summary>
    Task<bool> HasActiveRuleAsync(
        Guid categoryId,
        string jurisdiction,
        string market,
        CancellationToken cancellationToken);

    /// <summary>Classifications for the given offers.</summary>
    Task<IReadOnlyList<TaxClassificationSnapshot>> ListClassificationsByOfferIdsAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken);

    /// <summary>Categories for the given ids.</summary>
    Task<IReadOnlyList<TaxCategorySnapshot>> ListCategoriesByIdsAsync(
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken);
}
