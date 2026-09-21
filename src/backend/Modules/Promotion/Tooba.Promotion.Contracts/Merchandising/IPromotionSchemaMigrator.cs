namespace Tooba.Promotion.Contracts.Merchandising;

/// <summary>Promotion-owned schema migration entrypoint so Host bootstraps never type PromotionDbContext.</summary>
public interface IPromotionSchemaMigrator
{
    /// <summary>Applies pending Promotion EF migrations.</summary>
    Task MigrateAsync(CancellationToken cancellationToken = default);
}
