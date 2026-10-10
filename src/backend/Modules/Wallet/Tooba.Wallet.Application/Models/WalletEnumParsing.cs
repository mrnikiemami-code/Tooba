using Tooba.BuildingBlocks;
using Tooba.Wallet.Contracts.Errors;
using Tooba.Wallet.Application.Customer.Models;
using Tooba.Wallet.Domain.ValueObjects;

namespace Tooba.Wallet.Application.Models;

/// <summary>Parses Application-boundary enums.</summary>
public static class WalletEnumParsing
{
    /// <summary>Gift-card status filter.</summary>
    public static GiftCardStatus? TryParseGiftCardStatus(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : Enum.TryParse<GiftCardStatus>(value, ignoreCase: true, out var parsed)
                ? parsed
                : throw new ContractOperationException(WalletErrorCodes.GiftCardStatusParse);

    /// <summary>Adjustment direction.</summary>
    public static LedgerDirection ParseDirection(string value) =>
        Enum.TryParse<LedgerDirection>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new ContractOperationException(WalletErrorCodes.AdjustmentDirectionInvalid);
}
