namespace Tooba.Settlement.Application.Models;

/// <summary>ماندهٔ تسویه با نام نمایشی فروشنده برای گرید Admin.</summary>
public sealed record AdminSettlementBalanceListItem(
    Guid SettlementAccountId,
    Guid SellerPartyId,
    string SellerDisplayName,
    string Currency,
    decimal PostedCredits,
    decimal PostedDebits,
    decimal ReservedPayouts,
    decimal AvailableBalance);

/// <summary>درخواست payout با نام نمایشی فروشنده برای گرید Admin.</summary>
public sealed record AdminPayoutListItem(
    Guid PayoutRequestId,
    Guid SettlementAccountId,
    Guid SellerPartyId,
    string SellerDisplayName,
    decimal Amount,
    string Currency,
    string Status,
    string IdempotencyKey,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>بدنهٔ HTTP درخواست payout فروشنده.</summary>
public sealed record RequestPayoutBody(decimal Amount, string IdempotencyKey);
