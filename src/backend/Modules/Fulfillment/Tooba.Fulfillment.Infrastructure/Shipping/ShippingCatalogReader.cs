using Microsoft.EntityFrameworkCore;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Infrastructure.Persistence;

namespace Tooba.Fulfillment.Infrastructure.Shipping;

/// <summary>خواندن کاتالوگ سرویس ارسال از schema fulfillment.</summary>
public sealed class ShippingCatalogReader : IShippingCatalogReader
{
    private readonly FulfillmentDbContext _db;

    /// <summary>Reader را به DbContext وصل می‌کند.</summary>
    public ShippingCatalogReader(FulfillmentDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> ListAsync(CancellationToken cancellationToken)
    {
        var services = await _db.ShippingServices.AsNoTracking()
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
        return await MapServicesAsync(services.Select(x => x.ShippingServiceId).ToArray(), services, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ShippingCatalogServiceSnapshot?> GetAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var service = await _db.ShippingServices.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ShippingServiceId == serviceId, cancellationToken);
        if (service is null)
        {
            return null;
        }

        var mapped = await MapServicesAsync([serviceId], [service], cancellationToken);
        return mapped.FirstOrDefault();
    }

    private async Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> MapServicesAsync(
        Guid[] serviceIds,
        IReadOnlyList<Domain.Aggregates.ShippingService> services,
        CancellationToken cancellationToken)
    {
        if (serviceIds.Length == 0)
        {
            return [];
        }

        var translations = await _db.ShippingServiceTranslations.AsNoTracking()
            .Where(x => serviceIds.Contains(x.ShippingServiceId))
            .ToListAsync(cancellationToken);
        var options = await _db.ShippingServiceOptions.AsNoTracking()
            .Where(x => serviceIds.Contains(x.ShippingServiceId))
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
        var optionIds = options.Select(x => x.ShippingServiceOptionId).ToArray();
        var optionTranslations = optionIds.Length == 0
            ? []
            : await _db.ShippingServiceOptionTranslations.AsNoTracking()
                .Where(x => optionIds.Contains(x.ShippingServiceOptionId))
                .ToListAsync(cancellationToken);

        var translationsBy = translations.GroupBy(x => x.ShippingServiceId).ToDictionary(g => g.Key, g => g.ToList());
        var optionsBy = options.GroupBy(x => x.ShippingServiceId).ToDictionary(g => g.Key, g => g.ToList());
        var optionTranslationsBy = optionTranslations.GroupBy(x => x.ShippingServiceOptionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return services.Select(s =>
        {
            translationsBy.TryGetValue(s.ShippingServiceId, out var tr);
            optionsBy.TryGetValue(s.ShippingServiceId, out var opts);
            tr ??= [];
            opts ??= [];
            return new ShippingCatalogServiceSnapshot(
                s.ShippingServiceId,
                s.Code,
                s.ProviderKind,
                s.IconKey,
                s.ColorKey,
                s.IsActive,
                s.SortOrder,
                tr.Select(t => new ShippingCatalogTranslationSnapshot(t.LanguageId, t.Name, t.Description)).ToArray(),
                opts.Select(o =>
                {
                    optionTranslationsBy.TryGetValue(o.ShippingServiceOptionId, out var otr);
                    otr ??= [];
                    return new ShippingCatalogOptionSnapshot(
                        o.ShippingServiceOptionId,
                        o.ShippingServiceId,
                        o.Code,
                        o.IsActive,
                        o.SortOrder,
                        otr.Select(t => new ShippingCatalogOptionTranslationSnapshot(t.LanguageId, t.Name)).ToArray());
                }).ToArray());
        }).ToList();
    }
}
