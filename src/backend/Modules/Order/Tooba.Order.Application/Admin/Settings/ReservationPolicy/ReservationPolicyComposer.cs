using Tooba.Order.Application.Admin.Settings.ReservationPolicy.Models;
using Tooba.Order.Application.ReservationCycle.Contracts;

namespace Tooba.Order.Application.Admin.Settings.ReservationPolicy;

/// <summary>نقشهٔ پیش‌نمایش backend به ویوی Admin (بدون persistence).</summary>
public static class ReservationPolicyComposer
{
    /// <summary>حداقل دقیقه.</summary>
    public const int MinMinutes = 1;

    /// <summary>حداکثر دقیقه (۳۰ روز).</summary>
    public const int MaxMinutes = 24 * 60 * 30;

    /// <summary>حداقل سقف چرخه.</summary>
    public const int MinCycles = 1;

    /// <summary>حداکثر سقف چرخه.</summary>
    public const int MaxCycles = 20;

    /// <summary>آستانهٔ هشدار غیرمسدودکننده برای رزرو خیلی طولانی (۲۴ ساعت).</summary>
    public const int LongHoldWarningMinutes = 24 * 60;

    /// <summary>ویوی فروشگاه.</summary>
    public static ReservationPolicyEditorView ForStore(ReservationPolicyPreview preview, bool canEdit) =>
        Map(
            "store",
            null,
            preview.AfterStore,
            preview.StoreInitialOverride,
            preview.StoreRetryOverride,
            preview.StoreMaxOverride,
            preview.Platform,
            canEdit,
            inheritFa: "ارث‌بری از سامانه",
            inheritEn: "Inherit from platform");

    /// <summary>ویوی دسته.</summary>
    public static ReservationPolicyEditorView ForCategory(
        Guid categoryId,
        ReservationPolicyPreview preview,
        bool canEdit) =>
        Map(
            "category",
            categoryId,
            preview.AfterCategory,
            preview.CategoryInitialOverride,
            preview.CategoryRetryOverride,
            preview.CategoryMaxOverride,
            preview.AfterStore,
            canEdit,
            inheritFa: "ارث‌بری از فروشگاه",
            inheritEn: "Inherit from store");

    /// <summary>ویوی پیشنهاد.</summary>
    public static ReservationPolicyEditorView ForOffer(
        Guid offerId,
        ReservationPolicyPreview preview,
        bool canEdit) =>
        Map(
            "offer",
            offerId,
            preview.AfterOffer,
            preview.OfferInitialOverride,
            preview.OfferRetryOverride,
            preview.OfferMaxOverride,
            preview.AfterCategory,
            canEdit,
            inheritFa: "ارث‌بری از دسته یا فروشگاه",
            inheritEn: "Inherit from category or store");

    private static ReservationPolicyEditorView Map(
        string scope,
        Guid? scopeId,
        ReservationPolicyLayerPreview effective,
        int? initialOverride,
        int? retryOverride,
        int? maxOverride,
        ReservationPolicyLayerPreview inherited,
        bool canEdit,
        string inheritFa,
        string inheritEn)
    {
        var stricter = scope == "offer"
            && (effective.InitialHoldMinutes < inherited.InitialHoldMinutes
                || effective.RetryHoldMinutes < inherited.RetryHoldMinutes
                || effective.MaxCycles < inherited.MaxCycles);
        var longHold = scope == "offer"
            && ((initialOverride ?? 0) >= LongHoldWarningMinutes
                || (retryOverride ?? 0) >= LongHoldWarningMinutes);
        return new ReservationPolicyEditorView(
            scope,
            scopeId,
            Field(
                initialOverride,
                effective.InitialHoldMinutes,
                effective.InitialSource,
                "مدت رزرو اولیه",
                "Initial reservation hold",
                "از ورود سفارش به مرحله پرداخت شروع می‌شود. شکست پرداخت داخل رزرو فعال، تایمر را از نو شروع نمی‌کند.",
                "Starts when the order enters payment. A failed payment inside an active reservation does not reset the timer."),
            Field(
                retryOverride,
                effective.RetryHoldMinutes,
                effective.RetrySource,
                "مدت رزرو مجدد",
                "Retry reservation hold",
                "فقط پس از پایان رزرو قبلی و بازپس‌گیری موفق موجودی اعمال می‌شود.",
                "Applies only after the previous reservation ended and stock was reacquired."),
            Field(
                maxOverride,
                effective.MaxCycles,
                effective.MaxSource,
                "حداکثر دفعات رزرو",
                "Maximum reservation cycles",
                "شامل رزرو اولیه است. عدد ۱ یعنی فقط همان چرخه اول.",
                "Includes the initial reservation. 1 means only the first cycle."),
            canEdit,
            false,
            stricter,
            longHold,
            inheritFa,
            inheritEn,
            "در سفارش‌های چندکالایی، سخت‌گیرانه‌ترین سیاست اقلام اعمال می‌شود.",
            "For multi-item orders, the strictest line policy is applied.",
            "برای فروش‌های پرترافیک می‌توانید زمان رزرو این پیشنهاد را کوتاه‌تر کنید تا موجودی برای مدت طولانی قفل نشود.",
            "For high-traffic sales you can shorten this offer hold so inventory is not locked for long.",
            "این پیشنهاد از سیاست رزرو کوتاه‌تری نسبت به تنظیمات فروشگاه استفاده می‌کند.",
            "This offer uses a shorter reservation policy than the inherited settings.",
            "زمان رزرو طولانی می‌تواند در فروش‌های پرترافیک باعث قفل‌شدن موجودی شود.",
            "A long reservation hold can lock inventory during high-traffic sales.");
    }

    private static ReservationPolicyFieldView Field(
        int? overrideValue,
        int effective,
        string source,
        string labelFa,
        string labelEn,
        string helperFa,
        string helperEn) =>
        new(
            overrideValue,
            effective,
            source,
            overrideValue is not null,
            SourceFa(source),
            SourceEn(source),
            labelFa,
            labelEn,
            helperFa,
            helperEn);

    private static string SourceFa(string source) => source switch
    {
        "store" => "فروشگاه",
        "category" => "دسته",
        "offer" => "پیشنهاد",
        _ => "سامانه",
    };

    private static string SourceEn(string source) => source switch
    {
        "store" => "Store",
        "category" => "Category",
        "offer" => "Offer",
        _ => "Platform",
    };
}
