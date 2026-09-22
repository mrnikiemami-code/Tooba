using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Completeness.Errors;
using Tooba.Order.Application.Admin.Completeness.Models;
using Tooba.Order.Application.Admin.Completeness.Ports;

namespace Tooba.Order.Application.Admin.Completeness.Queries.GetAdminOrderInvoice;

public sealed record GetAdminOrderInvoiceQuery(Guid CheckoutId, AdminOrderActor Actor)
    : IRequest<Result<AdminOrderPrintableDocument>>;

public sealed class GetAdminOrderInvoiceHandler(IAdminOrderCompletenessStore store)
    : IRequestHandler<GetAdminOrderInvoiceQuery, Result<AdminOrderPrintableDocument>>
{
    public async Task<Result<AdminOrderPrintableDocument>> Handle(
        GetAdminOrderInvoiceQuery request,
        CancellationToken cancellationToken)
    {
        var html = await store.GetInvoiceHtmlAsync(request.CheckoutId, cancellationToken);
        return html is null
            ? Result.Failure<AdminOrderPrintableDocument>(new SemanticError(AdminOrderCompletenessErrors.InvoiceUnavailable))
            : Result.Success(new AdminOrderPrintableDocument(html));
    }
}
