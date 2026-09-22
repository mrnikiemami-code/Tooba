using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Completeness.Errors;
using Tooba.Order.Application.Admin.Completeness.Models;
using Tooba.Order.Application.Admin.Completeness.Ports;

namespace Tooba.Order.Application.Admin.Completeness.Queries.GetAdminOrderOperationalHistory;

public sealed record GetAdminOrderOperationalHistoryQuery(
    Guid CheckoutId,
    AdminOrderActor Actor,
    int Page,
    int PageSize) : IRequest<Result<AdminOrderOperationalHistoryPage>>;

public sealed class GetAdminOrderOperationalHistoryHandler(IAdminOrderCompletenessStore store)
    : IRequestHandler<GetAdminOrderOperationalHistoryQuery, Result<AdminOrderOperationalHistoryPage>>
{
    public async Task<Result<AdminOrderOperationalHistoryPage>> Handle(
        GetAdminOrderOperationalHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var result = await store.GetHistoryAsync(
            request.CheckoutId,
            request.Actor.UserId,
            Math.Max(1, request.Page),
            Math.Clamp(request.PageSize, 1, 50),
            cancellationToken);

        return result is null
            ? Result.Failure<AdminOrderOperationalHistoryPage>(new SemanticError(AdminOrderCompletenessErrors.Missing))
            : Result.Success(result);
    }
}
