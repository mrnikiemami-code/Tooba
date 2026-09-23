using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.LegacyList.Ports;
using Tooba.Order.Application.Admin.OrdersGrid;
using Tooba.Order.Application.Admin.OrdersGrid.Models;
using Tooba.Party.Contracts;
using Tooba.Returns.Contracts.Operations;

namespace Tooba.Order.Application.Admin.LegacyList.Queries.ListAdminOrders;

/// <summary>فهرست سازگاری آخرین Checkoutها برای GET /v1/admin/orders.</summary>
public sealed record ListAdminOrdersQuery : IRequest<Result<IReadOnlyList<AdminOrderListItem>>>;

/// <summary>آخرین ۲۰۰ گروه را می‌خواند و با Projection موجود نگاشت می‌کند.</summary>
public sealed class ListAdminOrdersHandler(
    IAdminOrderListStore store,
    IPartyLookup parties,
    IReturnAdminOperations returnOperations)
    : IRequestHandler<ListAdminOrdersQuery, Result<IReadOnlyList<AdminOrderListItem>>>
{
    private const int Take = 200;

    public async Task<Result<IReadOnlyList<AdminOrderListItem>>> Handle(
        ListAdminOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var groups = await store.ListLatestGroupsAsync(Take, cancellationToken);
        var sellerIds = groups.SelectMany(g => g.SellerOrders.Select(o => o.SellerPartyId)).Distinct().ToList();
        var sellerNames = sellerIds.Count == 0
            ? (IReadOnlyDictionary<Guid, string>)new Dictionary<Guid, string>()
            : await parties.GetDisplayNamesAsync(sellerIds, cancellationToken);

        var items = new List<AdminOrderListItem>(groups.Count);
        foreach (var group in groups)
        {
            var sellerOrderIds = group.SellerOrders.Select(x => x.SellerOrderId).ToList();
            var returns = await returnOperations.ListBySellerOrderIdsAsync(sellerOrderIds, cancellationToken);
            var returnsLookup = returns
                .GroupBy(x => x.SellerOrderId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<ReturnSnapshot>)g.ToList());
            items.Add(AdminOrdersGridProjection.MapOrderListItem(group, sellerNames, returnsLookup));
        }

        return Result.Success<IReadOnlyList<AdminOrderListItem>>(items);
    }
}
