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
        D(AddressBookErrorCodes.SessionRequired, ErrorClassification.Forbidden, StatusCodes.Status401Unauthorized,
            "A trusted customer session is required."),
    ];

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
