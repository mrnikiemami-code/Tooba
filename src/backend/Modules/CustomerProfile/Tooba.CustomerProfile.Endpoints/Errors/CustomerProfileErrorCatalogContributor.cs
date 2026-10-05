using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.CustomerProfile.Contracts.Errors;

namespace Tooba.CustomerProfile.Endpoints.Errors;

/// <summary>
/// Explicit CustomerProfile error catalog for the customer-account HTTP boundary. Status and
/// classification come from these descriptors, so no endpoint guesses a status locally.
/// </summary>
public sealed class CustomerProfileErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        // customer.session.required is a shared cross-cutting code owned by
        // FoundationErrorCatalogContributor; CustomerProfile consumes it without re-registering.
        V(CustomerProfileErrorCodes.ActorRequired, "A trusted customer identity is required."),
        V(CustomerProfileErrorCodes.DisplayNameInvalid, "Display name is invalid."),
        V(CustomerProfileErrorCodes.FirstNameInvalid, "First name is invalid."),
        V(CustomerProfileErrorCodes.LastNameInvalid, "Last name is invalid."),
        V(CustomerProfileErrorCodes.BirthDateInvalid, "Birth date is invalid."),
        V(CustomerProfileErrorCodes.BioInvalid, "Bio is invalid."),
    ];

    private static ErrorDescriptor V(string code, string fallback) =>
        new(
            Code: code,
            Classification: ErrorClassification.Validation,
            HttpStatus: StatusCodes.Status400BadRequest,
            LocalizationKey: code,
            Severity: ErrorSeverity.Warning,
            SafeTitleFallback: fallback);
}
