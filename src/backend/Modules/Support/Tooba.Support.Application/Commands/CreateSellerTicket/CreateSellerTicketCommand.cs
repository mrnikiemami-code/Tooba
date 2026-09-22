using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Errors;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;

namespace Tooba.Support.Application.Commands.CreateSellerTicket;

/// <summary>MediatR create seller ticket use case.</summary>
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

/// <summary>Creates a seller support ticket.</summary>
public sealed class CreateSellerTicketHandler(ISupportDirectory directory)
    : IRequestHandler<CreateSellerTicketCommand, Result<TicketSnapshotDto>>
{
    public Task<Result<TicketSnapshotDto>> Handle(CreateSellerTicketCommand request, CancellationToken cancellationToken) =>
        SupportExceptionMapper.TryAsync(
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
