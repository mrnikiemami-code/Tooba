using System.Collections.Generic;
using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain;

/// <summary>
/// ردیف append-only تاریخچهٔ محصول Catalog — بدون Offer/Price/Stock.
/// </summary>
public sealed class CatalogProductHistoryEntry
{
    /// <summary>شناسهٔ ردیف.</summary>
    public Guid HistoryId { get; init; }

    /// <summary>محصول مالک.</summary>
    public Guid ProductId { get; init; }

    /// <summary>کد پایدار رویداد (مثلاً product.publish).</summary>
    public string EventType { get; init; } = "";

    /// <summary>بخش Workspace: general/category/attributes/variants/media/seo/lifecycle.</summary>
    public string Section { get; init; } = "";

    /// <summary>خلاصهٔ انسانی فارسی برای UI.</summary>
    public string SummaryFa { get; init; } = "";

    /// <summary>خلاصهٔ فشردهٔ قبل (اختیاری).</summary>
    public string? BeforeSummary { get; init; }

    /// <summary>خلاصهٔ فشردهٔ بعد (اختیاری).</summary>
    public string? AfterSummary { get; init; }

    /// <summary>شناسهٔ بازیگر؛ null یعنی سیستم/نامشخص.</summary>
    public Guid? ActorUserId { get; init; }

    /// <summary>نام نمایشی بازیگر در زمان ثبت (اختیاری).</summary>
    public string? ActorDisplayName { get; init; }

    /// <summary>زمان رخداد UTC.</summary>
    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>ردیف تاریخچه می‌سازد.</summary>
    public static CatalogProductHistoryEntry Create(
        Guid productId,
        string eventType,
        string section,
        string summaryFa,
        DateTimeOffset occurredAt,
        Guid? actorUserId = null,
        string? actorDisplayName = null,
        string? beforeSummary = null,
        string? afterSummary = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(section);
        ArgumentException.ThrowIfNullOrWhiteSpace(summaryFa);
        return new CatalogProductHistoryEntry
        {
            HistoryId = UuidV7.New(),
            ProductId = productId,
            EventType = eventType.Trim(),
            Section = section.Trim(),
            SummaryFa = summaryFa.Trim(),
            BeforeSummary = Truncate(beforeSummary),
            AfterSummary = Truncate(afterSummary),
            ActorUserId = actorUserId,
            ActorDisplayName = string.IsNullOrWhiteSpace(actorDisplayName) ? null : actorDisplayName.Trim(),
            OccurredAt = occurredAt,
        };
    }

    private static string? Truncate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= 512 ? trimmed : trimmed[..512];
    }
}

/// <summary>کدها و برچسب‌های فارسی تاریخچهٔ محصول.</summary>
public static class ProductHistoryRules
{
    /// <summary>بخش عمومی.</summary>
    public const string SectionGeneral = "general";
    /// <summary>بخش ترجمه‌ها / محتوای محلی.</summary>
    public const string SectionTranslations = "translations";
    /// <summary>بخش دسته‌بندی.</summary>
    public const string SectionCategory = "category";
    /// <summary>بخش ویژگی‌ها.</summary>
    public const string SectionAttributes = "attributes";
    /// <summary>بخش تنوع‌ها.</summary>
    public const string SectionVariants = "variants";
    /// <summary>بخش رسانه.</summary>
    public const string SectionMedia = "media";
    /// <summary>بخش سئو.</summary>
    public const string SectionSeo = "seo";
    /// <summary>بخش چرخهٔ عمر انتشار.</summary>
    public const string SectionLifecycle = "lifecycle";

    /// <summary>رویداد ایجاد محصول.</summary>
    public const string EventCreated = "product.created";
    /// <summary>رویداد ویرایش اطلاعات اصلی.</summary>
    public const string EventGeneralChanged = "product.general.changed";
    /// <summary>رویداد تغییر دسته.</summary>
    public const string EventCategoryChanged = "product.category.changed";
    /// <summary>رویداد ویرایش محتوای محلی.</summary>
    public const string EventLocalizedChanged = "product.localized.changed";
    /// <summary>رویداد ویرایش ویژگی‌ها.</summary>
    public const string EventAttributesChanged = "product.attributes.changed";
    /// <summary>رویداد به‌روزرسانی تنوع‌ها.</summary>
    public const string EventVariantsChanged = "product.variants.changed";
    /// <summary>رویداد تغییر رسانه.</summary>
    public const string EventMediaChanged = "product.media.changed";
    /// <summary>رویداد ویرایش سئو.</summary>
    public const string EventSeoChanged = "product.seo.changed";
    /// <summary>رویداد انتشار.</summary>
    public const string EventPublished = "product.published";
    /// <summary>رویداد خروج از انتشار.</summary>
    public const string EventUnpublished = "product.unpublished";
    /// <summary>رویداد بایگانی.</summary>
    public const string EventArchived = "product.archived";
    /// <summary>رویداد خروج از بایگانی.</summary>
    public const string EventRestored = "product.restored";

    /// <summary>خلاصهٔ ایجاد.</summary>
    public const string SummaryCreatedFa = "محصول ایجاد شد";
    /// <summary>خلاصهٔ ویرایش عمومی.</summary>
    public const string SummaryGeneralFa = "اطلاعات اصلی محصول ویرایش شد";
    /// <summary>خلاصهٔ تغییر دسته.</summary>
    public const string SummaryCategoryFa = "دسته‌بندی محصول تغییر کرد";
    /// <summary>خلاصهٔ مهاجرت دستهٔ اصلی.</summary>
    public const string SummaryCategoryMigrationFa = "دسته اصلی محصول مهاجرت داده شد";
    /// <summary>خلاصهٔ Unpublish به‌خاطر مهاجرت ناسازگار.</summary>
    public const string SummaryUnpublishedByMigrationFa = "محصول به‌خاطر ناسازگاری مهاجرت دسته اصلی از انتشار خارج شد";

    /// <summary>
    /// خلاصهٔ انسانی مهاجرت دستهٔ اصلی بدون GUID/JSON خام.
    /// </summary>
    public static string FormatCategoryMigrationAfterSummaryFa(
        string newCategoryPath,
        int preservedAttributeCount,
        int newRequiredCount,
        int removedAttributeCount,
        int affectedVariantCount,
        bool unpublishedForSafety)
    {
        var parts = new List<string>
        {
            newCategoryPath.Trim(),
            $"حفظ ویژگی: {preservedAttributeCount}",
            $"جدید الزامی: {newRequiredCount}",
            $"حذف‌شده: {removedAttributeCount}",
            $"تنوع تحت تأثیر: {affectedVariantCount}",
        };
        if (unpublishedForSafety)
        {
            parts.Add("خروج از انتشار برای ایمنی");
        }

        return string.Join(" · ", parts);
    }
    /// <summary>خلاصهٔ محتوای محلی.</summary>
    public const string SummaryLocalizedFa = "محتوای محلی محصول ویرایش شد";
    /// <summary>خلاصهٔ ویژگی‌ها.</summary>
    public const string SummaryAttributesFa = "ویژگی‌های محصول ویرایش شد";
    /// <summary>خلاصهٔ تنوع‌ها.</summary>
    public const string SummaryVariantsFa = "تنوع‌های محصول به‌روزرسانی شد";
    /// <summary>خلاصهٔ رسانه.</summary>
    public const string SummaryMediaFa = "رسانهٔ محصول به‌روزرسانی شد";
    /// <summary>خلاصهٔ تصویر اصلی.</summary>
    public const string SummaryMediaPrimaryFa = "تصویر اصلی تغییر کرد";
    /// <summary>خلاصهٔ سئو.</summary>
    public const string SummarySeoFa = "اطلاعات سئو ویرایش شد";
    /// <summary>خلاصهٔ انتشار.</summary>
    public const string SummaryPublishedFa = "محصول منتشر شد";
    /// <summary>خلاصهٔ Unpublish.</summary>
    public const string SummaryUnpublishedFa = "محصول از انتشار خارج شد";
    /// <summary>خلاصهٔ بایگانی.</summary>
    public const string SummaryArchivedFa = "محصول بایگانی شد";
    /// <summary>خلاصهٔ Restore.</summary>
    public const string SummaryRestoredFa = "محصول از بایگانی خارج شد";

    /// <summary>نام نمایشی بازیگر سیستم.</summary>
    public const string ActorSystemFa = "سیستم";

    /// <summary>برچسب فارسی بخش.</summary>
    public static string SectionLabelFa(string section) =>
        section switch
        {
            SectionGeneral => "عمومی",
            SectionTranslations => "ترجمه‌ها",
            SectionCategory => "دسته‌بندی",
            SectionAttributes => "ویژگی‌ها",
            SectionVariants => "تنوع‌ها",
            SectionMedia => "رسانه",
            SectionSeo => "سئو",
            SectionLifecycle => "انتشار",
            _ => section,
        };
}
