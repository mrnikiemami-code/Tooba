namespace Tooba.Shipping.Application.Authoring.Models;

/// <summary>
/// نمای فشردهٔ مالک مجازشدهٔ قواعد ارسال. فقط گونهٔ مالک و — تنها برای فروشندهٔ مجاز — همان
/// SellerPartyId را برمی‌گرداند؛ هیچ Tenant، ارجاع اتصال، حالت نصب خام یا جزئیات امنیتی ندارد و
/// هیچ فهرستی از فروشندگان دیگر یا قواعد موجود در آن نیست.
/// </summary>
/// <param name="OwnerKind">گونهٔ مالک: <c>Store</c>، <c>Marketplace</c> یا <c>Seller</c>.</param>
/// <param name="SellerPartyId">
/// شناسهٔ Party فروشنده فقط وقتی <paramref name="OwnerKind"/> برابر <c>Seller</c> است؛ در غیر این
/// صورت <c>null</c>. این مقدار یعنی «مجوز خواندن برای همین فروشنده»، نه مالکیت ردیف‌های موجود.
/// </param>
public sealed record ShippingAuthoringOwnerView(
    string OwnerKind,
    Guid? SellerPartyId);
