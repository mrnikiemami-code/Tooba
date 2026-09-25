using FluentValidation;
using Tooba.AccessControl.Application.Commands.CloneRole;

namespace Tooba.AccessControl.Application.Validators.Role;

/// <summary>
/// Transport-shape validation for <see cref="CloneRoleCommand"/>.
/// Source-role existence, source-role mutability and clone code uniqueness across the owner scope
/// remain owned by Application/Domain.
/// </summary>
public sealed class CloneRoleCommandValidator : AbstractValidator<CloneRoleCommand>
{
    /// <summary>Registers primitive-shape rules for the command.</summary>
    public CloneRoleCommandValidator()
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
