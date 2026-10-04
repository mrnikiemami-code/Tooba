using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.AddressBook.Contracts.Errors;

namespace Tooba.AddressBook.Endpoints.Errors;

/// <summary>
/// کاتالوگ صریح کدهای خطای AddressBook برای مرز HTTP مشتری. نگاشت وضعیت/طبقه‌بندی از همین
/// توصیف‌گرها می‌آید و هیچ endpointی وضعیت را محلی حدس نمی‌زند.
/// </summary>
public sealed class AddressBookErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(AddressBookErrorCodes.AddressMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Address was not found."),
        // customer.session.required is a shared cross-cutting code owned by
        // FoundationErrorCatalogContributor; AddressBook consumes it without re-registering.
        V(AddressBookErrorCodes.ActorRequired, "A trusted customer identity is required."),
        V(AddressBookErrorCodes.RecipientNameRequired, "Recipient name is required."),
        V(AddressBookErrorCodes.ContactMobileInvalid, "Contact mobile is invalid."),
        V(AddressBookErrorCodes.CountryInvalid, "Country is invalid."),
        V(AddressBookErrorCodes.ProvinceNameInvalid, "Province name is invalid."),
        V(AddressBookErrorCodes.CityNameInvalid, "City name is invalid."),
        V(AddressBookErrorCodes.PostalCodeInvalid, "Postal code is invalid."),
        V(AddressBookErrorCodes.PostalAddressInvalid, "Postal address is invalid."),
        V(AddressBookErrorCodes.BuildingUnitInvalid, "Building unit is invalid."),
        V(AddressBookErrorCodes.LabelInvalid, "Address label is invalid."),
        V(AddressBookErrorCodes.RecipientNamePartsInvalid, "Recipient first and last name must be supplied together."),
    ];

    private static ErrorDescriptor V(string code, string fallback) =>
        D(code, ErrorClassification.Validation, StatusCodes.Status400BadRequest, fallback);

    private static ErrorDescriptor D(
        string code,
        ErrorClassification classification,
        int status,
        string fallback) =>
        new(
            Code: code,
            Classification: classification,
            HttpStatus: status,
            LocalizationKey: code,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: fallback);
}
