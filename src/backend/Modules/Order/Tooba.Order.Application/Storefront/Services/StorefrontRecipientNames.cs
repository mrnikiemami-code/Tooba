using Tooba.Order.Application.Storefront.Models;

namespace Tooba.Order.Application.Storefront.Services;

/// <summary>نام و نام خانوادگی را جدا نگه می‌دارد؛ برای رکورد قدیمی فقط RecipientName را بدون حدس شکستن برمی‌گرداند.</summary>
public static class StorefrontRecipientNames
{
    public static (string First, string Last, string Recipient) Resolve(string? firstName, string? lastName, string? recipientName)
    {
        var first = firstName?.Trim() ?? string.Empty;
        var last = lastName?.Trim() ?? string.Empty;
        var recipient = recipientName?.Trim() ?? string.Empty;
        if (first.Length > 0 && last.Length > 0)
        {
            return (first, last, $"{first} {last}");
        }

        return (first, last, recipient);
    }

    public static string Display(string? firstName, string? lastName, string? recipientName)
    {
        var resolved = Resolve(firstName, lastName, recipientName);
        return resolved.Recipient;
    }

    public static string DisplayOrFallback(string? firstName, string? lastName, string? recipientName, string fallback = "مشتری توبا")
    {
        var display = Display(firstName, lastName, recipientName);
        return display.Length == 0 ? fallback : display;
    }

    public static (string First, string Last, string Recipient) ResolveExplicitOverLegacy(
        string? explicitFirst,
        string? explicitLast,
        string? explicitRecipient,
        string? legacyFirst,
        string? legacyLast,
        string? legacyRecipient)
    {
        var explicitNames = Resolve(explicitFirst, explicitLast, explicitRecipient);
        if (explicitNames.First.Length > 0 && explicitNames.Last.Length > 0)
        {
            return explicitNames;
        }

        return Resolve(legacyFirst, legacyLast, legacyRecipient);
    }

    public static void EnsureNewAddressNames(string first, string last)
    {
        if (first.Length == 0)
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.ShippingFirstNameRequired);
        }

        if (last.Length == 0)
        {
            throw new StorefrontOrderException(StorefrontOrderErrors.ShippingLastNameRequired);
        }
    }
}

/// <summary>کاتالوگ استان/شهر ایران برای فرم ارسال فروشگاهی.</summary>
public static class StorefrontIranGeography
{
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
