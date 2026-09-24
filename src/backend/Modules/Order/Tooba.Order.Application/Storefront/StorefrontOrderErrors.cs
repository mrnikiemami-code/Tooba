namespace Tooba.Order.Application.Storefront;

/// <summary>Typed storefront Order fault — code is stable; message is diagnostic only.</summary>
public sealed class StorefrontOrderException : InvalidOperationException
{
    public string Code { get; }

    public StorefrontOrderException(string code, string? diagnostic = null)
        : base(string.IsNullOrWhiteSpace(diagnostic) ? code : $"{code}: {diagnostic}")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code.Trim();
    }
}

/// <summary>Stable storefront Order error codes (catalog + handler mapping).</summary>
public static class StorefrontOrderErrors
{
    public const string CheckoutMissing = "checkout.missing";
    public const string CheckoutAccessDenied = "checkout.access.denied";
    public const string CheckoutAddressForbidden = "checkout.address.forbidden";
    public const string CheckoutAuthenticationRequired = "checkout.authentication_required";
    public const string CheckoutOpenUnpaidLimit = "checkout.open_unpaid_limit_reached";
    public const string CheckoutReservationCommitLimit = "checkout.reservation_commit_limit_reached";
    public const string CheckoutInventoryUnavailable = "checkout.inventory.unavailable";
    public const string CheckoutPriceChanged = "checkout.price.changed";
    public const string CheckoutTaxUnavailable = "checkout.tax.unavailable";
    public const string CheckoutCartExpired = "checkout.cart.expired";
    public const string CheckoutShippingIncomplete = "checkout.shipping.incomplete";
    public const string CheckoutCartEmpty = "checkout.cart.empty";
    public const string CheckoutVersionConflict = "checkout.version.conflict";
    public const string CheckoutRejected = "checkout.rejected";
    public const string CheckoutCartMissing = "checkout.cart.missing";
    public const string ShippingAddressForbidden = "shipping.address.forbidden";
    public const string ShippingCartMissing = "shipping.cart.missing";
    public const string ShippingCartEmpty = "shipping.cart.empty";
    public const string ShippingCartStale = "shipping.cart.stale";
    public const string ShippingCartForbidden = "shipping.cart.forbidden";
    public const string ShippingMethodUnavailable = "shipping.method.unavailable";
    public const string ShippingDeliveryTooEarly = "shipping.delivery.too_early";
    public const string ShippingDeliverySlotUnavailable = "shipping.delivery.slot_unavailable";
    public const string ShippingDeliveryInvalid = "shipping.delivery.invalid";
    public const string ShippingNoteTooLong = "shipping.note.too_long";
    public const string ShippingSelectionRequired = "shipping.selection.required";
    public const string ShippingFirstNameRequired = "shipping.firstname.required";
    public const string ShippingLastNameRequired = "shipping.lastname.required";
    public const string ShippingRejected = "shipping.rejected";
    public const string OrderCancelUnpaidOnly = "order.cancel.unpaid_only";
    public const string OrderCancelForbidden = "order.cancel.forbidden";
    public const string PendingHideActiveHold = "pending.hide.active_hold";
    public const string PaymentMissing = "payment.missing";
    public const string PaymentRejected = "payment.rejected";

    /// <summary>
    /// Until the dedicated Order multi-currency wave, the Order/Checkout boundary accepts only a
    /// cart whose lines carry exactly one distinct non-empty quoted currency. Cart.DefaultCurrency
    /// is never transaction authority; a mixed or currency-less cart fails closed here.
    /// </summary>
    public const string CheckoutMultiCurrencyNotSupported = "checkout.multicurrency.not_supported";
}
