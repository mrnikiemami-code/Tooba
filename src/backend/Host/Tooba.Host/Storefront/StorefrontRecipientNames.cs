namespace Tooba.Host.Storefront;

/// <summary>نام و نام خانوادگی را جدا نگه می‌دارد؛ برای رکورد قدیمی فقط RecipientName را بدون حدس شکستن برمی‌گرداند.</summary>
internal static class StorefrontRecipientNames
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

    /// <summary>نمایش گیرنده: First+Last اگر هر دو موجود باشند؛ وگرنه RecipientName قدیمی بدون شکستن حدسی.</summary>
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

    /// <summary>اگر فرم First/Last صریح دارد همان برنده است؛ وگرنه تصویر دفترچه/legacy.</summary>
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
            throw new InvalidOperationException("shipping.firstname.required");
        }

        if (last.Length == 0)
        {
            throw new InvalidOperationException("shipping.lastname.required");
        }
    }
}
