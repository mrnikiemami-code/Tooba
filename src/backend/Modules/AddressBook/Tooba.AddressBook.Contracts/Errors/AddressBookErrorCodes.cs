namespace Tooba.AddressBook.Contracts.Errors;

/// <summary>Stable semantic error codes owned by AddressBook.</summary>
public static class AddressBookErrorCodes
{
    /// <summary>The requested address does not exist for the acting customer.</summary>
    public const string AddressMissing = "customer.address.missing";

    /// <summary>A trusted customer session is required.</summary>
    public const string SessionRequired = "customer.session.required";

    /// <summary>The acting customer identity supplied by the server trust boundary is not usable.</summary>
    public const string ActorRequired = "customer.address.actor_required";

    /// <summary>Recipient name is required after trimming.</summary>
    public const string RecipientNameRequired = "customer.address.recipient_name_required";

    /// <summary>Contact mobile is required and must satisfy the bounded length rule.</summary>
    public const string ContactMobileInvalid = "customer.address.contact_mobile_invalid";

    /// <summary>Country code is required and must satisfy the bounded length rule.</summary>
    public const string CountryInvalid = "customer.address.country_invalid";

    /// <summary>Province name exceeds the bounded length rule.</summary>
    public const string ProvinceNameInvalid = "customer.address.province_name_invalid";

    /// <summary>City name is required and must satisfy the bounded length rule.</summary>
    public const string CityNameInvalid = "customer.address.city_name_invalid";

    /// <summary>Postal code is required and must satisfy the bounded length rule.</summary>
    public const string PostalCodeInvalid = "customer.address.postal_code_invalid";

    /// <summary>Postal address is required and must satisfy the bounded length rule.</summary>
    public const string PostalAddressInvalid = "customer.address.postal_address_invalid";

    /// <summary>Building unit exceeds the bounded length rule.</summary>
    public const string BuildingUnitInvalid = "customer.address.building_unit_invalid";

    /// <summary>Address label exceeds the bounded length rule.</summary>
    public const string LabelInvalid = "customer.address.label_invalid";

    /// <summary>First and last name must be supplied together when either is supplied.</summary>
    public const string RecipientNamePartsInvalid = "customer.address.recipient_name_parts_invalid";
}
