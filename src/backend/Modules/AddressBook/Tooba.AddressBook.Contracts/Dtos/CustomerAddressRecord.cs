namespace Tooba.AddressBook.Contracts.Dtos;

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
