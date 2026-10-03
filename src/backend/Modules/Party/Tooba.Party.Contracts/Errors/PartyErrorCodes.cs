namespace Tooba.Party.Contracts.Errors;

/// <summary>
/// Stable Party-owned semantic error codes.
/// <para>
/// Seller settings wire codes keep Host parity:
/// <c>seller.settings.missing</c> (404), <c>seller.settings.rejected</c> (400).
/// <c>seller.authorization.denied</c> remains Foundation-owned and is not registered here.
/// </para>
/// </summary>
public static class PartyErrorCodes
{
    /// <summary>Organization seller profile missing.</summary>
    public const string SellerSettingsMissing = "seller.settings.missing";

    /// <summary>Seller organization profile write rejected.</summary>
    public const string SellerSettingsRejected = "seller.settings.rejected";

    /// <summary>Non-seller Party domain/directory operation rejected.</summary>
    public const string OperationRejected = "party.operation.rejected";

    /// <summary>Display name required.</summary>
    public const string DisplayNameRequired = "seller.settings.validation.display_name_required";

    /// <summary>Display name length exceeded.</summary>
    public const string DisplayNameLength = "seller.settings.validation.display_name_length";

    /// <summary>Legal name shape invalid.</summary>
    public const string LegalNameShape = "seller.settings.validation.legal_name_shape";

    /// <summary>Description shape invalid.</summary>
    public const string DescriptionShape = "seller.settings.validation.description_shape";

    /// <summary>Support phone shape invalid.</summary>
    public const string SupportPhoneShape = "seller.settings.validation.support_phone_shape";

    /// <summary>Support email shape invalid.</summary>
    public const string SupportEmailShape = "seller.settings.validation.support_email_shape";

    /// <summary>Address line shape invalid.</summary>
    public const string AddressLineShape = "seller.settings.validation.address_line_shape";

    /// <summary>Admin sellers grid request envelope required.</summary>
    public const string AdminSellersGridRequestRequired = "party.admin.sellers.validation.grid_request_required";
}
