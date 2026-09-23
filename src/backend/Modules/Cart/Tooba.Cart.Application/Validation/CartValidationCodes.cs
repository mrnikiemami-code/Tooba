namespace Tooba.Cart.Application.Validation;

/// <summary>Stable machine codes for Cart FluentValidation transport input (never localized identity).</summary>
public static class CartValidationCodes
{
    /// <summary>Cart identifier must be provided.</summary>
    public const string CartIdRequired = "cart.validation.cart_id_required";

    /// <summary>Offer identifier must be provided.</summary>
    public const string OfferIdRequired = "cart.validation.offer_id_required";

    /// <summary>Cart line identifier must be provided.</summary>
    public const string LineIdRequired = "cart.validation.line_id_required";

    /// <summary>Expected cart version must be non-negative.</summary>
    public const string ExpectedVersionMin = "cart.validation.expected_version_min";
}
