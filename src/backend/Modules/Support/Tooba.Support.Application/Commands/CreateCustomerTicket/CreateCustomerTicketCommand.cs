using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Errors;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;

namespace Tooba.Support.Application.Commands.CreateCustomerTicket;

/// <summary>MediatR create customer ticket use case.</summary>
public sealed record CreateCustomerTicketCommand(
    Guid ActorUserId,
    string Subject,
    string Category,
    string? Priority,
    string Body,
    string? RelatedEntityType,
    Guid? RelatedEntityId,
    string? IdempotencyKey) : IRequest<Result<TicketSnapshotDto>>;

/// <summary>Creates a customer support ticket.</summary>
public sealed class CreateCustomerTicketHandler(ISupportDirectory directory)
    : IRequestHandler<CreateCustomerTicketCommand, Result<TicketSnapshotDto>>
{
    public Task<Result<TicketSnapshotDto>> Handle(CreateCustomerTicketCommand request, CancellationToken cancellationToken) =>
        SupportExceptionMapper.TryAsync(
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
