namespace Tooba.Tax.Contracts.Ports;

/// <summary>Tax-owned schema migration entrypoint so Host bootstraps never type TaxDbContext.</summary>
public interface ITaxSchemaMigrator
{
    /// <summary>Applies pending Tax EF migrations.</summary>
    Task MigrateAsync(CancellationToken cancellationToken = default);
}
