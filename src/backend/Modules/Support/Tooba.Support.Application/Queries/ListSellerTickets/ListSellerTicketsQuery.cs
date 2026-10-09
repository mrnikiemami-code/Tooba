using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Composition;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;
using Tooba.Support.Contracts.Errors;

namespace Tooba.Support.Application.Queries.ListSellerTickets;

/// <summary>مورد استفادهٔ MediatR برای فهرست تیکت‌های فروشنده.</summary>
public sealed record ListSellerTicketsQuery(Guid SellerPartyId, string? Status, int Page, int PageSize)
    : IRequest<Result<TicketListPageDto>>;

/// <summary>تیکت‌های SellerParty را فهرست می‌کند.</summary>
public sealed class ListSellerTicketsHandler(ISupportDirectory directory)
    : IRequestHandler<ListSellerTicketsQuery, Result<TicketListPageDto>>
{
    /// <inheritdoc />
    public Task<Result<TicketListPageDto>> Handle(ListSellerTicketsQuery request, CancellationToken cancellationToken) =>
        SupportOperation.ExecuteAsync(
            () => directory.ListForSellerAsync(
                request.SellerPartyId,
                new AudienceTicketListQuery(request.Status, request.Page, request.PageSize),
                cancellationToken),
            SupportErrorCodes.Rejected);
}
