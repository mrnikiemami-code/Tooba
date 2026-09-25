using FluentValidation;
using Tooba.AccessControl.Application.Commands.CreateRole;

namespace Tooba.AccessControl.Application.Validators.Role;

/// <summary>
/// Transport-shape validation for <see cref="CreateRoleCommand"/>.
/// Owner resolution, role-code uniqueness, permission ceiling/delegability and ownership remain
/// owned by Application/Domain. Actor/tenant/trace values come from trusted server context and are
/// intentionally not validated here.
/// </summary>
public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    /// <summary>Registers primitive-shape rules for the command.</summary>
    public CreateRoleCommandValidator()
    {
        AccessControlFluentRules.RequireNonBlankWithin(
            this,
            x => x.Name,
            AccessControlFluentRules.RoleNameMaxLength,
            AccessControlValidationCodes.RoleNameRequired,
            AccessControlValidationCodes.RoleNameLength);
        AccessControlFluentRules.RequireRoleCodeShape(
            this,
            x => x.Code,
            AccessControlValidationCodes.RoleCodeRequired,
            AccessControlValidationCodes.RoleCodeShape);
        AccessControlFluentRules.OptionalWithin(
            this,
            x => x.Description,
            AccessControlFluentRules.RoleDescriptionMaxLength,
            AccessControlValidationCodes.RoleDescriptionLength);
    }
}
