using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Order.Application.Admin.Completeness;

public static class AdminOrderCompletenessErrors
{
    public const string Missing = "order.operation.invalid";
    public const string InvalidNote = "order.note.invalid";
    public const string DeleteForbidden = "order.note.delete.forbidden";
    public const string InvoiceUnavailable = "order.invoice.unavailable";
    public const string ReceiptUnavailable = "order.receipt.unavailable";
}

public sealed record AdminOrderActor(Guid UserId);
public sealed record AdminOrderNoteView(Guid NoteId, Guid CheckoutId, string Body, Guid CreatedByUserId, DateTimeOffset CreatedAt, bool CanDelete);
public sealed record AdminOrderHistoryEntry(DateTimeOffset OccurredAt, string Kind, string LabelFa, string LabelEn, string? SummaryFa, string? SummaryEn);
public sealed record AdminOrderOperationalHistoryPage(Guid CheckoutId, int Page, int PageSize, int Total, IReadOnlyList<AdminOrderHistoryEntry> Items);
public sealed record AdminOrderPrintableDocument(string Html);

public interface IAdminOrderCompletenessStore
{
    Task<bool> ExistsAsync(Guid checkoutId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminOrderNoteView>> ListNotesAsync(Guid checkoutId, Guid actorUserId, CancellationToken cancellationToken);
    Task<AdminOrderNoteView> AddNoteAsync(Guid checkoutId, Guid actorUserId, string body, DateTimeOffset now, CancellationToken cancellationToken);
    Task<bool> DeleteNoteAsync(Guid checkoutId, Guid noteId, Guid actorUserId, CancellationToken cancellationToken);
    Task<AdminOrderOperationalHistoryPage?> GetHistoryAsync(Guid checkoutId, Guid actorUserId, int page, int pageSize, CancellationToken cancellationToken);
    Task<string?> GetInvoiceHtmlAsync(Guid checkoutId, CancellationToken cancellationToken);
    Task<string?> GetReceiptHtmlAsync(Guid checkoutId, CancellationToken cancellationToken);
}

public sealed record ListAdminOrderNotesQuery(Guid CheckoutId, AdminOrderActor Actor) : IRequest<Result<IReadOnlyList<AdminOrderNoteView>>>;
public sealed class ListAdminOrderNotesHandler(IAdminOrderCompletenessStore store) : IRequestHandler<ListAdminOrderNotesQuery, Result<IReadOnlyList<AdminOrderNoteView>>>
{
    public async Task<Result<IReadOnlyList<AdminOrderNoteView>>> Handle(ListAdminOrderNotesQuery request, CancellationToken ct) =>
        await store.ExistsAsync(request.CheckoutId, ct)
            ? Result.Success(await store.ListNotesAsync(request.CheckoutId, request.Actor.UserId, ct))
            : Result.Failure<IReadOnlyList<AdminOrderNoteView>>(new SemanticError(AdminOrderCompletenessErrors.Missing));
}

public sealed record AddAdminOrderNoteCommand(Guid CheckoutId, AdminOrderActor Actor, string Body) : IRequest<Result<AdminOrderNoteView>>;
public sealed class AddAdminOrderNoteHandler(IAdminOrderCompletenessStore store, IClock clock) : IRequestHandler<AddAdminOrderNoteCommand, Result<AdminOrderNoteView>>
{
    public async Task<Result<AdminOrderNoteView>> Handle(AddAdminOrderNoteCommand request, CancellationToken ct)
    {
        if (!await store.ExistsAsync(request.CheckoutId, ct))
            return Result.Failure<AdminOrderNoteView>(new SemanticError(AdminOrderCompletenessErrors.Missing));
        if (string.IsNullOrWhiteSpace(request.Body))
            return Result.Failure<AdminOrderNoteView>(new SemanticError(AdminOrderCompletenessErrors.InvalidNote));
        return Result.Success(await store.AddNoteAsync(request.CheckoutId, request.Actor.UserId, request.Body.Trim(), clock.UtcNow, ct));
    }
}

public sealed record DeleteAdminOrderNoteCommand(Guid CheckoutId, Guid NoteId, AdminOrderActor Actor) : IRequest<Result>;
public sealed class DeleteAdminOrderNoteHandler(IAdminOrderCompletenessStore store) : IRequestHandler<DeleteAdminOrderNoteCommand, Result>
{
    public async Task<Result> Handle(DeleteAdminOrderNoteCommand request, CancellationToken ct)
    {
        if (!await store.ExistsAsync(request.CheckoutId, ct))
            return Result.Failure(new SemanticError(AdminOrderCompletenessErrors.Missing));
        return await store.DeleteNoteAsync(request.CheckoutId, request.NoteId, request.Actor.UserId, ct)
            ? Result.Success()
            : Result.Failure(new SemanticError(AdminOrderCompletenessErrors.DeleteForbidden));
    }
}

public sealed record GetAdminOrderOperationalHistoryQuery(Guid CheckoutId, AdminOrderActor Actor, int Page, int PageSize) : IRequest<Result<AdminOrderOperationalHistoryPage>>;
public sealed class GetAdminOrderOperationalHistoryHandler(IAdminOrderCompletenessStore store) : IRequestHandler<GetAdminOrderOperationalHistoryQuery, Result<AdminOrderOperationalHistoryPage>>
{
    public async Task<Result<AdminOrderOperationalHistoryPage>> Handle(GetAdminOrderOperationalHistoryQuery request, CancellationToken ct)
    {
        var result = await store.GetHistoryAsync(request.CheckoutId, request.Actor.UserId, Math.Max(1, request.Page), Math.Clamp(request.PageSize, 1, 50), ct);
        return result is null ? Result.Failure<AdminOrderOperationalHistoryPage>(new SemanticError(AdminOrderCompletenessErrors.Missing)) : Result.Success(result);
    }
}

public sealed record GetAdminOrderInvoiceQuery(Guid CheckoutId, AdminOrderActor Actor) : IRequest<Result<AdminOrderPrintableDocument>>;
public sealed class GetAdminOrderInvoiceHandler(IAdminOrderCompletenessStore store) : IRequestHandler<GetAdminOrderInvoiceQuery, Result<AdminOrderPrintableDocument>>
{
    public async Task<Result<AdminOrderPrintableDocument>> Handle(GetAdminOrderInvoiceQuery request, CancellationToken ct)
    {
        var html = await store.GetInvoiceHtmlAsync(request.CheckoutId, ct);
        return html is null ? Result.Failure<AdminOrderPrintableDocument>(new SemanticError(AdminOrderCompletenessErrors.InvoiceUnavailable)) : Result.Success(new AdminOrderPrintableDocument(html));
    }
}

public sealed record GetAdminOrderReceiptQuery(Guid CheckoutId, AdminOrderActor Actor) : IRequest<Result<AdminOrderPrintableDocument>>;
public sealed class GetAdminOrderReceiptHandler(IAdminOrderCompletenessStore store) : IRequestHandler<GetAdminOrderReceiptQuery, Result<AdminOrderPrintableDocument>>
{
    public async Task<Result<AdminOrderPrintableDocument>> Handle(GetAdminOrderReceiptQuery request, CancellationToken ct)
    {
        var html = await store.GetReceiptHtmlAsync(request.CheckoutId, ct);
        return html is null ? Result.Failure<AdminOrderPrintableDocument>(new SemanticError(AdminOrderCompletenessErrors.ReceiptUnavailable)) : Result.Success(new AdminOrderPrintableDocument(html));
    }
}
