using Tooba.Fulfillment.Application.WorkQueue.Models;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-271 — the employee job stage / time scope read model. These pin the ONE pure classifier, the ONE count
/// authority and the ONE legacy-token mapping the real queue query composes, over the physical facts the existing
/// projection already produces. No new lifecycle, no shipping policy, no clock other than the injected one.
/// </summary>
public sealed class AdminFulfillmentJobStage271Tests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 9, 0, 0, TimeSpan.Zero);
    private const string Tehran = "Asia/Tehran";

    [Fact]
    public void The_five_job_stages_are_classified_from_the_real_physical_facts()
    {
        // آماده‌سازی و بسته‌بندی — processing/packing of THIS group's remaining quantity.
        Assert.Equal(
            AdminFulfillmentJobStageNames.Preparation,
            Classify(AdminFulfillmentWorkUnitKinds.Group, "Packed", ["mark_processing", "mark_packed", "pack_selected"]).Stage);

        // آمادهٔ ارسال — an existing parcel still needs its tracking code.
        Assert.Equal(
            AdminFulfillmentJobStageNames.ReadyToSend,
            Classify(AdminFulfillmentWorkUnitKinds.Parcel, "Created", ["cancel_shipment", "assign_tracking"]).Stage);

        // تحویل به حامل — every owning prerequisite is present and the real dispatch operation is offered.
        Assert.Equal(
            AdminFulfillmentJobStageNames.Handoff,
            Classify(AdminFulfillmentWorkUnitKinds.Parcel, "Created", ["cancel_shipment", "correct_tracking", "dispatch_shipment"]).Stage);

        // پیگیری تحویل — already out of the warehouse.
        Assert.Equal(
            AdminFulfillmentJobStageNames.Delivery,
            Classify(AdminFulfillmentWorkUnitKinds.Parcel, "InTransit", ["deliver_shipment", "record_non_delivery"]).Stage);

        // برگشتی‌ها — actual return transit.
        Assert.Equal(
            AdminFulfillmentJobStageNames.Returns,
            Classify(AdminFulfillmentWorkUnitKinds.Parcel, "Returning", ["record_return_receipt"]).Stage);
    }

    [Fact]
    public void A_tracking_correction_never_outranks_the_real_handoff_and_grants_nothing()
    {
        var codes = new[] { "cancel_shipment", "correct_tracking", "dispatch_shipment" };
        var facts = AdminFulfillmentJobStageAuthority.Classify(
            AdminFulfillmentWorkUnitKinds.Parcel, "Created", codes, hasOpenObligation: true,
            preparationStartUtc: Now.AddHours(-4),
            transferDepartureUtc: Now.AddHours(-2),
            handoffDeadlineUtc: Now.AddHours(3),
            timeZone: Tehran, nowUtc: Now);

        Assert.Equal(AdminFulfillmentJobStageNames.Handoff, facts.Stage);
        Assert.Equal(AdminFulfillmentJobTimeMeanings.HandoffClose, facts.JobTimeMeaning);
        // The classification is a READ label: it neither adds nor removes a capability.
        Assert.Equal(codes, AdminFulfillmentQueueFilters.ProjectParcelActionCodes(
            Tooba.Fulfillment.Domain.ValueObjects.ShipmentStatus.Created, true, false, false));
    }

    [Fact]
    public void A_linked_attempt_that_still_needs_its_own_plan_is_ready_for_send_not_a_return_row()
    {
        // TB-270-R2 capability shape of an unplanned Created resend/replacement parcel.
        var facts = Classify(
            AdminFulfillmentWorkUnitKinds.Parcel,
            "Created",
            ["cancel_shipment", "plan_shipment_dispatch", "correct_tracking"]);

        Assert.Equal(AdminFulfillmentJobStageNames.ReadyToSend, facts.Stage);
        Assert.Equal("آمادهٔ ارسال", facts.StageLabelFa);
        // No plan was ever confirmed for this attempt: the job time is explicitly absent, never borrowed.
        Assert.Equal(AdminFulfillmentJobTimeMeanings.MissingPlan, facts.JobTimeMeaning);
        Assert.Null(facts.JobTimeUtc);
        Assert.Equal("موعد ارسال مشخص نشده", facts.JobTimeLabelFa);
    }

    [Fact]
    public void A_received_source_with_remaining_decision_is_return_work_while_a_settled_one_is_history()
    {
        // Remaining source-owned resend capacity → real return decision work.
        Assert.Equal(
            AdminFulfillmentJobStageNames.Returns,
            Classify(AdminFulfillmentWorkUnitKinds.Parcel, "Returned", ["resend_shipment"]).Stage);
        // Outstanding damaged decision without resend capacity is still return work.
        Assert.Equal(
            AdminFulfillmentJobStageNames.Returns,
            Classify(AdminFulfillmentWorkUnitKinds.Parcel, "Returned", ["resolve_damaged_goods"]).Stage);
        // Fully allocated/settled returned source: NOT active merely because its status says Returned.
        Assert.Equal(
            AdminFulfillmentJobStageNames.History,
            Classify(AdminFulfillmentWorkUnitKinds.Parcel, "Returned", []).Stage);
    }

    [Fact]
    public void Every_terminal_or_no_obligation_shape_is_the_archive()
    {
        Assert.Equal(
            AdminFulfillmentJobStageNames.History,
            Classify(AdminFulfillmentWorkUnitKinds.Fulfillment, "Delivered", []).Stage);
        Assert.Equal(
            AdminFulfillmentJobStageNames.History,
            Classify(AdminFulfillmentWorkUnitKinds.Parcel, "Delivered", []).Stage);
        Assert.Equal(
            AdminFulfillmentJobStageNames.History,
            Classify(AdminFulfillmentWorkUnitKinds.Parcel, "Cancelled", []).Stage);

        var history = Classify(AdminFulfillmentWorkUnitKinds.Fulfillment, "Delivered", []);
        Assert.Equal(AdminFulfillmentJobTimeMeanings.None, history.JobTimeMeaning);
        Assert.Equal(AdminFulfillmentJobTimeStates.Undated, history.JobTimeState);
    }

    [Fact]
    public void The_relevant_instant_and_meaning_follow_the_stage_never_a_merged_blur()
    {
        var preparation = AdminFulfillmentJobStageAuthority.Classify(
            AdminFulfillmentWorkUnitKinds.Group, "Packed", ["mark_processing"], true,
            preparationStartUtc: Now.AddHours(2),
            transferDepartureUtc: Now.AddDays(1),
            handoffDeadlineUtc: Now.AddDays(1).AddHours(4),
            timeZone: Tehran, nowUtc: Now);
        Assert.Equal(AdminFulfillmentJobTimeMeanings.PreparationStart, preparation.JobTimeMeaning);
        Assert.Equal("آخرین مهلت شروع آماده‌سازی", preparation.JobTimeLabelFa);
        Assert.Equal(Now.AddHours(2), preparation.JobTimeUtc);
        Assert.Equal(AdminFulfillmentJobTimeStates.DueToday, preparation.JobTimeState);

        var handoff = AdminFulfillmentJobStageAuthority.Classify(
            AdminFulfillmentWorkUnitKinds.Parcel, "Created", ["dispatch_shipment"], true,
            preparationStartUtc: Now.AddHours(-3),
            transferDepartureUtc: Now.AddHours(-1),
            handoffDeadlineUtc: Now.AddDays(1).AddHours(4),
            timeZone: Tehran, nowUtc: Now);
        Assert.Equal(AdminFulfillmentJobTimeMeanings.HandoffClose, handoff.JobTimeMeaning);
        Assert.Equal("آخرین مهلت تحویل به حامل", handoff.JobTimeLabelFa);
        Assert.Equal(AdminFulfillmentJobTimeStates.Future, handoff.JobTimeState);
    }

    [Fact]
    public void Preparation_today_with_a_handoff_tomorrow_belongs_to_today()
    {
        var facts = AdminFulfillmentJobStageAuthority.Classify(
            AdminFulfillmentWorkUnitKinds.Group, "Packed", ["mark_processing"], true,
            preparationStartUtc: Now.AddHours(1),
            transferDepartureUtc: Now.AddDays(1),
            handoffDeadlineUtc: Now.AddDays(1).AddHours(5),
            timeZone: Tehran, nowUtc: Now);

        Assert.Equal(AdminFulfillmentJobTimeStates.DueToday, facts.JobTimeState);
        Assert.True(AdminFulfillmentJobStageAuthority.MatchesTimeScope(facts.JobTimeState, "today"));
        Assert.False(AdminFulfillmentJobStageAuthority.MatchesTimeScope(facts.JobTimeState, "future"));
        // …and the SAME row is reachable through «همه».
        Assert.True(AdminFulfillmentJobStageAuthority.MatchesTimeScope(facts.JobTimeState, "all"));
    }

    [Fact]
    public void The_handoff_cutoff_is_half_open_so_exactly_at_the_close_it_is_already_late()
    {
        var facts = AdminFulfillmentJobStageAuthority.Classify(
            AdminFulfillmentWorkUnitKinds.Parcel, "Created", ["dispatch_shipment"], true,
            preparationStartUtc: null,
            transferDepartureUtc: null,
            handoffDeadlineUtc: Now,
            timeZone: Tehran, nowUtc: Now);

        Assert.Equal(AdminFulfillmentJobTimeStates.PastDue, facts.JobTimeState);
        Assert.True(AdminFulfillmentJobStageAuthority.MatchesTimeScope(facts.JobTimeState, "today"));
    }

    [Fact]
    public void The_day_boundary_is_evaluated_in_the_accepted_zone_only()
    {
        // 2026-11-15T21:00Z == 2026-11-16 00:30 in Tehran; the same instant is still 15 November in UTC.
        var lateUtcNow = new DateTimeOffset(2026, 11, 15, 21, 0, 0, TimeSpan.Zero);
        var instant = new DateTimeOffset(2026, 11, 16, 0, 30, 0, TimeSpan.Zero);

        var tehran = AdminFulfillmentJobStageAuthority.Classify(
            AdminFulfillmentWorkUnitKinds.Parcel, "Created", ["dispatch_shipment"], true,
            null, null, instant, Tehran, lateUtcNow);
        var utc = AdminFulfillmentJobStageAuthority.Classify(
            AdminFulfillmentWorkUnitKinds.Parcel, "Created", ["dispatch_shipment"], true,
            null, null, instant, "UTC", lateUtcNow);

        Assert.Equal(AdminFulfillmentJobTimeStates.DueToday, tehran.JobTimeState);
        Assert.Equal(AdminFulfillmentJobTimeStates.Future, utc.JobTimeState);
        // No browser zone is involved: the moment is ahead in both zones, only the DAY differs.
        Assert.NotEqual(tehran.JobTimeState, utc.JobTimeState);
    }

    [Fact]
    public void Delivery_and_return_follow_up_carry_no_invented_deadline()
    {
        foreach (var stage in new[] { AdminFulfillmentJobStageNames.Delivery, AdminFulfillmentJobStageNames.Returns })
        {
            var facts = stage == AdminFulfillmentJobStageNames.Delivery
                ? Classify(AdminFulfillmentWorkUnitKinds.Parcel, "Dispatched", ["deliver_shipment", "record_non_delivery"])
                : Classify(AdminFulfillmentWorkUnitKinds.Parcel, "Returning", ["record_return_receipt"]);

            Assert.Equal(AdminFulfillmentJobTimeMeanings.OngoingFollowUp, facts.JobTimeMeaning);
            Assert.Equal("پیگیری جاری؛ مهلت مشخصی ثبت نشده", facts.JobTimeLabelFa);
            Assert.Null(facts.JobTimeUtc);
            Assert.Equal(AdminFulfillmentJobTimeStates.Undated, facts.JobTimeState);
            // Undated CURRENT work stays visible in the default «امروز», clearly outside «آینده».
            Assert.True(AdminFulfillmentJobStageAuthority.MatchesTimeScope(facts.JobTimeState, "today"));
            Assert.False(AdminFulfillmentJobStageAuthority.MatchesTimeScope(facts.JobTimeState, "future"));
        }
    }

    [Fact]
    public void Counts_cover_the_whole_classified_set_per_dimension_and_never_an_archive()
    {
        var rows = new List<AdminFulfillmentWorkQueueRow>
        {
            Row("g1", AdminFulfillmentJobStageNames.Preparation, AdminFulfillmentJobTimeStates.DueToday),
            Row("p1", AdminFulfillmentJobStageNames.ReadyToSend, AdminFulfillmentJobTimeStates.Undated),
            Row("p2", AdminFulfillmentJobStageNames.Handoff, AdminFulfillmentJobTimeStates.PastDue),
            Row("p3", AdminFulfillmentJobStageNames.Delivery, AdminFulfillmentJobTimeStates.Undated),
            Row("p4", AdminFulfillmentJobStageNames.Returns, AdminFulfillmentJobTimeStates.Future),
            Row("h1", AdminFulfillmentJobStageNames.History, AdminFulfillmentJobTimeStates.Undated),
        };

        var counts = AdminFulfillmentWorkQueueCountsBuilder.Build(rows, "all", "today");

        // Stage badges: whole active set inside «امروز» = everything except the future-day row and the archive.
        Assert.Equal(4, counts.StageAll);
        Assert.Equal(1, counts.StagePreparation);
        Assert.Equal(1, counts.StageReadyToSend);
        Assert.Equal(1, counts.StageHandoff);
        Assert.Equal(1, counts.StageDelivery);
        Assert.Equal(0, counts.StageReturns);
        // The archive is time-independent and never part of an active total.
        Assert.Equal(1, counts.StageHistory);
        Assert.Equal(4, counts.SelectedTotal);
        Assert.Equal(2, counts.Undated);
        // Time badges describe the SELECTED stage (all) before the scope.
        Assert.Equal(4, counts.TimeToday);
        Assert.Equal(1, counts.TimeFuture);
        Assert.Equal(5, counts.TimeAll);
        Assert.Equal("all", counts.SelectedStage);
        Assert.Equal("today", counts.SelectedTimeScope);

        // Selecting one stage re-derives the time badges from THAT stage only.
        var returns = AdminFulfillmentWorkQueueCountsBuilder.Build(rows, "returns", "all");
        Assert.Equal(1, returns.SelectedTotal);
        Assert.Equal(0, returns.TimeToday);
        Assert.Equal(1, returns.TimeFuture);
        Assert.Equal(1, returns.TimeAll);
        Assert.Equal(1, returns.SelectedTotal);
    }

    [Fact]
    public void The_page_is_cut_from_exactly_the_counted_selection()
    {
        var rows = new List<AdminFulfillmentWorkQueueRow>
        {
            Row("g1", AdminFulfillmentJobStageNames.Preparation, AdminFulfillmentJobTimeStates.DueToday),
            Row("p1", AdminFulfillmentJobStageNames.Handoff, AdminFulfillmentJobTimeStates.Future),
            Row("h1", AdminFulfillmentJobStageNames.History, AdminFulfillmentJobTimeStates.Undated),
        };

        Assert.Equal(["handoff:p1"], AdminFulfillmentWorkQueueCountsBuilder
            .Select(rows, "handoff", "future")
            .Select(row => row.WorkUnitKey));
        // The archive selection is time-independent.
        Assert.Equal(["history"], AdminFulfillmentWorkQueueCountsBuilder
            .Select(rows, "history", "today")
            .Select(row => row.JobStage));
        // «امروز» never contains a later accepted day.
        Assert.DoesNotContain(AdminFulfillmentWorkQueueCountsBuilder.Select(rows, "all", "today"), row => row.WorkUnitKey == "handoff:p1");
    }

    [Fact]
    public void An_unknown_stage_or_scope_token_clears_to_the_documented_default()
    {
        Assert.Equal("all", AdminFulfillmentWorkQueueCountsBuilder.NormalizeStage("not-a-stage"));
        Assert.Equal("all", AdminFulfillmentWorkQueueCountsBuilder.NormalizeStage(null));
        Assert.Equal("returns", AdminFulfillmentWorkQueueCountsBuilder.NormalizeStage(" Returns "));
        Assert.Equal("today", AdminFulfillmentTimeScopeNames.Normalize("yesterday"));
        Assert.Equal("today", AdminFulfillmentTimeScopeNames.Normalize(null));
        Assert.Equal("future", AdminFulfillmentTimeScopeNames.Normalize("Future"));
    }

    [Fact]
    public void A_legacy_queue_token_is_mapped_once_instead_of_combining_with_a_hidden_filter()
    {
        Assert.Equal(
            (AdminFulfillmentJobStageNames.Preparation, AdminFulfillmentTimeScopeNames.All),
            AdminFulfillmentLegacyQueueSelection.Map("ready_to_process"));
        Assert.Equal(
            (AdminFulfillmentJobStageNames.Preparation, AdminFulfillmentTimeScopeNames.All),
            AdminFulfillmentLegacyQueueSelection.Map("ready_to_pack"));
        Assert.Equal(
            (AdminFulfillmentJobStageNames.ReadyToSend, AdminFulfillmentTimeScopeNames.All),
            AdminFulfillmentLegacyQueueSelection.Map("ready_to_ship"));
        Assert.Equal(
            (AdminFulfillmentJobStageNames.ReadyToSend, AdminFulfillmentTimeScopeNames.All),
            AdminFulfillmentLegacyQueueSelection.Map("missing_tracking"));
        Assert.Equal(
            (AdminFulfillmentJobStageNames.Delivery, AdminFulfillmentTimeScopeNames.All),
            AdminFulfillmentLegacyQueueSelection.Map("in_transit"));
        Assert.Equal(
            (AdminFulfillmentJobStageNames.History, AdminFulfillmentTimeScopeNames.All),
            AdminFulfillmentLegacyQueueSelection.Map("delivered"));
        Assert.Equal(
            (AdminFulfillmentJobStageNames.History, AdminFulfillmentTimeScopeNames.All),
            AdminFulfillmentLegacyQueueSelection.Map("problem"));
        // Timing tokens and unknown tokens clear to the live default work instead of inventing a stage.
        Assert.Equal(
            (AdminFulfillmentJobStageNames.All, AdminFulfillmentTimeScopeNames.Today),
            AdminFulfillmentLegacyQueueSelection.Map("dispatch_overdue"));
        Assert.Equal(
            (AdminFulfillmentJobStageNames.All, AdminFulfillmentTimeScopeNames.Today),
            AdminFulfillmentLegacyQueueSelection.Map("unknown-token"));
        Assert.Equal(
            (AdminFulfillmentJobStageNames.All, AdminFulfillmentTimeScopeNames.Today),
            AdminFulfillmentLegacyQueueSelection.Map(null));
    }

    [Fact]
    public void Default_order_puts_past_due_first_then_today_then_undated_then_future()
    {
        Assert.True(
            AdminFulfillmentJobStageAuthority.PriorityRank(AdminFulfillmentJobTimeStates.PastDue)
            < AdminFulfillmentJobStageAuthority.PriorityRank(AdminFulfillmentJobTimeStates.DueToday));
        Assert.True(
            AdminFulfillmentJobStageAuthority.PriorityRank(AdminFulfillmentJobTimeStates.DueToday)
            < AdminFulfillmentJobStageAuthority.PriorityRank(AdminFulfillmentJobTimeStates.Undated));
        Assert.True(
            AdminFulfillmentJobStageAuthority.PriorityRank(AdminFulfillmentJobTimeStates.Undated)
            < AdminFulfillmentJobStageAuthority.PriorityRank(AdminFulfillmentJobTimeStates.Future));
    }

    private static AdminFulfillmentJobStageFacts Classify(
        string kind,
        string state,
        IReadOnlyList<string> codes) =>
        AdminFulfillmentJobStageAuthority.Classify(
            kind,
            state,
            codes,
            hasOpenObligation: OpenObligation(state, codes),
            preparationStartUtc: null,
            transferDepartureUtc: null,
            handoffDeadlineUtc: null,
            timeZone: Tehran,
            nowUtc: Now,
            hasPreparationWork: codes.Any(code => code is "mark_processing" or "mark_packed"),
            hasAllocationWork: codes.Any(code => code == "create_shipment"));

    /// <summary>
    /// TB-271-R1: the pure-case physical obligation. A finished state owns nothing; a received source only owes work
    /// while a decision is still offered for it. These are only the fixture-side facts of this helper — the real
    /// projection derives them from quantities and parcel lifecycles.
    /// </summary>
    private static bool OpenObligation(string state, IReadOnlyList<string> codes) =>
        state is not ("Delivered" or "Cancelled" or "Failed")
        && (!string.Equals(state, "Returned", StringComparison.Ordinal) || codes.Count > 0);

    private static AdminFulfillmentWorkQueueRow Row(string key, string stage, string timeState) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "فروشنده",
            "ORD-1",
            "Packed",
            "گیرنده",
            "تهران",
            "post",
            "پست",
            1,
            1m,
            0m,
            0,
            null,
            string.Empty,
            Now,
            Now,
            [],
            WorkUnitKey: $"{stage}:{key}",
            WorkUnitKind: AdminFulfillmentWorkUnitKinds.Parcel,
            JobStage: stage,
            JobTimeState: timeState);
}
