using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Notification.Contracts.Commands;
using Tooba.Notification.Contracts.Copy;
using Tooba.Notification.Contracts.Dtos;
using Tooba.Notification.Contracts.Ports;
using Tooba.Notification.Contracts.Routes;
using Tooba.Wallet.Application.Models;
using Tooba.Wallet.Domain.ValueObjects;
using Tooba.Wallet.Infrastructure.Directories;
using Tooba.Wallet.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Wallet.Tests.Behavior;

/// <summary>
/// Focused financial characterization for Wallet ledger + notification side effects.
/// </summary>
public sealed class WalletFinancialCharacterizationTests
{
    [Fact]
    public async Task Order_payment_debit_is_idempotent_on_replay()
    {
        await using var db = CreateDb();
        var notes = new RecordingNotifications();
        var wallets = new WalletDirectory(db, notes, new SystemUtcClock(), new UuidV7IdGenerator());
        var actor = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
        var admin = Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd");
        var paymentId = Guid.Parse("01900000-0000-7000-8000-000000000201");

        await wallets.AdjustWalletForAdminAsync(
            actor,
            admin,
            new AdminWalletAdjustmentCommand(100_000m, "Credit", "seed", "wallet-char-seed-pay"),
            CancellationToken.None);

        var key = $"wallet-order-debit:{paymentId:D}";
        var first = await wallets.SpendForOrderPaymentAsync(actor, 40_000m, "IRR", paymentId, key, CancellationToken.None);
        var replay = await wallets.SpendForOrderPaymentAsync(actor, 40_000m, "IRR", paymentId, key, CancellationToken.None);

        Assert.False(first.IdempotentReplay);
        Assert.True(replay.IdempotentReplay);
        Assert.Equal(first.Entry.EntryId, replay.Entry.EntryId);
        Assert.Equal(1, await db.LedgerEntries.CountAsync(x => x.Type == LedgerEntryType.OrderPaymentDebit));
    }

    [Fact]
    public async Task Refund_credit_is_idempotent_on_replay()
    {
        await using var db = CreateDb();
        var notes = new RecordingNotifications();
        var wallets = new WalletDirectory(db, notes, new SystemUtcClock(), new UuidV7IdGenerator());
        var actor = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
        var returnId = Guid.Parse("01900000-0000-7000-8000-000000000301");
        var key = $"wallet-refund-credit:{returnId:D}";

        var first = await wallets.CreditRefundAsync(actor, 12_500m, "IRR", returnId, key, CancellationToken.None);
        var replay = await wallets.CreditRefundAsync(actor, 12_500m, "IRR", returnId, key, CancellationToken.None);

        Assert.False(first.IdempotentReplay);
        Assert.True(replay.IdempotentReplay);
        Assert.Equal(first.Entry.EntryId, replay.Entry.EntryId);
        Assert.Equal(1, await db.LedgerEntries.CountAsync(x => x.Type == LedgerEntryType.RefundCredit));
    }

    [Fact]
    public async Task Insufficient_balance_rejects_order_payment_debit()
    {
        await using var db = CreateDb();
        var notes = new RecordingNotifications();
        var wallets = new WalletDirectory(db, notes, new SystemUtcClock(), new UuidV7IdGenerator());
        var actor = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
        var paymentId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            wallets.SpendForOrderPaymentAsync(
                actor,
                10_000m,
                "IRR",
                paymentId,
                $"wallet-order-debit:{paymentId:D}",
                CancellationToken.None));
        Assert.Equal("wallet.rejected.2YXZiNis", ex.Message);
        Assert.Empty(notes.Commands);
    }

    [Fact]
    public async Task Payment_success_emits_wallet_payment_notification_semantics()
    {
        await using var db = CreateDb();
        var notes = new RecordingNotifications();
        var wallets = new WalletDirectory(db, notes, new SystemUtcClock(), new UuidV7IdGenerator());
        var actor = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
        var admin = Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd");
        var paymentId = Guid.Parse("01900000-0000-7000-8000-000000000401");

        await wallets.AdjustWalletForAdminAsync(
            actor,
            admin,
            new AdminWalletAdjustmentCommand(80_000m, "Credit", "seed", "wallet-char-seed-note"),
            CancellationToken.None);
        notes.Commands.Clear();

        await wallets.SpendForOrderPaymentAsync(
            actor, 25_000m, "IRR", paymentId, $"wallet-order-debit:{paymentId:D}", CancellationToken.None);

        var note = Assert.Single(notes.Commands);
        Assert.Equal(NotificationSemanticTypes.WalletPaymentSucceeded, note.Type);
        Assert.Equal(NotificationRecipientKind.Customer, note.RecipientKind);
        Assert.Equal(actor, note.RecipientPartyId);
        Assert.Equal(actor, note.RecipientActorUserId);
        Assert.Equal(NotificationTargetRoutes.CustomerWallet(), note.TargetRoute);
        Assert.Equal($"wallet.payment-succeeded:{paymentId:D}", note.SourceEventId);
        Assert.Equal("wallet.payment.succeeded", note.SourceType);
    }

    [Fact]
    public async Task Refund_credit_emits_wallet_refund_notification_semantics()
    {
        await using var db = CreateDb();
        var notes = new RecordingNotifications();
        var wallets = new WalletDirectory(db, notes, new SystemUtcClock(), new UuidV7IdGenerator());
        var actor = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
        var returnId = Guid.Parse("01900000-0000-7000-8000-000000000501");

        await wallets.CreditRefundAsync(
            actor, 9_000m, "IRR", returnId, $"wallet-refund-credit:{returnId:D}", CancellationToken.None);

        var note = Assert.Single(notes.Commands);
        Assert.Equal(NotificationSemanticTypes.WalletRefundCredited, note.Type);
        Assert.Equal(NotificationRecipientKind.Customer, note.RecipientKind);
        Assert.Equal(actor, note.RecipientPartyId);
        Assert.Equal(NotificationTargetRoutes.CustomerWallet(), note.TargetRoute);
        Assert.Equal($"wallet.refund-credited:{returnId:D}", note.SourceEventId);
        Assert.Equal("wallet.refund.credited", note.SourceType);
    }

    private static WalletDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<WalletDbContext>()
            .UseInMemoryDatabase("wallet-fin-" + Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new WalletDbContext(options);
    }

    private sealed class RecordingNotifications : INotificationCreationPort
    {
        public List<CreateNotificationCommand> Commands { get; } = [];

        public Task<bool> CreateIfAbsentAsync(CreateNotificationCommand command, CancellationToken cancellationToken)
        {
            Commands.Add(command);
            return Task.FromResult(true);
        }
    }
}
