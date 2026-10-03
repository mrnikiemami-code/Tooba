using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.OperatorProfile.Contracts.Errors;

/// <summary>کاتالوگ کدهای خطای OperatorProfile.</summary>
public sealed class OperatorProfileErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(OperatorProfileErrorCodes.ProfileRejected, StatusCodes.Status400BadRequest, "Operator profile was rejected."),
        D(OperatorProfileErrorCodes.ActorRequired, StatusCodes.Status400BadRequest, "Operator actor is required."),
        D(OperatorProfileErrorCodes.InvalidDisplayName, StatusCodes.Status400BadRequest, "Display name is invalid."),
        D(OperatorProfileErrorCodes.InvalidFirstName, StatusCodes.Status400BadRequest, "First name is invalid."),
        D(OperatorProfileErrorCodes.InvalidLastName, StatusCodes.Status400BadRequest, "Last name is invalid."),
        D(OperatorProfileErrorCodes.InvalidBio, StatusCodes.Status400BadRequest, "Bio is invalid."),
    ];

    private static ErrorDescriptor D(string code, int status, string fallback) =>
        new(
            Code: code,
            Classification: ErrorClassification.Validation,
            HttpStatus: status,
            LocalizationKey: code,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: fallback);
}
