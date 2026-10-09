using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Composition;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;
using Tooba.Support.Contracts.Errors;

namespace Tooba.Support.Application.Commands.PatchAdminTicket;

/// <summary>مورد استفادهٔ MediatR برای پچ مدیر.</summary>
public sealed record PatchAdminTicketCommand(
    Guid TicketId,
    string? Status,
    string? Priority,
    Guid? AssignedOperatorActorUserId) : IRequest<Result<TicketSnapshotDto>>;

/// <summary>وضعیت/اولویت/ارجاع تیکت را توسط مدیر پچ می‌کند.</summary>
public sealed class PatchAdminTicketHandler(ISupportDirectory directory)
    : IRequestHandler<PatchAdminTicketCommand, Result<TicketSnapshotDto>>
{
    /// <inheritdoc />
    public Task<Result<TicketSnapshotDto>> Handle(PatchAdminTicketCommand request, CancellationToken cancellationToken) =>
        SupportOperation.ExecuteAsync(
            () => directory.PatchForAdminAsync(
                request.TicketId,
                new AdminTicketPatchCommand(request.Status, request.Priority, request.AssignedOperatorActorUserId),
                cancellationToken),
            SupportErrorCodes.PatchRejected);
}
