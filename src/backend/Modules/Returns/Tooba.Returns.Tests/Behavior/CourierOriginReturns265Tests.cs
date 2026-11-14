using Tooba.BuildingBlocks;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;
using Xunit;

namespace Tooba.Returns.Tests.Behavior;

/// <summary>
/// TB-265: a courier-origin case REUSES the one physical origin receipt, keeps each line's recorded condition, and
/// advances through durable phases (obligation closed -> stock settled -> money released) exactly once. A damaged unit
/// is never recorded as saleable stock.
/// </summary>
public sealed class CourierOriginReturns265Tests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-13T10:00:00Z");

    private static ReturnRequest Case(params (Guid Line, decimal Quantity, ReturnItemCondition Condition)[] lines)
    {
        var id = Guid.NewGuid();
        return ReturnRequest.CreateCourierOrigin(
            id,
            () => Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "courier-return:abc",
            "tb265-idem",
            "بازگشت مرسوله",
            "IRR",
            lines.Select(x => (x.Line, x.Quantity, 1000m, (Guid?)null, x.Condition)),
            Now);
    }

    [Fact]
    public void Created_case_keeps_the_recorded_source_and_condition()
    {
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var request = Case((lineA, 2m, ReturnItemCondition.Usable), (lineB, 1m, ReturnItemCondition.Damaged));

        Assert.Equal(ReturnOrigin.CourierOrigin, request.Origin);
        Assert.Equal(CourierOriginClosurePhase.None, request.ClosurePhase);
        Assert.Equal("courier-return:abc", request.SourceOccurrenceKey);
        Assert.NotNull(request.SourceClaimId);
        Assert.All(request.Items, x => Assert.NotNull(x.SourceReceiptId));

        var usable = request.Items.Single(x => x.OrderLineId == lineA);
        var damaged = request.Items.Single(x => x.OrderLineId == lineB);
        Assert.Equal(ReturnItemCondition.Usable, usable.Condition);
        Assert.Equal(ReturnItemCondition.Damaged, damaged.Condition);
        // A damaged unit is non-saleable from the moment it is recorded.
        Assert.Equal(ReturnItemDisposition.DamagedNonSaleable, damaged.Disposition);
        Assert.Equal(3000m, request.RefundAmount);
    }

    [Fact]
    public void Courier_origin_requires_its_claim_receipt_and_occurrence()
    {
        Assert.Throws<InvalidOperationException>(() => ReturnRequest.CreateCourierOrigin(
            Guid.NewGuid(), () => Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "key", "idem", null, "IRR",
            [(Guid.NewGuid(), 1m, 1000m, (Guid?)null, ReturnItemCondition.Usable)], Now));

        Assert.Throws<InvalidOperationException>(() => ReturnRequest.CreateCourierOrigin(
            Guid.NewGuid(), () => Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), " ", "idem", null, "IRR",
            [(Guid.NewGuid(), 1m, 1000m, (Guid?)null, ReturnItemCondition.Usable)], Now));
    }

    [Fact]
    public void Closure_phases_are_monotonic_and_idempotent()
    {
        var request = Case((Guid.NewGuid(), 1m, ReturnItemCondition.Usable));

        request.MarkStockSettled(1m, Now);
        request.MarkObligationClosed(Now.AddMinutes(1));
        Assert.Equal(CourierOriginClosurePhase.StockSettled, request.ClosurePhase);
        Assert.NotNull(request.StockSettledAt);

        // Replays never move a phase backwards nor double-record the released quantity.
        request.MarkStockSettled(9m, Now.AddMinutes(2));
        request.MarkObligationClosed(Now.AddMinutes(2));
        Assert.Equal(1m, request.ReleasedHeldQuantity);
        Assert.Equal(Now, request.StockSettledAt);

        request.MarkRefundReleased(Now.AddMinutes(3));
        Assert.Equal(CourierOriginClosurePhase.RefundReleased, request.ClosurePhase);
    }

    [Fact]
    public void A_damaged_line_refuses_a_saleable_restock_disposition()
    {
        var request = Case((Guid.NewGuid(), 1m, ReturnItemCondition.Damaged));
        var item = request.Items.Single();

        Assert.Throws<ContractOperationException>(() => item.MarkDisposition(ReturnItemDisposition.SaleableRestock));
        Assert.Equal(ReturnItemDisposition.DamagedNonSaleable, item.Disposition);

        // A usable line can be restocked once, and a second disposition never overrides the first.
        var usable = Case((Guid.NewGuid(), 1m, ReturnItemCondition.Usable)).Items.Single();
        usable.MarkDisposition(ReturnItemDisposition.SaleableRestock);
        usable.MarkDisposition(ReturnItemDisposition.DamagedNonSaleable);
        Assert.Equal(ReturnItemDisposition.SaleableRestock, usable.Disposition);
    }

    [Fact]
    public void Ordinary_customer_returns_keep_their_existing_meaning()
    {
        var request = Case((Guid.NewGuid(), 1m, ReturnItemCondition.Usable));

        // Phase markers are no-ops for the delivered-customer RMA path: nothing about a legacy case changes.
        var ordinary = ReturnRequest.Create(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "legacy-idem",
            null,
            "IRR",
            [(Guid.NewGuid(), 1m, 500m, (Guid?)null)],
            Now);

        Assert.Equal(ReturnOrigin.CustomerDelivered, ordinary.Origin);
        ordinary.MarkObligationClosed(Now);
        ordinary.MarkStockSettled(1m, Now);
        Assert.Equal(CourierOriginClosurePhase.None, ordinary.ClosurePhase);
        Assert.Equal(0m, ordinary.ReleasedHeldQuantity);

        // And the courier case still reports its own origin.
        Assert.Equal(ReturnOrigin.CourierOrigin, request.Origin);
    }
}
