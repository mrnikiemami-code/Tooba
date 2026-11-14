using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Domain.ValueObjects;
using Xunit;

namespace Tooba.Inventory.Tests.Behavior;

/// <summary>
/// TB-265: an approved courier-origin return releases ONLY the unconsumed held quantity of the corresponding line.
/// Consume is not release, a fully released reservation answers zero, and an over-request can never release more than
/// the reservation actually holds.
/// </summary>
public sealed class ReturnObligationRelease265Tests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-13T10:00:00Z");

    private static StockReservation Reservation(decimal quantity, StockReservationStatus status = StockReservationStatus.Held)
    {
        var reservation = StockReservation.Hold(
            Guid.NewGuid(),
            Guid.NewGuid(),
            quantity,
            "tb265-test",
            "hold-key",
            Now,
            null);
        if (status != StockReservationStatus.Held)
        {
            reservation.MoveTo(status, Now);
        }

        return reservation;
    }

    [Fact]
    public void A_held_quantity_is_released_partially_and_the_sibling_hold_is_untouched()
    {
        var reservation = Reservation(3m);

        var released = reservation.ReleasePartially(1m, Now);

        Assert.Equal(1m, released);
        Assert.Equal(2m, reservation.Quantity);
        // Still held: the rest of THIS order's obligation remains reserved.
        Assert.Equal(StockReservationStatus.Held, reservation.Status);
    }

    [Fact]
    public void Releasing_the_whole_held_quantity_marks_the_reservation_released()
    {
        var reservation = Reservation(2m);

        Assert.Equal(2m, reservation.ReleasePartially(2m, Now));
        Assert.Equal(0m, reservation.Quantity);
        Assert.Equal(StockReservationStatus.Released, reservation.Status);

        // A retried release on a non-held reservation answers zero instead of releasing stock twice.
        Assert.Equal(0m, reservation.ReleasePartially(2m, Now.AddMinutes(1)));
        Assert.Equal(0m, reservation.Quantity);
    }

    [Fact]
    public void A_consumed_sale_reservation_is_never_released()
    {
        // The original sale stock was consumed by dispatch; "release" must be a documented no-op, never a resurrection.
        var consumed = Reservation(2m, StockReservationStatus.Consumed);

        Assert.Equal(0m, consumed.ReleasePartially(2m, Now));
        Assert.Equal(2m, consumed.Quantity);
        Assert.Equal(StockReservationStatus.Consumed, consumed.Status);
    }

    [Fact]
    public void An_over_request_can_never_release_more_than_the_reservation_holds()
    {
        var reservation = Reservation(1m);

        Assert.Equal(1m, reservation.ReleasePartially(5m, Now));
        Assert.Equal(0m, reservation.Quantity);
        Assert.Equal(StockReservationStatus.Released, reservation.Status);
    }

    [Fact]
    public void A_non_positive_release_is_refused_without_changing_the_hold()
    {
        var reservation = Reservation(2m);

        Assert.Equal(0m, reservation.ReleasePartially(0m, Now));
        Assert.Equal(0m, reservation.ReleasePartially(-1m, Now));
        Assert.Equal(2m, reservation.Quantity);
        Assert.Equal(StockReservationStatus.Held, reservation.Status);
    }
}
