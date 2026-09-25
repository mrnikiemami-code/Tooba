namespace Tooba.AccessControl.Application.Validators;

/// <summary>
/// Stable machine-readable codes for AccessControl FluentValidation transport-shape failures.
/// These are transport identity codes only; they are never localized and never classify business state.
/// </summary>
public static class AccessControlValidationCodes
{
    /// <summary>Role name must be supplied.</summary>
    public const string RoleNameRequired = "accesscontrol.validation.role_name_required";

    /// <summary>Role name must stay within the persisted transport length.</summary>
    public const string RoleNameLength = "accesscontrol.validation.role_name_length";

    /// <summary>Role code must be supplied.</summary>
    public const string RoleCodeRequired = "accesscontrol.validation.role_code_required";

    /// <summary>Role code must satisfy the established primitive lexical/length shape.</summary>
    public const string RoleCodeShape = "accesscontrol.validation.role_code_shape";

    /// <summary>Role description must stay within the persisted transport length when supplied.</summary>
    public const string RoleDescriptionLength = "accesscontrol.validation.role_description_length";

    /// <summary>Assignment user identifier must be supplied in the request payload.</summary>
    public const string UserIdRequired = "accesscontrol.validation.user_id_required";

    /// <summary>Assignment role identifier must be supplied in the request payload.</summary>
    public const string RoleIdRequired = "accesscontrol.validation.role_id_required";

    /// <summary>Role permission grant envelope must be supplied.</summary>
    public const string RoleGrantsRequired = "accesscontrol.validation.role_grants_required";

    /// <summary>Seller ceiling entries envelope must be supplied.</summary>
    public const string CeilingEntriesRequired = "accesscontrol.validation.ceiling_entries_required";

    /// <summary>Permission identifier inside a payload envelope must be supplied.</summary>
    public const string PermissionIdRequired = "accesscontrol.validation.permission_id_required";

    /// <summary>Permission identifier must stay within the persisted transport length.</summary>
    public const string PermissionIdLength = "accesscontrol.validation.permission_id_length";
}
