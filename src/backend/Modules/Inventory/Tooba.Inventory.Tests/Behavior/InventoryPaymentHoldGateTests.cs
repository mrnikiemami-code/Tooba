using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Infrastructure.Directories;
using Xunit;

namespace Tooba.Inventory.Tests.Behavior;

/// <summary>
/// Focused behavior for the payment-boundary hold gate verdict:
/// all-held, one-released, one-expired, one-missing and multi-line all-or-none.
/// The verdict is pure, so these tests need no database; PostgreSQL locking/enlistment itself is
/// separately recorded as UNVERIFIED in the task evidence.
/// </summary>
public sealed class InventoryPaymentHoldGateTests
{
    private static readonly Guid Held1 = Guid.Parse("11111111-1111-7111-8111-111111111111");
    private static readonly Guid Held2 = Guid.Parse("22222222-2222-7222-8222-222222222222");
    private static readonly Guid Held3 = Guid.Parse("33333333-3333-7333-8333-333333333333");

    [Fact]
    public void All_held_and_unexpired_reservations_verify()
    {
        var result = InventoryDirectory.EvaluatePaymentHoldGate([Held1, Held2, Held3], [Held1, Held2, Held3]);

        Assert.Equal(InventoryPaymentHoldGateOutcome.Verified, result.Outcome);
        Assert.True(result.AllHeld);
        Assert.Equal(3, result.RequestedCount);
        Assert.Equal(3, result.HeldCount);
        Assert.Empty(result.UnavailableReservationIds);
    }

    [Fact]
    public void One_released_reservation_fails_closed_and_names_the_id()
    {
        var result = InventoryDirectory.EvaluatePaymentHoldGate([Held1, Held2], [Held1]);

        Assert.Equal(InventoryPaymentHoldGateOutcome.NotHeld, result.Outcome);
        Assert.False(result.AllHeld);
        Assert.Equal(2, result.RequestedCount);
        Assert.Equal(0, result.HeldCount);
        Assert.Equal([Held2], result.UnavailableReservationIds);
    }

    [Fact]
    public void One_expired_reservation_fails_closed_and_names_the_id()
    {
        // The database clock decides expiry, so an expired row is simply absent from the live set.
        var result = InventoryDirectory.EvaluatePaymentHoldGate([Held1, Held2], [Held2]);

        Assert.Equal(InventoryPaymentHoldGateOutcome.NotHeld, result.Outcome);
        Assert.Equal([Held1], result.UnavailableReservationIds);
    }

    [Fact]
    public void One_missing_reservation_fails_closed_and_names_the_id()
    {
        var result = InventoryDirectory.EvaluatePaymentHoldGate([Held1, Held2, Held3], [Held1, Held2]);

        Assert.Equal(InventoryPaymentHoldGateOutcome.NotHeld, result.Outcome);
        Assert.Equal([Held3], result.UnavailableReservationIds);
    }

    [Fact]
    public void Multi_line_gate_is_all_or_none_and_never_partially_verifies()
    {
        var partial = InventoryDirectory.EvaluatePaymentHoldGate([Held1, Held2, Held3], [Held1, Held3]);
        Assert.Equal(InventoryPaymentHoldGateOutcome.NotHeld, partial.Outcome);
        Assert.False(partial.AllHeld);
        Assert.Equal([Held2], partial.UnavailableReservationIds);

        var complete = InventoryDirectory.EvaluatePaymentHoldGate([Held1, Held2, Held3], [Held3, Held1, Held2]);
        Assert.Equal(InventoryPaymentHoldGateOutcome.Verified, complete.Outcome);
        Assert.True(complete.AllHeld);
    }

    [Fact]
    public void Duplicates_and_caller_ordering_do_not_change_the_verdict()
    {
        var result = InventoryDirectory.EvaluatePaymentHoldGate(
            [Held2, Held1, Held2, Held1],
            [Held1, Held2]);

        Assert.Equal(InventoryPaymentHoldGateOutcome.Verified, result.Outcome);
        Assert.Equal(2, result.RequestedCount);
        Assert.Equal(2, result.HeldCount);
    }

    [Fact]
    public void Empty_request_proves_nothing_and_never_verifies()
    {
        var result = InventoryDirectory.EvaluatePaymentHoldGate([], []);

        Assert.Equal(InventoryPaymentHoldGateOutcome.NotHeld, result.Outcome);
        Assert.False(result.AllHeld);
        Assert.Equal(0, result.RequestedCount);
        Assert.Empty(result.UnavailableReservationIds);
    }

    [Fact]
    public void Empty_guid_is_never_live_even_if_reported_live()
    {
        var result = InventoryDirectory.EvaluatePaymentHoldGate([Held1, Guid.Empty], [Held1, Guid.Empty]);

        Assert.Equal(InventoryPaymentHoldGateOutcome.NotHeld, result.Outcome);
        Assert.Equal([Guid.Empty], result.UnavailableReservationIds);
    }

    [Fact]
    public void Ambient_transaction_failure_is_fail_closed_and_never_all_held()
    {
        var result = InventoryPaymentHoldGateResult.AmbientTransactionRequired(2);

        Assert.Equal(InventoryPaymentHoldGateOutcome.AmbientTransactionRequired, result.Outcome);
        Assert.False(result.AllHeld);
        Assert.Equal(0, result.HeldCount);
        Assert.Empty(result.UnavailableReservationIds);
    }
}
