using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Errors;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;

namespace Tooba.Support.Application.Commands.PatchAdminTicket;

/// <summary>MediatR patch admin ticket use case.</summary>
public sealed record PatchAdminTicketCommand(
    Guid TicketId,
    string? Status,
    string? Priority,
    Guid? AssignedOperatorActorUserId) : IRequest<Result<TicketSnapshotDto>>;

/// <summary>Patches admin ticket status/priority/assignment.</summary>
public sealed class PatchAdminTicketHandler(ISupportDirectory directory)
    : IRequestHandler<PatchAdminTicketCommand, Result<TicketSnapshotDto>>
{
    public Task<Result<TicketSnapshotDto>> Handle(PatchAdminTicketCommand request, CancellationToken cancellationToken) =>
        SupportExceptionMapper.TryAsync(
            () => directory.PatchForAdminAsync(
                request.TicketId,
                new AdminTicketPatchCommand(request.Status, request.Priority, request.AssignedOperatorActorUserId),
                cancellationToken),
            SupportErrorCodes.PatchRejected);
}
