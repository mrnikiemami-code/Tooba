namespace Tooba.Order.Application.Admin.Completeness;

public interface IAdminOrderCompletenessStore
{
    Task<bool> ExistsAsync(Guid checkoutId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminOrderNoteView>> ListNotesAsync(Guid checkoutId, Guid actorUserId, CancellationToken cancellationToken);
    Task<AdminOrderNoteView> AddNoteAsync(Guid checkoutId, Guid actorUserId, string body, CancellationToken cancellationToken);
    Task<bool> DeleteNoteAsync(Guid checkoutId, Guid noteId, Guid actorUserId, CancellationToken cancellationToken);
    Task<AdminOrderOperationalHistoryPage?> GetHistoryAsync(Guid checkoutId, Guid actorUserId, int page, int pageSize, CancellationToken cancellationToken);
    Task<string?> GetInvoiceHtmlAsync(Guid checkoutId, CancellationToken cancellationToken);
    Task<string?> GetReceiptHtmlAsync(Guid checkoutId, CancellationToken cancellationToken);
}
