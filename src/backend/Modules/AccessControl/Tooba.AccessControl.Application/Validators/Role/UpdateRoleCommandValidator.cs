using FluentValidation;
using Tooba.AccessControl.Application.Commands.UpdateRole;

namespace Tooba.AccessControl.Application.Validators.Role;

/// <summary>
/// Transport-shape validation for <see cref="UpdateRoleCommand"/>.
/// Role existence and role mutability remain owned by Application/Domain; this validator only checks
/// primitive name/description shape supplied by the caller.
/// </summary>
public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    /// <summary>Registers primitive-shape rules for the command.</summary>
    public UpdateRoleCommandValidator()
    {
        AccessControlFluentRules.RequireNonBlankWithin(
            this,
            x => x.Name,
            AccessControlFluentRules.RoleNameMaxLength,
            AccessControlValidationCodes.RoleNameRequired,
            AccessControlValidationCodes.RoleNameLength);
        AccessControlFluentRules.OptionalWithin(
            this,
            x => x.Description,
            AccessControlFluentRules.RoleDescriptionMaxLength,
            AccessControlValidationCodes.RoleDescriptionLength);
    }
}
