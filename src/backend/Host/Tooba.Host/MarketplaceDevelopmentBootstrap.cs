using Tooba.Content.Infrastructure.Persistence;
using Tooba.Content.Infrastructure;
using Tooba.PageComposition.Infrastructure.Persistence;
using Tooba.PageComposition.Infrastructure;
using global::Tooba.Story.Infrastructure.Persistence;
using global::Tooba.Story.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Identity.Infrastructure.Persistence;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.Payment.Infrastructure.Persistence;
using Tooba.Settlement.Infrastructure.Persistence;
using Tooba.Notification.Infrastructure.Persistence;
using Tooba.AccessControl.Infrastructure.Persistence;

namespace Tooba.Host;

/// <summary>
/// bootstrap Development برای Edition=Marketplace: فقط migrate schema روی connection marketplace.
/// دادهٔ tenant SingleStore را دست نمی‌زند.
/// </summary>
internal static class MarketplaceDevelopmentBootstrap
{
    public static async Task ApplyAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var registry = provider.GetRequiredService<ControlPlaneRegistry>();
        if (registry.Edition != ToobaEdition.Marketplace || registry.MarketplaceConnectionReference is null)
        {
            return;
        }

        var marketplaceRef = registry.MarketplaceConnectionReference
            ?? throw new InvalidOperationException("Marketplace connection reference is missing.");

        var assigner = provider.GetRequiredService<ICommerceContextAssigner>();
        assigner.Assign(new CommerceContext(
            new EditionContext(registry.Edition, registry.DeploymentId),
            null,
            marketplaceRef,
            TraceId: "marketplace-dev-bootstrap"));

        await MigrateAsync(provider.GetRequiredService<PartyDbContext>());
        await MigrateAsync(provider.GetRequiredService<IdentityDbContext>());
        await MigrateAsync(provider.GetRequiredService<OrderDbContext>());
        await MigrateAsync(provider.GetRequiredService<PaymentDbContext>());
        await MigrateAsync(provider.GetRequiredService<SettlementDbContext>());
        await MigrateAsync(provider.GetRequiredService<NotificationDbContext>());
        await MigrateAsync(provider.GetRequiredService<AccessControlDbContext>());
        await MigrateAsync(provider.GetRequiredService<ContentDbContext>());
        await MigrateAsync(provider.GetRequiredService<PageCompositionDbContext>());
        await MigrateAsync(provider.GetRequiredService<StoryDbContext>());
        await ContentDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await PageCompositionDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await StoryDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await Seller.SellerDevActorBootstrap.EnsureAsync(provider, CancellationToken.None);
        await MarketplaceSellerDevBootstrap.EnsureAsync(provider, CancellationToken.None);
        await MarketplaceAdminDevBootstrap.EnsureAsync(provider, CancellationToken.None);
    }

    private static Task MigrateAsync(DbContext context) => context.Database.MigrateAsync();
}
