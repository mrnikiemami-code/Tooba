namespace Tooba.AccessControl.Contracts.Errors;

/// <summary>Stable AccessControl machine error codes (descriptor ownership in Endpoints catalog).</summary>
public static class AccessControlErrorCodes
{
    public const string RoleCodeConflict = "access.role.code_conflict";
    public const string RoleSystemImmutable = "access.role.system_immutable";
    public const string RoleArchived = "access.role.archived";
    public const string RoleNotFound = "access.role.not_found";
    public const string UserInvalid = "access.user.invalid";
    public const string AssignmentExists = "access.assignment.exists";
    public const string AssignmentNotFound = "access.assignment.not_found";
    public const string CeilingNotDelegable = "access.ceiling.not_delegable";
    public const string ScopeUnsupported = "access.scope.unsupported";
    public const string ScopeUnknownResource = "access.scope.unknown_resource";
    public const string OwnerInvalid = "access.owner.invalid";
    public const string EscalationPlatformPermission = "access.escalation.platform_permission";
    public const string EscalationCeiling = "access.escalation.ceiling";
    public const string ValidationText = "access.validation.text";
    public const string ValidationCode = "access.validation.code";
    public const string PermissionUnknown = "access.permission.unknown";
    public const string AuthorizationUnavailable = "access.authorization.unavailable";
    public const string CapabilityDenied = "access.capability.denied";
    public const string SellerDevUnavailable = "seller.dev.unavailable";
    public const string SellerDevNotReady = "seller.dev.not-ready";
}
