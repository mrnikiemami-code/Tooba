using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Admin;
using Tooba.Promotion.Application;

namespace Tooba.Host.Tests;

/// <summary>ساخت Composer با MediatR + Directory برای تست‌های Landing write.</summary>
internal static class StoreLandingPageComposerTestFactory
{
    /// <summary>Composer و Catalog در-حافظه با همان مسیر CQRS تولید می‌کند.</summary>
    public static StoreLandingPageComposer Create(out CatalogDbContext catalog, IMerchandisingCampaignQuery? campaignQuery = null)
    {
        catalog = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
        var commerce = new FixedLandingCommerce(OutboxTestContextFactory.SingleStore("store-a", "conn-a"));
        var campaigns = campaignQuery ?? new EmptyMerchandisingCampaignQuery();
        var gate = new LandingCampaignGate(campaigns);
        var directory = new StoreLandingPageDirectory(catalog, new SystemUtcClock(), commerce, gate);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IStoreLandingPageDirectory>(directory);
        services.AddSingleton<IStoreLandingExternalReferenceGate>(gate);
        services.AddValidatorsFromAssembly(typeof(CreateStoreLandingPageCommand).Assembly);
        services.AddToobaCqrsFoundation(typeof(StoreLandingPageDirectory).Assembly);
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();
        return new StoreLandingPageComposer(catalog, commerce, new MemoryCache(new MemoryCacheOptions()), campaigns, sender);
    }

    private sealed class FixedLandingCommerce : ICurrentCommerceContext
    {
        public FixedLandingCommerce(CommerceContext current) => Current = current;

        public CommerceContext? Current { get; }
    }

    private sealed class LandingCampaignGate : IStoreLandingExternalReferenceGate
    {
        private readonly IMerchandisingCampaignQuery _campaigns;

        public LandingCampaignGate(IMerchandisingCampaignQuery campaigns) => _campaigns = campaigns;

        public Task<bool> CampaignBelongsToStoreAsync(Guid campaignId, Guid storeId, CancellationToken cancellationToken)
            => _campaigns.CampaignBelongsToStoreAsync(campaignId, storeId, cancellationToken);
    }
}
