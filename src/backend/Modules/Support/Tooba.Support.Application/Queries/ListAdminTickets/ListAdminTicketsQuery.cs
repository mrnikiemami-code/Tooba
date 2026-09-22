using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Errors;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;

namespace Tooba.Support.Application.Queries.ListAdminTickets;

/// <summary>MediatR list admin tickets use case.</summary>
public sealed record ListAdminTicketsQuery(
    string? Status,
    string? RequesterKind,
    string? Category,
    string? Priority,
    string? Q,
    int Page,
    int PageSize) : IRequest<Result<TicketListPageDto>>;

/// <summary>Lists tickets for Admin with filters.</summary>
public sealed class ListAdminTicketsHandler(ISupportDirectory directory)
    : IRequestHandler<ListAdminTicketsQuery, Result<TicketListPageDto>>
{
    public Task<Result<TicketListPageDto>> Handle(ListAdminTicketsQuery request, CancellationToken cancellationToken) =>
        SupportExceptionMapper.TryAsync(
            () => directory.ListForAdminAsync(
                new AdminTicketListQuery(
                    request.Status,
                    request.RequesterKind,
                    request.Category,
                    request.Priority,
                    request.Q,
                    request.Page,
                    request.PageSize),
                cancellationToken),
            SupportErrorCodes.Rejected);
}
