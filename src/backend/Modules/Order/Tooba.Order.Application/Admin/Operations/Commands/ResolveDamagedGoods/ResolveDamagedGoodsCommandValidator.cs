using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Operations.Commands.ResolveDamagedGoods;

/// <summary>Syntactic envelope for the TB-262 damaged-goods resolution command.</summary>
public sealed class ResolveDamagedGoodsCommandValidator : AbstractValidator<ResolveDamagedGoodsCommand>
{
    public ResolveDamagedGoodsCommandValidator()
    {
        OrderFluentRules.RequireAdminOperationEnvelope(
            this,
            x => x.CheckoutId,
            x => x.ActorUserId,
            x => x.Request);
    }
}
