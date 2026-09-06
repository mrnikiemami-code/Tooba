using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Offer.Domain;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Payment.Application;
using Tooba.Payment.Domain;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// تست‌های متمرکز یادداشت داخلی / فاکتور snapshot / قرارداد مسیرهای completeness.
/// </summary>
public sealed class AdminOrderCompletenessTests
{
    [Fact]
    public void Operational_note_create_rejects_empty_and_overlong()
    {
        var checkoutId = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<InvalidOperationException>(() =>
            CheckoutOperationalNote.Create(checkoutId, actor, "  ", now));
        Assert.Throws<InvalidOperationException>(() =>
            CheckoutOperationalNote.Create(checkoutId, actor, new string('x', 2001), now));
        var ok = CheckoutOperationalNote.Create(checkoutId, actor, "یادداشت", now);
        Assert.Equal("یادداشت", ok.Body);
        Assert.Equal(actor, ok.CreatedByUserId);
    }

    [Fact]
    public async Task Notes_add_and_list_newest_first_via_order_db()
    {
        await using var db = CreateOrderDb();
        var group = SeedCheckout(db, unitPrice: 12500m);
        await db.SaveChangesAsync();
        var actor = Guid.NewGuid();
        var first = CheckoutOperationalNote.Create(group.CheckoutId, actor, "اول", DateTimeOffset.UtcNow.AddSeconds(-2));
        var second = CheckoutOperationalNote.Create(group.CheckoutId, actor, "دوم", DateTimeOffset.UtcNow);
        db.OperationalNotes.AddRange(first, second);
        await db.SaveChangesAsync();

        var listed = await db.OperationalNotes.AsNoTracking()
            .Where(x => x.CheckoutId == group.CheckoutId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.NoteId)
            .Take(50)
            .ToListAsync();
        Assert.Equal(2, listed.Count);
        Assert.Equal("دوم", listed[0].Body);
        Assert.Equal("اول", listed[1].Body);
    }

    [Fact]
    public void Invoice_html_contains_snapshot_money_not_live_offer_price()
    {
        var group = SeedCheckoutInMemory(unitPrice: 7777.5m);
        var html = AdminOrderCompletenessComposer.RenderInvoiceHtml(group, payment: null);
        Assert.Contains("7777.5 IRR", html, StringComparison.Ordinal);
        Assert.Contains("فاکتور", html, StringComparison.Ordinal);
        Assert.DoesNotContain("commission", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("payable", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Receipt_html_masks_provider_reference_without_raw_secrets()
    {
        var group = SeedCheckoutInMemory(unitPrice: 1000m);
        var payment = new PaymentOperationalSnapshot(
            Guid.NewGuid(),
            group.CheckoutId,
            PaymentStatus.Succeeded,
            1000m,
            "IRR",
            "wallet",
            "w|abcdef0123456789secret",
            null,
            DateTimeOffset.UtcNow.AddMinutes(-2),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null,
            false);
        var html = AdminOrderCompletenessComposer.RenderReceiptHtml(group, payment);
        Assert.Contains("کیف پول", html, StringComparison.Ordinal);
        Assert.Contains("wallet:abcdef01", html, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", html, StringComparison.Ordinal);
        Assert.Contains("1000 IRR", html, StringComparison.Ordinal);
    }

    [Fact]
    public void History_entries_sort_by_occurred_at_descending()
    {
        var older = new AdminOperationalHistoryEntry(
            DateTimeOffset.Parse("2026-01-01T10:00:00Z"),
            "order_created",
            "ثبت سفارش",
            "Order created",
            "توسط سیستم",
            "By system");
        var newer = new AdminOperationalHistoryEntry(
            DateTimeOffset.Parse("2026-01-02T10:00:00Z"),
            "payment_succeeded",
            "پرداخت موفق",
            "Payment succeeded",
            "توسط سیستم",
            "By system");
        var ordered = new[] { older, newer }
            .OrderByDescending(x => x.OccurredAt)
            .ThenBy(x => x.Kind, StringComparer.Ordinal)
            .ToList();
        Assert.Equal("payment_succeeded", ordered[0].Kind);
        Assert.Equal("order_created", ordered[1].Kind);
    }

    [Fact]
    public void Completeness_and_directory_contracts_are_wired()
    {
        var endpoints = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Admin", "AdminOrderCompletenessEndpoints.cs"));
        var directory = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "CheckoutDirectory.cs"));
        var contracts = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Modules", "Order", "Tooba.Order.Application", "OrderContracts.cs"));
        Assert.Equal(5, Count(endpoints, "AdminPanelAccess.RequireAuthorizedAsync"));
        Assert.Contains("/notes", endpoints, StringComparison.Ordinal);
        Assert.Contains("/operational-history", endpoints, StringComparison.Ordinal);
        Assert.Contains("/invoice.html", endpoints, StringComparison.Ordinal);
        Assert.Contains("/receipt.html", endpoints, StringComparison.Ordinal);
        Assert.Contains("ListNotesAsync", contracts, StringComparison.Ordinal);
        Assert.Contains("AddNoteAsync", contracts, StringComparison.Ordinal);
        Assert.Contains("OperationalNotes", directory, StringComparison.Ordinal);
        Assert.Contains("CheckoutOperationalNote.Create", directory, StringComparison.Ordinal);
    }

    private static OrderDbContext CreateOrderDb()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new OrderDbContext(options);
    }

    private static CheckoutGroup SeedCheckout(OrderDbContext db, decimal unitPrice)
    {
        var group = SeedCheckoutInMemory(unitPrice);
        db.Checkouts.Add(group);
        return group;
    }

    private static CheckoutGroup SeedCheckoutInMemory(decimal unitPrice)
    {
        var checkoutId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var line = OrderLine.FromCheckout(
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            seller,
            2,
            unitPrice,
            "IRR",
            true,
            Guid.NewGuid(),
            null,
            "Taxable",
            0.09m,
            0m,
            unitPrice * 2,
            null);
        var order = SellerOrder.Open(
            checkoutId,
            seller,
            $"SO-{sellerOrderId:N}"[..20],
            OrderMode.OnlinePurchase,
            "IRR",
            [line]);
        return CheckoutGroup.Submit(
            checkoutId,
            $"idem-{checkoutId:N}",
            Guid.NewGuid(),
            OrderMode.OnlinePurchase,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "IR",
            "IRR",
            SalesChannel.Marketplace,
            [order],
            DateTimeOffset.UtcNow,
            "گیرنده تست",
            "09120000000",
            "تهران",
            "تهران",
            "آدرس تست",
            "1234567890",
            "post",
            "پست");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }

    private static int Count(string source, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }

        return count;
    }
}
