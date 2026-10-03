using Tooba.Order.Application.Storefront.Models;

namespace Tooba.Order.Application.Storefront.Geography;

/// <summary>کاتالوگ استان/شهر ایران برای فرم ارسال فروشگاهی.</summary>
public static class StorefrontIranGeography
{
    /// <summary>فهرست استان‌های ایران همراه شهرهای هر استان.</summary>
    public static IReadOnlyList<StorefrontProvinceOption> Provinces { get; } =
    [
        new("tehran", "تهران", ["تهران", "ری", "شهریار", "اسلامشهر", "ملارد", "پاکدشت"]),
        new("alborz", "البرز", ["کرج", "فردیس", "نظرآباد", "ساوجبلاغ"]),
        new("isfahan", "اصفهان", ["اصفهان", "کاشان", "خمینی‌شهر", "شاهین‌شهر", "نجف‌آباد"]),
        new("razavi_khorasan", "خراسان رضوی", ["مشهد", "نیشابور", "سبزوار", "تربت حیدریه"]),
        new("fars", "فارس", ["شیراز", "مرودشت", "جهرم", "فسا", "کازرون"]),
        new("east_azerbaijan", "آذربایجان شرقی", ["تبریز", "مرند", "مراغه", "اهر"]),
        new("khuzestan", "خوزستان", ["اهواز", "آبادان", "دزفول", "بندر ماهشهر"]),
        new("mazandaran", "مازندران", ["ساری", "بابل", "آمل", "قائم‌شهر"]),
        new("gilan", "گیلان", ["رشت", "بندرانزلی", "لاهیجان", "رودسر"]),
        new("qom", "قم", ["قم"]),
        new("yazd", "یزد", ["یزد", "اردکان", "میبد"]),
        new("kerman", "کرمان", ["کرمان", "رفسنجان", "سیرجان"]),
    ];
}
