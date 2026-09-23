using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Seller.Queries.ListSellerOrders;

public sealed class ListSellerOrdersQueryValidator : AbstractValidator<ListSellerOrdersQuery>
{
    public ListSellerOrdersQueryValidator()
    {
        OrderFluentRules.RequireSellerPartyId(this, x => x.SellerPartyId);
        OrderFluentRules.RequireActorUserId(this, x => x.ActorUserId);
    }
}