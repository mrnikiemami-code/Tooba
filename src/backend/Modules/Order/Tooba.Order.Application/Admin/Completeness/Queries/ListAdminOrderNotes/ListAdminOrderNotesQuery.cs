using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Order.Application.Admin.Completeness;

public sealed record ListAdminOrderNotesQuery(Guid CheckoutId, AdminOrderActor Actor)
    : IRequest<Result<IReadOnlyList<AdminOrderNoteView>>>;

public sealed class ListAdminOrderNotesHandler(IAdminOrderCompletenessStore store)
    : IRequestHandler<ListAdminOrderNotesQuery, Result<IReadOnlyList<AdminOrderNoteView>>>
{
    public async Task<Result<IReadOnlyList<AdminOrderNoteView>>> Handle(
        ListAdminOrderNotesQuery request,
        CancellationToken cancellationToken) =>
        await store.ExistsAsync(request.CheckoutId, cancellationToken)
            ? Result.Success(await store.ListNotesAsync(request.CheckoutId, request.Actor.UserId, cancellationToken))
            : Result.Failure<IReadOnlyList<AdminOrderNoteView>>(new SemanticError(AdminOrderCompletenessErrors.Missing));
}
