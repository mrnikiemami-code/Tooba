using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Composition;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;
using Tooba.Support.Contracts.Errors;

namespace Tooba.Support.Application.Commands.CreateCustomerTicket;

/// <summary>مورد استفادهٔ MediatR برای ایجاد تیکت مشتری.</summary>
public sealed record CreateCustomerTicketCommand(
    Guid ActorUserId,
    string Subject,
    string Category,
    string? Priority,
    string Body,
    string? RelatedEntityType,
    Guid? RelatedEntityId,
    string? IdempotencyKey) : IRequest<Result<TicketSnapshotDto>>;

/// <summary>تیکت پشتیبانی مشتری را ایجاد می‌کند.</summary>
public sealed class CreateCustomerTicketHandler(ISupportDirectory directory)
    : IRequestHandler<CreateCustomerTicketCommand, Result<TicketSnapshotDto>>
{
    /// <inheritdoc />
    public Task<Result<TicketSnapshotDto>> Handle(CreateCustomerTicketCommand request, CancellationToken cancellationToken) =>
        SupportOperation.ExecuteAsync(
            () => directory.CreateForCustomerAsync(
                request.ActorUserId,
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
