#pragma warning disable CS1591
namespace Tooba.AddressBook.Application.Models;

/// <summary>ورودی نوشتن دفترچه؛ OwnerUserId ندارد و هویت از Host می‌آید.</summary>
public sealed record CustomerAddressWrite(
    string RecipientName,
    string ContactMobile,
    string? Country,
    string? ProvinceName,
    string CityName,
    string PostalCode,
    string PostalAddress,
    string? BuildingUnit,
    string? Label,
    bool IsDefault,
    string FirstName = "",
    string LastName = "");

#pragma warning restore CS1591
