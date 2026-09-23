using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.InventoryRecovery.Queries.AuditOrderInventoryRecovery;

public sealed class AuditOrderInventoryRecoveryQueryValidator : AbstractValidator<AuditOrderInventoryRecoveryQuery>
{
    public AuditOrderInventoryRecoveryQueryValidator()
    {
        OrderFluentRules.RequireTakeRange(this, x => x.Take);
    }
}