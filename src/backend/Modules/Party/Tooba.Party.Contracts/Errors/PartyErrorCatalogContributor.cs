using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.Party.Contracts.Errors;

/// <summary>
/// Explicit Party-owned error catalog for seller settings and Party operations.
/// <para>
/// <c>seller.authorization.denied</c> is cross-cutting and owned by
/// <c>FoundationErrorCatalogContributor</c>; Party does not re-register it.
/// </para>
/// </summary>
public sealed class PartyErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(PartyErrorCodes.SellerSettingsMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Not Found"),
        D(PartyErrorCodes.SellerSettingsRejected, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Rejected"),
        D(PartyErrorCodes.OperationRejected, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Rejected"),
        D(PartyErrorCodes.DisplayNameRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Display name is required."),
        D(PartyErrorCodes.DisplayNameLength, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Display name is too long."),
        D(PartyErrorCodes.LegalNameShape, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Legal name is invalid."),
        D(PartyErrorCodes.DescriptionShape, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Description is invalid."),
        D(PartyErrorCodes.SupportPhoneShape, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Support phone is invalid."),
        D(PartyErrorCodes.SupportEmailShape, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Support email is invalid."),
        D(PartyErrorCodes.AddressLineShape, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Address line is invalid."),
        D(PartyErrorCodes.AdminSellersGridRequestRequired, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Grid request is required."),
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
