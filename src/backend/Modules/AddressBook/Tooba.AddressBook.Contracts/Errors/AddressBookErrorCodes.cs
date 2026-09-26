namespace Tooba.AddressBook.Contracts.Errors;

/// <summary>Stable semantic error codes owned by AddressBook.</summary>
public static class AddressBookErrorCodes
{
    /// <summary>The requested address does not exist for the acting customer.</summary>
    public const string AddressMissing = "customer.address.missing";

    /// <summary>A trusted customer session is required.</summary>
    public const string SessionRequired = "customer.session.required";
}
