using Tooba.Fulfillment.Contracts.Operations;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-257 A+B: the two pure authorities. These are SOURCE-level facts only — no database, no HTTP.
/// </summary>
public sealed class TrackingAndRemainingQuantityAuthority257Tests
{
    // Two UUIDv7 parcel ids inside the SAME ~65 second bucket: their first eight hex characters are equal, which
    // is exactly the shape the old suggestion truncated to (`TRK-` + 8 chars).
    private static readonly Guid SameBucketA = Guid.Parse("019a5c2e-4f10-7000-8000-000000000001");
    private static readonly Guid SameBucketB = Guid.Parse("019a5c2e-4f11-7000-8000-000000000002");

    [Fact]
    public void Parcels_sharing_the_old_eight_character_prefix_get_different_suggestions()
    {
        var a = FulfillmentInternalTrackingReference.ForShipment(SameBucketA);
        var b = FulfillmentInternalTrackingReference.ForShipment(SameBucketB);

        // The regression itself: the previous rule produced ONE value for both parcels.
        Assert.Equal(
            SameBucketA.ToString("N")[..8],
            SameBucketB.ToString("N")[..8]);

        Assert.NotEqual(a, b, StringComparer.OrdinalIgnoreCase);
        Assert.StartsWith(FulfillmentInternalTrackingReference.Prefix, a, StringComparison.Ordinal);
        Assert.Equal(
            FulfillmentInternalTrackingReference.Prefix + SameBucketA.ToString("N").ToUpperInvariant(),
            a);
        Assert.True(a.Length <= FulfillmentInternalTrackingReference.MaxLength);
    }

    [Fact]
    public void Sequential_parcels_never_collide_and_stay_within_the_column_ceiling()
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < 2000; index++)
        {
            // UUIDv7 with real 48-bit millisecond timestamps: parcels created in the same second must not collide.
            var id = Guid.Parse($"019a5c2e-4f10-7000-8000-{index:x12}");
            var candidate = FulfillmentInternalTrackingReference.ForShipment(id);
            Assert.True(candidate.Length <= FulfillmentInternalTrackingReference.MaxLength);
            Assert.True(candidates.Add(candidate), $"duplicate automatic candidate for parcel {id}");
        }

        Assert.Equal(2000, candidates.Count);
    }

    [Fact]
    public void Candidate_is_deterministic_for_its_parcel_and_bounded_retries_move_forward()
    {
        var canonical = FulfillmentInternalTrackingReference.ForShipment(SameBucketA);

        // Deliberate retry / double click / lost response of the SAME parcel resolves the same candidate.
        Assert.Equal(canonical, FulfillmentInternalTrackingReference.ForShipment(SameBucketA, 0));
        Assert.Equal(canonical, FulfillmentInternalTrackingReference.ForShipment(SameBucketA));

        var retries = Enumerable.Range(1, FulfillmentInternalTrackingReference.MaxAllocationAttempts)
            .Select(attempt => FulfillmentInternalTrackingReference.ForShipment(SameBucketA, attempt))
            .ToArray();
        Assert.Equal(retries.Length, retries.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.DoesNotContain(canonical, retries, StringComparer.OrdinalIgnoreCase);
        Assert.All(retries, value =>
            Assert.True(value.Length <= FulfillmentInternalTrackingReference.MaxLength));
    }

    [Fact]
    public void Only_the_zero_attempt_is_the_canonical_operator_suggestion()
    {
        Assert.True(FulfillmentInternalTrackingReference.IsCanonicalSuggestion(
            FulfillmentInternalTrackingReference.ForShipment(SameBucketA),
            SameBucketA));
        Assert.True(FulfillmentInternalTrackingReference.IsCanonicalSuggestion(
            $"  {FulfillmentInternalTrackingReference.ForShipment(SameBucketA).ToLowerInvariant()}  ",
            SameBucketA));
        Assert.False(FulfillmentInternalTrackingReference.IsCanonicalSuggestion(
            FulfillmentInternalTrackingReference.ForShipment(SameBucketA, 1),
            SameBucketA));
        Assert.False(FulfillmentInternalTrackingReference.IsCanonicalSuggestion(
            FulfillmentInternalTrackingReference.ForShipment(SameBucketB),
            SameBucketA));
        Assert.False(FulfillmentInternalTrackingReference.IsCanonicalSuggestion("OPERATOR-CODE", SameBucketA));
        Assert.False(FulfillmentInternalTrackingReference.IsCanonicalSuggestion(null, SameBucketA));
        Assert.False(FulfillmentInternalTrackingReference.IsCanonicalSuggestion("   ", SameBucketA));
    }

    // ---- B: one owning remaining-quantity rule -------------------------------------------------

    private static FulfillmentSnapshot Snapshot(
        IReadOnlyList<(Guid LineId, decimal Packed, decimal Shipped)> items,
        params ShipmentSnapshot[] shipments)
        => new(
            Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee"),
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
            Guid.Parse("99999999-9999-4999-8999-999999999999"),
            Guid.Parse("88888888-8888-4888-8888-888888888888"),
            FulfillmentOperationStatus.Packed,
            "r",
            "0",
            "p",
            "c",
            "a",
            "1",
            "m",
            "m",
            items.Select((x, index) => new FulfillmentItemSnapshot(
                Guid.Parse($"20000000-0000-4000-8000-{index:D12}"),
                x.LineId,
                x.Packed,
                x.Shipped,
                null,
                x.Packed,
                x.Packed)).ToArray(),
            shipments,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

    private static ShipmentSnapshot Shipment(
        string id,
        ShipmentOperationStatus status,
        Guid group,
        params (Guid LineId, decimal Quantity)[] lines)
        => new(
            Guid.Parse(id),
            status,
            "carrier",
            null,
            null,
            null,
            lines.Select(x => new ShipmentLineSnapshot(x.LineId, x.Quantity)).ToArray(),
            DateTimeOffset.UtcNow,
            "m",
            "m",
            null,
            0,
            null,
            group);

    [Fact]
    public void Remaining_is_packed_minus_shipped_minus_open_allocation()
    {
        var line = Guid.NewGuid();
        var group = Guid.NewGuid();
        var snapshot = Snapshot(
            [(line, 3m, 1m)],
            Shipment("30000000-0000-4000-8000-000000000001", ShipmentOperationStatus.Created, group, (line, 1m)));

        Assert.Equal(1m, FulfillmentPackedQuantityAuthority.RemainingPackedQuantity(snapshot, snapshot.Items[0]));
        Assert.True(FulfillmentPackedQuantityAuthority.HasRemainingPackedQuantity(snapshot));
    }

    [Fact]
    public void Wholly_shipped_quantity_is_never_available_again()
    {
        var line = Guid.NewGuid();
        // No open allocation at all: the parcel was dispatched, so only QuantityShipped carries the consumption.
        var snapshot = Snapshot([(line, 1m, 1m)]);
        Assert.Equal(0m, FulfillmentPackedQuantityAuthority.RemainingPackedQuantity(snapshot, snapshot.Items[0]));
        Assert.False(FulfillmentPackedQuantityAuthority.HasRemainingPackedQuantity(snapshot));

        // A dispatched shipment is still present in the snapshot; it must not release the remainder either.
        var withDispatched = Snapshot(
            [(line, 1m, 1m)],
            Shipment("30000000-0000-4000-8000-000000000002", ShipmentOperationStatus.Dispatched, Guid.NewGuid(), (line, 1m)));
        Assert.Equal(0m, FulfillmentPackedQuantityAuthority.RemainingPackedQuantity(withDispatched, withDispatched.Items[0]));
    }

    [Fact]
    public void Partial_shipment_with_open_allocation_leaves_only_the_real_remainder()
    {
        var line = Guid.NewGuid();
        var group = Guid.NewGuid();
        var snapshot = Snapshot(
            [(line, 5m, 2m)],
            Shipment("30000000-0000-4000-8000-000000000003", ShipmentOperationStatus.Created, group, (line, 1m)));

        Assert.Equal(2m, FulfillmentPackedQuantityAuthority.RemainingPackedQuantity(snapshot, snapshot.Items[0]));
    }

    [Fact]
    public void Cancelled_pre_dispatch_allocation_releases_only_its_own_reservation()
    {
        var line = Guid.NewGuid();
        var group = Guid.NewGuid();
        var snapshot = Snapshot(
            [(line, 3m, 0m)],
            Shipment("30000000-0000-4000-8000-000000000004", ShipmentOperationStatus.Created, group, (line, 3m)),
            Shipment("30000000-0000-4000-8000-000000000005", ShipmentOperationStatus.Cancelled, group, (line, 2m)));

        Assert.Equal(0m, FulfillmentPackedQuantityAuthority.RemainingPackedQuantity(snapshot, snapshot.Items[0]));
        Assert.Equal(3m, FulfillmentPackedQuantityAuthority.OpenAllocatedQuantity(snapshot, line));
    }

    [Fact]
    public void Sibling_group_allocation_is_not_counted_for_this_group()
    {
        var line = Guid.NewGuid();
        var mine = Guid.NewGuid();
        var sibling = Guid.NewGuid();
        var snapshot = Snapshot(
            [(line, 4m, 0m)],
            Shipment("30000000-0000-4000-8000-000000000006", ShipmentOperationStatus.Created, mine, (line, 1m)),
            Shipment("30000000-0000-4000-8000-000000000007", ShipmentOperationStatus.Created, sibling, (line, 3m)));

        Assert.Equal(3m, FulfillmentPackedQuantityAuthority.RemainingPackedQuantity(snapshot, snapshot.Items[0], mine));
        Assert.Equal(1m, FulfillmentPackedQuantityAuthority.RemainingPackedQuantity(snapshot, snapshot.Items[0], sibling));
        // Ungrouped (legacy) scope keeps every Created allocation visible.
        Assert.Equal(0m, FulfillmentPackedQuantityAuthority.RemainingPackedQuantity(snapshot, snapshot.Items[0]));
    }

    [Fact]
    public void Inconsistent_facts_never_manufacture_stock()
    {
        var line = Guid.NewGuid();
        var group = Guid.NewGuid();
        var snapshot = Snapshot(
            [(line, 1m, 1m)],
            Shipment("30000000-0000-4000-8000-000000000008", ShipmentOperationStatus.Created, group, (line, 2m)));

        Assert.Equal(0m, FulfillmentPackedQuantityAuthority.RemainingPackedQuantity(snapshot, snapshot.Items[0]));
        Assert.False(FulfillmentPackedQuantityAuthority.HasRemainingPackedQuantity(snapshot));
    }
}
