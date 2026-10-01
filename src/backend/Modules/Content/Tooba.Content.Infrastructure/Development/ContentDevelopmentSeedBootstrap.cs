using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Content.Infrastructure.Persistence;

namespace Tooba.Content.Infrastructure.Development;

/// <summary>
/// Orchestrates Development migrate + Content demo seed. Host composition only binds
/// CommerceContext, then calls this module-owned bootstrap.
/// </summary>
public static class ContentDevelopmentSeedBootstrap
{
    /// <summary>Migrates Content schema and applies idempotent Development seed.</summary>
    public static async Task ApplyAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        var db = services.GetRequiredService<ContentDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        await ContentDevelopmentSeed.ApplyAsync(services, cancellationToken);
    }
}
