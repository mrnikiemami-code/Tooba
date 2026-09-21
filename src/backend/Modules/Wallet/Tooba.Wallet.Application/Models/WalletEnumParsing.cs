using Tooba.Wallet.Domain.ValueObjects;

namespace Tooba.Wallet.Application.Models;

/// <summary>پارس enumهای مرز Application.</summary>
public static class WalletEnumParsing
{
    /// <summary>وضعیت کارت فیلتر.</summary>
    public static GiftCardStatus? TryParseGiftCardStatus(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : Enum.TryParse<GiftCardStatus>(value, ignoreCase: true, out var parsed)
                ? parsed
                : throw new InvalidOperationException("wallet.giftcard.status_parse");

    /// <summary>جهت تعدیل.</summary>
    public static LedgerDirection ParseDirection(string value) =>
        Enum.TryParse<LedgerDirection>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new InvalidOperationException("wallet.adjustment.direction_invalid");
}
