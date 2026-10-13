using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Infrastructure.Persistence;
using Tooba.Persistence;
using Xunit;

namespace Tooba.Inventory.Tests.Behavior;

/// <summary>
/// Fail-closed outcomes that are decided before the database is touched: empty input, an absent
/// deadline, and a caller-owned ambient transaction this seam deliberately refuses. The DbContext
/// points at an unreachable host on purpose, so any accidental database access fails loudly here.
/// </summary>
public sealed class InventoryPaymentHoldExtensionTests
{
    private static readonly Guid ReservationId = Guid.Parse("44444444-4444-7444-8444-444444444444");

    [Fact]
    public async Task Empty_input_proves_nothing_and_never_extends()
    {
        await using var db = UnreachableContext();
        var directory = InventoryHoldExtensionTestContext.CreateDirectory(db);

        var result = await directory.ExtendHeldForPaymentAsync([], Future, CancellationToken.None);

        Assert.Equal(InventoryPaymentHoldExtensionOutcome.NotHeld, result.Outcome);
        Assert.False(result.AllHeldAndExtended);
        Assert.Equal(0, result.RequestedCount);
        Assert.Null(result.GuaranteedUntil);
        Assert.Empty(result.UnavailableReservationIds);
    }

    [Fact]
    public async Task Absent_deadline_is_rejected_before_any_database_access()
    {
        await using var db = UnreachableContext();
        var directory = InventoryHoldExtensionTestContext.CreateDirectory(db);

        var result = await directory.ExtendHeldForPaymentAsync([ReservationId], default, CancellationToken.None);

        Assert.Equal(InventoryPaymentHoldExtensionOutcome.DeadlineInvalid, result.Outcome);
        Assert.False(result.AllHeldAndExtended);
        Assert.Equal(1, result.RequestedCount);
        Assert.Null(result.GuaranteedUntil);
    }

    [Fact]
    public async Task Caller_owned_ambient_transaction_is_refused_without_touching_the_database()
    {
        await using var db = UnreachableContext();
        var directory = InventoryHoldExtensionTestContext.CreateDirectory(db);

        using var ambient = new TransactionScope(TransactionScopeOption.Required, TransactionScopeAsyncFlowOption.Enabled);
        var result = await directory.ExtendHeldForPaymentAsync([ReservationId], Future, CancellationToken.None);

        Assert.Equal(InventoryPaymentHoldExtensionOutcome.AmbientTransactionUnsupported, result.Outcome);
        Assert.False(result.AllHeldAndExtended);
        Assert.Equal(1, result.RequestedCount);
    }

    [Fact]
    public void Outcome_records_are_fail_closed_by_default()
    {
        Assert.False(InventoryPaymentHoldExtensionResult.NotHeld(2, [ReservationId]).AllHeldAndExtended);
        Assert.False(InventoryPaymentHoldExtensionResult.DeadlineInvalid(2).AllHeldAndExtended);
        Assert.False(InventoryPaymentHoldExtensionResult.AmbientTransactionUnsupported(2).AllHeldAndExtended);
        Assert.True(InventoryPaymentHoldExtensionResult.Extended(2, Future).AllHeldAndExtended);
        Assert.Null(InventoryPaymentHoldExtensionResult.Extended(2, null).GuaranteedUntil);
    }

    private static DateTimeOffset Future => DateTimeOffset.Parse("2099-01-01T00:00:00Z");

    private static InventoryDbContext UnreachableContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>();
        ToobaNpgsql.ConfigureModuleContext(
            options,
            "Host=127.0.0.1;Port=1;Username=unreachable;Password=unreachable;Database=unreachable;Timeout=1",
            InventoryDbContext.Schema,
            typeof(InventoryDbContext));
        return new InventoryDbContext(options.Options);
    }
}
