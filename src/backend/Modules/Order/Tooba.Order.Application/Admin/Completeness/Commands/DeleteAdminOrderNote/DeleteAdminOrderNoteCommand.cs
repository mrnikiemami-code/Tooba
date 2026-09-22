using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Order.Application.Admin.Completeness;

public sealed record DeleteAdminOrderNoteCommand(Guid CheckoutId, Guid NoteId, AdminOrderActor Actor)
    : IRequest<Result>;

public sealed class DeleteAdminOrderNoteHandler(IAdminOrderCompletenessStore store)
    : IRequestHandler<DeleteAdminOrderNoteCommand, Result>
{
    public async Task<Result> Handle(DeleteAdminOrderNoteCommand request, CancellationToken cancellationToken)
    {
        if (!await store.ExistsAsync(request.CheckoutId, cancellationToken))
            return Result.Failure(new SemanticError(AdminOrderCompletenessErrors.Missing));

        return await store.DeleteNoteAsync(
            request.CheckoutId,
            request.NoteId,
            request.Actor.UserId,
            cancellationToken)
            ? Result.Success()
            : Result.Failure(new SemanticError(AdminOrderCompletenessErrors.DeleteForbidden));
    }
}
