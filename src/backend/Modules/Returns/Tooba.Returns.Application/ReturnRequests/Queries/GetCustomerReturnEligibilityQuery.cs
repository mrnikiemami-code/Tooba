using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Composition;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;

namespace Tooba.Returns.Application.ReturnRequests.Queries;

/// <summary>
/// TB-278: the authenticated customer's authoritative return eligibility of one seller order of a checkout.
/// </summary>
public sealed record GetCustomerReturnEligibilityQuery(Guid CustomerUserId, Guid SellerOrderId)
    : IRequest<Result<CustomerReturnEligibilitySnapshot>>;

/// <summary>Handler — Result via the canonical typed-fault seam (foreign order / missing order stay typed).</summary>
public sealed class GetCustomerReturnEligibilityHandler(ICustomerReturnEligibilityQuery eligibility)
    : IRequestHandler<GetCustomerReturnEligibilityQuery, Result<CustomerReturnEligibilitySnapshot>>
{
    public Task<Result<CustomerReturnEligibilitySnapshot>> Handle(
        GetCustomerReturnEligibilityQuery request,
        CancellationToken cancellationToken) =>
        ReturnsOperation.ExecuteAsync(() => eligibility.QueryAsync(
            request.CustomerUserId,
            request.SellerOrderId,
            cancellationToken));
}
