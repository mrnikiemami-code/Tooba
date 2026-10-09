using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Composition;
using Tooba.Support.Application.Tickets.Models;
using Tooba.Support.Application.Tickets.Ports;
using Tooba.Support.Contracts.Errors;

namespace Tooba.Support.Application.Tickets.Commands;

/// <summary>مورد استفادهٔ MediatR برای ایجاد تیکت فروشنده.</summary>
public sealed record CreateSellerTicketCommand(
    Guid ActorUserId,
    Guid SellerPartyId,
    string Subject,
    string Category,
    string? Priority,
    string Body,
    string? RelatedEntityType,
    Guid? RelatedEntityId,
    string? IdempotencyKey) : IRequest<Result<TicketSnapshotDto>>;

/// <summary>تیکت پشتیبانی فروشنده را ایجاد می‌کند.</summary>
public sealed class CreateSellerTicketHandler(ISupportDirectory directory)
    : IRequestHandler<CreateSellerTicketCommand, Result<TicketSnapshotDto>>
{
    /// <inheritdoc />
    public Task<Result<TicketSnapshotDto>> Handle(CreateSellerTicketCommand request, CancellationToken cancellationToken) =>
        SupportOperation.ExecuteAsync(
            () => directory.CreateForSellerAsync(
                request.ActorUserId,
                request.SellerPartyId,
                new CreateTicketCommand(
                    request.Subject,
                    request.Category,
                    request.Priority,
                    request.Body,
                    request.RelatedEntityType,
                    request.RelatedEntityId,
                    request.IdempotencyKey),
                cancellationToken),
            SupportErrorCodes.Rejected);
}
