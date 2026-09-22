namespace Tooba.AddressBook.Contracts;

/// <summary>Private customer address snapshot for checkout imaging (no owner id).</summary>
public sealed record CustomerAddressRecord(
    Guid AddressId,
    string RecipientName,
    string ContactMobile,
    string Country,
    string? ProvinceName,
    string CityName,
    string PostalCode,
    string PostalAddress,
    string? BuildingUnit,
    string? Label,
    bool IsDefault,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string FirstName = "",
    string LastName = "");

/// <summary>
/// Stable AddressBook read for Order checkout shipping imaging.
/// Write/list ownership remains AddressBook.Application.
/// </summary>
public interface IAddressBookCheckoutLookup
{
    /// <summary>Returns an address owned by the actor, or null when missing/foreign.</summary>
    Task<CustomerAddressRecord?> GetAsync(Guid actorUserId, Guid addressId, CancellationToken cancellationToken);
}
