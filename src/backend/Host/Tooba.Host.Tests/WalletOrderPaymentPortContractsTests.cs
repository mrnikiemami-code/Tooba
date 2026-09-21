using Tooba.Wallet.Contracts.Dtos;
using Tooba.Wallet.Contracts.Payments;
using Tooba.Wallet.Contracts.Refunds;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>Characterization for Wallet order-payment Contracts port used by Payment gateway.</summary>
public sealed class WalletOrderPaymentPortContractsTests
{
    [Fact]
    public void Wallet_checkout_quote_dto_shape_is_stable()
    {
        var quote = new WalletCheckoutQuoteDto(1000m, 800m, 200m, false, "IRR");
        Assert.Equal(1000m, quote.WalletBalance);
        Assert.Equal(800m, quote.MaxUsable);
        Assert.Equal(200m, quote.RemainingPayable);
        Assert.False(quote.CanPayFullyWithWallet);
        Assert.Equal("IRR", quote.Currency);
    }

    [Fact]
    public void Wallet_order_payment_debit_result_excludes_ledger_entry()
    {
        var debit = new WalletOrderPaymentDebitResultDto(500m, true);
        Assert.Equal(500m, debit.Balance);
        Assert.True(debit.IdempotentReplay);
        Assert.Null(typeof(WalletOrderPaymentDebitResultDto).GetProperty("Entry"));
    }
}
