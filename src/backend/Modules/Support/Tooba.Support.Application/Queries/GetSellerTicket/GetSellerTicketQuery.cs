using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Composition;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;
using Tooba.Support.Contracts.Errors;

namespace Tooba.Support.Application.Queries.GetSellerTicket;

/// <summary>مورد استفادهٔ MediatR برای دریافت تیکت فروشنده.</summary>
public sealed record GetSellerTicketQuery(Guid SellerPartyId, Guid TicketId)
    : IRequest<Result<TicketSnapshotDto>>;

/// <summary>یک تیکت فروشنده را می‌خواند؛ حالت یافت‌نشدن به <c>Result</c> نگاشت می‌شود.</summary>
public sealed class GetSellerTicketHandler(ISupportDirectory directory)
    : IRequestHandler<GetSellerTicketQuery, Result<TicketSnapshotDto>>
{
    /// <inheritdoc />
    public async Task<Result<TicketSnapshotDto>> Handle(GetSellerTicketQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await directory.GetForSellerAsync(request.SellerPartyId, request.TicketId, cancellationToken);
        return SupportOperation.NotFoundIfNull(snapshot, SupportErrorCodes.Missing);
    }
}
