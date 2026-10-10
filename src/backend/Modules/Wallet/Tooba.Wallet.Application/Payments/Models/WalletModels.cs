using Tooba.Wallet.Application.Customer.Models;

namespace Tooba.Wallet.Application.Payments.Models;

/// <summary>Order-payment debit result (Infrastructure-internal shape).</summary>
public sealed record WalletSpendResultDto(
    WalletLedgerEntryDto Entry,
    decimal Balance,
    bool IdempotentReplay);
