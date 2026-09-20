using Tooba.Wallet.Contracts;
using Tooba.Wallet.Domain;
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
        Assert.Equal("ارز الزامی است.", ex1.Message);
        var ex2 = Assert.Throws<InvalidOperationException>(() => WalletAccount.NormalizeCurrency(input!));
        Assert.Equal("ارز الزامی است.", ex2.Message);
    }

    [Theory]
    [InlineData("AB")]
    [InlineData("ABCDEFGHI")]
    public void Normalize_rejects_invalid_length(string input)
    {
        var ex1 = Assert.Throws<InvalidOperationException>(() => WalletCurrency.Normalize(input));
        Assert.Equal("ارز نامعتبر است.", ex1.Message);
        var ex2 = Assert.Throws<InvalidOperationException>(() => WalletAccount.NormalizeCurrency(input));
        Assert.Equal("ارز نامعتبر است.", ex2.Message);
    }

    [Fact]
    public void Wallet_gateway_compose_uses_normalized_currency()
    {
        var reference = Tooba.Payment.Infrastructure.WalletPaymentGateway.ComposeReference(
            Guid.Parse("11111111-1111-7111-8111-111111111111"),
            Guid.Parse("22222222-2222-7222-8222-222222222222"),
            1000m,
            WalletCurrency.Normalize("irr"));
        Assert.Contains("|IRR", reference, StringComparison.Ordinal);
    }
}
