namespace Tooba.Host.Security.Seller;

/// <summary>
/// کدهای پایدار امنیت فروشنده که مالک آن مرز پلتفرم امنیت Host است.
/// فقط کدهای مشترک این مرز اینجا هستند؛ هیچ کد کسب‌وکار هیچ ماژولی تکرار نمی‌شود.
/// </summary>
internal static class SellerSecurityErrorCodes
{
    /// <summary>Actor احرازنشده است.</summary>
    public const string ActorMissing = "seller.actor.missing";

    /// <summary>شناسهٔ فروشنده در زمینهٔ درخواست نامعتبر است.</summary>
    public const string IdentityMissing = "seller.identity.missing";

    /// <summary>مجوز دسترسی به فروشنده رد شد.</summary>
    public const string AuthorizationDenied = "seller.authorization.denied";

    /// <summary>سرویس مجوز در دسترس نیست.</summary>
    public const string AuthorizationUnavailable = "seller.authorization.unavailable";
}
