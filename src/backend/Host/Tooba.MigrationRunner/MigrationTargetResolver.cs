using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Host;

namespace Tooba.MigrationRunner;

/// <summary>
/// هدف منطقی مهاجرت: edition، tenant و مرجع اتصال بدون افشای secret.
/// </summary>
internal sealed record MigrationTarget(
    ToobaEdition Edition,
    string ConnectionReference,
    string? TenantId,
    string DatabaseLogicalName);

/// <summary>
/// انتخاب tenant برای Single-Store را از CLI و registry می‌سازد.
/// </summary>
internal static class MigrationTargetResolver
{
    /// <summary>
    /// اهداف مهاجرت را از پیکربندی و فلگ‌های CLI resolve می‌کند.
    /// </summary>
    internal static IReadOnlyList<MigrationTarget> Resolve(
        ControlPlaneRegistry registry,
        ToobaPlatformOptions options,
        MigrationRunnerCli cli)
    {
        if (registry.Edition == ToobaEdition.Marketplace)
        {
            var reference = registry.MarketplaceConnectionReference
                ?? throw new MigrationRunnerException(3, "Marketplace edition requires Tooba:Marketplace:ConnectionReference.");
            return
            [
                CreateTarget(
                    ToobaEdition.Marketplace,
                    reference.Value,
                    tenantId: null,
                    options),
            ];
        }

        if (registry.Edition == ToobaEdition.SingleStore)
        {
            if (cli.AllTenants)
            {
                return registry.Tenants.Values
                    .Where(t => t.Status == TenantStatus.Active)
                    .Select(t => CreateTarget(
                        ToobaEdition.SingleStore,
                        t.ConnectionReference.Value,
                        t.TenantId.Value,
                        options))
                    .ToList();
            }

            if (cli.TenantIds.Count > 0)
            {
                return cli.TenantIds
                    .Select(id =>
                    {
                        if (!registry.Tenants.TryGetValue(id, out var tenant))
                        {
                            throw new MigrationRunnerException(3, $"Unknown tenant '{id}'.");
                        }

                        if (tenant.Status != TenantStatus.Active)
                        {
                            throw new MigrationRunnerException(3, $"Tenant '{id}' is not Active.");
                        }

                        return CreateTarget(
                            ToobaEdition.SingleStore,
                            tenant.ConnectionReference.Value,
                            tenant.TenantId.Value,
                            options);
                    })
                    .ToList();
            }

            throw new MigrationRunnerException(
                3,
                "Single-Store requires --tenant <id>, --tenants <id,id>, or explicit --all-tenants.");
        }

        throw new MigrationRunnerException(3, "Tooba:Edition must be Marketplace or SingleStore for migration runner.");
    }

    private static MigrationTarget CreateTarget(
        ToobaEdition edition,
        string connectionReference,
        string? tenantId,
        ToobaPlatformOptions options)
    {
        if (!options.PostgreSQL.ConnectionReferences.TryGetValue(connectionReference, out var connectionString)
            || string.IsNullOrWhiteSpace(connectionString))
        {
            throw new MigrationRunnerException(
                3,
                $"Connection reference '{connectionReference}' is not configured.");
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var logicalName = builder.Database ?? connectionReference;
        return new MigrationTarget(edition, connectionReference, tenantId, logicalName);
    }
}

/// <summary>
/// خطای کنترل‌شده runner با exit code صریح.
/// </summary>
internal sealed class MigrationRunnerException : Exception
{
    public MigrationRunnerException(int exitCode, string message)
        : base(message) => ExitCode = exitCode;

    public int ExitCode { get; }
}
