namespace Tooba.Offer.Application;

/// <summary>
/// پیکربندی حاکمیت سیاست مرجوعی فروشگاه/پلتفرم (نه قانون سخت ۷ روز).
/// </summary>
public sealed class ReturnPolicyOptions
{
    /// <summary>نام بخش پیکربندی.</summary>
    public const string SectionName = "Tooba:ReturnPolicy";

    /// <summary>پنجرهٔ پیش‌فرض فروشگاه به روز.</summary>
    public int DefaultReturnWindowDays { get; set; } = 7;

    /// <summary>آیا فروشنده می‌تواند سیاست اختصاصی/غیرقابل‌مرجوعی بگذارد.</summary>
    public bool SellerCanOverrideReturnPolicy { get; set; } = true;

    /// <summary>حداقل روز برای مهلت اختصاصی.</summary>
    public int MinReturnWindowDays { get; set; } = 1;

    /// <summary>حداکثر روز برای مهلت اختصاصی.</summary>
    public int MaxReturnWindowDays { get; set; } = 30;

    /// <summary>اجازهٔ Offer غیرقابل مرجوعی.</summary>
    public bool AllowNonReturnableOffers { get; set; } = true;
}

/// <summary>
/// انتخاب canonical سیاست روی Offer.
/// </summary>
public static class OfferReturnPolicyChoices
{
    /// <summary>پیروی از پیش‌فرض فروشگاه.</summary>
    public const string Default = "Default";

    /// <summary>مهلت اختصاصی.</summary>
    public const string Custom = "Custom";

    /// <summary>غیرقابل مرجوعی.</summary>
    public const string NonReturnable = "NonReturnable";

    /// <summary>نرمال‌سازی ورودی.</summary>
    public static string Normalize(string? value) =>
        value?.Trim() switch
        {
            Custom => Custom,
            NonReturnable => NonReturnable,
            _ => Default,
        };
}

/// <summary>
/// نتیجهٔ resolve برای snapshot خط سفارش.
/// </summary>
public sealed record ResolvedReturnPolicy(
    bool IsReturnable,
    int WindowDays,
    string Source,
    string LabelFa);

/// <summary>
/// resolve سیاست مؤثر در زمان خرید.
/// </summary>
public interface IReturnPolicyResolver
{
    /// <summary>گزینه‌های حاکمیت جاری.</summary>
    ReturnPolicyOptions Options { get; }

    /// <summary>اعتبارسنجی انتخاب Offer قبل از ذخیره.</summary>
    void ValidateOfferChoice(string choice, int? customReturnWindowDays);

    /// <summary>resolve مؤثر برای checkout snapshot.</summary>
    ResolvedReturnPolicy ResolveForCheckout(string choice, int? customReturnWindowDays, bool? categoryForcesNonReturnable = null);
}

/// <summary>
/// پیاده‌سازی حاکمیت فروشگاه/پلتفرم برای سیاست مرجوعی.
/// </summary>
public sealed class ReturnPolicyResolver : IReturnPolicyResolver
{
    private readonly ReturnPolicyOptions _options;

    /// <summary>resolver را با گزینه‌ها می‌سازد.</summary>
    public ReturnPolicyResolver(ReturnPolicyOptions options) =>
        _options = options ?? new ReturnPolicyOptions();

    /// <inheritdoc />
    public ReturnPolicyOptions Options => _options;

    /// <inheritdoc />
    public void ValidateOfferChoice(string choice, int? customReturnWindowDays)
    {
        var normalized = OfferReturnPolicyChoices.Normalize(choice);
        if (normalized == OfferReturnPolicyChoices.Default)
        {
            return;
        }

        if (!_options.SellerCanOverrideReturnPolicy)
        {
            throw new InvalidOperationException("تغییر سیاست مرجوعی برای فروشنده مجاز نیست.");
        }

        if (normalized == OfferReturnPolicyChoices.NonReturnable)
        {
            if (!_options.AllowNonReturnableOffers)
            {
                throw new InvalidOperationException("ثبت پیشنهاد غیرقابل مرجوعی مجاز نیست.");
            }

            return;
        }

        if (normalized == OfferReturnPolicyChoices.Custom)
        {
            if (customReturnWindowDays is null)
            {
                throw new InvalidOperationException("مهلت اختصاصی مرجوعی الزامی است.");
            }

            if (customReturnWindowDays < _options.MinReturnWindowDays
                || customReturnWindowDays > _options.MaxReturnWindowDays)
            {
                throw new InvalidOperationException(
                    $"مهلت مرجوعی باید بین {_options.MinReturnWindowDays} و {_options.MaxReturnWindowDays} روز باشد.");
            }
        }
    }

    /// <inheritdoc />
    public ResolvedReturnPolicy ResolveForCheckout(
        string choice,
        int? customReturnWindowDays,
        bool? categoryForcesNonReturnable = null)
    {
        if (categoryForcesNonReturnable == true)
        {
            return new ResolvedReturnPolicy(false, 0, "category_restriction", "غیرقابل مرجوعی");
        }

        var normalized = OfferReturnPolicyChoices.Normalize(choice);
        try
        {
            ValidateOfferChoice(normalized, customReturnWindowDays);
        }
        catch (InvalidOperationException)
        {
            // انتخاب نامعتبر فروشنده در checkout به پیش‌فرض امن فروشگاه می‌افتد.
            normalized = OfferReturnPolicyChoices.Default;
            customReturnWindowDays = null;
        }

        if (normalized == OfferReturnPolicyChoices.NonReturnable)
        {
            return new ResolvedReturnPolicy(false, 0, "offer_override", "غیرقابل مرجوعی");
        }

        if (normalized == OfferReturnPolicyChoices.Custom && customReturnWindowDays is int days)
        {
            return new ResolvedReturnPolicy(
                true,
                days,
                "offer_override",
                FormatWindowLabel(days));
        }

        var defaults = Math.Max(0, _options.DefaultReturnWindowDays);
        return new ResolvedReturnPolicy(
            defaults > 0,
            defaults,
            "platform_default",
            defaults > 0 ? FormatWindowLabel(defaults) : "غیرقابل مرجوعی");
    }

    private static string FormatWindowLabel(int days) => $"{days} روز پس از تحویل";
}
