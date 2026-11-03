namespace Tooba.Order.Endpoints.Errors;

/// <summary>کدهای پایدار خطای سطح Order که Host admin adapter و لایه‌های presentation مصرف می‌کنند.</summary>
public static class OrderErrorCodes
{
    /// <summary>رد صریح مجوز عملیات admin سفارش (403).</summary>
    public const string OperationDenied = "order.operation.denied";

    /// <summary>سرویس مجوز در دسترس نیست؛ مسیر admin باید fail-closed بماند (503).</summary>
    public const string AuthorizationUnavailable = "order.authorization.unavailable";

    public const string AcceptedGroupRequired = "order.shipment.accepted_group_required";
    public const string AcceptedGroupUnavailable = "order.shipment.accepted_group_unavailable";
    public const string AcceptedGroupForeign = "order.shipment.accepted_group_foreign";
    public const string AcceptedGroupMethodMismatch = "order.shipment.accepted_group_method_mismatch";
    public const string AcceptedGroupLineForeign = "order.shipment.accepted_group_line_foreign";
    public const string AcceptedGroupQuantityExcess = "order.shipment.accepted_group_quantity_excess";
    public const string AcceptedGroupNotExecutable = "order.shipment.accepted_group_not_executable";
}
