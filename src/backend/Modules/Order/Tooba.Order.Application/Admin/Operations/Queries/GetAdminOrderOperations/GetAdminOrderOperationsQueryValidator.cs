using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Queries.GetAdminOrderOperations;

public sealed class GetAdminOrderOperationsQueryValidator : AbstractValidator<GetAdminOrderOperationsQuery>
{
    public GetAdminOrderOperationsQueryValidator()
    {
        OrderFluentRules.RequireCheckoutId(this, x => x.CheckoutId);
        OrderFluentRules.RequireActorUserId(this, x => x.ActorUserId);
    }
}