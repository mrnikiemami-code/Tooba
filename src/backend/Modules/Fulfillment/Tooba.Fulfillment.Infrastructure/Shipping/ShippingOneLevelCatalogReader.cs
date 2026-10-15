using Microsoft.EntityFrameworkCore;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Infrastructure.Persistence;

namespace Tooba.Fulfillment.Infrastructure.Shipping;

/// <summary>
/// خواندن کاتالوگ روش یک‌سطحی از schema fulfillment. فقط‌خواندنی و بدون تغییر مسیر قیمت.
/// </summary>
/// <remarks>
/// این reader هیچ سرویس/گزینهٔ قدیمی را تغییر نمی‌دهد و هیچ رفتاری در Checkout یا اسنپ‌شات
/// پرداخت‌شده ندارد؛ تا وقتی دروازهٔ <see cref="ShippingOneLevelCatalogOptions"/> خاموش است،
/// مصرف‌کننده‌ای به آن وصل نیست.
/// </remarks>
public sealed class ShippingOneLevelCatalogReader : IShippingOneLevelCatalogReader
{
    private readonly FulfillmentDbContext _db;

    /// <summary>Reader را به DbContext وصل می‌کند.</summary>
    public ShippingOneLevelCatalogReader(FulfillmentDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingOneLevelMethodSnapshot>> ListAsync(CancellationToken cancellationToken) =>
        await LoadAsync(activeOnly: false, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingOneLevelMethodSnapshot>> ListActiveAsync(CancellationToken cancellationToken) =>
        await LoadAsync(activeOnly: true, cancellationToken);

    /// <inheritdoc />
    public async Task<ShippingOneLevelMethodSnapshot?> GetByCodeAsync(string methodCode, CancellationToken cancellationToken)
    {
        var normalized = Domain.Aggregates.ShippingMethod.TryNormalizeCode(methodCode);
        if (normalized is null)
        {
            return null;
        }

        var rows = await LoadAsync(activeOnly: false, cancellationToken);
        return rows.FirstOrDefault(x => string.Equals(x.MethodCode, normalized, StringComparison.Ordinal));
    }

    private async Task<IReadOnlyList<ShippingOneLevelMethodSnapshot>> LoadAsync(
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var query = _db.ShippingMethods.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        var methods = await query
            .OrderBy(x => x.SortOrder).ThenBy(x => x.MethodCode)
            .ToListAsync(cancellationToken);

        if (methods.Count == 0)
        {
            return [];
        }

        var ids = methods.Select(x => x.ShippingMethodId).ToArray();
        var translations = await _db.ShippingMethodTranslations.AsNoTracking()
            .Where(x => ids.Contains(x.ShippingMethodId))
            .ToListAsync(cancellationToken);
        var translationsBy = translations.GroupBy(x => x.ShippingMethodId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return methods.Select(m =>
        {
            translationsBy.TryGetValue(m.ShippingMethodId, out var tr);
            tr ??= [];
            return new ShippingOneLevelMethodSnapshot(
                m.ShippingMethodId,
                m.MethodCode,
                m.ProviderKind,
                m.LegacyServiceCode,
                m.LegacyOptionCode,
                m.IsActive,
                m.SortOrder,
                tr.Select(t => new ShippingOneLevelMethodTranslationSnapshot(t.LanguageId, t.Name, t.Description)).ToArray());
        }).ToList();
    }
}
