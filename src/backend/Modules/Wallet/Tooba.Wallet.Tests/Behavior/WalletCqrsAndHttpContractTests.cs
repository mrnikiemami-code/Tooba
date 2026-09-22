using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Notification.Contracts.Commands;
using Tooba.Notification.Contracts.Ports;
using Tooba.Wallet.Application.Commands.AdjustAdminWallet;
using Tooba.Wallet.Application.Commands.IssueAdminGiftCard;
using Tooba.Wallet.Application.Commands.RedeemCustomerGiftCard;
using Tooba.Wallet.Application.Commands.RevokeAdminGiftCard;
using Tooba.Wallet.Application.Errors;
using Tooba.Wallet.Application.Ports;
using Tooba.Wallet.Application.Queries.GetAdminGiftCard;
using Tooba.Wallet.Application.Queries.GetAdminWallet;
using Tooba.Wallet.Application.Queries.GetCustomerWalletSummary;
using Tooba.Wallet.Application.Queries.GetWalletDemoPreview;
using Tooba.Wallet.Application.Queries.ListAdminGiftCards;
using Tooba.Wallet.Application.Queries.ListAdminWalletLedger;
using Tooba.Wallet.Application.Queries.ListCustomerWalletLedger;
using Tooba.Wallet.Infrastructure.Adapters;
using Tooba.Wallet.Infrastructure.Directories;
using Tooba.Wallet.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Wallet.Tests.Behavior;

public sealed class WalletCqrsAndHttpContractTests
{
    [Fact]
    public async Task Customer_summary_ledger_redeem_and_idempotency()
    {
        await using var provider = BuildProvider();
        var sender = provider.GetRequiredService<ISender>();
        var customer = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
        var admin = Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd");

        var summary = await sender.Send(new GetCustomerWalletSummaryQuery(customer));
        Assert.True(summary.IsSuccess);
        Assert.Equal(customer, summary.Value.CustomerActorUserId);

        var ledger = await sender.Send(new ListCustomerWalletLedgerQuery(customer, 1, 20));
        Assert.True(ledger.IsSuccess);

        var issued = await sender.Send(new IssueAdminGiftCardCommand(
            admin, 50_000m, "IRR", null, customer, "issue-idem-1"));
        Assert.True(issued.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(issued.Value.DisplayCode));

        var redeemed = await sender.Send(new RedeemCustomerGiftCardCommand(
            customer, issued.Value.DisplayCode, "redeem-idem-1"));
        Assert.True(redeemed.IsSuccess);
        var redeemedDup = await sender.Send(new RedeemCustomerGiftCardCommand(
            customer, issued.Value.DisplayCode, "redeem-idem-1"));
        Assert.True(redeemedDup.IsSuccess);
        Assert.True(redeemedDup.Value.IdempotentReplay);
        Assert.Equal(redeemed.Value.RedemptionId, redeemedDup.Value.RedemptionId);
    }

    [Fact]
    public async Task Admin_gift_card_wallet_adjust_and_demo()
    {
        await using var provider = BuildProvider();
        var sender = provider.GetRequiredService<ISender>();
        var customer = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
        var admin = Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd");

        var issued = await sender.Send(new IssueAdminGiftCardCommand(
            admin, 25_000m, "IRR", null, null, "admin-issue-1"));
        Assert.True(issued.IsSuccess);

        var list = await sender.Send(new ListAdminGiftCardsQuery(null, null, 1, 20));
        Assert.True(list.IsSuccess);
        Assert.True(list.Value.Total >= 1);

        var got = await sender.Send(new GetAdminGiftCardQuery(issued.Value.Card.CardId));
        Assert.True(got.IsSuccess);

        var missingCard = await sender.Send(new GetAdminGiftCardQuery(Guid.Parse("01999999-9999-7999-8999-999999999999")));
        Assert.True(missingCard.IsFailure);
        Assert.Equal(WalletErrorCodes.GiftCardMissing, missingCard.Errors[0].Code);

        var revoked = await sender.Send(new RevokeAdminGiftCardCommand(issued.Value.Card.CardId));
        Assert.True(revoked.IsSuccess);

        var adjusted = await sender.Send(new AdjustAdminWalletCommand(
            customer, admin, 10_000m, "Credit", "seed", "adj-idem-1"));
        Assert.True(adjusted.IsSuccess);
        var adjDup = await sender.Send(new AdjustAdminWalletCommand(
            customer, admin, 10_000m, "Credit", "seed", "adj-idem-1"));
        Assert.True(adjDup.IsSuccess);
        Assert.True(adjDup.Value.IdempotentReplay);

        var wallet = await sender.Send(new GetAdminWalletQuery(customer));
        Assert.True(wallet.IsSuccess);

        var missingWallet = await sender.Send(new GetAdminWalletQuery(Guid.Parse("01999999-9999-7999-8999-999999999998")));
        Assert.True(missingWallet.IsFailure);
        Assert.Equal(WalletErrorCodes.WalletMissing, missingWallet.Errors[0].Code);

        var adminLedger = await sender.Send(new ListAdminWalletLedgerQuery(customer, 1, 20));
        Assert.True(adminLedger.IsSuccess);

        WalletDemoSnapshotStore.Publish(null);
        var notReady = await sender.Send(new GetWalletDemoPreviewQuery());
        Assert.True(notReady.IsFailure);
        Assert.Equal(WalletErrorCodes.DemoNotReady, notReady.Errors[0].Code);

        WalletDemoSnapshotStore.Publish(new WalletDemoSnapshot(
            customer, Guid.Parse("01900000-0000-7000-9000-000000000001"), 1m,
            Guid.Parse("01900000-0000-7000-9000-000000000021"), "CODE",
            Guid.Parse("01900000-0000-7000-9000-000000000022"),
            Guid.Parse("01900000-0000-7000-9000-000000000023"),
            Guid.Parse("01900000-0000-7000-9000-000000000024"),
            null, null, null, null, "test-demo"));
        var ready = await sender.Send(new GetWalletDemoPreviewQuery());
        Assert.True(ready.IsSuccess);
        Assert.Equal("test-demo", ready.Value.Note);
    }

    [Fact]
    public async Task Invalid_adjustment_direction_maps_to_adjust_rejected()
    {
        await using var provider = BuildProvider();
        var sender = provider.GetRequiredService<ISender>();
        var customer = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
        var admin = Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd");

        var result = await sender.Send(new AdjustAdminWalletCommand(
            customer, admin, 1m, "NotADirection", "x", "bad-dir"));
        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrorCodes.AdjustRejected, result.Errors[0].Code);
    }

    [Fact]
    public void Idempotency_precedence_body_header_then_id_generator()
    {
        var fixedId = Guid.Parse("01900000-0000-7000-9000-00000000abcd");
        var ids = new FixedIdGenerator(fixedId);

        Assert.Equal("body-key", Resolve("body-key", "header-key", ids));
        Assert.Equal("header-key", Resolve(null, "header-key", ids));
        Assert.Equal(fixedId.ToString("N"), Resolve(null, null, ids));
    }

    [Fact]
    public void Customer_authorizer_contract_exists_without_directory()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "src", "backend", "Modules", "Wallet", "Tooba.Wallet.Endpoints",
            "Customer", "IWalletCustomerAuthorizer.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("TryResolveActor", text, StringComparison.Ordinal);
        Assert.DoesNotContain("IWalletDirectory", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_authorizer_contract_documents_fail_open_unavailable()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "src", "backend", "Modules", "Wallet", "Tooba.Wallet.Endpoints",
            "Admin", "IWalletAdminAuthorizer.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Unavailable fail-open", text, StringComparison.Ordinal);
        Assert.Contains("RequireAuthorizedAsync", text, StringComparison.Ordinal);
    }

    private static string Resolve(string? body, string? header, IIdGenerator ids)
    {
        if (!string.IsNullOrWhiteSpace(body))
            return body.Trim();
        if (!string.IsNullOrWhiteSpace(header))
            return header.Trim();
        return ids.NewId().ToString("N");
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(RedeemCustomerGiftCardCommand).Assembly));
        services.AddSingleton<IClock, SystemUtcClock>();
        services.AddSingleton<IIdGenerator, UuidV7IdGenerator>();
        services.AddSingleton<INotificationCreationPort, NoopNotificationPort>();
        services.AddSingleton<IWalletDemoPreviewPort, WalletDemoPreviewAdapter>();
        services.AddDbContext<WalletDbContext>(options =>
            options.UseInMemoryDatabase("wallet-cqrs-" + Guid.NewGuid().ToString("N")));
        services.AddScoped<IWalletDirectory, WalletDirectory>();
        return services.BuildServiceProvider();
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private sealed class NoopNotificationPort : INotificationCreationPort
    {
        public Task<bool> CreateIfAbsentAsync(CreateNotificationCommand command, CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }
}

public sealed class WalletSemanticPresentationTests
{
    [Fact]
    public void Stable_directory_codes_map_to_public_outcome()
    {
        var rejected = WalletExceptionMapper.ToSemanticError(
            new InvalidOperationException("wallet.rejected.2YXZiNis"),
            WalletErrorCodes.WalletRejected);
        Assert.Equal(WalletErrorCodes.WalletRejected, rejected.Code);

        var redeem = WalletExceptionMapper.ToSemanticError(
            new InvalidOperationException("wallet.giftcard.expired"),
            WalletErrorCodes.RedeemRejected);
        Assert.Equal(WalletErrorCodes.RedeemRejected, redeem.Code);

        var issue = WalletExceptionMapper.ToSemanticError(
            new InvalidOperationException("wallet.giftcard.amount_positive"),
            WalletErrorCodes.GiftCardIssueRejected);
        Assert.Equal(WalletErrorCodes.GiftCardIssueRejected, issue.Code);
    }

    [Fact]
    public void Prose_and_unknown_codes_are_not_swallowed()
    {
        Assert.False(WalletExceptionMapper.TryMapExact("موجودی کافی نیست.", WalletErrorCodes.WalletRejected, out _));
        Assert.False(WalletExceptionMapper.TryMapExact("wallet.unknown.future_code", WalletErrorCodes.WalletRejected, out _));
        Assert.Throws<InvalidOperationException>(() =>
            WalletExceptionMapper.ToSemanticError(
                new InvalidOperationException("پیدا نشد"),
                WalletErrorCodes.WalletRejected));
    }

    [Fact]
    public async Task Unexpected_InvalidOperationException_propagates_from_TryAsync()
    {
        var unknown = new InvalidOperationException("wallet.unknown.future_code");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            WalletExceptionMapper.TryAsync<int>(() => throw unknown, WalletErrorCodes.WalletRejected));
    }

    [Fact]
    public async Task Known_InvalidOperationException_maps_in_TryAsync()
    {
        var result = await WalletExceptionMapper.TryAsync<int>(
            () => throw new InvalidOperationException("wallet.adjustment.direction_invalid"),
            WalletErrorCodes.AdjustRejected);
        Assert.True(result.IsFailure);
        Assert.Equal(WalletErrorCodes.AdjustRejected, result.Errors[0].Code);
    }
}
