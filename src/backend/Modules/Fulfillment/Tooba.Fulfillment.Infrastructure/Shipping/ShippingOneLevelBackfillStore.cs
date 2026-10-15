using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Application.Shipping.Ports;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Persistence;

namespace Tooba.Fulfillment.Infrastructure.Shipping;

/// <summary>
/// اجراکنندهٔ ماندگار backfill کاتالوگ یک‌سطحی روی schema fulfillment.
/// </summary>
/// <remarks>
/// <para>
/// این کلاس هیچ قاعدهٔ نگاشتی از خود ندارد: کاتالوگ منبع را از درز قراردادی
/// <see cref="IShippingCatalogReader"/> می‌خواند، تصمیم را به
/// <see cref="ShippingMethodBackfillPlanner"/> می‌سپارد و فقط تصمیم را می‌نویسد.
/// </para>
/// <para>
/// اتمی است: کل برنامه در یک تراکنش اعمال می‌شود؛ شکست هر ردیف، همه را برمی‌گرداند. برنامهٔ دارای
/// تخلف هرگز نوشته نمی‌شود و هیچ ردیف موجودی حذف یا بازنویسی نمی‌شود — فقط ردیف تازه ساخته می‌شود و
/// فعال/غیرفعال بودن صریحاً هم‌راستا می‌شود.
/// </para>
/// </remarks>
public sealed class ShippingOneLevelBackfillStore : IShippingOneLevelBackfillStore
{
    private readonly FulfillmentDbContext _db;
    private readonly IShippingCatalogReader _catalog;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;

    /// <summary>درز را به DbContext و کاتالوگ قراردادی وصل می‌کند.</summary>
    public ShippingOneLevelBackfillStore(
        FulfillmentDbContext db,
        IShippingCatalogReader catalog,
        IClock clock,
        IIdGenerator ids)
    {
        _db = db;
        _catalog = catalog;
        _clock = clock;
        _ids = ids;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingMethodExistingRow>> ListExistingAsync(CancellationToken cancellationToken) =>
        await _db.ShippingMethods.AsNoTracking()
            .OrderBy(x => x.SortOrder).ThenBy(x => x.MethodCode)
            .Select(x => new ShippingMethodExistingRow(
                x.ShippingMethodId,
                x.MethodCode,
                x.LegacyServiceCode,
                x.LegacyOptionCode,
                x.IsActive))
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task ApplyAsync(ShippingMethodBackfillPlan plan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);

        // تخلف ⇒ هیچ نوشتنی. این نگهبان دوم است تا حتی فراخوانی مستقیم هم کاتالوگ را خراب نکند.
        if (!plan.IsExecutable)
        {
            throw new InvalidOperationException("shipping.one_level.backfill.plan_not_executable");
        }

        var now = _clock.UtcNow;
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var item in plan.Items)
        {
            switch (item.Action)
            {
                case ShippingMethodPlanAction.Create:
                    AddMethod(item.Desired, now);
                    break;

                case ShippingMethodPlanAction.AlignActivation:
                    var existing = await _db.ShippingMethods
                        .FirstOrDefaultAsync(x => x.ShippingMethodId == item.ExistingShippingMethodId, cancellationToken)
                        ?? throw new InvalidOperationException("shipping.one_level.backfill.row_missing");
                    // فقط ترتیب/فعال بودن؛ کد و پیوند قدیمی تغییرناپذیرند.
                    existing.Update(item.Desired.SortOrder, item.Desired.IsActive, now);
                    break;

                case ShippingMethodPlanAction.Skip:
                default:
                    break;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private void AddMethod(ShippingMethodDesiredRow desired, DateTimeOffset now)
    {
        var id = _ids.NewId();
        _db.ShippingMethods.Add(ShippingMethod.Create(
            id,
            desired.MethodCode,
            desired.ProviderKind,
            desired.LegacyServiceCode,
            desired.LegacyOptionCode,
            desired.SortOrder,
            desired.IsActive,
            now));

        foreach (var translation in desired.Translations)
        {
            _db.ShippingMethodTranslations.Add(ShippingMethodTranslation.Create(
                _ids.NewId(),
                id,
                translation.LanguageId,
                translation.Name,
                translation.Description));
        }
    }
}
