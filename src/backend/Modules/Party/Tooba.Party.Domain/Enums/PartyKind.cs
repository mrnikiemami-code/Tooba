namespace Tooba.Party.Domain.Enums;

/// <summary>
/// گونهٔ Party. شخص کسب‌وکار است نه UserAccount؛ سازمان هم ردیف User نیست.
/// Seller/Agency/Customer اینجا enum نهایی مجوز یا نقش ورود نیستند.
/// </summary>
public enum PartyKind
{
    /// <summary>
    /// موجودیت انسانی کسب‌وکار. اعتبار ورود Identity را کپی نمی‌کند.
    /// </summary>
    Person = 1,

    /// <summary>
    /// موجودیت سازمانی. قابلیت‌های تجاری بعدی با کدهای گسترش‌پذیر ثبت می‌شوند نه با SellerOnly.
    /// </summary>
    Organization = 2,
}
