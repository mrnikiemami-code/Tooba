using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Customer.Models;
using Tooba.Order.Application.Customer.Ports;

namespace Tooba.Order.Application.Customer.Queries.GetCustomerOrderDashboardSummary;

/// <summary>شمارنده‌ها و سفارش‌های اخیر داشبورد مشتری.</summary>
public sealed record GetCustomerOrderDashboardSummaryQuery(Guid ActorUserId)
    : IRequest<Result<CustomerOrderDashboardSummary>>;

/// <summary>خلاصهٔ سفارش را برای ترکیب نازک Host (Wishlist/AddressBook/profile) می‌سازد.</summary>
public sealed class GetCustomerOrderDashboardSummaryHandler(
    ICustomerOrderCheckoutStore store,
    CustomerOrderComposer composer)
    : IRequestHandler<GetCustomerOrderDashboardSummaryQuery, Result<CustomerOrderDashboardSummary>>
{
    public async Task<Result<CustomerOrderDashboardSummary>> Handle(
        GetCustomerOrderDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (request.ActorUserId == Guid.Empty)
        {
            return Result.Failure<CustomerOrderDashboardSummary>(
                new SemanticError(CustomerOrderErrors.SessionRequired));
        }

        var groups = await store.ListByActorAsync(request.ActorUserId, take: 200, cancellationToken);
        var orders = new List<CustomerOrderListItem>(groups.Count);
        foreach (var group in groups)
        {
            orders.Add(await composer.MapListItemAsync(group, request.ActorUserId, cancellationToken));
        }

        var latest = groups.Count == 0 ? null : groups[0];
        return Result.Success(CustomerOrderComposer.BuildDashboardSummary(orders, latest));
    }
}
