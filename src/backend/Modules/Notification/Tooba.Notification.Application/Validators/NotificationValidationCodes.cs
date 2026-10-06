namespace Tooba.Notification.Application.Validators;

/// <summary>
/// Stable machine-readable codes for Notification FluentValidation transport-shape failures.
/// These are transport identity codes only; they are never localized and never classify business
/// state. They are deliberately NOT registered as error-catalog descriptors: the canonical
/// <c>ValidationBehavior</c> pipeline maps them through the foundation <c>validation.failed</c>
/// descriptor, matching the certified Localization/Content/Cart/Media/Offer precedent.
/// </summary>
public static class NotificationValidationCodes
{
    /// <summary>Notification identifier must be supplied in the request.</summary>
    public const string NotificationIdRequired = "notification.validation.notification_id_required";

    /// <summary>Customer list page size must be between 1 and 100.</summary>
    public const string CustomerTakeOutOfRange = "notification.validation.customer_take_out_of_range";

    /// <summary>Customer list skip must not be negative.</summary>
    public const string CustomerSkipNegative = "notification.validation.customer_skip_negative";

    /// <summary>Seller list page size must be between 1 and 100.</summary>
    public const string SellerTakeOutOfRange = "notification.validation.seller_take_out_of_range";

    /// <summary>Seller list skip must not be negative.</summary>
    public const string SellerSkipNegative = "notification.validation.seller_skip_negative";
}
