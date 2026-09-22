using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Completeness.Errors;
using Tooba.Order.Application.Admin.Completeness.Models;
using Tooba.Order.Application.Admin.Completeness.Ports;

namespace Tooba.Order.Application.Admin.Completeness.Commands.AddAdminOrderNote;

public sealed record AddAdminOrderNoteCommand(Guid CheckoutId, AdminOrderActor Actor, string Body)
    : IRequest<Result<AdminOrderNoteView>>;

public sealed class AddAdminOrderNoteHandler(IAdminOrderCompletenessStore store)
    : IRequestHandler<AddAdminOrderNoteCommand, Result<AdminOrderNoteView>>
{
    public async Task<Result<AdminOrderNoteView>> Handle(
        AddAdminOrderNoteCommand request,
        CancellationToken cancellationToken)
    {
        if (!await store.ExistsAsync(request.CheckoutId, cancellationToken))
            return Result.Failure<AdminOrderNoteView>(new SemanticError(AdminOrderCompletenessErrors.Missing));
        if (string.IsNullOrWhiteSpace(request.Body))
            return Result.Failure<AdminOrderNoteView>(new SemanticError(AdminOrderCompletenessErrors.InvalidNote));

        return Result.Success(await store.AddNoteAsync(
            request.CheckoutId,
            request.Actor.UserId,
            request.Body.Trim(),
            cancellationToken));
    }
}
