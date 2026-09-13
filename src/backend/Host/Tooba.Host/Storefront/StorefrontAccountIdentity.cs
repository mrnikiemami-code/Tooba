namespace Tooba.Host.Storefront;

/// <summary>
/// هویت نمایشی هدر ویترین: نام پروفایل، وگرنه موبایل Identity. Recipient ارسال منبع هویت نیست.
/// </summary>
internal static class StorefrontAccountIdentity
{
    /// <summary>نام نمایشی پروفایل وقتی موجود باشد؛ وگرنه First+Last؛ بدون حدس از Recipient.</summary>
    public static string? CanonicalName(string? displayName, string? firstName, string? lastName)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName.Trim();
        }

        var first = firstName?.Trim() ?? string.Empty;
        var last = lastName?.Trim() ?? string.Empty;
        if (first.Length > 0 && last.Length > 0)
        {
            return first + " " + last;
        }

        return null;
    }

    /// <summary>برچسب هدر: نام، وگرنه موبایل، وگرنه برچسب عمومی.</summary>
    public static string Label(string? canonicalName, string? mobile, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(canonicalName))
        {
            return canonicalName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(mobile))
        {
            return mobile.Trim();
        }

        return fallback;
    }
}
