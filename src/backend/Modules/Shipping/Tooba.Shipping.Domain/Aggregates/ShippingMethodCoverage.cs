using Tooba.BuildingBlocks;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Shipping.Domain.Aggregates;

/// <summary>
/// اعلام حالت تحدید جغرافیایی برای یک روش canonical ارسال (کد خدمت + گزینهٔ اختیاری).
/// </summary>
/// <remarks>
/// <para>
/// <b>چرا این aggregate جدا از <see cref="ShippingCoverageGrant"/> است.</b> مجوز پوشش می‌گوید «کدام
/// مقصد مجاز است»؛ اما «مجموعهٔ مجاز خالی» و «ادمین محدودیتی تعریف نکرده» هر دو بدون ردیف مجوز
/// دیده می‌شوند و با هم قابل تفکیک نیستند. این اعلام دقیقاً همان تفکیک را ممکن می‌کند:
/// <see cref="ShippingCoverageMode.Restricted"/> + صفر مجوز = هیچ مقصدی مجاز نیست، و نبود اعلام =
/// بدون محدودیت. برای این کار هیچ ردیف مقصد فراگیری ساخته نمی‌شود.
/// </para>
/// <para>
/// <b>دامنهٔ اختیار.</b> این ردیف فقط یک تصمیم ادمین دربارهٔ تحدید جغرافیایی است: نه «فعال بودن
/// خدمت»، نه «وجود نرخ» و نه «آمادهٔ استعلام بودن». هیچ ردیف قدیمی مجوز به‌طور خودکار به گزینه‌ها
/// نسبت داده نمی‌شود و این اعلام هیچ اثری روی مسیر legacy ندارد.
/// </para>
/// <para>
/// <b>یکتایی.</b> برای هر (کد خدمت، گزینه) حداکثر یک ردیف وجود دارد؛ تکرار همان هویت یک اعلام دوم
/// می‌ساخت که «کدام حالت معتبر است» را به ترتیب ردیف‌ها وابسته می‌کرد.
/// </para>
/// </remarks>
public sealed class ShippingMethodCoverage
{
    private ShippingMethodCoverage()
    {
    }

    /// <summary>شناسهٔ پایدار اعلام.</summary>
    public Guid MethodCoverageId { get; init; }

    /// <summary>کد خدمت ارسال (کلید سازگاری).</summary>
    public string ServiceCode { get; private set; } = string.Empty;

    /// <summary>گزینهٔ روش ارسال؛ تهی یعنی روش در سطح خدمت است.</summary>
    public string? OptionCode { get; private set; }

    /// <summary>حالت تحدید اعلام‌شده برای این روش.</summary>
    public ShippingCoverageMode Mode { get; private set; }

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان آخرین ویرایش UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>اعلام حالت جدید می‌سازد.</summary>
    /// <param name="serviceCode">کد خدمت ارسال.</param>
    /// <param name="optionCode">گزینهٔ روش؛ تهی یعنی روش در سطح خدمت.</param>
    /// <param name="mode">حالت تحدید.</param>
    /// <param name="now">زمان UTC ایجاد.</param>
    /// <param name="methodCoverageId">شناسهٔ قطعی اختیاری برای بازپخش قطعی.</param>
    public static ShippingMethodCoverage Create(
        string serviceCode,
        string? optionCode,
        ShippingCoverageMode mode,
        DateTimeOffset now,
        Guid? methodCoverageId = null)
    {
        var coverage = new ShippingMethodCoverage
        {
            MethodCoverageId = methodCoverageId is { } id && id != Guid.Empty ? id : UuidV7.New(),
            ServiceCode = NormalizeServiceCode(serviceCode),
            OptionCode = NormalizeOptionCode(optionCode),
            Mode = EnsureKnownMode(mode),
            CreatedAt = now,
            UpdatedAt = now,
        };

        return coverage;
    }

    /// <summary>حالت تحدید اعلام‌شده را تغییر می‌دهد (روشن/خاموش کردن تحدید برای همین روش).</summary>
    /// <param name="mode">حالت هدف.</param>
    /// <param name="now">زمان UTC ویرایش.</param>
    public void ChangeMode(ShippingCoverageMode mode, DateTimeOffset now)
    {
        Mode = EnsureKnownMode(mode);
        UpdatedAt = now;
    }

    /// <summary>
    /// آیا این اعلام بلوک‌کننده (تحدیدکننده) است. <see cref="ShippingCoverageMode.Unrestricted"/>
    /// هیچ‌گاه مقصدی را رد نمی‌کند.
    /// </summary>
    public bool IsRestrictive => Mode == ShippingCoverageMode.Restricted;

    /// <summary>آیا این اعلام دقیقاً به روش داده‌شده تعلق دارد (همان کد خدمت و همان گزینه).</summary>
    /// <param name="serviceCode">کد خدمت درخواست.</param>
    /// <param name="optionCode">گزینهٔ درخواست؛ تهی یعنی روش سطح خدمت.</param>
    public bool BelongsToMethod(string serviceCode, string? optionCode) =>
        string.Equals(ServiceCode, serviceCode?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase)
        && string.Equals(
            OptionCode ?? string.Empty,
            NormalizeOptionCode(optionCode) ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);

    private static ShippingCoverageMode EnsureKnownMode(ShippingCoverageMode mode) =>
        Enum.IsDefined(mode)
            ? mode
            : throw new SemanticException(new SemanticError(ShippingErrorCodes.CoverageScopeInvalid));

    private static string NormalizeServiceCode(string? serviceCode)
    {
        var trimmed = serviceCode?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new SemanticException(new SemanticError(ShippingErrorCodes.ServiceCodeRequired));
        }

        return trimmed.Length > ShippingRatePolicy.ServiceCodeMaxLength
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.ServiceCodeInvalid))
            : trimmed;
    }

    private static string? NormalizeOptionCode(string? optionCode)
    {
        var trimmed = optionCode?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        return trimmed.Length > ShippingRatePolicy.OptionCodeMaxLength
            ? throw new SemanticException(new SemanticError(ShippingErrorCodes.OptionCodeInvalid))
            : trimmed;
    }
}
