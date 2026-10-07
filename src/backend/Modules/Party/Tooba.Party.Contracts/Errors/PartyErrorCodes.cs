namespace Tooba.Party.Contracts.Errors;

/// <summary>
/// Stable semantic error codes owned by Party. Values are the machine codes emitted by the
/// Party Domain/Application/Infrastructure and mapped by the canonical composed error catalog;
/// they must never be renamed or repurposed.
/// <para>
/// Seller settings wire codes keep Host parity:
/// <c>seller.settings.missing</c> (404), <c>seller.settings.rejected</c> (400).
/// <c>seller.authorization.denied</c> remains Foundation-owned and is not registered here.
/// </para>
/// </summary>
public static class PartyErrorCodes
{
    private static readonly HashSet<string> KnownCodes = new(StringComparer.Ordinal)
    {
        SellerSettingsMissing,
        SellerSettingsRejected,
        OperationRejected,
        DisplayNameRequired,
        DisplayNameLength,
        LegalNameShape,
        DescriptionShape,
        SupportPhoneShape,
        SupportEmailShape,
        AddressLineShape,
        AdminSellersGridRequestRequired,
    };

    /// <summary>
    /// True when <paramref name="code"/> is a stable code declared by this Party catalog.
    /// Used by the module composition seam so Party faults map to <c>Result</c> while codes
    /// owned by another module propagate untouched to the canonical global exception boundary.
    /// </summary>
    public static bool IsKnown(string? code) =>
        !string.IsNullOrWhiteSpace(code) && KnownCodes.Contains(code);

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
