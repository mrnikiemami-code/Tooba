using Tooba.Order.Application.Admin.Completeness.Models;
using Tooba.Order.Application.Admin.Completeness.Ports;

namespace Tooba.Order.Tests.Application;

/// <summary>درگاه جعلی کامل‌بودن سفارش؛ فقط رفتار قابل مشاهدهٔ handler را می‌سنجد.</summary>
internal sealed class FakeAdminOrderCompletenessStore : IAdminOrderCompletenessStore
{
    public bool Exists { get; set; } = true;

    public AdminOrderNoteDeleteOutcome DeleteOutcome { get; set; } = AdminOrderNoteDeleteOutcome.Deleted;

    public string? InvoiceHtml { get; set; } = "<html>invoice</html>";

    public string? ReceiptHtml { get; set; } = "<html>receipt</html>";

    public AdminOrderOperationalHistoryPage? History { get; set; }

    public List<AdminOrderNoteView> Notes { get; } = [];

    public List<string> AddedBodies { get; } = [];

    public int RecordedPage { get; private set; }

    public int RecordedPageSize { get; private set; }

    public Task<bool> ExistsAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        Task.FromResult(Exists);

    public Task<IReadOnlyList<AdminOrderNoteView>> ListNotesAsync(
        Guid checkoutId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AdminOrderNoteView>>(Notes);

    public Task<AdminOrderNoteView> AddNoteAsync(
        Guid checkoutId,
        Guid actorUserId,
        string body,
        CancellationToken cancellationToken)
    {
        AddedBodies.Add(body);
        var note = new AdminOrderNoteView(
            Guid.NewGuid(),
            checkoutId,
            body,
            actorUserId,
            DateTimeOffset.UnixEpoch,
            "user",
            "اپراتور آلفا",
            "توسط اپراتور آلفا",
            "By اپراتور آلفا",
            true);
        Notes.Insert(0, note);
        return Task.FromResult(note);
    }

    public Task<AdminOrderNoteDeleteOutcome> DeleteNoteAsync(
        Guid checkoutId,
        Guid noteId,
        Guid actorUserId,
        CancellationToken cancellationToken) =>
        Task.FromResult(DeleteOutcome);

    public Task<AdminOrderOperationalHistoryPage?> GetHistoryAsync(
        Guid checkoutId,
        Guid actorUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        RecordedPage = page;
        RecordedPageSize = pageSize;
        if (!Exists)
        {
            return Task.FromResult<AdminOrderOperationalHistoryPage?>(null);
        }

        return Task.FromResult<AdminOrderOperationalHistoryPage?>(
            History ?? new AdminOrderOperationalHistoryPage(checkoutId, page, pageSize, 0, []));
    }

    public Task<string?> GetInvoiceHtmlAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        Task.FromResult(InvoiceHtml);

    public Task<string?> GetReceiptHtmlAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        Task.FromResult(ReceiptHtml);
}
