namespace Tooba.Order.Application.Customer;

/// <summary>کدهای پایدار خطای پنل سفارش مشتری.</summary>
public static class CustomerOrderErrors
{
    /// <summary>سفارش متعلق به Actor یافت نشد.</summary>
    public const string Missing = "customer.order.missing";

    /// <summary>نشست مشتری برای مسیر سفارش لازم است.</summary>
    public const string SessionRequired = "customer.session.required";

    /// <summary>تأمین موجودی برای retry پرداخت منقضی ممکن نیست.</summary>
    public const string SupplyUnavailable = "payment.unpaid.supply_unavailable";
}
