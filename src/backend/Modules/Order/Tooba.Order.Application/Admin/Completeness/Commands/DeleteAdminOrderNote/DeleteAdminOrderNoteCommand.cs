using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Completeness.Errors;
using Tooba.Order.Application.Admin.Completeness.Models;
using Tooba.Order.Application.Admin.Completeness.Ports;

namespace Tooba.Order.Application.Admin.Completeness.Commands.DeleteAdminOrderNote;

public sealed record DeleteAdminOrderNoteCommand(Guid CheckoutId, Guid NoteId, AdminOrderActor Actor)
    : IRequest<Result>;

public sealed class DeleteAdminOrderNoteHandler(IAdminOrderCompletenessStore store)
    : IRequestHandler<DeleteAdminOrderNoteCommand, Result>
{
    public async Task<Result> Handle(DeleteAdminOrderNoteCommand request, CancellationToken cancellationToken)
    {
        if (!await store.ExistsAsync(request.CheckoutId, cancellationToken))
            return Result.Failure(new SemanticError(AdminOrderCompletenessErrors.Missing));

        var outcome = await store.DeleteNoteAsync(
            request.CheckoutId,
            request.NoteId,
            request.Actor.UserId,
            cancellationToken);
        return outcome switch
        {
            AdminOrderNoteDeleteOutcome.Deleted => Result.Success(),
            AdminOrderNoteDeleteOutcome.Forbidden or AdminOrderNoteDeleteOutcome.NotFound =>
                Result.Failure(new SemanticError(AdminOrderCompletenessErrors.DeleteForbidden)),
            _ => throw new InvalidOperationException("Unknown admin order note deletion outcome.")
        };
    }
}
