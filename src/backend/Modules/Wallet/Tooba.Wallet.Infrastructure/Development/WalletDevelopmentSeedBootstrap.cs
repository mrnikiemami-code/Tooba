using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Wallet.Infrastructure.Adapters;
using Tooba.Wallet.Infrastructure.Persistence;

namespace Tooba.Wallet.Infrastructure.Development;

/// <summary>
/// Orchestrates Development migrate + Wallet demo seed. Host composition only binds
/// CommerceContext / Admin actor, then calls this module-owned bootstrap.
/// </summary>
public static class WalletDevelopmentSeedBootstrap
{
    /// <summary>Customer Actor shared with Support demo (stable).</summary>
    public static readonly Guid DemoCustomerActorUserId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-000000000009");

    /// <summary>Migrates Wallet schema and applies idempotent Development seed.</summary>
    public static async Task ApplyAsync(
        IServiceProvider services,
        Guid customerActorUserId,
        Guid adminActorUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        var db = services.GetRequiredService<WalletDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        await WalletDevelopmentSeed.ApplyAsync(
            services,
            customerActorUserId,
            adminActorUserId,
            cancellationToken);
    }
}
