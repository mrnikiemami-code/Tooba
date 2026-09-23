using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.InventoryRecovery.Queries.AssessOrderInventoryRecovery;

public sealed class AssessOrderInventoryRecoveryQueryValidator : AbstractValidator<AssessOrderInventoryRecoveryQuery>
{
    public AssessOrderInventoryRecoveryQueryValidator()
    {
        OrderFluentRules.RequireCheckoutId(this, x => x.CheckoutId);
    }
}