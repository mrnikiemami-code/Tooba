using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.StoreLandingPages.Ports;
using Tooba.Catalog.Infrastructure;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Promotion.Application.Merchandising;

namespace Tooba.Host.Tests;

/// <summary>ساخت Workspace با MediatR + Directory برای تست‌های Landing write.</summary>
internal static class StoreLandingPageWorkspaceTestFactory
{
    /// <summary>Workspace و Catalog در-حافظه با همان مسیر CQRS تولید می‌کند.</summary>
    public static StoreLandingPageWorkspace Create(
        out CatalogDbContext catalog,
        IMerchandisingCampaignQuery? campaignQuery = null)
    {
        catalog = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
        var commerce = new FixedLandingCommerce(OutboxTestContextFactory.SingleStore("store-a", "conn-a"));
        var campaigns = campaignQuery ?? new EmptyMerchandisingCampaignQuery();
        var gate = new LandingCampaignGate(campaigns);
        var directory = new StoreLandingPageDirectory(catalog, new SystemUtcClock(), commerce, gate);
        var merchandising = new TestMerchandisingPort(campaigns, commerce);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IStoreLandingPageDirectory>(directory);
        services.AddSingleton<IStoreLandingExternalReferenceGate>(gate);
        services.AddValidatorsFromAssembly(typeof(CreateStoreLandingPageCommand).Assembly);
        services.AddToobaCqrsFoundation(typeof(CreateStoreLandingPageCommand).Assembly);
        var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();
        return new StoreLandingPageWorkspace(
            catalog,
            commerce,
            new MemoryCache(new MemoryCacheOptions()),
            merchandising,
            sender);
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

    private sealed class TestMerchandisingPort : IStoreLandingMerchandisingPort
    {
        private readonly IMerchandisingCampaignQuery _campaigns;
        private readonly ICurrentCommerceContext _commerce;

        public TestMerchandisingPort(IMerchandisingCampaignQuery campaigns, ICurrentCommerceContext commerce)
        {
            _campaigns = campaigns;
            _commerce = commerce;
        }

        public int MaxMemberTake => MerchandisingCampaignRuntimeLimits.MaxMemberTake;

        public string AmazingTypeCode => "AMAZING";

        public Guid? ResolveStoreId()
        {
            var tenantId = _commerce.Current?.Tenant?.TenantId.Value;
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return null;
            }

            return Guid.TryParse(tenantId, out var parsed) ? parsed : null;
        }

        public async Task<IReadOnlyList<StoreLandingMerchandisingMember>> ResolveCampaignMembersAsync(
            Guid campaignId,
            Guid storeId,
            string locale,
            DateTimeOffset now,
            int take,
            CancellationToken cancellationToken)
        {
            var members = await _campaigns.ResolveCampaignMembersAsync(
                campaignId, storeId, locale, now, take, null, cancellationToken);
            return members.Select(m => new StoreLandingMerchandisingMember(
                m.CatalogVariantId,
                m.IsMarketable,
                m.AvailableQuantity,
                m.PriceAmount,
                m.PriceCurrency,
                m.CompareAtAmount)).ToList();
        }

        public async Task<StoreLandingMerchandisingCampaign?> ResolveActiveByTypeAsync(
            Guid storeId,
            string typeCode,
            string locale,
            DateTimeOffset now,
            int take,
            CancellationToken cancellationToken)
        {
            var active = await _campaigns.ResolveActiveByTypeAsync(
                storeId, typeCode, locale, now, take, null, cancellationToken);
            if (active is null)
            {
                return null;
            }

            return new StoreLandingMerchandisingCampaign(
                active.CampaignId,
                active.BadgeText,
                active.Members.Select(m => new StoreLandingMerchandisingMember(
                    m.CatalogVariantId,
                    m.IsMarketable,
                    m.AvailableQuantity,
                    m.PriceAmount,
                    m.PriceCurrency,
                    m.CompareAtAmount)).ToList());
        }
    }
}
