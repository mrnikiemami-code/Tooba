using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191 — domain semantics of persisted manual rate rows.
/// </summary>
/// <remarks>
/// These facts exercise the real aggregate: half-open intervals, AND-inside-a-row / alternatives-between-rows,
/// full three-axis product overlap, canonical money precision, delivery duration, retained row identity and
/// the currency binding. No database is involved here; the relational proof lives in the Npgsql facts.
/// </remarks>
public sealed class ShippingAuthoringGroupRateRowTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 5, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Rows_persist_under_the_stable_group_identity_and_copy_no_geography()
    {
        var draft = NewDraft(out var firstGroup, out var secondGroup);
        var first = draft.FindRegionGroup(firstGroup)!;
        var second = draft.FindRegionGroup(secondGroup)!;
        var areasBefore = first.Areas.Select(area => area.NodeId).ToArray();

        draft.ReplaceGroupRateRows(
            firstGroup,
            [Row(1000, 2000, null, null, null, null, 2, 4, freight: 25000m, packaging: 5000m)],
            "IRR",
            Now);

        var row = Assert.Single(first.RateRows);
        Assert.NotEqual(Guid.Empty, row.RateRowId);
        Assert.Equal(firstGroup, row.GroupId);
        Assert.Equal(1000, row.MinWeightGrams);
        Assert.Equal(2000, row.MaxWeightGrams);
        Assert.Equal(25000m, row.FreightAmount);
        Assert.Equal(5000m, row.PackagingAmount);
        Assert.Equal(2, row.MinDeliveryDays);
        Assert.Equal(4, row.MaxDeliveryDays);
        Assert.Equal("IRR", first.CurrencyCode);

        // Geography lives once on the group and is untouched by a rate row write.
        Assert.Equal(areasBefore, first.Areas.Select(area => area.NodeId).ToArray());
        Assert.Empty(second.RateRows);
        Assert.Null(second.CurrencyCode);
        Assert.Equal(2, draft.Revision);

        // A retained row id keeps its identity; the value is updated in place.
        var retained = row.RateRowId;
        draft.ReplaceGroupRateRows(firstGroup, [Row(1500, 3000, null, null, null, null, 3, 5, retained, 30000m, 6000m)], "IRR", Now);
        var reread = Assert.Single(first.RateRows);
        Assert.Equal(retained, reread.RateRowId);
        Assert.Equal(1500, reread.MinWeightGrams);
        Assert.Equal(3, draft.Revision);
    }

    [Fact]
    public void Half_open_adjacent_splits_on_every_axis_are_not_overlaps()
    {
        var draft = NewDraft(out var groupId, out _);

        // Same weight [1000,2000) with disjoint counts [1,4) / [4,11): adjacent half-open, no overlap.
        draft.ReplaceGroupRateRows(
            groupId,
            [
                Row(1000, 2000, 1, 4, null, null, 1, 2),
                Row(1000, 2000, 4, 11, null, null, 1, 2),
            ],
            "IRR",
            Now);
        Assert.Equal(2, draft.FindRegionGroup(groupId)!.RateRows.Count);

        // Amount split [5000,10000) / [10000,20000) on the same weight/count tuple succeeds as well.
        draft.ReplaceGroupRateRows(
            groupId,
            [
                Row(1000, 2000, 1, 4, 5000m, 10000m, 1, 2),
                Row(1000, 2000, 1, 4, 10000m, 20000m, 1, 2),
            ],
            "IRR", Now);
        Assert.Equal(2, draft.FindRegionGroup(groupId)!.RateRows.Count);
    }

    [Fact]
    public void Full_tuple_overlap_identical_rows_and_unconditioned_overlap_are_refused()
    {
        var draft = NewDraft(out var groupId, out _);

        // Weight overlaps, count overlaps, amount is unconditioned on both -> valid point in all three.
        AssertRejected(
            ShippingErrorCodes.AuthoringRateOverlap,
            () => draft.ReplaceGroupRateRows(
                groupId,
                [
                    Row(1000, 2000, 1, 4, null, null, 1, 2),
                    Row(1500, 2500, 2, 5, null, null, 1, 2),
                ],
                "IRR", Now));

        // Identical rows conflict even though their costs match.
        AssertRejected(
            ShippingErrorCodes.AuthoringRateOverlap,
            () => draft.ReplaceGroupRateRows(groupId, [Row(0, 1000, 1, 2, null, null, 1, 1), Row(0, 1000, 1, 2, null, null, 1, 1)], "IRR", Now));

        // One unconditioned row conflicts with any other row in the same group.
        AssertRejected(
            ShippingErrorCodes.AuthoringRateOverlap,
            () => draft.ReplaceGroupRateRows(groupId, [Row(null, null, null, null, null, null, 0, 0), Row(1000, 2000, null, null, null, null, 1, 2)], "IRR", Now));

        // A single unconditioned row is legal draft data (incomplete, never zero/free coverage).
        draft.ReplaceGroupRateRows(groupId, [Row(null, null, null, null, null, null, 0, 0)], "IRR", Now);
        Assert.Single(draft.FindRegionGroup(groupId)!.RateRows);
        Assert.Null(draft.FindRegionGroup(groupId)!.RateRows[0].MinWeightGrams);
    }

    [Fact]
    public void Invalid_rows_are_refused_before_any_mutation_of_this_aggregate()
    {
        var draft = NewDraft(out var groupId, out _);
        draft.ReplaceGroupRateRows(groupId, [Row(1000, 2000, null, null, null, null, 2, 4, freight: 25000m, packaging: 5000m)], "IRR", Now);
        var group = draft.FindRegionGroup(groupId)!;
        var accepted = Assert.Single(group.RateRows).RateRowId;
        var revision = draft.Revision;
        var updatedAt = draft.UpdatedAt;

        // max-only, inverted, zero-width, negative fee, over-precise amount, bad duration, missing fields.
        AssertRejected(ShippingErrorCodes.AuthoringRateConditionInvalid,
            () => draft.ReplaceGroupRateRows(groupId, [Row(null, 2000, null, null, null, null, 1, 2)], "IRR", Now));
        AssertRejected(ShippingErrorCodes.AuthoringRateConditionInvalid,
            () => draft.ReplaceGroupRateRows(groupId, [Row(2000, 1000, null, null, null, null, 1, 2)], "IRR", Now));
        AssertRejected(ShippingErrorCodes.AuthoringRateConditionInvalid,
            () => draft.ReplaceGroupRateRows(groupId, [Row(1000, 1000, null, null, null, null, 1, 2)], "IRR", Now));
        AssertRejected(ShippingErrorCodes.AuthoringRateConditionInvalid,
            () => draft.ReplaceGroupRateRows(groupId, [Row(null, null, 0, 5, null, null, 1, 2)], "IRR", Now));
        AssertRejected(ShippingErrorCodes.AuthoringRateAmountInvalid,
            () => draft.ReplaceGroupRateRows(groupId, [Row(null, null, null, null, null, null, 1, 2, null, -1m, 0m)], "IRR", Now));
        AssertRejected(ShippingErrorCodes.AuthoringRateAmountInvalid,
            () => draft.ReplaceGroupRateRows(groupId, [Row(null, null, null, null, null, null, 1, 2, null, 0.005m, 0m)], "IRR", Now));
        AssertRejected(ShippingErrorCodes.AuthoringRateDurationInvalid,
            () => draft.ReplaceGroupRateRows(groupId, [Row(null, null, null, null, null, null, 5, 2)], "IRR", Now));
        AssertRejected(ShippingErrorCodes.AuthoringRateDurationInvalid,
            () => draft.ReplaceGroupRateRows(groupId, [Row(null, null, null, null, null, null, 0, 366)], "IRR", Now));
        AssertRejected(ShippingErrorCodes.AuthoringRateRowPayloadInvalid,
            () => draft.ReplaceGroupRateRows(groupId, [Row(null, null, null, null, null, null, 1, 2, null, null, 0m)], "IRR", Now));
        AssertRejected(ShippingErrorCodes.AuthoringRateRowPayloadInvalid,
            () => draft.ReplaceGroupRateRows(groupId, [Row(null, null, null, null, null, null, null, null)], "IRR", Now));

        // The rejected collection left the SAME aggregate (rows, revision, timestamp) untouched.
        Assert.Equal([accepted], group.RateRows.Select(row => row.RateRowId).ToArray());
        Assert.Equal(revision, draft.Revision);
        Assert.Equal(updatedAt, draft.UpdatedAt);
        Assert.Equal("IRR", group.CurrencyCode);
    }

    [Fact]
    public void Equal_delivery_duration_is_allowed_and_stays_separate_from_preparation()
    {
        var draft = NewDraft(out var groupId, out _);
        draft.ReplaceGroupRateRows(groupId, [Row(null, null, null, null, null, null, 3, 3)], "IRR", Now);

        var row = Assert.Single(draft.FindRegionGroup(groupId)!.RateRows);
        Assert.Equal(3, row.MinDeliveryDays);
        Assert.Equal(3, row.MaxDeliveryDays);

        // Duration is a separate authoring field: no preparation/lead/promise value is derived from it.
        Assert.DoesNotContain(
            typeof(ShippingAuthoringRateRow).GetProperties(),
            property => property.Name.Contains("Preparation", StringComparison.Ordinal)
                        || property.Name.Contains("Lead", StringComparison.Ordinal)
                        || property.Name.Contains("Promise", StringComparison.Ordinal));
    }

    [Fact]
    public void Foreign_unknown_and_duplicate_retained_row_ids_are_refused()
    {
        var draft = NewDraft(out var groupId, out var otherGroupId);
        draft.ReplaceGroupRateRows(groupId, [Row(null, null, null, null, null, null, 1, 2)], "IRR", Now);
        draft.ReplaceGroupRateRows(otherGroupId, [Row(null, null, null, null, null, null, 1, 2)], "IRR", Now);
        var mine = draft.FindRegionGroup(groupId)!.RateRows.Single().RateRowId;
        var foreign = draft.FindRegionGroup(otherGroupId)!.RateRows.Single().RateRowId;

        // A row of ANOTHER group is not retainable here.
        AssertRejected(ShippingErrorCodes.AuthoringRateRowPayloadInvalid,
            () => draft.ReplaceGroupRateRows(groupId, [Row(null, null, null, null, null, null, 1, 2, foreign)], "IRR", Now));

        // An invented id is unknown, and the same retained id twice is a duplicate claim.
        AssertRejected(ShippingErrorCodes.AuthoringRateRowPayloadInvalid,
            () => draft.ReplaceGroupRateRows(groupId, [Row(null, null, null, null, null, null, 1, 2, Guid.NewGuid())], "IRR", Now));
        AssertRejected(ShippingErrorCodes.AuthoringRateRowPayloadInvalid,
            () => draft.ReplaceGroupRateRows(groupId, [Row(null, null, null, null, null, null, 1, 2, mine), Row(null, null, null, null, null, null, 3, 4, mine)], "IRR", Now));

        Assert.Equal(mine, draft.FindRegionGroup(groupId)!.RateRows.Single().RateRowId);
        Assert.Equal(foreign, draft.FindRegionGroup(otherGroupId)!.RateRows.Single().RateRowId);
    }

    [Fact]
    public void Currency_binds_on_the_first_non_empty_save_and_never_reinterprets_stored_amounts()
    {
        var draft = NewDraft(out var groupId, out _);
        var group = draft.FindRegionGroup(groupId)!;

        // An empty first save does not bind anything: "no rows yet" is not a currency decision.
        draft.ReplaceGroupRateRows(groupId, [], "USD", Now);
        Assert.Null(group.CurrencyCode);
        Assert.Equal(2, draft.Revision);

        // The first NON-EMPTY set binds the trusted currency.
        draft.ReplaceGroupRateRows(groupId, [Row(null, null, null, null, null, null, 1, 2, freight: 10000m, packaging: 0m)], "USD", Now);
        Assert.Equal("USD", group.CurrencyCode);

        // Clearing the rows keeps the binding: stored amounts are never reinterpreted or converted.
        draft.ReplaceGroupRateRows(groupId, [], "USD", Now);
        Assert.Equal("USD", group.CurrencyCode);
        Assert.Empty(group.RateRows);

        // A configuration change to another currency fails closed instead of silently relabelling amounts.
        AssertRejected(ShippingErrorCodes.AuthoringRateCurrencyMismatch,
            () => draft.ReplaceGroupRateRows(groupId, [Row(null, null, null, null, null, null, 1, 2, freight: 10000m, packaging: 0m)], "EUR", Now));

        // An unresolved/malformed trusted currency is a configuration failure, never a guessed value.
        AssertRejected(ShippingErrorCodes.AuthoringConfigurationUnavailable,
            () => draft.ReplaceGroupRateRows(groupId, [], "  ", Now));

        Assert.Equal("USD", group.CurrencyCode);
    }

    [Fact]
    public void A_rate_row_write_advances_only_the_root_revision_and_keeps_region_identity()
    {
        var draft = NewDraft(out var groupId, out _);
        var groupsBefore = draft.RegionGroups.Select(group => group.GroupId).ToArray();
        var orderBefore = draft.RegionGroups.Select(group => group.Order).ToArray();

        draft.ReplaceGroupRateRows(groupId, [Row(null, null, null, null, null, null, 1, 1)], "IRR", Now);

        Assert.Equal(2, draft.Revision);
        Assert.Equal(groupsBefore, draft.RegionGroups.Select(group => group.GroupId).ToArray());
        Assert.Equal(orderBefore, draft.RegionGroups.Select(group => group.Order).ToArray());
    }

    private static void AssertRejected(string expectedCode, Action action)
    {
        var exception = Assert.Throws<SemanticException>(action);
        Assert.Equal(expectedCode, exception.Error.Code);
    }

    private static ShippingAuthoringDraft NewDraft(out Guid firstGroup, out Guid secondGroup)
    {
        var draft = ShippingAuthoringDraft.Create(
            ShippingDraftOwner.ForStore("tb191-store"),
            "post",
            null,
            [
                new ShippingAuthoringRegionGroupInput(null, [Node(Guid.Parse("22222222-2222-2222-2222-222222222222"))]),
                new ShippingAuthoringRegionGroupInput(null, [Node(Guid.Parse("44444444-4444-4444-4444-444444444444"))]),
            ],
            Now);
        var ordered = draft.RegionGroups.OrderBy(group => group.Order).ToArray();
        firstGroup = ordered[0].GroupId;
        secondGroup = ordered[1].GroupId;
        return draft;
    }

    private static ShippingDestinationNode Node(Guid id)
        => new(ShippingDestinationNodeKind.AdministrativeArea, id);

    private static ShippingAuthoringDraft.ShippingAuthoringRateRowInput Row(
        long? minWeight,
        long? maxWeight,
        int? minCount,
        int? maxCount,
        decimal? minAmount,
        decimal? maxAmount,
        int? minDays,
        int? maxDays,
        Guid? rateRowId = null,
        decimal? freight = 25000m,
        decimal? packaging = 5000m)
        => new(rateRowId, minWeight, maxWeight, minCount, maxCount, minAmount, maxAmount, freight, packaging, minDays, maxDays);
}
