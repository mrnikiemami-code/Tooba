using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Composition;
using Tooba.Support.Application.Tickets.Models;
using Tooba.Support.Application.Tickets.Ports;
using Tooba.Support.Contracts.Errors;

namespace Tooba.Support.Application.Tickets.Queries;

/// <summary>مورد استفادهٔ MediatR برای فهرست تیکت‌های مشتری.</summary>
public sealed record ListCustomerTicketsQuery(Guid ActorUserId, string? Status, int Page, int PageSize)
    : IRequest<Result<TicketListPageDto>>;

/// <summary>تیکت‌های مالکیت‌شدهٔ مشتری را فهرست می‌کند.</summary>
public sealed class ListCustomerTicketsHandler(ISupportDirectory directory)
    : IRequestHandler<ListCustomerTicketsQuery, Result<TicketListPageDto>>
{
    /// <inheritdoc />
    public Task<Result<TicketListPageDto>> Handle(ListCustomerTicketsQuery request, CancellationToken cancellationToken) =>
        SupportOperation.ExecuteAsync(
            () => directory.ListForCustomerAsync(
                request.ActorUserId,
                new AudienceTicketListQuery(request.Status, request.Page, request.PageSize),
                cancellationToken),
            SupportErrorCodes.Rejected);
}
