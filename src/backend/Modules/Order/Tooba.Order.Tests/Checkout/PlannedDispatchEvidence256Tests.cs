using Tooba.BuildingBlocks;
using Tooba.Order.Domain.Checkout;
using Xunit;
using static Tooba.Order.Tests.Checkout.ShippingGroupAcceptedEvidenceTests;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-256: شاهد برنامهٔ ارسال روی گروه پذیرفته‌شده — نسخهٔ قالب ۳ فقط برای <b>پذیرش جدید</b> است،
/// نسخه‌های ۱ و ۲ خوانده می‌شوند و غیبت برنامه برای تاریخ قدیمی «نامشخص صریح» می‌ماند.
/// </summary>
public sealed class PlannedDispatchEvidence256Tests
{
    private static readonly DateOnly ReceivingDate = new(2026, 11, 12);
    private static readonly Guid Snapshot = Guid.Parse("01a17d0e-0419-7000-b575-f507d2f89abc");

    [Fact]
    public void Format_versions_one_to_four_are_readable_and_anything_else_is_refused()
    {
        Assert.True(ShippingGroupAcceptedEvidence.IsSupportedFormat(1));
        Assert.True(ShippingGroupAcceptedEvidence.IsSupportedFormat(2));
        Assert.True(ShippingGroupAcceptedEvidence.IsSupportedFormat(ShippingGroupAcceptedEvidence.CarrierScheduleFormatVersion));
        // TB-264-R2: the canonical capture writer emits 4 for a method with an explicit processing week, so
        // a real customer selection must be acceptable. Unknown/absent versions are still refused.
        Assert.True(ShippingGroupAcceptedEvidence.IsSupportedFormat(
            ShippingGroupAcceptedEvidence.ProcessingScheduleFormatVersion));
        Assert.False(ShippingGroupAcceptedEvidence.IsSupportedFormat(5));
        Assert.False(ShippingGroupAcceptedEvidence.IsSupportedFormat(0));

        // Legacy versions remain acceptable and carry no dispatch plan (never backfilled with a guess).
        var legacy = Evidence(formatVersion: 1);
        Assert.Equal(1, legacy.FormatVersion);
        Assert.Null(legacy.PlannedDispatch);
    }

    [Fact]
    public void Accepted_evidence_roundtrips_the_planned_handoff_with_full_provenance()
    {
        var plan = ShippingGroupPlannedDispatch.Create(
            ReceivingDate.AddDays(-3),
            new TimeOnly(9, 0).Ticks,
            new TimeOnly(17, 0).Ticks,
            "Asia/Tehran",
            transportWorkingDays: 3,
            policyVersion: 1,
            Snapshot,
            scheduleSourceRevision: 2_000_000_001L);

        var evidence = Evidence(
            formatVersion: ShippingGroupAcceptedEvidence.CarrierScheduleFormatVersion,
            plannedDispatch: plan);

        Assert.NotNull(evidence.PlannedDispatch);
        Assert.Equal(ReceivingDate.AddDays(-3), evidence.PlannedDispatch!.HandoffDate);
        Assert.Equal(new TimeOnly(9, 0).Ticks, evidence.PlannedDispatch.HandoffOpenTicks);
        Assert.Equal(new TimeOnly(17, 0).Ticks, evidence.PlannedDispatch.HandoffCloseTicks);
        Assert.Equal("Asia/Tehran", evidence.PlannedDispatch.HandoffTimeZone);
        Assert.Equal(3, evidence.PlannedDispatch.TransportWorkingDays);
        Assert.Equal(1, evidence.PlannedDispatch.PolicyVersion);
        Assert.Equal(Snapshot, evidence.PlannedDispatch.ScheduleSnapshotId);
        Assert.Equal(2_000_000_001L, evidence.PlannedDispatch.ScheduleSourceRevision);

        // The customer commitment itself is untouched by planning the handoff.
        Assert.Equal(ReceivingDate, evidence.ReceivingDate);
        Assert.Equal(OpenTicks, evidence.ReceivingOpenTicks);
        Assert.Equal(CloseTicks, evidence.ReceivingCloseTicks);
    }

    [Fact]
    public void Handoff_after_the_customer_receiving_date_is_refused()
    {
        var after = ShippingGroupPlannedDispatch.Create(
            ReceivingDate.AddDays(1),
            new TimeOnly(9, 0).Ticks,
            new TimeOnly(17, 0).Ticks,
            "Asia/Tehran",
            2,
            1,
            Snapshot,
            3);

        AssertFault(ShippingGroupEvidenceFault.DispatchPlanInvalid, () =>
            Evidence(
                formatVersion: ShippingGroupAcceptedEvidence.CarrierScheduleFormatVersion,
                plannedDispatch: after));
    }

    [Fact]
    public void Incomplete_or_incoherent_dispatch_plan_is_refused_before_storage()
    {
        AssertFault(ShippingGroupEvidenceFault.DispatchPlanInvalid, () =>
            ShippingGroupPlannedDispatch.Create(
                default, 0, 1, "Asia/Tehran", 1, 1, Snapshot, 1));
        AssertFault(ShippingGroupEvidenceFault.DispatchPlanInvalid, () =>
            ShippingGroupPlannedDispatch.Create(
                ReceivingDate, new TimeOnly(9, 0).Ticks, new TimeOnly(9, 0).Ticks, "Asia/Tehran", 1, 1, Snapshot, 1));
        AssertFault(ShippingGroupEvidenceFault.DispatchPlanInvalid, () =>
            ShippingGroupPlannedDispatch.Create(
                ReceivingDate, new TimeOnly(17, 0).Ticks, new TimeOnly(9, 0).Ticks, "Asia/Tehran", 1, 1, Snapshot, 1));
        AssertFault(ShippingGroupEvidenceFault.DispatchPlanInvalid, () =>
            ShippingGroupPlannedDispatch.Create(
                ReceivingDate, 0, ShippingGroupAcceptedEvidence.MaxTimeOfDayTicks + 1, "Asia/Tehran", 1, 1, Snapshot, 1));
        AssertFault(ShippingGroupEvidenceFault.DispatchPlanInvalid, () =>
            ShippingGroupPlannedDispatch.Create(
                ReceivingDate, 0, 1, "  ", 1, 1, Snapshot, 1));
        AssertFault(ShippingGroupEvidenceFault.DispatchPlanInvalid, () =>
            ShippingGroupPlannedDispatch.Create(
                ReceivingDate, 0, 1, "Asia/Tehran", -1, 1, Snapshot, 1));
        AssertFault(ShippingGroupEvidenceFault.DispatchPlanInvalid, () =>
            ShippingGroupPlannedDispatch.Create(
                ReceivingDate, 0, 1, "Asia/Tehran", 1, 0, Snapshot, 1));
        AssertFault(ShippingGroupEvidenceFault.DispatchPlanInvalid, () =>
            ShippingGroupPlannedDispatch.Create(
                ReceivingDate, 0, 1, "Asia/Tehran", 1, 1, Guid.Empty, 1));
        AssertFault(ShippingGroupEvidenceFault.DispatchPlanInvalid, () =>
            ShippingGroupPlannedDispatch.Create(
                ReceivingDate, 0, 1, "Asia/Tehran", 1, 1, Snapshot, 0));
    }

    private static void AssertFault(string expected, Action action)
    {
        var fault = Assert.Throws<ContractOperationException>(action);
        Assert.Equal(expected, fault.Code);
    }

    private static ShippingGroupAcceptedEvidence Evidence(
        int formatVersion,
        ShippingGroupPlannedDispatch? plannedDispatch = null) =>
        ShippingGroupAcceptedEvidence.Create(
            Snapshot,
            Draft,
            3_000_000_000L,
            formatVersion,
            5,
            ShippingGroupOwner.Store(TenantKey),
            AreaA,
            RowA,
            TypeId,
            120_000m,
            5_000m,
            "IRR",
            125_000m,
            0m,
            ReceivingDate,
            OpenTicks,
            CloseTicks,
            "Asia/Tehran",
            minimumDeliveryDays: 2,
            maximumDeliveryDays: 3,
            carrierQuote: null,
            plannedDispatch: plannedDispatch);
}
