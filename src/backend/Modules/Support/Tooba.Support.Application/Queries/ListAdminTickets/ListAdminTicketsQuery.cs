using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Composition;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;
using Tooba.Support.Contracts.Errors;

namespace Tooba.Support.Application.Queries.ListAdminTickets;

/// <summary>مورد استفادهٔ MediatR برای فهرست تیکت‌های مدیر.</summary>
public sealed record ListAdminTicketsQuery(
    string? Status,
    string? RequesterKind,
    string? Category,
    string? Priority,
    string? Q,
    int Page,
    int PageSize) : IRequest<Result<TicketListPageDto>>;

/// <summary>تیکت‌ها را با فیلتر برای مدیر فهرست می‌کند.</summary>
public sealed class ListAdminTicketsHandler(ISupportDirectory directory)
    : IRequestHandler<ListAdminTicketsQuery, Result<TicketListPageDto>>
{
    /// <inheritdoc />
    public Task<Result<TicketListPageDto>> Handle(ListAdminTicketsQuery request, CancellationToken cancellationToken) =>
        SupportOperation.ExecuteAsync(
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
