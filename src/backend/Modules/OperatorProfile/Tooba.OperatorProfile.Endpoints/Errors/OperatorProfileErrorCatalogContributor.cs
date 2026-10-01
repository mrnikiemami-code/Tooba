using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.OperatorProfile.Contracts.Errors;

namespace Tooba.OperatorProfile.Endpoints.Errors;

/// <summary>کاتالوگ کدهای خطای OperatorProfile.</summary>
public sealed class OperatorProfileErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        new(
            Code: OperatorProfileErrorCodes.ProfileRejected,
            Classification: ErrorClassification.Validation,
            HttpStatus: StatusCodes.Status400BadRequest,
            LocalizationKey: OperatorProfileErrorCodes.ProfileRejected,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: "Operator profile was rejected."),
    ];
}
