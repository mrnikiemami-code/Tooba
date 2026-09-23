namespace Tooba.Order.Application.Seller;

/// <summary>کدهای پایدار خطای پنل سفارش فروشنده.</summary>
public static class SellerOrderErrors
{
    /// <summary>فروشنده یافت نشد.</summary>
    public const string SellerMissing = "seller.missing";

    /// <summary>دسترسی order.view برای این سفارش رد شد.</summary>
    public const string ViewDenied = "seller.order.view.denied";

    /// <summary>سفارش فروشنده یافت نشد.</summary>
    public const string OrderMissing = "seller.order.missing";

    /// <summary>هویت Actor احراز نشده است.</summary>
    public const string ActorMissing = "seller.actor.missing";

    /// <summary>شناسهٔ فروشنده در زمینهٔ درخواست نامعتبر است.</summary>
    public const string IdentityMissing = "seller.identity.missing";

    /// <summary>مجوز party#view برای فروشنده رد شد.</summary>
    public const string PartyViewDenied = "seller.party.view.denied";
}
