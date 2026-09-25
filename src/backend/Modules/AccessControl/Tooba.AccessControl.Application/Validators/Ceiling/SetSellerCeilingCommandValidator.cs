using FluentValidation;
using Tooba.AccessControl.Application.Commands.SetSellerCeiling;

namespace Tooba.AccessControl.Application.Validators.Ceiling;

/// <summary>
/// Transport-shape validation for <see cref="SetSellerCeilingCommand"/>.
/// Permission catalog membership, delegability, category existence and ceiling semantics remain
/// owned by Application/Domain; only the envelope and primitive entry shape are checked here.
/// </summary>
public sealed class SetSellerCeilingCommandValidator : AbstractValidator<SetSellerCeilingCommand>
{
    /// <summary>Registers primitive-shape rules for the command.</summary>
    public SetSellerCeilingCommandValidator()
    {
        AccessControlFluentRules.RequireEnvelope(
            this,
            x => x.Entries,
            AccessControlValidationCodes.CeilingEntriesRequired);
        AccessControlFluentRules.RequireId(
            this,
            x => x.SellerPartyId,
            AccessControlValidationCodes.RoleIdRequired);
        RuleForEach(x => x.Entries)
            .Must(entry => AccessControlFluentRules.IsPermissionIdNonBlank(entry.PermissionId))
            .WithErrorCode(AccessControlValidationCodes.PermissionIdRequired);
        RuleForEach(x => x.Entries)
            .Must(entry => AccessControlFluentRules.IsPermissionIdWithin(
                entry.PermissionId,
                AccessControlFluentRules.PermissionIdMaxLength))
            .WithErrorCode(AccessControlValidationCodes.PermissionIdLength);
    }
}
