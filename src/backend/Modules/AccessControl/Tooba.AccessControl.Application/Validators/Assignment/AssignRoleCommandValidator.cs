using FluentValidation;
using Tooba.AccessControl.Application.Commands.AssignRole;

namespace Tooba.AccessControl.Application.Validators.Assignment;

/// <summary>
/// Transport-shape validation for <see cref="AssignRoleCommand"/>.
/// Role existence, archived state, assignment uniqueness, ownership and actor authorization remain
/// owned by Application/Domain. Actor/tenant/trace values come from trusted server context.
/// </summary>
public sealed class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    /// <summary>Registers primitive-shape rules for the command.</summary>
    public AssignRoleCommandValidator()
    {
        AccessControlFluentRules.RequireId(
            this,
            x => x.UserId,
            AccessControlValidationCodes.UserIdRequired);
        AccessControlFluentRules.RequireId(
            this,
            x => x.RoleId,
            AccessControlValidationCodes.RoleIdRequired);
    }
}
