namespace Tooba.Content.Domain;

/// <summary>سیاست حذف/بایگانی مقاله — مرجع backend برای UI.</summary>
public static class ContentArticleLifecycleRules
{
    /// <summary>حذف دائمی فقط برای پیش‌نویسِ هرگز منتشرنشده.</summary>
    public static bool CanHardDelete(ContentPublicationStatus status) =>
        status == ContentPublicationStatus.Draft;

    /// <summary>بایگانی برای منتشرشده یا پیش‌نویسِ منتشرشدهٔ قبلی (غیر Draft خالص).</summary>
    public static bool CanArchive(ContentPublicationStatus status) =>
        status == ContentPublicationStatus.Published;

    /// <summary>برچسب عملیات تخریب‌پذیر برای UI.</summary>
    public static string DestructiveActionLabel(ContentPublicationStatus status) =>
        CanHardDelete(status) ? "delete" : CanArchive(status) ? "archive" : "none";
}
