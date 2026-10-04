namespace Tooba.Offer.Contracts.ReturnPolicy;

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
