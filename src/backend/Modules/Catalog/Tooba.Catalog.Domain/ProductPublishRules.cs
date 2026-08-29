namespace Tooba.Catalog.Domain;

/// <summary>
/// برچسب‌ها و خلاصهٔ فارسی آمادگی انتشار محصول — بدون Offer/Price/Stock.
/// </summary>
public static class ProductPublishRules
{
    /// <summary>آمادگی کامل برای انتشار.</summary>
    public const string MessageReadyFa = "محصول برای انتشار آماده است.";

    /// <summary>دستهٔ سطح سوم ناقص است.</summary>
    public const string MessageCategoryIncompleteFa = "دسته‌بندی معتبر (سطح سوم) تکمیل نشده است.";

    /// <summary>هویت/ترجمه ناقص است.</summary>
    public const string MessageIdentityIncompleteFa = "اطلاعات اصلی / ترجمهٔ محصول تکمیل نشده است.";

    /// <summary>ویژگی‌های الزامی ناقص است.</summary>
    public const string MessageAttributesIncompleteFa = "ویژگی‌های الزامی تکمیل نشده است.";

    /// <summary>تنوع‌ها نامعتبر است.</summary>
    public const string MessageVariantsIncompleteFa = "تنوع‌های محصول آماده نیست.";

    /// <summary>رسانه ناقص است.</summary>
    public const string MessageMediaIncompleteFa = "تصویر اصلی تعیین نشده است.";

    /// <summary>سئو ناقص است.</summary>
    public const string MessageSeoIncompleteFa = "اطلاعات سئو تکمیل نشده است.";

    /// <summary>انتشار از بایگانی بدون آمادگی رد می‌شود.</summary>
    public const string MessageNotReadyFa = "محصول برای انتشار آماده نیست.";

    /// <summary>برچسب فارسی وضعیت چرخهٔ عمر.</summary>
    public static string LifecycleLabelFa(CatalogPublicationStatus status) =>
        status switch
        {
            CatalogPublicationStatus.Draft => "پیش‌نویس",
            CatalogPublicationStatus.Published => "منتشرشده",
            CatalogPublicationStatus.Archived => "بایگانی‌شده",
            _ => status.ToString(),
        };

    /// <summary>
    /// خلاصهٔ فارسی تعداد موارد ناقص: «برای انتشار، N مورد دیگر باید تکمیل شود.»
    /// </summary>
    public static string SummarizeMissingFa(int missingCount)
    {
        if (missingCount <= 0)
        {
            return MessageReadyFa;
        }

        return $"برای انتشار، {missingCount} مورد دیگر باید تکمیل شود.";
    }
}
