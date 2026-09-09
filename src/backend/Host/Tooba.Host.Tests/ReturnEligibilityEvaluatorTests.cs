using Microsoft.EntityFrameworkCore;
using Tooba.Fulfillment.Application;
using Tooba.Order.Application;
using Tooba.Returns.Application;
using Tooba.Returns.Infrastructure;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// unit تست‌های ReturnEligibilityEvaluator با fakes — بدون Host کامل.
/// </summary>
public sealed class ReturnEligibilityEvaluatorTests
{
    [Fact]
    public async Task Eligible_within_window_when_delivered_and_remaining()
    {
        var sellerOrderId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var checkoutId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
        var lineId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1");
        var deliveredAt = DateTimeOffset.UtcNow.AddDays(-5);
        var evaluator = new ReturnEligibilityEvaluator(
            new FakeOrderReturnReader(new OrderReturnContextSnapshot(
                sellerOrderId,
                checkoutId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                true,
                "IRR",
                [new OrderReturnLineSnapshot(lineId, 2, 1000m, "IRR", null, true, 7)])),
            new FakeFulfillmentReturnReader(new FulfillmentReturnEligibilitySnapshot(
                sellerOrderId,
                new Dictionary<Guid, decimal> { [lineId] = 2 },
                deliveredAt)),
            CreateEmptyReturnsDb());

        var result = await evaluator.EvaluateAsync(sellerOrderId, CancellationToken.None);
        Assert.True(result.Eligible);
        Assert.Equal(ReturnEligibilityReasonCodes.Eligible, result.ReasonCode);
        Assert.Equal(deliveredAt.AddDays(7), result.EligibleUntil);
        Assert.Equal(2, result.Lines.Single().RemainingReturnableQuantity);
    }

    [Fact]
    public async Task Ineligible_when_window_expired()
    {
        var sellerOrderId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");
        var checkoutId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
        var lineId = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2");
        var deliveredAt = DateTimeOffset.UtcNow.AddDays(-40);
        var evaluator = new ReturnEligibilityEvaluator(
            new FakeOrderReturnReader(new OrderReturnContextSnapshot(
                sellerOrderId,
                checkoutId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                true,
                "IRR",
                [new OrderReturnLineSnapshot(lineId, 1, 1000m, "IRR", null, true, 7)])),
            new FakeFulfillmentReturnReader(new FulfillmentReturnEligibilitySnapshot(
                sellerOrderId,
                new Dictionary<Guid, decimal> { [lineId] = 1 },
                deliveredAt)),
            CreateEmptyReturnsDb());

        var result = await evaluator.EvaluateAsync(sellerOrderId, CancellationToken.None);
        Assert.False(result.Eligible);
        Assert.Equal(ReturnEligibilityReasonCodes.WindowExpired, result.ReasonCode);
    }

    [Fact]
    public void Evaluator_source_never_references_settlement()
    {
        var root = FindRepoRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "backend",
            "Modules",
            "Returns",
            "Tooba.Returns.Infrastructure",
            "ReturnEligibilityEvaluator.cs"));
        Assert.DoesNotContain("ISettlement", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SettlementDirectory", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AdjustFromRefund", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ReturnWindow", source, StringComparison.Ordinal);
    }

    private static Tooba.Returns.Infrastructure.Persistence.ReturnsDbContext CreateEmptyReturnsDb()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Tooba.Returns.Infrastructure.Persistence.ReturnsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new Tooba.Returns.Infrastructure.Persistence.ReturnsDbContext(options);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private sealed class FakeOrderReturnReader(OrderReturnContextSnapshot? snapshot) : IOrderReturnReader
    {
        public Task<OrderReturnContextSnapshot?> GetReturnContextAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult(snapshot);
    }

    private sealed class FakeFulfillmentReturnReader(FulfillmentReturnEligibilitySnapshot? snapshot)
        : IFulfillmentReturnReader
    {
        public Task<FulfillmentReturnEligibilitySnapshot?> GetEligibilityAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult(snapshot);
    }
}
