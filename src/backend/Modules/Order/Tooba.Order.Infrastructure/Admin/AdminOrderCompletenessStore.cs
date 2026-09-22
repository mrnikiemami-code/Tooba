using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Order.Application;
using Tooba.Order.Application.Admin.Completeness;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Payment.Contracts.Admin;

namespace Tooba.Order.Infrastructure.Admin;

internal sealed class AdminOrderCompletenessStore(
    OrderDbContext db,
    ICheckoutDirectory checkout,
    IPaymentAdminGateway payments,
    IClock clock) : IAdminOrderCompletenessStore
{
    public Task<bool> ExistsAsync(Guid checkoutId, CancellationToken ct) =>
        db.Checkouts.AsNoTracking().AnyAsync(x => x.CheckoutId == checkoutId, ct);

    public async Task<IReadOnlyList<AdminOrderNoteView>> ListNotesAsync(Guid checkoutId, Guid actorUserId, CancellationToken ct)
    {
        await checkout.RecordAdminViewAsync(checkoutId, actorUserId, ct);
        var notes = await checkout.ListNotesAsync(checkoutId, actorUserId, 50, ct);
        return notes.Select(Map).ToList();
    }

    public async Task<AdminOrderNoteView> AddNoteAsync(Guid checkoutId, Guid actorUserId, string body, DateTimeOffset now, CancellationToken ct) =>
        Map(await checkout.AddNoteAsync(checkoutId, actorUserId, body, ct));

    public async Task<bool> DeleteNoteAsync(Guid checkoutId, Guid noteId, Guid actorUserId, CancellationToken ct)
    {
        try
        {
            await checkout.DeleteNoteAsync(checkoutId, noteId, actorUserId, ct);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public async Task<AdminOrderOperationalHistoryPage?> GetHistoryAsync(Guid checkoutId, Guid actorUserId, int page, int pageSize, CancellationToken ct)
    {
        var group = await db.Checkouts.AsNoTracking().Include(x => x.SellerOrders)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, ct);
        if (group is null) return null;
        await checkout.RecordAdminViewAsync(checkoutId, actorUserId, ct);
        var entries = new List<AdminOrderHistoryEntry>
        {
            new(group.SubmittedAt, "order_created", "ثبت سفارش", "Order created", null, $"Checkout {group.CheckoutId:N}"[..20])
        };
        entries.AddRange(group.SellerOrders.Where(x => x.Status == Domain.SellerOrderStatus.Cancelled)
            .Select(x => new AdminOrderHistoryEntry(group.SubmittedAt, "order_cancelled", "سفارش لغو شد", "Order cancelled", $"سفارش {x.OrderNumber}", $"Order {x.OrderNumber}")));
        var notes = await checkout.ListNotesAsync(checkoutId, Guid.Empty, 50, ct);
        entries.AddRange(notes.Select(x => new AdminOrderHistoryEntry(x.CreatedAt, "operational_note", "یادداشت داخلی", "Internal note", Truncate(x.Body), Truncate(x.Body))));
        var ordered = entries.OrderByDescending(x => x.OccurredAt).ToList();
        return new(checkoutId, page, pageSize, ordered.Count, ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList());
    }

    public async Task<string?> GetInvoiceHtmlAsync(Guid checkoutId, CancellationToken ct)
    {
        var group = await db.Checkouts.AsNoTracking().Include(x => x.SellerOrders).ThenInclude(x => x.Lines)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, ct);
        if (group is null) return null;
        var reference = group.SellerOrders.Select(x => x.OrderNumber).FirstOrDefault() ?? checkoutId.ToString("N")[..12];
        var grand = group.SellerOrders.Sum(x => x.GrandTotalSnapshot);
        var currency = group.SellerOrders.Select(x => x.Currency).FirstOrDefault() ?? group.Currency;
        return Document("فاکتور فروش",
            $"<p>شماره سفارش: <strong dir=\"ltr\">{WebUtility.HtmlEncode(reference)}</strong></p>" +
            $"<p>تاریخ: <span dir=\"ltr\">{group.SubmittedAt:yyyy-MM-dd HH:mm} UTC</span></p>" +
            $"<p>مبلغ قابل پرداخت: <strong dir=\"ltr\">{grand.ToString("0.####", CultureInfo.InvariantCulture)} {WebUtility.HtmlEncode(currency)}</strong></p>");
    }

    public async Task<string?> GetReceiptHtmlAsync(Guid checkoutId, CancellationToken ct)
    {
        var group = await db.Checkouts.AsNoTracking().Include(x => x.SellerOrders)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, ct);
        if (group is null) return null;
        var payment = await payments.GetLatestOperationalForCheckoutAsync(checkoutId, ct);
        if (payment is null) return null;
        var reference = payment.ProviderTransactionReference ?? payment.ProviderRequestReference ?? payment.PaymentId.ToString("N")[..12];
        return Document("رسید پرداخت",
            $"<p>مبلغ: <strong dir=\"ltr\">{payment.Amount.ToString("0.####", CultureInfo.InvariantCulture)} {WebUtility.HtmlEncode(payment.Currency)}</strong></p>" +
            $"<p>وضعیت: {WebUtility.HtmlEncode(payment.Status)}</p><p>مرجع: <span dir=\"ltr\">{WebUtility.HtmlEncode(reference)}</span></p>");
    }

    private static AdminOrderNoteView Map(CheckoutOperationalNoteSnapshot x) =>
        new(x.NoteId, x.CheckoutId, x.Body, x.CreatedByUserId, x.CreatedAt, x.CanDelete);
    private static string Truncate(string value) => value.Length <= 120 ? value : value[..119] + "…";
    private static string Document(string title, string body) =>
        $"<!DOCTYPE html><html lang=\"fa\" dir=\"rtl\"><head><meta charset=\"utf-8\"/><title>{title}</title></head><body><h1>{title}</h1>{body}</body></html>";
}
