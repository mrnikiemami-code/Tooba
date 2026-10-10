using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Notification.Contracts.Commands;
using Tooba.Notification.Contracts.Copy;
using Tooba.Notification.Contracts.Dtos;
using Tooba.Notification.Contracts.Routes;
using Tooba.Wallet.Application.Payments.Models;
using Tooba.Wallet.Contracts.Errors;
using Tooba.Wallet.Contracts.Payments;
using Tooba.Wallet.Domain.Aggregates;
using Tooba.Wallet.Domain.ValueObjects;

namespace Tooba.Wallet.Infrastructure.Persistence;

/// <summary>Order-payment capability: atomic wallet debit plus the checkout quote seam.</summary>
public sealed partial class WalletDirectory
{
    /// <inheritdoc />
    public async Task<WalletSpendResultDto> SpendForOrderPaymentAsync(
        Guid customerActorId,
        decimal amount,
        string currency,
        Guid paymentId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (customerActorId == Guid.Empty || paymentId == Guid.Empty)
            throw new ContractOperationException(WalletErrorCodes.IdsRequired);
        if (amount <= 0)
            throw new ContractOperationException(WalletErrorCodes.AmountPositive);
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ContractOperationException(WalletErrorCodes.IdempotencyRequired);

        var key = idempotencyKey.Trim();
        var normalizedCurrency = WalletAccount.NormalizeCurrency(currency);
        var existing = await _db.LedgerEntries.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
        if (existing is not null)
        {
            if (existing.SourceId != paymentId
                || existing.Type != LedgerEntryType.OrderPaymentDebit
                || existing.Amount != decimal.Round(amount, 0, MidpointRounding.AwayFromZero)
                || !string.Equals(existing.Currency, normalizedCurrency, StringComparison.Ordinal))
            {
                throw new ContractOperationException(WalletErrorCodes.IdempotencyConflict);
            }

            var balanceReplay = await DeriveBalanceAsync(existing.AccountId, cancellationToken);
            return new WalletSpendResultDto(MapEntry(existing), balanceReplay, IdempotentReplay: true);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
        try
        {
            existing = await _db.LedgerEntries.AsNoTracking()
                .SingleOrDefaultAsync(x => x.IdempotencyKey == key, cancellationToken);
            if (existing is not null)
            {
                await tx.CommitAsync(cancellationToken);
                var balanceReplay = await DeriveBalanceAsync(existing.AccountId, cancellationToken);
                return new WalletSpendResultDto(MapEntry(existing), balanceReplay, IdempotentReplay: true);
            }

            var account = await EnsureAccountTrackedAsync(customerActorId, cancellationToken);
            if (!account.CanMutateLedger)
                throw new ContractOperationException(WalletErrorCodes.AccountNotMutable);
            if (!string.Equals(account.Currency, normalizedCurrency, StringComparison.Ordinal))
                throw new ContractOperationException(WalletErrorCodes.CurrencyMismatch);

            var balance = await DeriveBalanceAsync(account.AccountId, cancellationToken);
            var rounded = decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
            if (rounded > balance)
                throw new ContractOperationException(WalletErrorCodes.BalanceInsufficient);

            var now = _clock.UtcNow;
            var entry = WalletLedgerEntry.PostOrderPaymentDebit(
                _ids.NewId(),
                account.AccountId,
                paymentId,
                rounded,
                normalizedCurrency,
                key,
                now,
                JsonSerializer.Serialize(new { reason = "order_payment_debit", paymentId }));
            _db.LedgerEntries.Add(entry);
            // لمس ردیف حساب برای قفل خوش‌بینانه/سریال در Serializable.
            _db.Entry(account).Property(x => x.Status).IsModified = true;
            await _db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            await _notifications.CreateIfAbsentAsync(
                new CreateNotificationCommand(
                    NotificationRecipientKind.Customer,
                    customerActorId,
                    customerActorId,
                    NotificationSemanticTypes.WalletPaymentSucceeded,
                    new { amount = entry.Amount, currency = entry.Currency, paymentId },
                    NotificationTargetRoutes.CustomerWallet(),
                    $"wallet.payment-succeeded:{paymentId:D}",
                    "wallet.payment.succeeded"),
                cancellationToken);

            var newBalance = await DeriveBalanceAsync(account.AccountId, cancellationToken);
            return new WalletSpendResultDto(MapEntry(entry), newBalance, IdempotentReplay: false);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    async Task<WalletOrderPaymentDebitResultDto> IWalletOrderPaymentPort.SpendForOrderPaymentAsync(
        Guid customerActorId,
        decimal amount,
        string currency,
        Guid paymentId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await SpendForOrderPaymentAsync(
            customerActorId, amount, currency, paymentId, idempotencyKey, cancellationToken);
        return new WalletOrderPaymentDebitResultDto(result.Balance, result.IdempotentReplay);
    }

    /// <inheritdoc />
    public async Task<WalletCheckoutQuoteDto> QuoteForPayableAsync(
        Guid customerActorId,
        decimal payableAmount,
        string currency,
        CancellationToken cancellationToken)
    {
        if (payableAmount < 0)
            throw new ContractOperationException(WalletErrorCodes.AmountPositive);
        var normalizedCurrency = WalletAccount.NormalizeCurrency(currency);
        var summary = await GetOrCreateSummaryForCustomerAsync(customerActorId, cancellationToken);
        if (!string.Equals(summary.Currency, normalizedCurrency, StringComparison.Ordinal))
        {
            return new WalletCheckoutQuoteDto(
                summary.Balance,
                0m,
                payableAmount,
                false,
                normalizedCurrency);
        }

        var maxUsable = Math.Min(summary.Balance, payableAmount);
        if (summary.Status != nameof(WalletAccountStatus.Active))
            maxUsable = 0m;
        var remaining = payableAmount - maxUsable;
        return new WalletCheckoutQuoteDto(
            summary.Balance,
            maxUsable,
            remaining,
            payableAmount > 0 && remaining == 0 && maxUsable == payableAmount,
            normalizedCurrency);
    }
}
