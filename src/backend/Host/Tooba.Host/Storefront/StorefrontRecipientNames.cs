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
