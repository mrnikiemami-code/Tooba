using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Support.Infrastructure.Persistence;
using Tooba.Support.Infrastructure.Development;

namespace Tooba.Support.Infrastructure.Development;

/// <summary>
/// Orchestrates Development migrate + Support demo seed. Host composition binds
/// CommerceContext / Admin / Seller / AccessControl prerequisites, then calls this.
/// </summary>
public static class SupportDevelopmentSeedBootstrap
{
    /// <summary>Migrates Support schema and applies idempotent Development seed.</summary>
    public static async Task ApplyAsync(
        IServiceProvider services,
        Guid customerActorUserId,
        Guid sellerPartyId,
        Guid sellerActorUserId,
        Guid adminActorUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        var db = services.GetRequiredService<SupportDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        await SupportDevelopmentSeed.ApplyAsync(
            services,
            customerActorUserId,
            sellerPartyId,
            sellerActorUserId,
            adminActorUserId,
            cancellationToken);
    }
}
