using Tooba.Wallet.Domain.Aggregates;
using Tooba.Wallet.Domain.ValueObjects;
using Xunit;

namespace Tooba.Wallet.Tests.Behavior;

public sealed class WalletAccountFactoryBehaviorTests
{
    [Fact]
    public void Create_requires_explicit_ids_and_uses_caller_now()
    {
        var now = DateTimeOffset.Parse("2026-03-21T12:00:00Z");
        var accountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var customerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var account = WalletAccount.Create(accountId, customerId, "IRR", now);
        Assert.Equal(accountId, account.AccountId);
        Assert.Equal(customerId, account.CustomerActorUserId);
        Assert.Equal(WalletAccountStatus.Active, account.Status);
        Assert.Equal(now, account.CreatedAt);
    }
}
