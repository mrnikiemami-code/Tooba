using Microsoft.AspNetCore.Http;
using Tooba.AccessControl.Contracts.Errors;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.AccessControl.Endpoints.Errors;

/// <summary>Canonical AccessControl error descriptors (HTTP status owned here — no code.Contains heuristics).</summary>
public sealed class AccessControlErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(AccessControlErrorCodes.RoleNotFound, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Role was not found."),
        D(AccessControlErrorCodes.AssignmentNotFound, ErrorClassification.NotFound, StatusCodes.Status404NotFound, "Assignment was not found."),
        D(AccessControlErrorCodes.RoleCodeConflict, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Role code conflicts."),
        D(AccessControlErrorCodes.AssignmentExists, ErrorClassification.Conflict, StatusCodes.Status409Conflict, "Assignment already exists."),
        D(AccessControlErrorCodes.RoleSystemImmutable, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "System role is immutable."),
        D(AccessControlErrorCodes.RoleArchived, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Archived role cannot be assigned."),
        D(AccessControlErrorCodes.UserInvalid, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "User is invalid."),
        D(AccessControlErrorCodes.CeilingNotDelegable, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Permission is not ceiling-delegable."),
        D(AccessControlErrorCodes.ScopeUnsupported, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Scope is unsupported for permission."),
        D(AccessControlErrorCodes.ScopeUnknownResource, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Scope resource was not found."),
        D(AccessControlErrorCodes.OwnerInvalid, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Owner scope is invalid."),
        D(AccessControlErrorCodes.ValidationText, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Text is invalid."),
        D(AccessControlErrorCodes.ValidationCode, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Role code is invalid."),
        D(AccessControlErrorCodes.PermissionUnknown, ErrorClassification.Validation, StatusCodes.Status400BadRequest, "Permission is unknown."),
        D(AccessControlErrorCodes.EscalationPlatformPermission, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden, "Platform permission escalation denied."),
        D(AccessControlErrorCodes.EscalationCeiling, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden, "Ceiling escalation denied."),
        D(AccessControlErrorCodes.AuthorizationUnavailable, ErrorClassification.Platform, StatusCodes.Status503ServiceUnavailable, "Authorization service unavailable."),
        D(AccessControlErrorCodes.CapabilityDenied, ErrorClassification.Forbidden, StatusCodes.Status403Forbidden, "Capability denied."),
    ];

    private static ErrorDescriptor D(string code, ErrorClassification classification, int status, string fallback) =>
        new(code, classification, status, code, ErrorSeverity.Warning, fallback);
}
