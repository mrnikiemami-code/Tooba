using Tooba.Wallet.Application.Customer.Models;

namespace Tooba.Wallet.Application.Refunds.Models;

/// <summary>Refund-credit result (Infrastructure-internal shape).</summary>
public sealed record WalletCreditResultDto(
    WalletLedgerEntryDto Entry,
    decimal Balance,
    bool IdempotentReplay);
