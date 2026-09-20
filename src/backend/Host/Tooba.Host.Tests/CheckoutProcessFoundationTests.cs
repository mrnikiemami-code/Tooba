using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure;
using Tooba.Order.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-TMAR-CHECKOUT-IMPL-W1 — process state + idempotency foundation.</summary>
public sealed class CheckoutProcessFoundationTests
{
    [Fact]
    public void Transitions_follow_allowed_graph_and_reject_invalid()
    {
        var now = DateTimeOffset.Parse("2026-09-20T10:00:00Z");
        var process = CheckoutProcess.Start(Guid.NewGuid(), "key-1", Guid.NewGuid(), "corr", now);
        Assert.Equal(CheckoutProcessStatus.Started, process.Status);
        process.MarkValidating(now);
        process.MarkInventoryReserving(now);
        var checkoutId = Guid.NewGuid();
        process.MarkOrderPersisting(checkoutId, now);
        Assert.Equal(checkoutId, process.CheckoutId);
        process.MarkCartCommitting(now);
        process.MarkPaymentPending(now);
        Assert.True(process.IsSubmitSucceeded);
        Assert.Throws<InvalidOperationException>(() => process.MarkFailed("x", now));
        Assert.Throws<InvalidOperationException>(() => process.MarkValidating(now));
    }

    [Fact]
    public async Task Duplicate_begin_after_success_returns_same_succeeded_process()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateDb(dbName);
        var tracker = new CheckoutProcessTracker(db, new UuidV7IdGenerator());
        var now = DateTimeOffset.UtcNow;
        var cartId = Guid.NewGuid();
        var first = await tracker.BeginAsync("idem-a", cartId, now, CancellationToken.None);
        tracker.MarkValidating(first, now);
        tracker.MarkInventoryReserving(first, now);
        tracker.MarkOrderPersisting(first, Guid.NewGuid(), now);
        tracker.MarkCartCommitting(first, now);
        tracker.MarkPaymentPending(first, now);
        await db.SaveChangesAsync();

        var second = await tracker.BeginAsync("idem-a", cartId, now, CancellationToken.None);
        Assert.Equal(first.ProcessId, second.ProcessId);
        Assert.True(second.IsSubmitSucceeded);
    }

    [Fact]
    public async Task Process_persists_and_survives_new_context()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var processId = Guid.Empty;
        var key = "idem-persist";
        var checkoutId = Guid.NewGuid();
        {
            await using var db = CreateDb(dbName);
            var tracker = new CheckoutProcessTracker(db, new UuidV7IdGenerator());
            var now = DateTimeOffset.UtcNow;
            var process = await tracker.BeginAsync(key, Guid.NewGuid(), now, CancellationToken.None);
            processId = process.ProcessId;
            tracker.MarkValidating(process, now);
            tracker.MarkInventoryReserving(process, now);
            tracker.MarkOrderPersisting(process, checkoutId, now);
            tracker.MarkCartCommitting(process, now);
            tracker.MarkPaymentPending(process, now);
            await db.SaveChangesAsync();
        }

        {
            await using var db = CreateDb(dbName);
            var tracker = new CheckoutProcessTracker(db, new UuidV7IdGenerator());
            var found = await tracker.FindSucceededBySubmissionKeyAsync(key, CancellationToken.None);
            Assert.NotNull(found);
            Assert.Equal(processId, found!.ProcessId);
            Assert.Equal(checkoutId, found.CheckoutId);
            Assert.Equal(CheckoutProcessStatus.PaymentPending, found.Status);
        }
    }

    [Fact]
    public async Task Rollback_clears_uncommitted_process()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var db = CreateDb(dbName);
        var tracker = new CheckoutProcessTracker(db, new UuidV7IdGenerator());
        var now = DateTimeOffset.UtcNow;
        var process = await tracker.BeginAsync("idem-roll", Guid.NewGuid(), now, CancellationToken.None);
        tracker.MarkValidating(process, now);
        // no SaveChanges → new context sees nothing
        await using var db2 = CreateDb(dbName);
        Assert.Empty(db2.CheckoutProcesses);
    }

    [Fact]
    public void Submit_path_still_uses_TransactionScope()
    {
        var root = FindRepoRoot();
        var executor = File.ReadAllText(Path.Combine(root, "src/backend/Modules/Order/Tooba.Order.Infrastructure/CheckoutSubmitExecutor.cs"));
        var directory = File.ReadAllText(Path.Combine(root, "src/backend/Modules/Order/Tooba.Order.Infrastructure/CheckoutDirectory.cs"));
        Assert.Contains("new TransactionScope(", executor, StringComparison.Ordinal);
        Assert.Contains("ICheckoutProcessTracker", directory, StringComparison.Ordinal);
        Assert.Contains("MarkPaymentPending", executor, StringComparison.Ordinal);
        Assert.Contains("CheckoutSubmitExecutor.ExecuteAsync", directory, StringComparison.Ordinal);
    }

    [Fact]
    public void Invalid_failed_after_payment_pending_rejected()
    {
        var now = DateTimeOffset.UtcNow;
        var p = CheckoutProcess.Start(Guid.NewGuid(), "k", Guid.NewGuid(), "c", now);
        p.MarkValidating(now);
        p.MarkInventoryReserving(now);
        p.MarkOrderPersisting(Guid.NewGuid(), now);
        p.MarkCartCommitting(now);
        p.MarkPaymentPending(now);
        var ex = Assert.Throws<InvalidOperationException>(() => p.MarkFailed("nope", now));
        Assert.Equal("checkout_process.transition.invalid", ex.Message);
    }

    private static OrderDbContext CreateDb(string? name = null)
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString("N"))
            .Options;
        return new OrderDbContext(options);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs/architecture/TOOBA-LOCKS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
