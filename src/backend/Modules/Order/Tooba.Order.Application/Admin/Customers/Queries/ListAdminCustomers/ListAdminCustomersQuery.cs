using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Customers.Models;
using Tooba.Order.Application.Admin.Customers.Ports;

namespace Tooba.Order.Application.Admin.Customers.Queries.ListAdminCustomers;

/// <summary>فهرست مشتریان مشتق‌شده از Checkout.</summary>
public sealed record ListAdminCustomersQuery : IRequest<Result<IReadOnlyList<AdminCustomerListItem>>>;

/// <summary>فهرست کامل مشتریان را از خوانندهٔ Order می‌گیرد.</summary>
public sealed class ListAdminCustomersHandler(IAdminCustomersGridReader reader)
    : IRequestHandler<ListAdminCustomersQuery, Result<IReadOnlyList<AdminCustomerListItem>>>
{
    public async Task<Result<IReadOnlyList<AdminCustomerListItem>>> Handle(
        ListAdminCustomersQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await reader.ListAsync(cancellationToken));
}
