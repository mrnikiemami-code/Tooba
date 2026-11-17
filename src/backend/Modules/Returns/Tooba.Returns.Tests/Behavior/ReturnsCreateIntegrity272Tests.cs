using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Contracts.Returns;
using Tooba.Order.Contracts.Returns;
using Tooba.Payment.Contracts.Returns;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Returns.Application.Validation;
using Tooba.Returns.Contracts.Errors;
using Tooba.Returns.Domain.Aggregates;
using Tooba.Returns.Domain.ValueObjects;
using Tooba.Returns.Infrastructure.Directories;
using Tooba.Returns.Infrastructure.Evaluators;
using Tooba.Returns.Infrastructure.Observability;
using Tooba.Returns.Infrastructure.Persistence;
using Tooba.Wallet.Contracts.Dtos;
using Tooba.Wallet.Contracts.Refunds;
using Xunit;

namespace Tooba.Returns.Tests.Behavior;

/// <summary>
/// TB-272 — the ordinary creation contract at its real owning boundary (<see cref="ReturnDirectory"/>, the aggregate
/// and the transport validator): authorized exact replay, one line per order line, a refund-failed case that keeps its
/// return capacity, and the serialization identity that makes the quantity decision atomic under concurrency.
/// </summary>
public sealed class ReturnsCreateIntegrity272Tests
{
    private static readonly DateTimeOffset DeliveredAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

    // ------------------------------------------------------------------ A: authorized, exact replay

    [Fact]
    public async Task Same_key_submitted_for_a_different_order_is_a_typed_conflict_with_zero_new_case()
    {
        var customer = Guid.NewGuid();
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var orderA = Guid.NewGuid();
        var orderB = Guid.NewGuid();
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));
        var directory = BuildDirectory(
            db,
            clock,
            [BuildOrder(orderA, customer, 2m, [lineA]), BuildOrder(orderB, customer, 2m, [lineB])],
            [BuildFulfillment(orderA, (lineA, 2m)), BuildFulfillment(orderB, (lineB, 2m))]);

        var created = await directory.CreateAsync(
            Command(orderA, customer, "tb272-order", [new ReturnLineCommand(lineA, 1m)]),
            CancellationToken.None);

        // The SAME key addresses a DIFFERENT order: the stored case must not be handed back.
        var conflict = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
            Command(orderB, customer, "tb272-order", [new ReturnLineCommand(lineB, 1m)]),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.IdempotencyConflict, conflict.Code);

        Assert.Equal(1, await db.ReturnRequests.CountAsync());
        Assert.Equal(created.ReturnRequestId, (await db.ReturnRequests.SingleAsync()).ReturnRequestId);
    }

    [Fact]
    public async Task A_non_owner_is_refused_before_any_stored_snapshot_is_answered()
    {
        var customer = Guid.NewGuid();
        var stranger = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));
        var directory = BuildDirectory(
            db,
            clock,
            [BuildOrder(sellerOrderId, customer, 2m, [lineId])],
            [BuildFulfillment(sellerOrderId, (lineId, 2m))]);

        await directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272-owner", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None);

        // The customer path authorizes BEFORE the stored-replay mapping.
        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
            Command(sellerOrderId, stranger, "tb272-owner", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.NotOwner, refused.Code);

        // The admin path is authorized by its own route authority (not by ownership); it still answers only the
        // OWNING customer's case, so the stored snapshot never becomes the admin actor's request.
        var adminReplay = await directory.CreateAdminInitiatedAsync(
            Command(sellerOrderId, stranger, "tb272-owner", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None);
        Assert.Equal(customer, adminReplay.RequestedByUserId);

        // A key that addresses a DIFFERENT order is a typed conflict, never a borrowed case.
        var otherOrder = Guid.NewGuid();
        var otherLine = Guid.NewGuid();
        var other = BuildDirectory(
            db,
            clock,
            [BuildOrder(otherOrder, customer, 1m, [otherLine])],
            [BuildFulfillment(otherOrder, (otherLine, 1m))]);
        var conflict = await Assert.ThrowsAsync<ContractOperationException>(() => other.CreateAsync(
            Command(otherOrder, customer, "tb272-owner", [new ReturnLineCommand(otherLine, 1m)]),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.IdempotencyConflict, conflict.Code);

        Assert.Equal(1, await db.ReturnRequests.CountAsync());
    }

    [Fact]
    public async Task Exact_replay_after_the_window_elapsed_answers_the_same_case_once()
    {
        var customer = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));
        var directory = BuildDirectory(
            db,
            clock,
            [BuildOrder(sellerOrderId, customer, 2m, [lineId], windowDays: 7)],
            [BuildFulfillment(sellerOrderId, (lineId, 2m))]);

        var first = await directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272-elapsed", [new ReturnLineCommand(lineId, 2m)]),
            CancellationToken.None);

        // Far beyond the 7-day captured window AND with the whole quantity already occupied by this very case: the
        // identical intent is still the same request, because replay answers the recorded fact instead of re-deciding.
        clock.UtcNow = DeliveredAt.AddDays(90);
        var replay = await directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272-elapsed", [new ReturnLineCommand(lineId, 2m)]),
            CancellationToken.None);

        Assert.Equal(first.ReturnRequestId, replay.ReturnRequestId);
        Assert.Equal(first.RefundAmount, replay.RefundAmount);
        Assert.Single(replay.Items);
        Assert.Equal(1, await db.ReturnRequests.CountAsync());
        Assert.Equal(1, await db.ReturnItems.CountAsync());
    }

    [Fact]
    public async Task Reordered_lines_are_the_same_intent_and_replay_the_same_case()
    {
        var customer = Guid.NewGuid();
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));
        var directory = BuildDirectory(
            db,
            clock,
            [BuildOrder(sellerOrderId, customer, 3m, [lineA, lineB])],
            [BuildFulfillment(sellerOrderId, (lineA, 3m), (lineB, 3m))]);

        var first = await directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272-order-of-items",
                [new ReturnLineCommand(lineA, 1m), new ReturnLineCommand(lineB, 2m)]),
            CancellationToken.None);
        var replay = await directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272-order-of-items",
                [new ReturnLineCommand(lineB, 2m), new ReturnLineCommand(lineA, 1m)]),
            CancellationToken.None);

        Assert.Equal(first.ReturnRequestId, replay.ReturnRequestId);
        Assert.Equal(2, await db.ReturnItems.CountAsync());
    }

    [Fact]
    public async Task Changed_quantity_destination_or_reason_conflicts_with_zero_new_case()
    {
        var customer = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));
        var directory = BuildDirectory(
            db,
            clock,
            [BuildOrder(sellerOrderId, customer, 3m, [lineId])],
            [BuildFulfillment(sellerOrderId, (lineId, 3m))]);

        var created = await directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272-intent", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None);

        var quantity = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272-intent", [new ReturnLineCommand(lineId, 2m)]),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.IdempotencyConflict, quantity.Code);

        var destination = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272-intent", [new ReturnLineCommand(lineId, 1m)],
                RefundDestination.Wallet),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.IdempotencyConflict, destination.Code);

        var lineSet = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272-intent",
                [new ReturnLineCommand(lineId, 1m), new ReturnLineCommand(Guid.NewGuid(), 1m)]),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.IdempotencyConflict, lineSet.Code);

        var reason = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
            new CreateReturnCommand(
                sellerOrderId, customer, "tb272-intent", "دلیل دیگر", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.IdempotencyConflict, reason.Code);

        Assert.Equal(1, await db.ReturnRequests.CountAsync());
        Assert.Equal(1, await db.ReturnItems.CountAsync());
        var stored = await db.ReturnRequests.SingleAsync();
        Assert.Equal(created.ReturnRequestId, stored.ReturnRequestId);
        Assert.Equal(1m, stored.RefundAmount / 1000m);
        Assert.Equal(RefundDestination.OriginalPayment, stored.RefundDestination);
    }

    [Fact]
    public async Task Whitespace_only_reason_difference_is_the_same_intent()
    {
        var customer = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));
        var directory = BuildDirectory(
            db,
            clock,
            [BuildOrder(sellerOrderId, customer, 2m, [lineId])],
            [BuildFulfillment(sellerOrderId, (lineId, 2m))]);

        var first = await directory.CreateAsync(
            new CreateReturnCommand(sellerOrderId, customer, "  tb272-trim  ", "  خراب است  ",
                [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None);
        var replay = await directory.CreateAsync(
            new CreateReturnCommand(sellerOrderId, customer, "tb272-trim", "خراب است",
                [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None);

        Assert.Equal(first.ReturnRequestId, replay.ReturnRequestId);
        Assert.Equal(1, await db.ReturnRequests.CountAsync());
    }

    [Fact]
    public async Task A_stored_courier_origin_key_is_not_replayed_as_an_ordinary_case()
    {
        var customer = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));

        var courierCase = ReturnRequest.CreateCourierOrigin(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            customer,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "courier-return:tb272",
            "tb272-courier-key",
            "مرجوعی مرسوله",
            "IRR",
            [(lineId, 1m, 1000m, (Guid?)null, ReturnItemCondition.Usable)],
            clock.UtcNow);
        db.ReturnRequests.Add(courierCase);
        db.ReturnItems.AddRange(courierCase.Items);
        await db.SaveChangesAsync();

        var directory = BuildDirectory(
            db,
            clock,
            [BuildOrder(sellerOrderId, customer, 3m, [lineId])],
            [BuildFulfillment(sellerOrderId, (lineId, 3m))]);

        // A courier-origin case has no ordinary creation intent, so its key can never answer an ordinary request.
        var conflict = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272-courier-key", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None));

        Assert.Equal(ReturnsErrorCodes.IdempotencyConflict, conflict.Code);
        Assert.Equal(1, await db.ReturnRequests.CountAsync());
        Assert.Equal(ReturnOrigin.CourierOrigin, (await db.ReturnRequests.SingleAsync()).Origin);
    }

    [Fact]
    public async Task Admin_and_customer_creation_share_the_same_exact_intent_rule()
    {
        var customer = Guid.NewGuid();
        var admin = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));
        var directory = BuildDirectory(
            db,
            clock,
            [BuildOrder(sellerOrderId, customer, 3m, [lineId])],
            [BuildFulfillment(sellerOrderId, (lineId, 3m))]);

        // The admin path creates the case for the OWNING customer, not for the admin actor.
        var created = await directory.CreateAdminInitiatedAsync(
            Command(sellerOrderId, admin, "tb272-admin", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None);
        Assert.Equal(customer, created.RequestedByUserId);

        // The same intent replayed by the customer is the SAME case...
        var customerReplay = await directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272-admin", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None);
        Assert.Equal(created.ReturnRequestId, customerReplay.ReturnRequestId);

        // ...and a changed intent is a conflict on BOTH paths.
        var adminConflict = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAdminInitiatedAsync(
            Command(sellerOrderId, admin, "tb272-admin", [new ReturnLineCommand(lineId, 2m)]),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.IdempotencyConflict, adminConflict.Code);

        var customerConflict = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272-admin", [new ReturnLineCommand(lineId, 2m)]),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.IdempotencyConflict, customerConflict.Code);

        Assert.Equal(1, await db.ReturnRequests.CountAsync());
    }

    // ------------------------------------------------------------------ B: one line per order line

    [Fact]
    public async Task Duplicate_order_lines_are_refused_by_the_directory_and_by_the_aggregate()
    {
        var customer = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));
        var directory = BuildDirectory(
            db,
            clock,
            [BuildOrder(sellerOrderId, customer, 4m, [lineId])],
            [BuildFulfillment(sellerOrderId, (lineId, 4m))]);

        // Same line twice with the SAME quantity would double the refund for one physical line...
        var same = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272-dup-1",
                [new ReturnLineCommand(lineId, 1m), new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.DuplicateLine, same.Code);

        // ...and twice with DIFFERENT quantities would pass each per-line check separately.
        var differing = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272-dup-2",
                [new ReturnLineCommand(lineId, 1m), new ReturnLineCommand(lineId, 3m)]),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.DuplicateLine, differing.Code);

        Assert.Equal(0, await db.ReturnRequests.CountAsync());

        // The aggregate carries the same invariant, so no future caller can bypass the Directory.
        var aggregate = Assert.Throws<InvalidOperationException>(() => ReturnRequest.Create(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            customer,
            "tb272-dup-aggregate",
            null,
            "IRR",
            [(lineId, 1m, 1000m, (Guid?)null), (lineId, 2m, 1000m, (Guid?)null)],
            clock.UtcNow));
        Assert.Equal("returns.duplicate_line", aggregate.Message);

        // Courier-origin creation is deliberately exempt: its durable claim identity is (line, condition).
        var courier = ReturnRequest.CreateCourierOrigin(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            customer,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "courier-return:tb272-dup",
            "tb272-dup-courier",
            null,
            "IRR",
            [
                (lineId, 1m, 1000m, (Guid?)null, ReturnItemCondition.Usable),
                (lineId, 1m, 1000m, (Guid?)null, ReturnItemCondition.Damaged),
            ],
            clock.UtcNow);
        Assert.Equal(2, courier.Items.Count);
    }

    [Fact]
    public async Task The_transport_validator_refuses_duplicate_order_lines()
    {
        var lineId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var validator = new CreateReturnCommandValidator();

        var duplicate = await validator.ValidateAsync(Command(
            sellerOrderId, Guid.NewGuid(), "tb272-validator-dup",
            [new ReturnLineCommand(lineId, 1m), new ReturnLineCommand(lineId, 2m)]));
        Assert.False(duplicate.IsValid);
        Assert.Contains(duplicate.Errors, x => x.ErrorCode == ReturnsValidationCodes.DuplicateOrderLine);

        var single = await validator.ValidateAsync(Command(
            sellerOrderId, Guid.NewGuid(), "tb272-validator-ok", [new ReturnLineCommand(lineId, 1m)]));
        Assert.True(single.IsValid);
    }

    // ------------------------------------------------------------------ A (R1): persisted precision, full binding

    [Fact]
    public async Task Unrepresentable_quantities_are_refused_before_any_write_or_replay()
    {
        var customer = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));
        var directory = BuildDirectory(
            db,
            clock,
            [BuildOrder(sellerOrderId, customer, 2m, [lineId], decimalPlaces: 6)],
            [BuildFulfillment(sellerOrderId, (lineId, 2m))]);

        // A legitimate fractional quantity at the persisted scale is accepted and is the ONLY stored fact.
        // TB-278: this line is a 6-decimal unit (a weight), so the owning unit quantum admits the exact fraction the
        // persisted scale allows; a whole-piece line would refuse it before this scale question is even reached.
        var created = await directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272r1-precision-ok", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None);
        Assert.Equal(1m, created.Items.Single().Quantity);

        // 1.0000001 and 1.0000002 are DIFFERENT raw quantities that the six-decimal column would both store as
        // 1.000000, and 0.0000004/0.0000006 would be stored as a different number of physical units (0 and 1).
        // Each is refused with a typed fault, and none of them may be answered by the stored case above.
        foreach (var quantity in new[] { 1.0000001m, 1.0000002m, 0.0000004m, 0.0000006m })
        {
            var refused = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
                Command(sellerOrderId, customer, "tb272r1-precision-ok", [new ReturnLineCommand(lineId, quantity)]),
                CancellationToken.None));
            Assert.Equal(ReturnsErrorCodes.QuantityPrecision, refused.Code);
        }

        // A fresh key with an invalid quantity is refused too: no case and no item is written for it.
        var fresh = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272r1-precision-fresh", [new ReturnLineCommand(lineId, 0.0000004m)]),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.QuantityPrecision, fresh.Code);

        // Exactly six fractional digits (and its trailing-zero form) stay representable and replay the same case.
        var small = await directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272r1-precision-small", [new ReturnLineCommand(lineId, 0.000001m)]),
            CancellationToken.None);
        var trailingZeros = await directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272r1-precision-small", [new ReturnLineCommand(lineId, 0.0000010m)]),
            CancellationToken.None);
        Assert.Equal(small.ReturnRequestId, trailingZeros.ReturnRequestId);

        Assert.Equal(2, await db.ReturnRequests.CountAsync());
        Assert.Equal(2, await db.ReturnItems.CountAsync());
        Assert.Equal(0m, await db.ReturnRequests
            .Where(x => x.IdempotencyKey == "tb272r1-precision-fresh")
            .SumAsync(x => x.RefundAmount));
    }

    [Fact]
    public void The_aggregate_refuses_unrepresentable_quantities_and_keeps_no_bypass()
    {
        var sellerOrderId = Guid.NewGuid();
        var lineId = Guid.NewGuid();

        var precision = Assert.Throws<InvalidOperationException>(() => ReturnRequest.Create(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "tb272r1-aggregate-precision",
            null,
            "IRR",
            [(lineId, 1.0000001m, 1000m, (Guid?)null)],
            DeliveredAt));
        Assert.Equal("returns.quantity_precision", precision.Message);

        // The ordinary factory must never expose a way to weaken the one-line-per-order-line invariant.
        var bypass = typeof(ReturnRequest)
            .GetMethods()
            .SelectMany(x => x.GetParameters())
            .Select(x => x.Name)
            .ToArray();
        Assert.DoesNotContain("enforceOneLinePerOrderLine", bypass);

        // Courier-origin keeps its own (line, condition) identity through the shared private factory.
        var courier = ReturnRequest.CreateCourierOrigin(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            sellerOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "courier-return:tb272r1-precision",
            "tb272r1-courier-precision",
            null,
            "IRR",
            [
                (lineId, 1m, 1000m, (Guid?)null, ReturnItemCondition.Usable),
                (lineId, 1m, 1000m, (Guid?)null, ReturnItemCondition.Damaged),
            ],
            DeliveredAt);
        Assert.Equal(2, courier.Items.Count);
        Assert.Equal(ReturnItemDisposition.DamagedNonSaleable, courier.Items[1].Disposition);
    }

    [Fact]
    public async Task A_stored_case_with_another_checkout_or_seller_binding_is_not_replayed()
    {
        var customer = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        var sellerPartyId = Guid.NewGuid();
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));

        // The case is created against the REAL binding (checkout + seller party).
        var created = await BuildDirectory(
                db,
                clock,
                [BuildOrder(sellerOrderId, customer, 2m, [lineId], checkoutId: checkoutId, sellerPartyId: sellerPartyId)],
                [BuildFulfillment(sellerOrderId, (lineId, 2m))])
            .CreateAsync(
                Command(sellerOrderId, customer, "tb272r1-binding", [new ReturnLineCommand(lineId, 1m)]),
                CancellationToken.None);

        foreach (var (otherCheckout, otherSeller) in new[]
                 {
                     (Guid.NewGuid(), sellerPartyId),
                     (checkoutId, Guid.NewGuid()),
                 })
        {
            var mismatch = await Assert.ThrowsAsync<ContractOperationException>(() => BuildDirectory(
                    db,
                    clock,
                    [BuildOrder(sellerOrderId, customer, 2m, [lineId], checkoutId: otherCheckout,
                        sellerPartyId: otherSeller)],
                    [BuildFulfillment(sellerOrderId, (lineId, 2m))])
                .CreateAsync(
                    Command(sellerOrderId, customer, "tb272r1-binding", [new ReturnLineCommand(lineId, 1m)]),
                    CancellationToken.None));
            Assert.Equal(ReturnsErrorCodes.IdempotencyConflict, mismatch.Code);
        }

        // The stored case is still the single, unchanged row of the original binding.
        Assert.Equal(1, await db.ReturnRequests.CountAsync());
        var stored = await db.ReturnRequests.SingleAsync();
        Assert.Equal(created.ReturnRequestId, stored.ReturnRequestId);
        Assert.Equal(checkoutId, stored.CheckoutId);
        Assert.Equal(sellerPartyId, stored.SellerPartyId);
    }

    // ------------------------------------------------------------------ B (R1): the caller's tracking survives

    [Fact]
    public async Task A_refused_creation_preserves_unrelated_caller_tracked_work()
    {
        var customer = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var otherOrder = Guid.NewGuid();
        var otherLine = Guid.NewGuid();
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));

        // Caller work that is already tracked and DIRTY before the refused creation runs.
        var pending = ReturnRequest.Create(
            Guid.NewGuid(),
            () => Guid.NewGuid(),
            otherOrder,
            Guid.NewGuid(),
            Guid.NewGuid(),
            customer,
            "tb272r1-caller-work",
            "کار در جریان",
            "IRR",
            [(otherLine, 1m, 1000m, (Guid?)null)],
            clock.UtcNow);
        db.ReturnRequests.Add(pending);
        db.ReturnItems.AddRange(pending.Items);
        await db.SaveChangesAsync();
        var paymentId = Guid.NewGuid();
        pending.Approve(paymentId, clock.UtcNow);
        Assert.Equal(EntityState.Modified, db.Entry(pending).State);

        var directory = BuildDirectory(
            db,
            clock,
            [BuildOrder(otherOrder, customer, 1m, [otherLine])],
            [BuildFulfillment(otherOrder, (otherLine, 1m))]);

        // A refused creation (unrepresentable quantity) must not touch the caller's tracked work.
        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
            Command(otherOrder, customer, "tb272r1-caller-refused", [new ReturnLineCommand(otherLine, 1.0000001m)]),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.QuantityPrecision, refused.Code);

        var entry = db.Entry(pending);
        Assert.Equal(EntityState.Modified, entry.State);
        Assert.Same(pending, entry.Entity);
        Assert.Equal(ReturnRequestStatus.Approved, entry.Entity.Status);
        Assert.Equal(paymentId, entry.Entity.PaymentId);
        Assert.Equal("کار در جریان", entry.Entity.Reason);

        // The refused attempt leaked neither a request nor an item into the tracker.
        Assert.DoesNotContain(db.ChangeTracker.Entries<ReturnRequest>(), x => x.State == EntityState.Added);
        Assert.DoesNotContain(db.ChangeTracker.Entries<ReturnItem>(), x => x.State == EntityState.Added);
        Assert.Equal(1, await db.ReturnRequests.CountAsync());
    }

    [Fact]
    public async Task The_transport_validator_refuses_unrepresentable_quantities()
    {
        var lineId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        var validator = new CreateReturnCommandValidator();

        var tooPrecise = await validator.ValidateAsync(Command(
            sellerOrderId, Guid.NewGuid(), "tb272r1-validator-precision",
            [new ReturnLineCommand(lineId, 1.0000001m)]));
        Assert.False(tooPrecise.IsValid);
        Assert.Contains(tooPrecise.Errors, x => x.ErrorCode == ReturnsValidationCodes.QuantityPrecision);

        var exactlySix = await validator.ValidateAsync(Command(
            sellerOrderId, Guid.NewGuid(), "tb272r1-validator-six",
            [new ReturnLineCommand(lineId, 0.000001m)]));
        Assert.True(exactlySix.IsValid);
    }

    // ------------------------------------------------------------------ C: refund failed still occupies capacity

    [Fact]
    public async Task Refund_failed_keeps_its_capacity_while_a_rejected_case_releases_it()
    {
        var customer = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));
        var directory = BuildDirectory(
            db,
            clock,
            [BuildOrder(sellerOrderId, customer, 2m, [lineId])],
            [BuildFulfillment(sellerOrderId, (lineId, 2m))]);

        var failed = await directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272-failed", [new ReturnLineCommand(lineId, 2m)]),
            CancellationToken.None);
        var failedRow = await db.ReturnRequests.SingleAsync(x => x.ReturnRequestId == failed.ReturnRequestId);
        failedRow.Approve(Guid.NewGuid(), clock.UtcNow);
        failedRow.MarkRefundProcessing(clock.UtcNow);
        failedRow.MarkRefundFailed(clock.UtcNow);
        await db.SaveChangesAsync();
        Assert.Equal(ReturnRequestStatus.RefundFailed, (await db.ReturnRequests.SingleAsync()).Status);

        // Money failed; the physical intent did not. The same units cannot fund a second request.
        var occupied = await directory.EvaluateEligibilityAsync(sellerOrderId, CancellationToken.None);
        Assert.Equal(0m, occupied.Lines.Single(x => x.OrderLineId == lineId).RemainingReturnableQuantity);
        var refused = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272-failed-second", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.QuantityExceeded, refused.Code);

        // A REJECTED case releases its capacity again: only the terminal rejection/cancellation is absent from the
        // occupying set, so a failed refund is never mistaken for a released one.
        var rejectOrder = Guid.NewGuid();
        var rejectLine = Guid.NewGuid();
        var rejectDirectory = BuildDirectory(
            db,
            clock,
            [BuildOrder(rejectOrder, customer, 2m, [rejectLine])],
            [BuildFulfillment(rejectOrder, (rejectLine, 2m))]);
        var rejected = await rejectDirectory.CreateAsync(
            Command(rejectOrder, customer, "tb272-reject", [new ReturnLineCommand(rejectLine, 2m)]),
            CancellationToken.None);
        var rejectedRow = await db.ReturnRequests.SingleAsync(x => x.ReturnRequestId == rejected.ReturnRequestId);
        rejectedRow.Reject(clock.UtcNow);
        await db.SaveChangesAsync();

        var released = await rejectDirectory.EvaluateEligibilityAsync(rejectOrder, CancellationToken.None);
        Assert.Equal(2m, released.Lines.Single(x => x.OrderLineId == rejectLine).RemainingReturnableQuantity);
        var again = await rejectDirectory.CreateAsync(
            Command(rejectOrder, customer, "tb272-after-reject", [new ReturnLineCommand(rejectLine, 2m)]),
            CancellationToken.None);
        Assert.NotEqual(rejected.ReturnRequestId, again.ReturnRequestId);

        // The refund-failed order is untouched by that release.
        Assert.Equal(0m, (await directory.EvaluateEligibilityAsync(sellerOrderId, CancellationToken.None))
            .Lines.Single(x => x.OrderLineId == lineId).RemainingReturnableQuantity);
    }

    [Fact]
    public async Task A_partial_return_preserves_the_remaining_quantity_and_over_remaining_is_refused()
    {
        var customer = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var sellerOrderId = Guid.NewGuid();
        await using var db = CreateDb();
        var clock = new MutableClock(DeliveredAt.AddDays(2));
        var directory = BuildDirectory(
            db,
            clock,
            [BuildOrder(sellerOrderId, customer, 3m, [lineId])],
            [BuildFulfillment(sellerOrderId, (lineId, 3m))]);

        await directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272-partial", [new ReturnLineCommand(lineId, 2m)]),
            CancellationToken.None);

        var afterPartial = await directory.EvaluateEligibilityAsync(sellerOrderId, CancellationToken.None);
        var line = afterPartial.Lines.Single(x => x.OrderLineId == lineId);
        Assert.True(afterPartial.Eligible);
        Assert.Equal(1m, line.RemainingReturnableQuantity);
        Assert.Equal(2m, line.AlreadyReturnedQuantity);

        var over = await Assert.ThrowsAsync<ContractOperationException>(() => directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272-partial-over", [new ReturnLineCommand(lineId, 2m)]),
            CancellationToken.None));
        Assert.Equal(ReturnsErrorCodes.QuantityExceeded, over.Code);

        var rest = await directory.CreateAsync(
            Command(sellerOrderId, customer, "tb272-partial-rest", [new ReturnLineCommand(lineId, 1m)]),
            CancellationToken.None);
        Assert.Equal(1000m, rest.RefundAmount);
        Assert.False((await directory.EvaluateEligibilityAsync(sellerOrderId, CancellationToken.None)).Eligible);
    }

    // ------------------------------------------------------------------ D: the serialization identity itself

    [Fact]
    public void Advisory_lock_identity_is_stable_per_seller_order_and_namespaced()
    {
        var order = Guid.NewGuid();

        Assert.Equal(
            ReturnCreationSerialization.AdvisoryLockKey(order),
            ReturnCreationSerialization.AdvisoryLockKey(order));
        Assert.NotEqual(
            ReturnCreationSerialization.AdvisoryLockKey(order),
            ReturnCreationSerialization.AdvisoryLockKey(Guid.NewGuid()));
        Assert.NotEqual(0L, ReturnCreationSerialization.AdvisoryLockNamespace);

        // The index identity is the ONE constraint a duplicate key may collide on. It is a durable contract with the
        // real schema, so a rename would surface here instead of silently degrading the recovery branch.
        Assert.Equal("returns", ReturnCreationSerialization.IdempotencySchema);
        Assert.Equal("return_requests", ReturnCreationSerialization.IdempotencyTable);
        Assert.Equal("ix_return_requests_idempotency_key", ReturnCreationSerialization.IdempotencyUniqueIndex);
    }

    // ------------------------------------------------------------------ fixtures

    private static CreateReturnCommand Command(
        Guid sellerOrderId,
        Guid actorUserId,
        string idempotencyKey,
        IReadOnlyList<ReturnLineCommand> items,
        RefundDestination destination = RefundDestination.OriginalPayment) =>
        new(sellerOrderId, actorUserId, idempotencyKey, "دلیل مرجوعی", items, destination);

    private static ReturnDirectory BuildDirectory(
        ReturnsDbContext db,
        MutableClock clock,
        IReadOnlyList<OrderReturnContextSnapshot> orders,
        IReadOnlyList<FulfillmentReturnEligibilitySnapshot> fulfillments)
    {
        var orderReader = new MapOrderReader(orders);
        var eligibility = new ReturnEligibilityEvaluator(
            orderReader,
            new MapFulfillmentReader(fulfillments),
            db,
            clock);
        return new ReturnDirectory(
            db,
            new OpenReturnUseCaseGuard(),
            orderReader,
            eligibility,
            new FixedPaymentReader(Guid.NewGuid()),
            new RecordingRefundGateway(),
            new RecordingWallet(),
            new RecordingInventory(),
            new ReturnsInstrumentation(),
            NullLogger<ReturnDirectory>.Instance,
            clock,
            new UuidV7IdGenerator());
    }

    private static OrderReturnContextSnapshot BuildOrder(
        Guid sellerOrderId,
        Guid customerUserId,
        decimal quantity,
        Guid[] lineIds,
        int windowDays = 7,
        decimal unitPrice = 1000m,
        Guid? checkoutId = null,
        Guid? sellerPartyId = null,
        int? decimalPlaces = null) =>
        new(
            sellerOrderId,
            checkoutId ?? Guid.NewGuid(),
            sellerPartyId ?? Guid.NewGuid(),
            customerUserId,
            true,
            "IRR",
            lineIds
                .Select(x => new OrderReturnLineSnapshot(
                    x, quantity, unitPrice, "IRR", null, true, windowDays, null, null, decimalPlaces))
                .ToArray());

    private static FulfillmentReturnEligibilitySnapshot BuildFulfillment(
        Guid sellerOrderId,
        params (Guid LineId, decimal Delivered)[] lines) =>
        new(
            sellerOrderId,
            lines.ToDictionary(x => x.LineId, x => x.Delivered),
            DeliveredAt,
            lines.ToDictionary(x => x.LineId, _ => DeliveredAt),
            lines.Select(x => new LineDeliverySlice(x.LineId, x.Delivered, DeliveredAt)).ToArray());

    private static ReturnsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ReturnsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new ReturnsDbContext(options);
    }

    private sealed class MutableClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }

    private sealed class MapOrderReader(IReadOnlyList<OrderReturnContextSnapshot> orders) : IOrderReturnReader
    {
        public Task<OrderReturnContextSnapshot?> GetReturnContextAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult(orders.FirstOrDefault(x => x.SellerOrderId == sellerOrderId));
    }

    private sealed class MapFulfillmentReader(IReadOnlyList<FulfillmentReturnEligibilitySnapshot> fulfillments)
        : IFulfillmentReturnReader
    {
        public Task<FulfillmentReturnEligibilitySnapshot?> GetEligibilityAsync(
            Guid sellerOrderId,
            CancellationToken cancellationToken) =>
            Task.FromResult(fulfillments.FirstOrDefault(x => x.SellerOrderId == sellerOrderId));

        public Task<IReadOnlyDictionary<Guid, DateTimeOffset?>> GetLastDeliveredAtBySellerOrderIdsAsync(
            IReadOnlyList<Guid> sellerOrderIds,
            CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<Guid, DateTimeOffset?> map = fulfillments
                .Where(x => sellerOrderIds.Contains(x.SellerOrderId))
                .ToDictionary(x => x.SellerOrderId, x => x.LastDeliveredAt);
            return Task.FromResult(map);
        }
    }

    private sealed class FixedPaymentReader(Guid paymentId) : IPaymentReturnReader
    {
        public Task<PaymentReturnSnapshot?> GetAsync(
            Guid payment,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(
                new PaymentReturnSnapshot(paymentId, Guid.NewGuid(), 1000m, "IRR", "Succeeded"));

        public Task<PaymentReturnSnapshot?> GetLatestForCheckoutAsync(
            Guid checkoutId,
            Guid actorUserId,
            Guid? buyerPartyId,
            CancellationToken cancellationToken) =>
            Task.FromResult<PaymentReturnSnapshot?>(
                new PaymentReturnSnapshot(paymentId, checkoutId, 1000m, "IRR", "Succeeded"));
    }

    private sealed class RecordingRefundGateway : IPaymentRefundGateway
    {
        public Task<GatewayRefundResult> RefundAsync(
            Guid paymentId,
            decimal amount,
            string currency,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(new GatewayRefundResult(true, "ref-272", null));
    }

    private sealed class RecordingWallet : IWalletRefundCreditPort
    {
        public Task<WalletRefundCreditResultDto> CreditRefundAsync(
            Guid customerActorUserId,
            decimal amount,
            string currency,
            Guid returnRequestId,
            string idempotencyKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(new WalletRefundCreditResultDto(amount, false));
    }

    private sealed class RecordingInventory : IReturnInventoryGateway
    {
        public Task RestockConsumedReservationAsync(
            Guid reservationId,
            decimal quantity,
            string idempotencyKey,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
