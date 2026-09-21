using Tooba.Wallet.Contracts.Dtos;
using Tooba.Wallet.Contracts.Payments;
using Tooba.Wallet.Contracts.Refunds;
using Tooba.Wallet.Domain.Aggregates;
using Tooba.Wallet.Domain.ValueObjects;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// Characterization of wallet currency normalization before/after Contracts extraction.
/// </summary>
public sealed class WalletCurrencyContractsTests
{
    [Theory]
    [InlineData("irr", "IRR")]
    [InlineData(" IRR ", "IRR")]
    [InlineData("USDT", "USDT")]
    [InlineData("ABCDEFGH", "ABCDEFGH")]
    public void Normalize_trims_and_uppercases_valid_codes(string input, string expected)
    {
        Assert.Equal(expected, WalletCurrency.Normalize(input));
        Assert.Equal(expected, WalletAccount.NormalizeCurrency(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_rejects_missing_currency(string? input)
    {
        var ex1 = Assert.Throws<InvalidOperationException>(() => WalletCurrency.Normalize(input!));
        Assert.Equal("wallet.currency_required", ex1.Message);
        var ex2 = Assert.Throws<InvalidOperationException>(() => WalletAccount.NormalizeCurrency(input!));
        Assert.Equal("wallet.currency_required", ex2.Message);
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("ABCDEFGHI")]
    public void Normalize_rejects_invalid_length(string input)
    {
        var ex1 = Assert.Throws<InvalidOperationException>(() => WalletCurrency.Normalize(input));
        Assert.Equal("wallet.currency_invalid", ex1.Message);
        var ex2 = Assert.Throws<InvalidOperationException>(() => WalletAccount.NormalizeCurrency(input));
        Assert.Equal("wallet.currency_invalid", ex2.Message);
    }

    [Fact]
    public void Wallet_gateway_compose_uses_normalized_currency()
    {
        var reference = Tooba.Payment.Infrastructure.Providers.WalletPaymentGateway.ComposeReference(
            Guid.Parse("11111111-1111-7111-8111-111111111111"),
            Guid.Parse("22222222-2222-7222-8222-222222222222"),
            1000m,
            WalletCurrency.Normalize("irr"));
        Assert.Contains("|IRR", reference, StringComparison.Ordinal);
    }
}
