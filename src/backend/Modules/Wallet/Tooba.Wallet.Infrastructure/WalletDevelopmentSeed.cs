using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Wallet.Domain;
using Tooba.Wallet.Infrastructure.Persistence;

namespace Tooba.Wallet.Infrastructure;

/// <summary>دانهٔ توسعهٔ idempotent برای کیف پول و کارت هدیه.</summary>
public static class WalletDevelopmentSeed
{
    /// <summary>
    /// حساب مشتری با چند سطر دفتر، کارت استفاده‌نشده، بخشی، منقضی و باطل درج می‌کند.
    /// </summary>
    public static async Task ApplyAsync(
        IServiceProvider services,
        Guid customerActorUserId,
        Guid adminActorUserId,
        CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<WalletDbContext>();
        var now = new DateTimeOffset(2026, 8, 27, 14, 0, 0, TimeSpan.Zero);

        await EnsureAccountAsync(db, customerActorUserId, now, cancellationToken);
        await EnsureLedgerAsync(db, adminActorUserId, now, cancellationToken);
        await EnsureGiftCardsAsync(db, customerActorUserId, adminActorUserId, now, cancellationToken);
        await EnsureSpareUnusedIfNeededAsync(db, customerActorUserId, adminActorUserId, now, cancellationToken);

        var balance = await DeriveBalanceAsync(db, WalletDemoIds.AccountId, cancellationToken);
        var (previewCardId, previewCode) = await ResolveUnusedPreviewAsync(db, cancellationToken);
        WalletDemoSnapshotStore.Publish(
            new WalletDemoSnapshot(
                customerActorUserId,
                WalletDemoIds.AccountId,
                balance,
                previewCardId,
                previewCode,
                WalletDemoIds.PartiallyRedeemedGiftCardId,
                WalletDemoIds.ExpiredGiftCardId,
                WalletDemoIds.RevokedGiftCardId,
                "wallet-demo: unused preview code; ledger admin+gift; checkout/refund deferred"));
    }

    private static async Task EnsureSpareUnusedIfNeededAsync(
        WalletDbContext db,
        Guid customerActorUserId,
        Guid adminActorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var primary = await db.GiftCards.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CardId == WalletDemoIds.UnusedGiftCardId, cancellationToken);
        if (primary is null || (primary.Status == GiftCardStatus.Active && primary.RemainingAmount > 0))
            return;

        if (await db.GiftCards.AnyAsync(x => x.CardId == WalletDemoIds.SpareUnusedGiftCardId, cancellationToken))
        {
            // اگر یدکی هم مصرف شده، کارت Repair را یک‌بار درج کن.
            var spare = await db.GiftCards.AsNoTracking()
                .SingleAsync(x => x.CardId == WalletDemoIds.SpareUnusedGiftCardId, cancellationToken);
            if (spare.Status == GiftCardStatus.Active && spare.RemainingAmount > 0)
                return;
            if (await db.GiftCards.AnyAsync(x => x.CardId == WalletDemoIds.RepairPreviewGiftCardId, cancellationToken))
                return;
            db.GiftCards.Add(GiftCard.CreateSeeded(
                WalletDemoIds.RepairPreviewGiftCardId,
                WalletDemoIds.RepairPreviewGiftCardDemoCode,
                100_000m,
                100_000m,
                WalletAccount.DefaultCurrency,
                GiftCardStatus.Active,
                adminActorUserId,
                "wallet-seed-repair-preview-gift-v1",
                now,
                expiresAt: now.AddYears(1),
                recipientActorUserId: customerActorUserId));
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        db.GiftCards.Add(GiftCard.CreateSeeded(
            WalletDemoIds.SpareUnusedGiftCardId,
            WalletDemoIds.SpareUnusedGiftCardDemoCode,
            250_000m,
            250_000m,
            WalletAccount.DefaultCurrency,
            GiftCardStatus.Active,
            adminActorUserId,
            "wallet-seed-spare-unused-gift-v1",
            now,
            expiresAt: now.AddYears(1),
            recipientActorUserId: customerActorUserId));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<(Guid CardId, string Code)> ResolveUnusedPreviewAsync(
        WalletDbContext db,
        CancellationToken cancellationToken)
    {
        foreach (var (id, code) in new (Guid, string)[]
                 {
                     (WalletDemoIds.UnusedGiftCardId, WalletDemoIds.UnusedGiftCardDemoCode),
                     (WalletDemoIds.SpareUnusedGiftCardId, WalletDemoIds.SpareUnusedGiftCardDemoCode),
                     (WalletDemoIds.RepairPreviewGiftCardId, WalletDemoIds.RepairPreviewGiftCardDemoCode),
                 })
        {
            var card = await db.GiftCards.AsNoTracking()
                .SingleOrDefaultAsync(x => x.CardId == id, cancellationToken);
            if (card is not null && card.Status == GiftCardStatus.Active && card.RemainingAmount > 0)
                return (id, code);
        }

        return (WalletDemoIds.UnusedGiftCardId, WalletDemoIds.UnusedGiftCardDemoCode);
    }

    private static async Task EnsureAccountAsync(
        WalletDbContext db,
        Guid customerActorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (await db.Accounts.AnyAsync(x => x.AccountId == WalletDemoIds.AccountId, cancellationToken))
            return;

        db.Accounts.Add(WalletAccount.CreateSeeded(
            WalletDemoIds.AccountId,
            customerActorUserId,
            WalletAccount.DefaultCurrency,
            WalletAccountStatus.Active,
            now));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureLedgerAsync(
        WalletDbContext db,
        Guid adminActorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!await db.LedgerEntries.AnyAsync(x => x.EntryId == WalletDemoIds.AdminAdjustmentEntryId, cancellationToken))
        {
            db.LedgerEntries.Add(WalletLedgerEntry.CreateSeeded(
                WalletDemoIds.AdminAdjustmentEntryId,
                WalletDemoIds.AccountId,
                LedgerEntryType.AdminAdjustment,
                250_000m,
                WalletAccount.DefaultCurrency,
                LedgerDirection.Credit,
                "admin_adjustment",
                adminActorUserId,
                "wallet-seed-admin-credit-v1",
                now,
                """{"reason":"seed admin welcome credit"}"""));
        }

        if (!await db.LedgerEntries.AnyAsync(x => x.EntryId == WalletDemoIds.GiftCreditEntryId, cancellationToken))
        {
            db.LedgerEntries.Add(WalletLedgerEntry.CreateSeeded(
                WalletDemoIds.GiftCreditEntryId,
                WalletDemoIds.AccountId,
                LedgerEntryType.GiftCardCredit,
                100_000m,
                WalletAccount.DefaultCurrency,
                LedgerDirection.Credit,
                "gift_card",
                WalletDemoIds.PartiallyRedeemedGiftCardId,
                "wallet-seed-gift-credit-v1",
                now.AddMinutes(5),
                """{"reason":"seed partial gift redemption"}"""));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureGiftCardsAsync(
        WalletDbContext db,
        Guid customerActorUserId,
        Guid adminActorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!await db.GiftCards.AnyAsync(x => x.CardId == WalletDemoIds.UnusedGiftCardId, cancellationToken))
        {
            db.GiftCards.Add(GiftCard.CreateSeeded(
                WalletDemoIds.UnusedGiftCardId,
                WalletDemoIds.UnusedGiftCardDemoCode,
                500_000m,
                500_000m,
                WalletAccount.DefaultCurrency,
                GiftCardStatus.Active,
                adminActorUserId,
                "wallet-seed-unused-gift-v1",
                now,
                expiresAt: now.AddYears(1),
                recipientActorUserId: customerActorUserId));
        }

        if (!await db.GiftCards.AnyAsync(x => x.CardId == WalletDemoIds.PartiallyRedeemedGiftCardId, cancellationToken))
        {
            db.GiftCards.Add(GiftCard.CreateSeeded(
                WalletDemoIds.PartiallyRedeemedGiftCardId,
                WalletDemoIds.PartialGiftCardDemoCode,
                200_000m,
                100_000m,
                WalletAccount.DefaultCurrency,
                GiftCardStatus.PartiallyRedeemed,
                adminActorUserId,
                "wallet-seed-partial-gift-v1",
                now.AddMinutes(-30),
                expiresAt: now.AddMonths(6),
                recipientActorUserId: customerActorUserId));
        }

        if (!await db.GiftCards.AnyAsync(x => x.CardId == WalletDemoIds.ExpiredGiftCardId, cancellationToken))
        {
            db.GiftCards.Add(GiftCard.CreateSeeded(
                WalletDemoIds.ExpiredGiftCardId,
                WalletDemoIds.ExpiredGiftCardDemoCode,
                50_000m,
                50_000m,
                WalletAccount.DefaultCurrency,
                GiftCardStatus.Expired,
                adminActorUserId,
                "wallet-seed-expired-gift-v1",
                now.AddMonths(-3),
                expiresAt: now.AddDays(-1)));
        }

        if (!await db.GiftCards.AnyAsync(x => x.CardId == WalletDemoIds.RevokedGiftCardId, cancellationToken))
        {
            db.GiftCards.Add(GiftCard.CreateSeeded(
                WalletDemoIds.RevokedGiftCardId,
                WalletDemoIds.RevokedGiftCardDemoCode,
                75_000m,
                0m,
                WalletAccount.DefaultCurrency,
                GiftCardStatus.Revoked,
                adminActorUserId,
                "wallet-seed-revoked-gift-v1",
                now.AddDays(-7)));
        }

        if (!await db.Redemptions.AnyAsync(x => x.RedemptionId == WalletDemoIds.PartialRedemptionId, cancellationToken))
        {
            db.Redemptions.Add(GiftCardRedemption.CreateSeeded(
                WalletDemoIds.PartialRedemptionId,
                WalletDemoIds.PartiallyRedeemedGiftCardId,
                WalletDemoIds.AccountId,
                100_000m,
                "wallet-seed-partial-redeem-v1",
                now.AddMinutes(5)));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<decimal> DeriveBalanceAsync(
        WalletDbContext db,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var entries = await db.LedgerEntries.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .Select(x => new { x.Direction, x.Amount })
            .ToListAsync(cancellationToken);
        return entries.Sum(x => x.Direction == LedgerDirection.Credit ? x.Amount : -x.Amount);
    }
}
