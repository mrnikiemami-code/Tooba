using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Composition;
using Tooba.Support.Application.Tickets.Models;
using Tooba.Support.Application.Tickets.Ports;
using Tooba.Support.Contracts.Errors;

namespace Tooba.Support.Application.Tickets.Queries;

/// <summary>مورد استفادهٔ MediatR برای دریافت تیکت مشتری.</summary>
public sealed record GetCustomerTicketQuery(Guid ActorUserId, Guid TicketId)
    : IRequest<Result<TicketSnapshotDto>>;

/// <summary>یک تیکت مشتری را می‌خواند؛ حالت یافت‌نشدن به <c>Result</c> نگاشت می‌شود.</summary>
public sealed class GetCustomerTicketHandler(ISupportDirectory directory)
    : IRequestHandler<GetCustomerTicketQuery, Result<TicketSnapshotDto>>
{
    /// <inheritdoc />
    public async Task<Result<TicketSnapshotDto>> Handle(GetCustomerTicketQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await directory.GetForCustomerAsync(request.ActorUserId, request.TicketId, cancellationToken);
        return SupportOperation.NotFoundIfNull(snapshot, SupportErrorCodes.Missing);
    }
}
