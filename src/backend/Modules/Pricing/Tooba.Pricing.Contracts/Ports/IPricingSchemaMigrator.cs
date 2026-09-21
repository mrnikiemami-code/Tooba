namespace Tooba.Pricing.Contracts;

/// <summary>Pricing-owned schema migration entrypoint so Host bootstraps never type PricingDbContext.</summary>
public interface IPricingSchemaMigrator
{
    /// <summary>Applies pending Pricing EF migrations.</summary>
    Task MigrateAsync(CancellationToken cancellationToken = default);
}
