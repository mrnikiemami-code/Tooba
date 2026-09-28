namespace Tooba.Order.Endpoints.Errors;

/// <summary>کدهای پایدار خطای سطح Order که Host admin adapter و لایه‌های presentation مصرف می‌کنند.</summary>
public static class OrderErrorCodes
{
    /// <summary>رد صریح مجوز عملیات admin سفارش (403).</summary>
    public const string OperationDenied = "order.operation.denied";

    /// <summary>سرویس مجوز در دسترس نیست؛ مسیر admin باید fail-closed بماند (503).</summary>
    public const string AuthorizationUnavailable = "order.authorization.unavailable";
}
