#pragma warning disable CS1591
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Tooba.Catalog.Application.Development;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Development;

/// <summary>
/// Development orchestration for the workspace demo dataset, owned by Catalog.
/// Host calls <see cref="IsLiveProductSeededAsync"/> first and then either the refresh or the
/// seed path, exactly mirroring the ordering of the former Host bootstrap.
/// </summary>
public sealed class WorkspaceDemoSeed(
    CatalogDbContext catalogDb,
    WorkspaceDemoProductSeed productSeed,
    WorkspaceDemoMarketplaceSeed marketplaceSeed,
    IHostEnvironment environment) : IWorkspaceDemoSeed
{
    /// <inheritdoc />
    public async Task<bool> IsLiveProductSeededAsync(CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            return false;
        }

        return await catalogDb.Products.AsNoTracking()
            .AnyAsync(p => p.SlugSeam == WorkspaceDemoProductSeed.SeedSlug, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SeedNewProductAsync(CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        var variantId = await productSeed.SeedProductAsync(cancellationToken);
        await marketplaceSeed.EnsureMarketplaceAsync(variantId, cancellationToken);

        // (TB-136) Development-only companion fixture: one extra live product whose offer belongs to the
        // SAME Seller A, so a single real seller presents two distinct sellable cart lines.
        var companionVariantId = await productSeed.EnsureCompanionLiveProductAsync(cancellationToken);
        await marketplaceSeed.EnsureSameSellerCompanionOfferAsync(companionVariantId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RefreshExistingCopyAsync(CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        await productSeed.RefreshOperatorFacingCopyAsync(cancellationToken);
        await marketplaceSeed.RefreshOperatorFacingCopyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task EnsureAdminR3PreviewAsync(CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        await productSeed.EnsureAdminR3PreviewAsync(cancellationToken);
    }
}
