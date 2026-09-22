using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Queries.GetAdminOrderOperations;

public sealed record GetAdminOrderOperationsQuery(Guid CheckoutId, Guid ActorUserId)
    : IRequest<Result<AdminOrderOperationsPage>>;

public sealed class GetAdminOrderOperationsHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<GetAdminOrderOperationsQuery, Result<AdminOrderOperationsPage>>
{
    public Task<Result<AdminOrderOperationsPage>> Handle(
        GetAdminOrderOperationsQuery request,
        CancellationToken cancellationToken) =>
        operations.ListAsync(request.CheckoutId, request.ActorUserId, cancellationToken);
}