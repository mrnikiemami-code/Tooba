using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Contracts;
using Tooba.Fulfillment.Contracts.History;
using Tooba.Identity.Contracts;
using Tooba.OperatorProfile.Contracts;
using Tooba.Order.Application;
using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;
using Tooba.Order.Application.Admin.Completeness.Documents;
using Tooba.Order.Application.Admin.Completeness.History;
using Tooba.Order.Application.Admin.Completeness.Models;
using Tooba.Order.Application.Admin.Completeness.Ports;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Party.Contracts;
using Tooba.Payment.Contracts.Admin;
using Tooba.Returns.Contracts.History;
using Tooba.Settlement.Contracts.History;

namespace Tooba.Order.Infrastructure.Admin;

/// <summary>
/// درگاه خواندنی کامل‌بودن سفارش Admin: یادداشت، تاریخچهٔ عملیاتی، فاکتور و رسید.
/// دادهٔ ماژول بیگانه فقط از قرارداد خوانده می‌شود.
/// </summary>
internal sealed class AdminOrderCompletenessStore(
    OrderDbContext db,
    ICheckoutDirectory checkout,
    IPaymentAdminGateway payments,
    IFulfillmentHistoryReader fulfillmentHistory,
    IReturnHistoryReader returnHistory,
    ISettlementHistoryReader settlementHistory,
    IPartyLookup parties,
    ICatalogVariantLookup catalog,
    IActorDisplayLookup actorDisplays,
    IActorContactLookup actorContacts) : IAdminOrderCompletenessStore
{
    private const int NoteTake = 50;

    public Task<bool> ExistsAsync(Guid checkoutId, CancellationToken ct) =>
        db.Checkouts.AsNoTracking().AnyAsync(x => x.CheckoutId == checkoutId, ct);

    public async Task<IReadOnlyList<AdminOrderNoteView>> ListNotesAsync(
        Guid checkoutId,
        Guid actorUserId,
        CancellationToken ct)
    {
        await checkout.RecordAdminViewAsync(checkoutId, actorUserId, ct);
        var notes = await checkout.ListNotesAsync(checkoutId, actorUserId, NoteTake, ct);
        var labels = await ResolveLabelsAsync(notes.Select(x => (Guid?)x.CreatedByUserId), ct);
        return notes.Select(x => MapNote(x, labels)).ToList();
    }

    public async Task<AdminOrderNoteView> AddNoteAsync(
        Guid checkoutId,
        Guid actorUserId,
        string body,
        CancellationToken ct)
    {
        var note = await checkout.AddNoteAsync(checkoutId, actorUserId, body, ct);
        var labels = await ResolveLabelsAsync([note.CreatedByUserId], ct);
        return MapNote(note, labels);
    }

    public async Task<AdminOrderNoteDeleteOutcome> DeleteNoteAsync(
        Guid checkoutId,
        Guid noteId,
        Guid actorUserId,
        CancellationToken ct) =>
        await checkout.DeleteNoteAsync(checkoutId, noteId, actorUserId, ct) switch
        {
            CheckoutNoteDeleteOutcome.Deleted => AdminOrderNoteDeleteOutcome.Deleted,
            CheckoutNoteDeleteOutcome.Forbidden => AdminOrderNoteDeleteOutcome.Forbidden,
            CheckoutNoteDeleteOutcome.NotFound => AdminOrderNoteDeleteOutcome.NotFound,
            _ => throw new InvalidOperationException("Unknown checkout note deletion outcome.")
        };

    public async Task<AdminOrderOperationalHistoryPage?> GetHistoryAsync(
        Guid checkoutId,
        Guid actorUserId,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var group = await LoadGroupAsync(checkoutId, ct);
        if (group is null)
        {
            return null;
        }

        await checkout.RecordAdminViewAsync(checkoutId, actorUserId, ct);
        var notes = await checkout.ListNotesAsync(checkoutId, Guid.Empty, NoteTake, ct);
        var payment = await payments.GetLatestOperationalForCheckoutAsync(checkoutId, ct);
        var drafts = await AdminOrderHistoryComposer.ComposeAsync(
            group,
            notes,
            payment,
            fulfillmentHistory,
            returnHistory,
            settlementHistory,
            parties,
            catalog,
            ct);

        var safePage = Math.Max(1, page);
        var safeSize = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 50);
        var pageDrafts = drafts.Skip((safePage - 1) * safeSize).Take(safeSize).ToList();
        var labels = await ResolveLabelsAsync(pageDrafts.Select(x => x.ActorUserId), ct);
        var items = pageDrafts.Select(x => MapEntry(x, labels)).ToList();
        return new AdminOrderOperationalHistoryPage(checkoutId, safePage, safeSize, drafts.Count, items);
    }

    public async Task<string?> GetInvoiceHtmlAsync(Guid checkoutId, CancellationToken ct)
    {
        var group = await LoadGroupAsync(checkoutId, ct);
        if (group is null)
        {
            return null;
        }

        var payment = await payments.GetLatestOperationalForCheckoutAsync(checkoutId, ct);
        return AdminOrderDocumentRenderer.RenderInvoiceHtml(group, payment);
    }

    public async Task<string?> GetReceiptHtmlAsync(Guid checkoutId, CancellationToken ct)
    {
        var group = await LoadGroupAsync(checkoutId, ct);
        if (group is null)
        {
            return null;
        }

        var payment = await payments.GetLatestOperationalForCheckoutAsync(checkoutId, ct);
        return payment is null ? null : AdminOrderDocumentRenderer.RenderReceiptHtml(group, payment);
    }

    private Task<CheckoutGroup?> LoadGroupAsync(Guid checkoutId, CancellationToken ct) =>
        db.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, ct);

    private async Task<IReadOnlyDictionary<Guid, AdminOrderActorLabel>> ResolveLabelsAsync(
        IEnumerable<Guid?> actorUserIds,
        CancellationToken ct)
    {
        var ids = actorUserIds
            .Where(x => x is { } value && value != Guid.Empty)
            .Select(x => x!.Value)
            .Distinct()
            .ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, AdminOrderActorLabel>();
        }

        var displays = await actorDisplays.GetActorDisplaysAsync(ids, ct);
        var contacts = await actorContacts.GetActorContactsAsync(ids, ct);
        return AdminOrderActorLabels.Build(ids, displays, contacts);
    }

    private static AdminOrderNoteView MapNote(
        CheckoutOperationalNoteSnapshot note,
        IReadOnlyDictionary<Guid, AdminOrderActorLabel> labels)
    {
        var label = AdminOrderActorLabels.Resolve(note.CreatedByUserId, labels);
        return new AdminOrderNoteView(
            note.NoteId,
            note.CheckoutId,
            note.Body,
            note.CreatedByUserId,
            note.CreatedAt,
            label.Kind,
            label.DisplayName,
            label.DisplayFa,
            label.DisplayEn,
            note.CanDelete);
    }

    private static AdminOrderHistoryEntry MapEntry(
        AdminOrderHistoryDraft draft,
        IReadOnlyDictionary<Guid, AdminOrderActorLabel> labels)
    {
        var label = AdminOrderActorLabels.Resolve(draft.ActorUserId, labels);
        return new AdminOrderHistoryEntry(
            draft.OccurredAt,
            draft.Kind,
            draft.LabelFa,
            draft.LabelEn,
            label.Kind,
            label.DisplayName,
            label.DisplayFa,
            label.DisplayEn,
            string.IsNullOrWhiteSpace(draft.SummaryFa) ? null : draft.SummaryFa,
            string.IsNullOrWhiteSpace(draft.SummaryEn) ? null : draft.SummaryEn);
    }
}
