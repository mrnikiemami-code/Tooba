using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Errors;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;

namespace Tooba.Support.Application.Queries.ListSellerTickets;

/// <summary>MediatR list seller tickets use case.</summary>
public sealed record ListSellerTicketsQuery(Guid SellerPartyId, string? Status, int Page, int PageSize)
    : IRequest<Result<TicketListPageDto>>;

/// <summary>Lists tickets for the seller party.</summary>
public sealed class ListSellerTicketsHandler(ISupportDirectory directory)
    : IRequestHandler<ListSellerTicketsQuery, Result<TicketListPageDto>>
{
    public Task<Result<TicketListPageDto>> Handle(ListSellerTicketsQuery request, CancellationToken cancellationToken) =>
        SupportExceptionMapper.TryAsync(
            () => directory.ListForSellerAsync(
                request.SellerPartyId,
                new AudienceTicketListQuery(request.Status, request.Page, request.PageSize),
                cancellationToken),
            SupportErrorCodes.Rejected);
}
