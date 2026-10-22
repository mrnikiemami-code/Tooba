namespace Tooba.Shipping.Application.Authoring.Models;

/// <summary>
/// گونهٔ مالک قواعد ارسال که سرور برای همین درخواست مجاز کرده است.
/// </summary>
/// <remarks>
/// این enum معنای «مالک تنظیمات ارسال» است، نه هویت تجاری فروشنده و نه حالت نصب:
/// <list type="bullet">
/// <item><see cref="Store"/> فقط در نصب SingleStore معنا دارد و مالک آن خود فروشگاه است.</item>
/// <item><see cref="Marketplace"/> مالک تنطیمات در سطح نصب Marketplace است و نه یک فروشندهٔ خاص.</item>
/// <item><see cref="Seller"/> یعنی همین actor برای همین SellerPartyId مجاز شده است.</item>
/// </list>
/// صرف <c>Marketplace</c> بودن نصب هرگز یعنی نویسندگی seller-scoped آماده است.
/// </remarks>
public enum ShippingAuthoringOwnerKind
{
    /// <summary>مالکیت تنظیمات ارسال برای فروشگاه نصب SingleStore.</summary>
    Store = 1,

    /// <summary>مالکیت تنظیمات ارسال در سطح نصب Marketplace (بدون فروشنده).</summary>
    Marketplace = 2,

    /// <summary>مالکیت تنظیمات ارسال یک فروشندهٔ مجاز در Marketplace.</summary>
    Seller = 3,
}
