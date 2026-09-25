using FluentValidation;
using Tooba.AccessControl.Application.Commands.SetRolePermissions;
using Tooba.AccessControl.Application.Models;

namespace Tooba.AccessControl.Application.Validators.Permissions;

/// <summary>
/// Transport-shape validation for <see cref="SetRolePermissionsCommand"/>.
/// Permission catalog membership, delegability, ceiling and category-existence compatibility are
/// deliberately NOT validated here; those remain Application/Domain owned business rules.
/// </summary>
public sealed class SetRolePermissionsCommandValidator : AbstractValidator<SetRolePermissionsCommand>
{
    /// <summary>Registers primitive-shape rules for the command.</summary>
    public SetRolePermissionsCommandValidator()
    {
        AccessControlFluentRules.RequireEnvelope(
            this,
            x => x.Grants,
            AccessControlValidationCodes.RoleGrantsRequired);
        AccessControlFluentRules.RequireId(
            this,
            x => x.RoleId,
            AccessControlValidationCodes.RoleIdRequired);
        RuleForEach(x => x.Grants)
            .Must(grant => AccessControlFluentRules.IsPermissionIdNonBlank(grant.PermissionId))
            .WithErrorCode(AccessControlValidationCodes.PermissionIdRequired);
        RuleForEach(x => x.Grants)
            .Must(grant => AccessControlFluentRules.IsPermissionIdWithin(
                grant.PermissionId,
                AccessControlFluentRules.PermissionIdMaxLength))
            .WithErrorCode(AccessControlValidationCodes.PermissionIdLength);
    }
}
