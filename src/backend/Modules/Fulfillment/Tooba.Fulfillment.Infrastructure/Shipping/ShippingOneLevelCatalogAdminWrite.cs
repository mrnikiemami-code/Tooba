using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Shipping.Ports;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Persistence;

namespace Tooba.Fulfillment.Infrastructure.Shipping;

/// <summary>
/// درز نوشتن نام روش یک‌سطحی روی schema fulfillment — باریک، fail-closed و فقط «نام».
/// </summary>
/// <remarks>
/// <para>
/// دروازه از پیکربندی معتبر سرور خوانده می‌شود و فقط مقدار صریح <c>true</c> آن را روشن می‌کند. دروازهٔ
/// خاموش ⇒ هیچ پرس‌وجو/نوشتنی اجرا نمی‌شود و نتیجهٔ <c>GateDisabled</c> است.
/// </para>
/// <para>
/// فقط نام ترجمهٔ روش نوشته می‌شود: <c>method_code</c>، <c>legacy_service_code</c>،
/// <c>legacy_option_code</c>، <c>provider_kind</c>، ترتیب و فعال/غیرفعال بودن دست‌نخورده می‌مانند. یکتایی
/// canonical <c>(shipping_method_id, language_id)</c> مبنای به‌روزرسانی/درج است، بنابراین اجرای دوباره با
/// همان مقدار idempotent است و ردیف تکراری نمی‌سازد. توضیح اختیاری ردیف موجود هم دست‌نخورده می‌ماند.
/// </para>
/// <para>
/// جدول مهاجرت‌نشده یک نتیجهٔ کنترل‌شدهٔ <c>CatalogNotReady</c> است، نه خطای ۵۰۰ و نه ادعای موفقیت. تنها
/// خطای شناخته‌شدهٔ «رابط وجود ندارد» مهار می‌شود؛ هر خطای دیگر دست‌نخورده بالا می‌رود تا پنهان نشود.
/// </para>
/// <para>
/// این درز <c>Tooba:ShippingRateAuthority</c> و <c>Tooba:ShippingOperationalShadow</c> را نمی‌خواند،
/// القا نمی‌کند و تغییر نمی‌دهد و هیچ دادهٔ تاریخی checkout/cart/shipment را لمس نمی‌کند.
/// </para>
/// </remarks>
public sealed class ShippingOneLevelCatalogAdminWrite : IShippingOneLevelCatalogAdminWrite
{
    private readonly IOptions<ShippingOneLevelCatalogOptions> _options;
    private readonly FulfillmentDbContext _db;
    private readonly IIdGenerator _ids;

    /// <summary>درز را به پیکربندی و DbContext مالک ماژول وصل می‌کند.</summary>
    public ShippingOneLevelCatalogAdminWrite(
        IOptions<ShippingOneLevelCatalogOptions> options,
        FulfillmentDbContext db,
        IIdGenerator ids)
    {
        _options = options;
        _db = db;
        _ids = ids;
    }

    /// <inheritdoc />
    public async Task<ShippingOneLevelMethodNameWriteResult> UpdateMethodNameAsync(
        string methodCode,
        Guid languageId,
        string? name,
        CancellationToken cancellationToken)
    {
        var gate = ShippingOneLevelCatalogPolicy.Evaluate(_options.Value);
        if (gate != ShippingOneLevelCatalogGate.Enabled)
        {
            // دروازهٔ خاموش ⇒ هیچ پرس‌وجو/نوشتنی روی دیتابیس.
            return Outcome(ShippingOneLevelMethodNameWriteStatus.GateDisabled, string.Empty, languageId, string.Empty);
        }

        var normalized = ShippingMethod.TryNormalizeCode(methodCode);
        if (normalized is null)
        {
            // کد خام/نامعتبر هرگز نرمال یا به هویت تازه تبدیل نمی‌شود.
            return Outcome(ShippingOneLevelMethodNameWriteStatus.InvalidMethodCode, string.Empty, languageId, string.Empty);
        }

        if (languageId == Guid.Empty)
        {
            return Outcome(ShippingOneLevelMethodNameWriteStatus.MethodNotFound, normalized, languageId, string.Empty);
        }

        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return Outcome(ShippingOneLevelMethodNameWriteStatus.NameRequired, normalized, languageId, string.Empty);
        }

        if (trimmed.Length > ShippingOneLevelMethodNameLimits.MaxNameLength)
        {
            return Outcome(ShippingOneLevelMethodNameWriteStatus.NameTooLong, normalized, languageId, string.Empty);
        }

        try
        {
            // هویت روش فقط از کد canonical خودش می‌آید؛ هیچ بخشی از ورودی کلاینت به سرویس/گزینه تجزیه نمی‌شود.
            var method = await _db.ShippingMethods
                .FirstOrDefaultAsync(x => x.MethodCode == normalized, cancellationToken);
            if (method is null)
            {
                return Outcome(ShippingOneLevelMethodNameWriteStatus.MethodNotFound, normalized, languageId, string.Empty);
            }

            var existing = await _db.ShippingMethodTranslations
                .FirstOrDefaultAsync(
                    x => x.ShippingMethodId == method.ShippingMethodId && x.LanguageId == languageId,
                    cancellationToken);

            if (existing is null)
            {
                _db.ShippingMethodTranslations.Add(ShippingMethodTranslation.Create(
                    _ids.NewId(), method.ShippingMethodId, languageId, trimmed, description: null));
            }
            else
            {
                // فقط ستون نام؛ توضیح اختیاری ردیف موجود بازنویسی نمی‌شود و هیچ فیلد هویتی لمس نمی‌شود.
                var entry = _db.Entry(existing);
                entry.Property(x => x.Name).CurrentValue = trimmed;
                entry.Property(x => x.Name).IsModified = true;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return Outcome(ShippingOneLevelMethodNameWriteStatus.Updated, normalized, languageId, trimmed);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            // کاتالوگ یک‌سطحی هنوز مهاجرت نشده است. «آماده‌نبودن» صادقانه گزارش می‌شود و هیچ نوشتنی رخ نداده است.
            return Outcome(ShippingOneLevelMethodNameWriteStatus.CatalogNotReady, normalized, languageId, string.Empty);
        }
    }

    private static ShippingOneLevelMethodNameWriteResult Outcome(
        ShippingOneLevelMethodNameWriteStatus status,
        string methodCode,
        Guid languageId,
        string name) =>
        new(status, methodCode, languageId, name);
}
