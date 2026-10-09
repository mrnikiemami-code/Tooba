using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Composition;
using Tooba.Support.Application.Tickets.Models;
using Tooba.Support.Application.Tickets.Ports;
using Tooba.Support.Contracts.Errors;

namespace Tooba.Support.Application.Tickets.Commands;

/// <summary>مورد استفادهٔ MediatR برای پاسخ مدیر.</summary>
public sealed record ReplyAdminTicketCommand(
    Guid ActorUserId,
    Guid TicketId,
    string Body,
    bool IsInternalNote,
    string? IdempotencyKey) : IRequest<Result<TicketSnapshotDto>>;

/// <summary>پاسخ مدیر را اضافه می‌کند؛ پاسخ عمومی ممکن است اعلان بسازد.</summary>
public sealed class ReplyAdminTicketHandler(ISupportDirectory directory)
    : IRequestHandler<ReplyAdminTicketCommand, Result<TicketSnapshotDto>>
{
    /// <inheritdoc />
    public Task<Result<TicketSnapshotDto>> Handle(ReplyAdminTicketCommand request, CancellationToken cancellationToken) =>
        SupportOperation.ExecuteAsync(
            () => directory.ReplyForAdminAsync(
                request.ActorUserId,
                request.TicketId,
                new ReplyTicketCommand(request.Body, request.IsInternalNote, request.IdempotencyKey),
                cancellationToken),
            SupportErrorCodes.ReplyRejected);
}
