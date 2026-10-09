using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Composition;
using Tooba.Support.Application.Tickets.Models;
using Tooba.Support.Application.Tickets.Ports;
using Tooba.Support.Contracts.Errors;

namespace Tooba.Support.Application.Tickets.Queries;

/// <summary>مورد استفادهٔ MediatR برای دریافت تیکت مدیر (شامل یادداشت داخلی).</summary>
public sealed record GetAdminTicketQuery(Guid TicketId) : IRequest<Result<TicketSnapshotDto>>;

/// <summary>یک تیکت را با دید مدیر می‌خواند؛ حالت یافت‌نشدن به <c>Result</c> نگاشت می‌شود.</summary>
public sealed class GetAdminTicketHandler(ISupportDirectory directory)
    : IRequestHandler<GetAdminTicketQuery, Result<TicketSnapshotDto>>
{
    /// <inheritdoc />
    public async Task<Result<TicketSnapshotDto>> Handle(GetAdminTicketQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await directory.GetForAdminAsync(request.TicketId, cancellationToken);
        return SupportOperation.NotFoundIfNull(snapshot, SupportErrorCodes.Missing);
    }
}
