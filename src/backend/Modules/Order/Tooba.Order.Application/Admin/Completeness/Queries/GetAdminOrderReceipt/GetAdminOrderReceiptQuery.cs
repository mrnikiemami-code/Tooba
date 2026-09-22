using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Order.Application.Admin.Completeness;

public sealed record GetAdminOrderReceiptQuery(Guid CheckoutId, AdminOrderActor Actor)
    : IRequest<Result<AdminOrderPrintableDocument>>;

public sealed class GetAdminOrderReceiptHandler(IAdminOrderCompletenessStore store)
    : IRequestHandler<GetAdminOrderReceiptQuery, Result<AdminOrderPrintableDocument>>
{
    public async Task<Result<AdminOrderPrintableDocument>> Handle(
        GetAdminOrderReceiptQuery request,
        CancellationToken cancellationToken)
    {
        var html = await store.GetReceiptHtmlAsync(request.CheckoutId, cancellationToken);
        return html is null
            ? Result.Failure<AdminOrderPrintableDocument>(new SemanticError(AdminOrderCompletenessErrors.ReceiptUnavailable))
            : Result.Success(new AdminOrderPrintableDocument(html));
    }
}
