using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Fulfillment.Domain.Aggregates;

/// <summary>
/// روش ارسال یک‌سطحی (merchant-visible). هر روش مستقل نام‌گذاری/فعال‌سازی می‌شود و یک کد عمومی
/// پایدار دارد. گروه‌بندی ارائه‌دهنده فقط metadata پنهان است و هرگز سطح دوم پیکربندی فروشنده نیست.
/// </summary>
/// <remarks>
/// <para>
/// هویت این رکورد بر <see cref="MethodCode"/> پایدار است، نه بر شناسهٔ گزینهٔ قدیمی: در
/// <c>UpdateAsync</c> سرویس، ردیف‌های گزینه حذف/بازساخته می‌شوند و شناسهٔ آن‌ها پایدار نیست.
/// </para>
/// <para>
/// <see cref="LegacyServiceCode"/> و <see cref="LegacyOptionCode"/> پیوند سازگاری با کاتالوگ دوسطحی
/// موجود است و تغییرناپذیر می‌ماند؛ <see cref="LegacyOptionCode"/> تهی یعنی روش به سرویس بدون گزینه
/// نگاشت شده است. یک سیاست fallback در سطح سرویس، روش فروشی دیگری محسوب نمی‌شود.
/// </para>
/// </remarks>
public sealed class ShippingMethod
{
    /// <summary>حداکثر طول کد روش؛ باریک‌ترین ستون پرداخت‌شدهٔ ارجاع‌دهندهٔ کد روش ۳۲ کاراکتر است.</summary>
    public const int MaxCodeLength = 32;

    /// <summary>حداکثر طول کد سرویس/گزینهٔ قدیمی (هم‌اندازهٔ ستون‌های موجود کاتالوگ).</summary>
    public const int MaxLegacyCodeLength = 64;

    /// <summary>شناسه پایدار روش.</summary>
    public Guid ShippingMethodId { get; init; }

    /// <summary>کد عمومی پایدار روش، مثل <c>post:standard</c> یا <c>store_courier</c>.</summary>
    public string MethodCode { get; private set; } = string.Empty;

    /// <summary>نوع ارائه‌دهنده برای metadata مرسوله (post/tipax/courier/store_courier/in_person).</summary>
    public string ProviderKind { get; private set; } = string.Empty;

    /// <summary>کد سرویس قدیمی والد (تغییرناپذیر).</summary>
    public string LegacyServiceCode { get; init; } = string.Empty;

    /// <summary>کد گزینهٔ قدیمی (تغییرناپذیر)؛ تهی یعنی نگاشت به سرویس بدون گزینه.</summary>
    public string? LegacyOptionCode { get; init; }

    /// <summary>ترتیب نمایش.</summary>
    public int SortOrder { get; private set; }

    /// <summary>فعال بودن برای انتخاب.</summary>
    public bool IsActive { get; private set; }

    /// <summary>زمان ایجاد.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>روش جدید می‌سازد و شکل کد را اعتبارسنجی می‌کند.</summary>
    public static ShippingMethod Create(
        Guid id,
        string methodCode,
        string providerKind,
        string legacyServiceCode,
        string? legacyOptionCode,
        int sortOrder,
        bool isActive,
        DateTimeOffset now)
    {
        var code = NormalizeCode(methodCode);
        var serviceCode = NormalizeLegacyCode(legacyServiceCode, "shipping_service.code.required");
        var optionCode = string.IsNullOrWhiteSpace(legacyOptionCode)
            ? null
            : NormalizeLegacyCode(legacyOptionCode, "shipping_service_option.code.required");

        return new ShippingMethod
        {
            ShippingMethodId = id,
            MethodCode = code,
            ProviderKind = string.IsNullOrWhiteSpace(providerKind) ? serviceCode : providerKind.Trim().ToLowerInvariant(),
            LegacyServiceCode = serviceCode,
            LegacyOptionCode = optionCode,
            SortOrder = sortOrder,
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>ترتیب/فعال بودن را به‌روز می‌کند؛ کد و پیوند قدیمی تغییرناپذیرند.</summary>
    public void Update(int sortOrder, bool isActive, DateTimeOffset now)
    {
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAt = now;
    }

    /// <summary>
    /// شکل کد روش را اعتبارسنجی و نرمال می‌کند. کد باید حروف کوچک/رقم/زیرخط باشد و در حالت ترکیبی
    /// فقط یک جداکنندهٔ <c>:</c> داشته باشد؛ سایر جداکننده‌ها مبهم‌اند و رد می‌شوند.
    /// </summary>
    /// <returns>کد نرمال‌شده یا <c>null</c> وقتی نامعتبر است.</returns>
    public static string? TryNormalizeCode(string? methodCode)
    {
        if (string.IsNullOrWhiteSpace(methodCode))
        {
            return null;
        }

        var candidate = methodCode.Trim().ToLowerInvariant();
        if (candidate.Length > MaxCodeLength)
        {
            return null;
        }

        var parts = candidate.Split(':');
        if (parts.Length is 0 or > 2)
        {
            return null;
        }

        foreach (var part in parts)
        {
            if (!IsCanonicalSegment(part))
            {
                return null;
            }
        }

        return candidate;
    }

    private static string NormalizeCode(string methodCode)
    {
        var normalized = TryNormalizeCode(methodCode);
        if (normalized is null)
        {
            throw new SemanticException(new SemanticError(
                string.IsNullOrWhiteSpace(methodCode) ? "shipping_service.code.required" : "fulfillment.rejected"));
        }

        return normalized;
    }

    private static bool IsCanonicalSegment(string segment)
    {
        if (segment.Length == 0)
        {
            return false;
        }

        foreach (var ch in segment)
        {
            var ok = (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_';
            if (!ok)
            {
                return false;
            }
        }

        return true;
    }

    private static string NormalizeLegacyCode(string value, string requiredCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new SemanticException(new SemanticError(requiredCode));
        }

        var trimmed = value.Trim().ToLowerInvariant();
        if (trimmed.Length > MaxLegacyCodeLength)
        {
            throw new SemanticException(new SemanticError("fulfillment.rejected"));
        }

        return trimmed;
    }
}
