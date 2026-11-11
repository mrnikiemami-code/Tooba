namespace Tooba.Order.Application.Admin.Detail.Errors;

/// <summary>
/// کدهای پایدار خطای خواندن جزئیات سفارش در پنل مدیر.
/// کد اینجا (Application) تعریف می‌شود تا هم لایهٔ Application و هم catalog/منابع presentation
/// از یک منبع حقیقت بخوانند؛ هندر نباید رشتهٔ خطای خودش را بسازد.
/// </summary>
public static class AdminOrderDetailErrors
{
    /// <summary>Checkout درخواستی وجود ندارد — منبع موجود نیست (۴۰۴)، نه درخواست نامعتبر (۴۰۰).</summary>
    public const string Missing = "admin.order.missing";
}
