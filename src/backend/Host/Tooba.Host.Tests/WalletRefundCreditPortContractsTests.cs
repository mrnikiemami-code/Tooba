using Tooba.Wallet.Contracts.Dtos;
using Tooba.Wallet.Contracts.Payments;
using Tooba.Wallet.Contracts.Refunds;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>Characterization for Wallet refund-credit Contracts port used by Returns.</summary>
public sealed class WalletRefundCreditPortContractsTests
{
    [Fact]
    public void Wallet_refund_credit_result_excludes_ledger_entry()
    {
        var credit = new WalletRefundCreditResultDto(750m, true);
        Assert.Equal(750m, credit.Balance);
        Assert.True(credit.IdempotentReplay);
        Assert.Null(typeof(WalletRefundCreditResultDto).GetProperty("Entry"));
    }

    [Fact]
    public void Wallet_refund_credit_port_is_contracts_owned()
    {
        Assert.Equal("Tooba.Wallet.Contracts.Refunds", typeof(IWalletRefundCreditPort).Namespace);
        Assert.Equal("Tooba.Wallet.Contracts", typeof(IWalletRefundCreditPort).Assembly.GetName().Name);
    }
}
