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
/// درز ایجاد روش یک‌سطحی روی schema fulfillment — باریک، fail-closed و فقط ردیف canonical روش.
/// </summary>
/// <remarks>
/// <para>
/// دروازه از پیکربندی معتبر سرور خوانده می‌شود و فقط مقدار صریح <c>true</c> آن را روشن می‌کند. دروازهٔ
/// خاموش ⇒ هیچ پرس‌وجو/نوشتنی اجرا نمی‌شود.
/// </para>
/// <para>
/// این درز هیچ ردیف <c>shipping_services</c>/<c>shipping_service_options</c> نمی‌سازد و هیچ سیاست
/// نرخ/پوشش/برنامه/بسته‌بندی و هیچ دادهٔ Checkout/Order/محموله را لمس نمی‌کند؛ بنابراین روش تازه
/// در فرافکنی فروشگاه (که از همان کاتالوگ قدیمی خوانده می‌شود) ظاهر نمی‌شود و قابل‌انتخاب مشتری
/// نیست. پیوند قدیمی از همان کد canonical و با همان قاعدهٔ backfill مشتق می‌شود، پس کلاینت هرگز
/// پیوند دلخواه ارسال نمی‌کند.
/// </para>
/// <para>
/// روش تازه همیشه <c>is_active = false</c> نوشته می‌شود. فعال‌سازی فقط از درز تراکنشی ۰۹۶ می‌آید که
/// وجود سرویس/گزینهٔ قدیمی و فعال بودنشان را شرط می‌کند؛ در نتیجه روش بدون هویت قدیمی معتبر هرگز
/// فعال و قابل‌انتخاب نمی‌شود.
/// </para>
/// <para>
/// جدول مهاجرت‌نشده یک نتیجهٔ کنترل‌شدهٔ <c>CatalogNotReady</c> است. یکتایی واقعی دیتابیس
/// (<c>ix_shipping_methods_method_code</c> / <c>ux_shipping_methods_legacy_binding</c>) برای درخواست‌های
/// هم‌زمان صادقانه به <c>ConcurrentConflict</c>/<c>BindingDuplicate</c> نگاشت می‌شود؛ هیچ ادعای
/// موفقیتی در برخورد نوشته نمی‌شود.
/// </para>
/// </remarks>
public sealed class ShippingOneLevelCatalogAdminCreate : IShippingOneLevelCatalogAdminCreate
{
    private const string UniqueViolation = "23505";

    private readonly IOptions<ShippingOneLevelCatalogOptions> _options;
    private readonly FulfillmentDbContext _db;
    private readonly IIdGenerator _ids;
    private readonly IClock _clock;

    /// <summary>درز را به پیکربندی، DbContext مالک، شناسه‌ساز و ساعت وصل می‌کند.</summary>
    public ShippingOneLevelCatalogAdminCreate(
        IOptions<ShippingOneLevelCatalogOptions> options,
        FulfillmentDbContext db,
        IIdGenerator ids,
        IClock clock)
    {
        _options = options;
        _db = db;
        _ids = ids;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<ShippingOneLevelMethodCreateOutcome> CreateMethodAsync(
        string methodCode,
        string? providerKind,
        IReadOnlyList<ShippingOneLevelMethodCreateName> translations,
        CancellationToken cancellationToken)
    {
        var gate = ShippingOneLevelCatalogPolicy.Evaluate(_options.Value);
        if (gate != ShippingOneLevelCatalogGate.Enabled)
        {
            // دروازهٔ خاموش ⇒ هیچ پرس‌وجو/نوشتنی روی دیتابیس.
            return Failed(ShippingOneLevelMethodCreateStatus.GateDisabled);
        }

        var normalized = ShippingMethod.TryNormalizeCode(methodCode);
        if (normalized is null)
        {
            // کد خام/نامعتبر هرگز نرمال یا به هویت تازه تبدیل نمی‌شود.
            return Failed(ShippingOneLevelMethodCreateStatus.InvalidMethodCode);
        }

        var binding = ShippingOneLevelMethodBinding.Derive(normalized);
        if (binding is null)
        {
            return Failed(ShippingOneLevelMethodCreateStatus.InvalidMethodCode);
        }

        var provider = string.IsNullOrWhiteSpace(providerKind)
            ? binding.Value.ServiceCode
            : providerKind.Trim().ToLowerInvariant();
        if (!IsCanonicalProviderKind(provider))
        {
            return Failed(ShippingOneLevelMethodCreateStatus.InvalidProviderKind);
        }

        var names = new List<(Guid LanguageId, string Name)>(translations.Count);
        var seenLanguages = new HashSet<Guid>();
        foreach (var translation in translations)
        {
            if (translation.LanguageId == Guid.Empty || !seenLanguages.Add(translation.LanguageId))
            {
                return Failed(ShippingOneLevelMethodCreateStatus.DuplicateLanguage);
            }

            var name = translation.Name?.Trim() ?? string.Empty;
            if (name.Length == 0)
            {
                return Failed(ShippingOneLevelMethodCreateStatus.NameRequired);
            }

            if (name.Length > ShippingOneLevelMethodNameLimits.MaxNameLength)
            {
                return Failed(ShippingOneLevelMethodCreateStatus.NameTooLong);
            }

            names.Add((translation.LanguageId, name));
        }

        if (names.Count == 0)
        {
            return Failed(ShippingOneLevelMethodCreateStatus.NameRequired);
        }

        try
        {
            // کد canonical یکتا است؛ بررسی صریح هم برای پیام دقیق و هم برای provider in-memory.
            var codeTaken = await _db.ShippingMethods
                .AnyAsync(x => x.MethodCode == normalized, cancellationToken);
            if (codeTaken)
            {
                return Failed(ShippingOneLevelMethodCreateStatus.MethodCodeDuplicate);
            }

            // نگهبان جدایی روش‌ها: پیوند مشتق‌شده نباید در اختیار روش دیگری باشد. ردیف legacy/backfill
            // با کد متفاوت اما همان پیوند، این‌جا fail-closed رد می‌شود.
            var bindingTaken = await _db.ShippingMethods.AnyAsync(
                x => x.LegacyServiceCode == binding.Value.ServiceCode
                    && x.LegacyOptionCode == binding.Value.OptionCode,
                cancellationToken);
            if (bindingTaken)
            {
                return Failed(ShippingOneLevelMethodCreateStatus.BindingDuplicate);
            }

            var maxSort = await _db.ShippingMethods
                .Select(x => (int?)x.SortOrder)
                .MaxAsync(cancellationToken) ?? 0;

            var method = ShippingMethod.Create(
                _ids.NewId(),
                normalized,
                provider,
                binding.Value.ServiceCode,
                binding.Value.OptionCode,
                maxSort + 1,
                // هیچ روش تازه‌ای فعال ساخته نمی‌شود؛ فعال‌سازی فقط از درز ۰۹۶.
                isActive: false,
                _clock.UtcNow);

            _db.ShippingMethods.Add(method);
            foreach (var (languageId, name) in names)
            {
                _db.ShippingMethodTranslations.Add(ShippingMethodTranslation.Create(
                    _ids.NewId(), method.ShippingMethodId, languageId, name, description: null));
            }

            // یک واحد کار: ردیف روش و ترجمه‌هایش با هم نوشته می‌شوند؛ خطا ⇒ هیچ‌کدام.
            await _db.SaveChangesAsync(cancellationToken);

            return new ShippingOneLevelMethodCreateOutcome(
                ShippingOneLevelMethodCreateStatus.Created,
                method.MethodCode,
                method.ProviderKind,
                method.LegacyServiceCode,
                method.LegacyOptionCode,
                method.IsActive,
                method.SortOrder);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            // کاتالوگ یک‌سطحی هنوز مهاجرت نشده است؛ «آماده‌نبودن» صادقانه گزارش می‌شود.
            return Failed(ShippingOneLevelMethodCreateStatus.CatalogNotReady);
        }
        catch (DbUpdateException ex) when (IsUniqueConflict(ex, out var bindingConflict))
        {
            return Failed(bindingConflict
                ? ShippingOneLevelMethodCreateStatus.BindingDuplicate
                : ShippingOneLevelMethodCreateStatus.ConcurrentConflict);
        }
    }

    /// <summary>شکل مجاز metadata ارائه‌دهنده: حروف کوچک/رقم/زیرخط و حداکثر ۶۴ نویسه.</summary>
    private static bool IsCanonicalProviderKind(string value)
    {
        if (value.Length is 0 or > ShippingMethod.MaxLegacyCodeLength)
        {
            return false;
        }

        foreach (var ch in value)
        {
            var ok = (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_';
            if (!ok)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsUniqueConflict(DbUpdateException exception, out bool bindingConflict)
    {
        bindingConflict = false;
        if (exception.InnerException is not PostgresException postgres || postgres.SqlState != UniqueViolation)
        {
            return false;
        }

        bindingConflict = postgres.ConstraintName is not null
            && postgres.ConstraintName.Contains("legacy_binding", StringComparison.OrdinalIgnoreCase);
        return true;
    }

    private static ShippingOneLevelMethodCreateOutcome Failed(ShippingOneLevelMethodCreateStatus status) =>
        new(status, string.Empty, string.Empty, string.Empty, null, IsActive: false, SortOrder: 0);
}
