using Tooba.Content.Infrastructure.Persistence;
using Tooba.Content.Infrastructure;
using Tooba.Content.Infrastructure.Development;
using Tooba.Media.Infrastructure.Persistence;
using Tooba.PageComposition.Infrastructure.Persistence;
using Tooba.PageComposition.Infrastructure.Development;
using Tooba.Story.Infrastructure.Persistence;
using Tooba.Story.Infrastructure;
using Tooba.Story.Infrastructure.Development;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Host.Configuration;
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
    public static async Task ApplyAsync(IServiceProvider services, IHostEnvironment environment)
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
        await MigrateAsync(provider.GetRequiredService<MediaDbContext>());
        await MigrateAsync(provider.GetRequiredService<PageCompositionDbContext>());
        await MigrateAsync(provider.GetRequiredService<StoryDbContext>());
        await ContentDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await PageCompositionDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        await StoryDevelopmentSeed.ApplyAsync(provider, CancellationToken.None);
        // TB240-R1: opt-in commission of the REFERENCE data that must exist before any shipping method can be
        // authored — the canonical Localization languages first, then the Geography module's own bounded seed.
        // Inert by default; each module keeps its own persistence authority.
        await EnsureReferenceDataAsync(provider, environment, CancellationToken.None);
        // TB240: opt-in commissioning of the EXISTING Catalog-owned Development sellable fixture inside
        // THIS assigned Tenant=NULL Marketplace scope, so real seller organizations and buyable offers exist
        // before seller contexts are reconciled. Inert by default; the module keeps all data authority.
        await EnsureSellableFixturesAsync(provider, environment, CancellationToken.None);
        var sellerDevContexts = provider.GetRequiredService<Tooba.AccessControl.Application.Development.Seller.ISellerDevContextStore>();
        await sellerDevContexts.EnsureAsync(CancellationToken.None);
        await MarketplaceSellerDevBootstrap.EnsureAsync(provider, CancellationToken.None);
        await MarketplaceAdminDevBootstrap.EnsureAsync(provider, CancellationToken.None);
        // TB239-R1: opt-in binding of ONE normally registered Development administrator to the EXISTING
        // Marketplace platform membership. Inert by default; the legacy tuple above stays unchanged.
        await MarketplaceAdminDevBootstrap.ProvisionConfiguredAsync(provider, environment, CancellationToken.None);
        // TB240-R2: opt-in binding of BOTH real seller identities to their own seller-owner scope (the
        // existing bootstrap creates the scope but never assigns it) and re-publication of the REAL pairs
        // after the legacy synthetic Marketplace pair above. Inert by default.
        await EnsureMarketplaceSellerAccessAsync(provider, environment, CancellationToken.None);
    }

    private static Task MigrateAsync(DbContext context) => context.Database.MigrateAsync();

    /// <summary>
    /// TB240-R2: commissions the MISSING step of the existing access-control bootstrap — assigning each real
    /// seller identity its own already-created seller-owner role — inside the CURRENT Marketplace scope only.
    /// Guarded by real Development + explicit local opt-in; AccessControl keeps all persistence authority.
    /// </summary>
    private static async Task EnsureMarketplaceSellerAccessAsync(
        IServiceProvider provider,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        var options = provider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<MarketplaceSellerAccessOptions>>()
            .Value;
        if (!options.Enabled)
        {
            return;
        }

        var admin = provider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<MarketplaceAdminDevOptions>>()
            .Value;
        Guid? auditActor = null;
        var identifier = (admin.Identifier ?? string.Empty).Trim();
        if (identifier.Length != 0)
        {
            // Same real principal resolution used by the admin provisioning seam; never a fixed actor id.
            auditActor = await provider.GetRequiredService<Tooba.Identity.Contracts.Auth.IIdentityAuthenticationService>()
                .FindUserIdByIdentifierAsync(
                    Tooba.Identity.Contracts.Auth.LoginIdentifierKind.Email,
                    identifier,
                    cancellationToken);
        }

        var prelude = provider
            .GetRequiredService<Tooba.AccessControl.Contracts.Development.IAccessControlDevelopmentSeedPrelude>();
        await prelude.EnsureMarketplaceSellerAccessAsync(auditActor, cancellationToken);
    }

    /// <summary>
    /// TB240-R1: commissions the reference data that shipping authoring depends on, inside the CURRENT
    /// assigned Marketplace scope only. Guarded by real Development + explicit local opt-in; the canonical
    /// languages come from the EXISTING Localization Development entry point, and the bounded Iran/Tehran
    /// geography comes from the Geography-owned Development seed. Ordering matters: languages first.
    /// </summary>
    private static async Task EnsureReferenceDataAsync(
        IServiceProvider provider,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        var options = provider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<MarketplaceReferenceDataOptions>>()
            .Value;
        if (!options.Enabled)
        {
            return;
        }

        await EnsureReferenceDataAsync(
            environment,
            options,
            ct => Tooba.Localization.Infrastructure.Bootstrap.LanguageBootstrapDevelopmentSeed.PostMigration
                .EnsureAsync(provider, ct),
            async ct =>
            {
                var outcome = await Tooba.Geography.Infrastructure.Development.GeographyDevelopmentSeed
                    .EnsureAsync(provider, ct);
                // TB240-R2: a non-Completed commissioning is an explicit startup refusal, never success. The
                // seed now validates the existing shape before staging, so nothing stays eligible for a later
                // SaveChanges on this scope.
                if (outcome.Status != Tooba.Geography.Infrastructure.Development.GeographyDevSeedStatus.Completed)
                {
                    throw new InvalidOperationException(
                        "Marketplace reference-data commissioning refused: geography seed status " +
                        outcome.Status + ".");
                }
            },
            cancellationToken);
    }

    /// <summary>
    /// Guard + ordering seam with explicit inputs so the shape is provable without a database: the canonical
    /// languages are commissioned FIRST and the Geography seed runs only after that step succeeded.
    /// </summary>
    internal static async Task EnsureReferenceDataAsync(
        IHostEnvironment environment,
        MarketplaceReferenceDataOptions options,
        Func<CancellationToken, Task> ensureLanguages,
        Func<CancellationToken, Task> ensureGeography,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() || !options.Enabled)
        {
            return;
        }

        await ensureLanguages(cancellationToken);
        await ensureGeography(cancellationToken);
    }

    /// <summary>
    /// TB240: asks the Catalog-owned Development seam to create its documented live sellable fixture
    /// (one live product, Seller A + Seller B organizations, their offers/prices/tax/locations/stock and the
    /// Seller A companion) inside the CURRENT assigned Marketplace scope. Guarded by real Development +
    /// explicit local opt-in; the existence check is the exact live fixture, never "any product exists".
    /// </summary>
    private static async Task EnsureSellableFixturesAsync(
        IServiceProvider provider,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MarketplaceSellableSeedOptions>>().Value;
        var seed = provider.GetRequiredService<Tooba.Catalog.Application.Development.IWorkspaceDemoSeed>();
        await EnsureSellableFixturesAsync(environment, options, seed, cancellationToken);
    }

    /// <summary>
    /// Seam body with explicit inputs so the guard shape is provable without a database: it seeds ONLY on
    /// real Development + explicit opt-in + an absent EXACT live fixture.
    /// </summary>
    internal static async Task EnsureSellableFixturesAsync(
        IHostEnvironment environment,
        MarketplaceSellableSeedOptions options,
        Tooba.Catalog.Application.Development.IWorkspaceDemoSeed seed,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() || !options.Enabled)
        {
            return;
        }

        if (await seed.IsLiveProductSeededAsync(cancellationToken))
        {
            return;
        }

        await seed.SeedNewProductAsync(cancellationToken);
    }
}
