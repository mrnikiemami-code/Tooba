using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Errors;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;

namespace Tooba.Support.Application.Queries.ListCustomerTickets;

/// <summary>MediatR list customer tickets use case.</summary>
public sealed record ListCustomerTicketsQuery(Guid ActorUserId, string? Status, int Page, int PageSize)
    : IRequest<Result<TicketListPageDto>>;

/// <summary>Lists tickets owned by the customer actor.</summary>
public sealed class ListCustomerTicketsHandler(ISupportDirectory directory)
    : IRequestHandler<ListCustomerTicketsQuery, Result<TicketListPageDto>>
{
    public Task<Result<TicketListPageDto>> Handle(ListCustomerTicketsQuery request, CancellationToken cancellationToken) =>
        SupportExceptionMapper.TryAsync(
            () => directory.ListForCustomerAsync(
                request.ActorUserId,
                new AudienceTicketListQuery(request.Status, request.Page, request.PageSize),
                cancellationToken),
            SupportErrorCodes.Rejected);
}
