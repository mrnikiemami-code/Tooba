using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Contracts.Scheduling;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.Shipping.Infrastructure.DependencyInjection;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.Shipping.Infrastructure.Scheduling;
using Xunit;

namespace Tooba.Shipping.Tests.Scheduling;

/// <summary>
/// TB-270-R1 — the additive Contracts seam that exposes the CAPTURED scheduling inputs of one exact
/// revision plus one exact accepted group/rate row, WITHOUT destination, measurements, currency, rate
/// selection, Geography or any provider call.
/// </summary>
/// <remarks>
/// The real module adapter runs against the real Shipping directory (EF InMemory, query-only) and the
/// real captured-revision snapshot body. No transaction/schema/provider claim is made here and no
/// activation/coverage/feasibility of a future attempt is implied by a successful read.
/// </remarks>
public sealed class ShippingAttemptSchedulingLookupTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid CityA = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid CityB = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
    private const string Tenant = "tb270r1-store";
    private const string Service = "post";
    private const string Option = "express";

    [Fact]
    public async Task An_exact_captured_read_maps_every_stored_input_without_inventing_anything()
    {
        var fixture = Build();
        using var database = new Database(fixture.Snapshot);

        var result = await Lookup(database).LookupAsync(
            Request(fixture, fixture.GroupA, fixture.RowA1),
            CancellationToken.None);

        Assert.Equal(ShippingAttemptSchedulingVerdict.CapturedInputs, result.Verdict);
        Assert.Equal(ShippingAttemptSchedulingReasonCodes.CapturedInputs, result.ReasonCode);
        Assert.True(result.HasInputs);

        var inputs = result.Inputs!;
        Assert.Equal(ShippingCapturedQuoteOwnerKind.Store, inputs.OwnerKind);
        Assert.Equal(Tenant, inputs.OwnerTenantId);
        Assert.Null(inputs.OwnerSellerPartyId);
        Assert.Equal(fixture.Snapshot.SnapshotId, inputs.SnapshotId);
        Assert.Equal(fixture.Snapshot.DraftId, inputs.DraftId);
        Assert.Equal(fixture.Snapshot.SourceRevision, inputs.SourceRevision);
        Assert.Equal(ShippingAuthoringRevisionSnapshot.ProcessingHoursFormatVersion, inputs.FormatVersion);
        Assert.Equal(Service, inputs.ServiceCode);
        Assert.Equal(Option, inputs.OptionCode);
        Assert.Equal(fixture.GroupA, inputs.GroupId);
        Assert.Equal(fixture.RowA1, inputs.RateRowId);

        // The accepted row's OWN stored transport bounds, not the sibling's and not a default.
        Assert.Equal(2, inputs.MinDeliveryDays);
        Assert.Equal(5, inputs.MaxDeliveryDays);

        // The explicit physical timing policy, exactly as stored.
        Assert.Equal(240, inputs.PreparationMinutes);
        Assert.Equal(60, inputs.HandoffMinutes);
        Assert.Equal(
            ShippingAuthoringPreparationProcessingMode.WorkingWeek,
            inputs.ProcessingMode);

        // Receiving week: closed weekday keeps its remembered hours; the slot duration is preserved.
        var receiving = Assert.IsType<ShippingAttemptReceivingWeekView>(inputs.ReceivingWeek);
        Assert.Equal("Asia/Tehran", receiving.TimeZoneId);
        Assert.Equal(120, receiving.SlotDurationMinutes);
        Assert.Equal(7, receiving.Days.Count);
        var closedReceivingDay = receiving.Days.Single(day => day.Weekday == 2);
        Assert.False(closedReceivingDay.IsWorking);
        Assert.Equal(new TimeOnly(10, 0), closedReceivingDay.OpensAt);
        Assert.Equal(new TimeOnly(12, 0), closedReceivingDay.ClosesAt);
        Assert.Equal(
            new TimeOnly(9, 0),
            receiving.Days.Single(day => day.Weekday == 1).OpensAt);

        // Carrier week is its OWN stored calendar: an independent zone and its own closed day.
        var carrier = Assert.IsType<ShippingSchedulingWeekView>(inputs.CarrierWeek);
        Assert.Equal("Asia/Tehran", carrier.TimeZoneId);
        Assert.Equal(7, carrier.Days.Count);
        var closedCarrierDay = carrier.Days.Single(day => day.Weekday == 5);
        Assert.False(closedCarrierDay.IsWorking);
        Assert.Null(closedCarrierDay.OpensAt);
        Assert.Equal(
            new TimeOnly(20, 0),
            carrier.Days.Single(day => day.Weekday == 1).ClosesAt);

        // Processing week keeps its own identifier: the three weeks are never collapsed into one.
        var processing = Assert.IsType<ShippingSchedulingWeekView>(inputs.ProcessingWeek);
        Assert.Equal("Asia/Dubai", processing.TimeZoneId);
        Assert.Equal(7, processing.Days.Count);
        Assert.False(processing.Days.Single(day => day.Weekday == 6).IsWorking);
        Assert.Equal(
            new TimeOnly(16, 0),
            processing.Days.Single(day => day.Weekday == 1).ClosesAt);

        Assert.Empty(inputs.MissingInputs);
        Assert.True(inputs.HasCompleteInputs);

        // Read-only: the exact revision is read with AsNoTracking and nothing is captured/written.
        Assert.Empty(database.Context.ChangeTracker.Entries());
        Assert.Equal(1, database.Context.AuthoringRevisionSnapshots.Count());
    }

    [Fact]
    public async Task A_measurement_dependent_row_returns_its_own_duration_without_destination_or_size()
    {
        var fixture = Build();
        using var database = new Database(fixture.Snapshot);

        // Two rows in ONE group carry DIFFERENT stored durations; only the exact requested id answers.
        var second = await Lookup(database).LookupAsync(
            Request(fixture, fixture.GroupA, fixture.RowA2),
            CancellationToken.None);
        Assert.Equal(ShippingAttemptSchedulingVerdict.CapturedInputs, second.Verdict);
        Assert.Equal(fixture.RowA2, second.Inputs!.RateRowId);
        Assert.Equal(7, second.Inputs.MinDeliveryDays);
        Assert.Equal(9, second.Inputs.MaxDeliveryDays);

        var first = await Lookup(database).LookupAsync(
            Request(fixture, fixture.GroupA, fixture.RowA1),
            CancellationToken.None);
        Assert.Equal(fixture.RowA1, first.Inputs!.RateRowId);
        Assert.Equal(2, first.Inputs.MinDeliveryDays);
        Assert.Equal(5, first.Inputs.MaxDeliveryDays);

        // A row whose stored conditions are count/amount based is read the same way: no weight, no
        // destination, no currency and no rate selection is supplied anywhere in the request.
        var otherGroup = await Lookup(database).LookupAsync(
            Request(fixture, fixture.GroupB, fixture.RowB1),
            CancellationToken.None);
        Assert.Equal(ShippingAttemptSchedulingVerdict.CapturedInputs, otherGroup.Verdict);
        Assert.Equal(fixture.GroupB, otherGroup.Inputs!.GroupId);
        Assert.Equal(12, otherGroup.Inputs.MinDeliveryDays);
        Assert.Equal(20, otherGroup.Inputs.MaxDeliveryDays);

        // A group id is not a ticket to a row of another group.
        var foreign = await Lookup(database).LookupAsync(
            Request(fixture, fixture.GroupB, fixture.RowA1),
            CancellationToken.None);
        Assert.Equal(ShippingAttemptSchedulingVerdict.RateRowUnavailable, foreign.Verdict);
        Assert.Null(foreign.Inputs);
    }

    [Fact]
    public async Task Wrong_owner_and_absent_capture_expose_no_usable_payload()
    {
        var fixture = Build();
        using var database = new Database(fixture.Snapshot);

        // A residual/foreign tenant context is unusable: nothing is defaulted.
        var wrongTenant = await Lookup(database, tenant: "tb270r1-other").LookupAsync(
            Request(fixture, fixture.GroupA, fixture.RowA1),
            CancellationToken.None);
        AssertRefused(wrongTenant, ShippingAttemptSchedulingVerdict.OwnerScopeUnavailable);

        // SingleStore edition cannot answer for a Marketplace/absent tenant pairing either.
        var wrongEdition = await Lookup(database, edition: ToobaEdition.Marketplace, tenant: null).LookupAsync(
            Request(fixture, fixture.GroupA, fixture.RowA1),
            CancellationToken.None);
        AssertRefused(wrongEdition, ShippingAttemptSchedulingVerdict.OwnerScopeUnavailable);

        // An un-captured revision is a safe not-found: no latest/current fallback exists.
        var missingRevision = await Lookup(database).LookupAsync(
            Request(fixture, fixture.GroupA, fixture.RowA1, revision: fixture.Snapshot.SourceRevision + 7),
            CancellationToken.None);
        AssertRefused(missingRevision, ShippingAttemptSchedulingVerdict.CapturedRevisionUnavailable);

        // Another owner's capture is the same safe not-found, never a cross-owner read.
        var foreignSeller = new ShippingAttemptSchedulingLookupRequest(
            ShippingCapturedQuoteOwnerKind.Seller,
            null,
            Guid.NewGuid(),
            fixture.Snapshot.DraftId,
            fixture.Snapshot.SourceRevision,
            Service,
            Option,
            fixture.GroupA,
            fixture.RowA1);
        using var sellerDatabase = new Database(fixture.Snapshot);
        var foreignOwner = await Lookup(sellerDatabase, edition: ToobaEdition.Marketplace, tenant: null)
            .LookupAsync(foreignSeller, CancellationToken.None);
        AssertRefused(foreignOwner, ShippingAttemptSchedulingVerdict.CapturedRevisionUnavailable);

        // A caller-bound snapshot identity must be the one that was actually resolved.
        var boundMismatch = await Lookup(database).LookupAsync(
            Request(fixture, fixture.GroupA, fixture.RowA1, expectedSnapshotId: Guid.NewGuid()),
            CancellationToken.None);
        AssertRefused(boundMismatch, ShippingAttemptSchedulingVerdict.SnapshotMismatch);

        // The bound identity that DOES match is answered normally.
        var boundMatch = await Lookup(database).LookupAsync(
            Request(fixture, fixture.GroupA, fixture.RowA1, expectedSnapshotId: fixture.Snapshot.SnapshotId),
            CancellationToken.None);
        Assert.Equal(ShippingAttemptSchedulingVerdict.CapturedInputs, boundMatch.Verdict);
    }

    [Fact]
    public async Task Service_option_group_and_ambiguous_identity_are_refused_explicitly()
    {
        var fixture = Build();
        using var database = new Database(fixture.Snapshot);

        var wrongService = await Lookup(database).LookupAsync(
            Request(fixture, fixture.GroupA, fixture.RowA1, service: "tipax"),
            CancellationToken.None);
        AssertRefused(wrongService, ShippingAttemptSchedulingVerdict.ServiceOptionMismatch);

        var wrongOption = await Lookup(database).LookupAsync(
            Request(fixture, fixture.GroupA, fixture.RowA1, option: null),
            CancellationToken.None);
        AssertRefused(wrongOption, ShippingAttemptSchedulingVerdict.ServiceOptionMismatch);

        // The canonical comparison (trim + case-insensitive) still matches the same stored identity.
        var canonical = await Lookup(database).LookupAsync(
            Request(fixture, fixture.GroupA, fixture.RowA1, service: " POST ", option: " EXPRESS "),
            CancellationToken.None);
        Assert.Equal(ShippingAttemptSchedulingVerdict.CapturedInputs, canonical.Verdict);
        Assert.Equal(Service, canonical.Inputs!.ServiceCode);
        Assert.Equal(Option, canonical.Inputs.OptionCode);

        var unknownGroup = await Lookup(database).LookupAsync(
            Request(fixture, Guid.NewGuid(), fixture.RowA1),
            CancellationToken.None);
        AssertRefused(unknownGroup, ShippingAttemptSchedulingVerdict.GroupUnavailable);

        // Two stored groups with one id is a data conflict, never "the first one".
        using var duplicateGroups = Corrupted(fixture, payload => payload with
        {
            Groups = [.. payload.Groups, payload.Groups.Single(group => group.GroupId == fixture.GroupA)],
        });
        var ambiguousGroup = await Lookup(duplicateGroups).LookupAsync(
            Request(fixture, fixture.GroupA, fixture.RowA1),
            CancellationToken.None);
        AssertRefused(ambiguousGroup, ShippingAttemptSchedulingVerdict.GroupAmbiguous);

        // Two stored rows with one id in the SAME group is refused, not first-matched.
        using var duplicateRows = Corrupted(fixture, payload => payload with
        {
            Groups = payload.Groups
                .Select(group => group.GroupId != fixture.GroupA
                    ? group
                    : group with
                    {
                        RateRows = [.. group.RateRows, group.RateRows.Single(row => row.RateRowId == fixture.RowA1)],
                    })
                .ToArray(),
        });
        var ambiguousRow = await Lookup(duplicateRows).LookupAsync(
            Request(fixture, fixture.GroupA, fixture.RowA1),
            CancellationToken.None);
        AssertRefused(ambiguousRow, ShippingAttemptSchedulingVerdict.RateRowAmbiguous);
    }

    [Fact]
    public async Task A_historical_capture_reads_honestly_and_states_what_it_lacks()
    {
        // Format 1: no carrier acceptance and no processing week, no slot duration. A lawful legacy body.
        var legacy = Build(withCarrier: false, withProcessing: false, slotMinutes: null);
        using var legacyDatabase = new Database(legacy.Snapshot);
        var legacyRead = await Lookup(legacyDatabase).LookupAsync(
            Request(legacy, legacy.GroupA, legacy.RowA1),
            CancellationToken.None);

        Assert.Equal(ShippingAttemptSchedulingVerdict.CapturedInputs, legacyRead.Verdict);
        Assert.Equal(ShippingAuthoringRevisionSnapshot.LegacyFormatVersion, legacyRead.Inputs!.FormatVersion);
        Assert.Null(legacyRead.Inputs.CarrierWeek);
        Assert.Null(legacyRead.Inputs.ProcessingWeek);
        // A legacy full-day receiving window keeps its meaning: no slot duration is invented.
        Assert.Null(legacyRead.Inputs.ReceivingWeek!.SlotDurationMinutes);
        Assert.Equal(
            new[] { ShippingAttemptSchedulingInputCodes.CarrierWeek, ShippingAttemptSchedulingInputCodes.ProcessingWeek },
            legacyRead.Inputs.MissingInputs);
        Assert.False(legacyRead.Inputs.HasCompleteInputs);

        // Format 3: carrier acceptance exists, the processing week still does not.
        var carrierOnly = Build(withProcessing: false);
        using var carrierDatabase = new Database(carrierOnly.Snapshot);
        var carrierRead = await Lookup(carrierDatabase).LookupAsync(
            Request(carrierOnly, carrierOnly.GroupA, carrierOnly.RowA1),
            CancellationToken.None);
        Assert.Equal(ShippingAttemptSchedulingVerdict.CapturedInputs, carrierRead.Verdict);
        Assert.Equal(
            ShippingAuthoringRevisionSnapshot.CarrierAcceptanceFormatVersion,
            carrierRead.Inputs!.FormatVersion);
        Assert.NotNull(carrierRead.Inputs.CarrierWeek);
        Assert.Null(carrierRead.Inputs.ProcessingWeek);
        Assert.Equal(
            new[] { ShippingAttemptSchedulingInputCodes.ProcessingWeek },
            carrierRead.Inputs.MissingInputs);

        // Absent timing policy is a stored absence, not 0/0 and not an assumed continuous week.
        var noTiming = Build(withTiming: false);
        using var noTimingDatabase = new Database(noTiming.Snapshot);
        var noTimingRead = await Lookup(noTimingDatabase).LookupAsync(
            Request(noTiming, noTiming.GroupA, noTiming.RowA1),
            CancellationToken.None);
        Assert.Equal(ShippingAttemptSchedulingVerdict.CapturedInputs, noTimingRead.Verdict);
        Assert.Null(noTimingRead.Inputs!.PreparationMinutes);
        Assert.Null(noTimingRead.Inputs.HandoffMinutes);
        Assert.Equal(
            ShippingAuthoringPreparationProcessingMode.Unspecified,
            noTimingRead.Inputs.ProcessingMode);
        Assert.Contains(
            ShippingAttemptSchedulingInputCodes.PreparationHandoffMinutes,
            noTimingRead.Inputs.MissingInputs);
        Assert.Contains(
            ShippingAttemptSchedulingInputCodes.ProcessingMode,
            noTimingRead.Inputs.MissingInputs);

        // An unknown/corrupt format is refused through the canonical payload validation: no payload.
        using var corrupt = Corrupted(legacy, payload => payload, formatVersion: 99);
        var corruptRead = await Lookup(corrupt).LookupAsync(
            Request(legacy, legacy.GroupA, legacy.RowA1),
            CancellationToken.None);
        AssertRefused(corruptRead, ShippingAttemptSchedulingVerdict.CapturedRevisionUnavailable);

        // A body whose owner/draft/revision headers disagree with its row is corrupt, not a read.
        using var mismatchedHeader = Corrupted(legacy, payload => payload with
        {
            SourceRevision = payload.SourceRevision + 5,
        });
        var mismatchedRead = await Lookup(mismatchedHeader).LookupAsync(
            Request(legacy, legacy.GroupA, legacy.RowA1),
            CancellationToken.None);
        AssertRefused(mismatchedRead, ShippingAttemptSchedulingVerdict.CapturedRevisionUnavailable);
    }

    [Fact]
    public async Task Cancellation_and_unexpected_failure_are_never_disguised_as_a_missing_capture()
    {
        var fixture = Build();

        // An unexpected dependency failure propagates unchanged, with its own identity.
        var sentinel = new InvalidOperationException("directory-sentinel");
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ShippingAttemptSchedulingLookup(
                    new FaultyDrafts { Failure = sentinel },
                    new StubEdition(ToobaEdition.SingleStore),
                    new StubTenant(Tenant),
                    NullLogger<ShippingAttemptSchedulingLookup>.Instance)
                .LookupAsync(Request(fixture, fixture.GroupA, fixture.RowA1), CancellationToken.None));
        Assert.Same(sentinel, thrown);

        // The caller's token is never swallowed and never converted into "unavailable".
        using var cancelSource = new CancellationTokenSource();
        var token = cancelSource.Token;
        cancelSource.Cancel();
        var cancelled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ShippingAttemptSchedulingLookup(
                    new FaultyDrafts { CancelWith = token },
                    new StubEdition(ToobaEdition.SingleStore),
                    new StubTenant(Tenant),
                    NullLogger<ShippingAttemptSchedulingLookup>.Instance)
                .LookupAsync(Request(fixture, fixture.GroupA, fixture.RowA1), token));
        Assert.Equal(token, cancelled.CancellationToken);
    }

    [Fact]
    public void The_real_module_registers_exactly_one_scoped_attempt_scheduling_lookup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentEdition>(new StubEdition(ToobaEdition.SingleStore));
        services.AddSingleton<ICurrentCommerceContext>(new StubCommerceContext());
        services.AddSingleton<IDatabaseConnectionResolver>(new StubConnectionResolver());
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<ICurrentTenant>(new StubTenant(null));

        new ShippingModule().AddServices(
            services,
            new ConfigurationBuilder().Build(),
            new StubHostEnvironment());

        services.AddSingleton(new DbContextOptionsBuilder<ShippingDbContext>()
            .UseInMemoryDatabase($"tb270r1-di-{Guid.NewGuid():N}")
            .Options);

        var descriptor = Assert.Single(services
            .Where(candidate => candidate.ServiceType == typeof(IShippingAttemptSchedulingLookup))
            .ToArray());
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(ShippingAttemptSchedulingLookup), descriptor.ImplementationType);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var a = first.ServiceProvider.GetRequiredService<IShippingAttemptSchedulingLookup>();
        var b = first.ServiceProvider.GetRequiredService<IShippingAttemptSchedulingLookup>();
        var c = second.ServiceProvider.GetRequiredService<IShippingAttemptSchedulingLookup>();
        Assert.IsType<ShippingAttemptSchedulingLookup>(a);
        Assert.Same(a, b);
        Assert.NotSame(a, c);
    }

    [Fact]
    public async Task The_seam_is_addressed_by_the_CAPTURED_area_group_and_refuses_an_order_side_group_identity()
    {
        // TB-270-R3 Repair A: `payload.Groups[].GroupId` (the captured area group) and the Order checkout
        // shipping-group id are DIFFERENT identity namespaces. Only the captured id can address this read.
        var fixture = Build();
        using var database = new Database(fixture.Snapshot);

        var captured = await Lookup(database).LookupAsync(
            Request(fixture, fixture.GroupA, fixture.RowA1),
            CancellationToken.None);
        Assert.Equal(ShippingAttemptSchedulingVerdict.CapturedInputs, captured.Verdict);
        Assert.Equal(fixture.GroupA, captured.Inputs!.GroupId);

        // An Order-side group id (a different namespace entirely) is not a group of this capture: no payload at all.
        var orderSideGroupId = Guid.Parse("01a1d270-0000-7000-8000-000000000007");
        var refused = await Lookup(database).LookupAsync(
            Request(fixture, orderSideGroupId, fixture.RowA1),
            CancellationToken.None);
        AssertRefused(refused, ShippingAttemptSchedulingVerdict.GroupUnavailable);

        // The exact rate row of the captured group is still found when the captured group is addressed.
        var siblingRow = await Lookup(database).LookupAsync(
            Request(fixture, fixture.GroupA, fixture.RowA2),
            CancellationToken.None);
        Assert.Equal(ShippingAttemptSchedulingVerdict.CapturedInputs, siblingRow.Verdict);
        Assert.Equal(fixture.GroupA, siblingRow.Inputs!.GroupId);
        Assert.Equal(fixture.RowA2, siblingRow.Inputs.RateRowId);
    }

    // ------------------------------------------------------------------ fixtures

    private sealed record Fixture(
        ShippingAuthoringRevisionSnapshot Snapshot,
        Guid RowA1,
        Guid RowA2,
        Guid RowB1)
    {
        public Guid GroupA { get; init; }

        public Guid GroupB { get; init; }
    }

    private static ShippingAttemptSchedulingLookup Lookup(
        Database database,
        ToobaEdition edition = ToobaEdition.SingleStore,
        string? tenant = Tenant)
        => new(
            new ShippingAuthoringDraftDirectory(database.Context),
            new StubEdition(edition),
            new StubTenant(tenant),
            NullLogger<ShippingAttemptSchedulingLookup>.Instance);

    private static ShippingAttemptSchedulingLookupRequest Request(
        Fixture fixture,
        Guid groupId,
        Guid rateRowId,
        string service = Service,
        string? option = Option,
        long? revision = null,
        Guid? expectedSnapshotId = null)
        => new(
            ShippingCapturedQuoteOwnerKind.Store,
            Tenant,
            null,
            fixture.Snapshot.DraftId,
            revision ?? fixture.Snapshot.SourceRevision,
            service,
            option,
            groupId,
            rateRowId,
            expectedSnapshotId);

    private static void AssertRefused(
        ShippingAttemptSchedulingResult result,
        ShippingAttemptSchedulingVerdict expected)
    {
        Assert.Equal(expected, result.Verdict);
        Assert.False(result.HasInputs);
        Assert.Null(result.Inputs);
        Assert.NotEqual(ShippingAttemptSchedulingReasonCodes.CapturedInputs, result.ReasonCode);
    }

    private static Fixture Build(
        bool withTiming = true,
        bool withCarrier = true,
        bool withProcessing = true,
        int? slotMinutes = 120,
        ShippingFreightPayerMode payerMode = ShippingFreightPayerMode.Prepaid)
    {
        var draft = ShippingAuthoringDraft.Create(
            ShippingDraftOwner.ForStore(Tenant),
            Service,
            Option,
            [
                new ShippingAuthoringRegionGroupInput(
                    null,
                    [new ShippingDestinationNode(ShippingDestinationNodeKind.City, CityA)]),
                new ShippingAuthoringRegionGroupInput(
                    null,
                    [new ShippingDestinationNode(ShippingDestinationNodeKind.City, CityB)]),
            ],
            Now);

        // Group identity is aggregate-owned: the two stable ids are read back, never assumed.
        var ordered = draft.RegionGroups.OrderBy(group => group.Order).ToArray();
        var groupA = ordered[0].GroupId;
        var groupB = ordered[1].GroupId;

        draft.ReplaceGroupRateRows(
            groupA,
            [
                new(null, 0, 5_000, null, null, null, null, 100_000.00m, 5_000.00m, 2, 5),
                new(null, 5_001, null, null, null, null, null, 250_000.00m, 9_000.00m, 7, 9),
            ],
            "IRT",
            Now.AddMinutes(1));

        draft.ReplaceGroupRateRows(
            groupB,
            [
                new(null, null, null, 5, 10, 100_000.00m, 500_000.00m, 400_000.00m, 1_000.00m, 12, 20),
            ],
            "IRT",
            Now.AddMinutes(2));

        draft.ApplyWeeklySchedule(
            draft.PreflightWeeklySchedule(
                "Asia/Tehran",
                [
                    new ShippingAuthoringDraft.ScheduleDayInput(1, true, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                    new ShippingAuthoringDraft.ScheduleDayInput(2, false, new TimeOnly(10, 0), new TimeOnly(12, 0)),
                    new ShippingAuthoringDraft.ScheduleDayInput(3, true, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                    new ShippingAuthoringDraft.ScheduleDayInput(4, true, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                    new ShippingAuthoringDraft.ScheduleDayInput(5, true, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                    new ShippingAuthoringDraft.ScheduleDayInput(6, true, new TimeOnly(9, 0), new TimeOnly(13, 0)),
                    new ShippingAuthoringDraft.ScheduleDayInput(7, false, null, null),
                ],
                slotMinutes),
            Now.AddMinutes(3));

        if (withCarrier)
        {
            draft.ApplyCarrierSchedule(
                draft.PreflightCarrierSchedule(
                    "Asia/Tehran",
                    [
                        new ShippingAuthoringDraft.CarrierScheduleDayInput(1, true, new TimeOnly(8, 0), new TimeOnly(20, 0)),
                        new ShippingAuthoringDraft.CarrierScheduleDayInput(2, true, new TimeOnly(8, 0), new TimeOnly(18, 0)),
                        new ShippingAuthoringDraft.CarrierScheduleDayInput(3, true, new TimeOnly(8, 0), new TimeOnly(18, 0)),
                        new ShippingAuthoringDraft.CarrierScheduleDayInput(4, true, new TimeOnly(8, 0), new TimeOnly(18, 0)),
                        new ShippingAuthoringDraft.CarrierScheduleDayInput(5, false, null, null),
                        new ShippingAuthoringDraft.CarrierScheduleDayInput(6, true, new TimeOnly(9, 0), new TimeOnly(14, 0)),
                        new ShippingAuthoringDraft.CarrierScheduleDayInput(7, false, null, null),
                    ]),
                Now.AddMinutes(4));
        }

        if (withProcessing)
        {
            draft.ApplyProcessingSchedule(
                draft.PreflightProcessingSchedule(
                    "Asia/Dubai",
                    [
                        new ShippingAuthoringDraft.ProcessingScheduleDayInput(1, true, new TimeOnly(8, 0), new TimeOnly(16, 0)),
                        new ShippingAuthoringDraft.ProcessingScheduleDayInput(2, true, new TimeOnly(8, 0), new TimeOnly(16, 0)),
                        new ShippingAuthoringDraft.ProcessingScheduleDayInput(3, true, new TimeOnly(8, 0), new TimeOnly(16, 0)),
                        new ShippingAuthoringDraft.ProcessingScheduleDayInput(4, true, new TimeOnly(8, 0), new TimeOnly(16, 0)),
                        new ShippingAuthoringDraft.ProcessingScheduleDayInput(5, true, new TimeOnly(8, 0), new TimeOnly(16, 0)),
                        new ShippingAuthoringDraft.ProcessingScheduleDayInput(6, false, null, null),
                        new ShippingAuthoringDraft.ProcessingScheduleDayInput(7, false, null, null),
                    ]),
                Now.AddMinutes(5));
        }

        draft.ApplyMethodSettings(
            withTiming
                ? draft.PreflightMethodSettings(
                    payerMode,
                    ShippingAuthoringCalculationMode.Manual,
                    240,
                    60,
                    ShippingAuthoringPreparationProcessingMode.WorkingWeek)
                : draft.PreflightMethodSettings(payerMode, ShippingAuthoringCalculationMode.Manual),
            Now.AddMinutes(6));

        draft.ApplyTypeAssignment(draft.PreflightTypeAssignment(null), Now.AddMinutes(7));

        var snapshot = ShippingAuthoringRevisionSnapshot.Capture(draft, Now.AddHours(1));
        var payload = snapshot.ReadPayload();
        var capturedA = payload.Groups.Single(group => group.GroupId == groupA);

        return new Fixture(
            snapshot,
            capturedA.RateRows.Single(row => row.MinWeightGrams == 0).RateRowId,
            capturedA.RateRows.Single(row => row.MinWeightGrams == 5_001).RateRowId,
            payload.Groups.Single(group => group.GroupId == groupB).RateRows.Single().RateRowId)
        {
            GroupA = groupA,
            GroupB = groupB,
        };
    }

    /// <summary>
    /// یک ردیف capture <b>عمداً ناسازگار</b> برای اثبات fail-closed؛ بدنه با همان رمزگذار واقعی
    /// بازسازی می‌شود، نه با JSON دستی.
    /// </summary>
    /// <param name="fixture">شاهد سالم.</param>
    /// <param name="mutate">تغییر بدنه پیش از ذخیره.</param>
    /// <param name="formatVersion">نسخهٔ قالب هدر/بدنه یا تهی برای حفظ نسخهٔ اصلی.</param>
    private static Database Corrupted(
        Fixture fixture,
        Func<ShippingAuthoringRevisionPayload, ShippingAuthoringRevisionPayload> mutate,
        int? formatVersion = null)
    {
        var snapshot = fixture.Snapshot;
        var payload = mutate(snapshot.ReadPayload());
        var version = formatVersion ?? payload.FormatVersion;
        payload = payload with { FormatVersion = version };

        var restored = ShippingAuthoringRevisionSnapshot.RestoreForRead(
            snapshot.SnapshotId,
            snapshot.OwnerKind,
            snapshot.OwnerTenantId,
            snapshot.OwnerSellerPartyId,
            snapshot.DraftId,
            snapshot.SourceRevision,
            version,
            snapshot.CapturedAt,
            JsonSerializer.Serialize(
                payload,
                new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.Never }));

        return new Database(restored);
    }

    private sealed class Database : IDisposable
    {
        public Database(ShippingAuthoringRevisionSnapshot snapshot)
        {
            Context = new ShippingDbContext(new DbContextOptionsBuilder<ShippingDbContext>()
                .UseInMemoryDatabase($"tb270r1-{Guid.NewGuid():N}")
                .Options);
            Context.AuthoringRevisionSnapshots.Add(snapshot);
            Context.SaveChanges();
            Context.ChangeTracker.Clear();
        }

        public ShippingDbContext Context { get; }

        public void Dispose() => Context.Dispose();
    }

    /// <summary>
    /// درز ماندگاری معیوب: همهٔ متدها پشتیبانی نمی‌شوند و فقط خواندن capture یا شکست سنتینل را
    /// می‌دهد یا توکن فراخوان را عیناً لغو می‌کند.
    /// </summary>
    private sealed class FaultyDrafts : IShippingAuthoringDraftDirectory
    {
        public Exception? Failure { get; init; }

        public CancellationToken? CancelWith { get; init; }

        public Task<Result<ShippingAuthoringRevisionSnapshot>> GetCapturedRevisionAsync(
            ShippingAuthoringOwnerScope owner,
            Guid draftId,
            long sourceRevision,
            CancellationToken cancellationToken)
        {
            if (Failure is { } failure)
            {
                throw failure;
            }

            throw new OperationCanceledException(CancelWith ?? cancellationToken);
        }

        public Task<Result<IReadOnlyList<ShippingAuthoringDraft>>> ListAsync(
            ShippingAuthoringOwnerScope owner,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Result<ShippingAuthoringDraft>> GetAsync(
            ShippingAuthoringOwnerScope owner,
            Guid draftId,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Result<ShippingAuthoringDraft>> CreateAsync(
            ShippingAuthoringOwnerScope owner,
            string serviceCode,
            string? optionCode,
            IReadOnlyList<ShippingAuthoringRegionGroupInput> regionGroups,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Result<ShippingAuthoringDraft>> ReplaceRegionGroupsAsync(
            ShippingAuthoringOwnerScope owner,
            Guid draftId,
            long expectedRevision,
            IReadOnlyList<ShippingAuthoringRegionGroupInput> regionGroups,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Result<ShippingAuthoringDraft>> ReplaceGroupRateRowsAsync(
            ShippingAuthoringOwnerScope owner,
            Guid draftId,
            Guid groupId,
            long expectedRevision,
            IReadOnlyList<ShippingAuthoringDraft.ShippingAuthoringRateRowInput> rows,
            string trustedCurrencyCode,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Result<ShippingAuthoringDraft>> ReplacePresentationAsync(
            ShippingAuthoringOwnerScope owner,
            Guid draftId,
            long expectedRevision,
            IReadOnlyList<ShippingAuthoringDraft.MethodTranslationInput> translations,
            IReadOnlyList<ShippingAuthoringDraft.GroupTitleInput> titles,
            IReadOnlySet<Guid> activeLanguageIds,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Result<ShippingAuthoringDraft>> ReplaceWeeklyScheduleAsync(
            ShippingAuthoringOwnerScope owner,
            Guid draftId,
            long expectedRevision,
            string? timeZoneId,
            IReadOnlyList<ShippingAuthoringDraft.ScheduleDayInput> days,
            CancellationToken cancellationToken,
            int? slotDurationMinutes = null)
            => throw new NotSupportedException();

        public Task<Result<ShippingAuthoringDraft>> ReplaceMethodSettingsAsync(
            ShippingAuthoringOwnerScope owner,
            Guid draftId,
            long expectedRevision,
            ShippingFreightPayerMode? freightPaymentMode,
            ShippingAuthoringCalculationMode? calculationMode,
            CancellationToken cancellationToken,
            int? preparationMinutes = null,
            int? handoffMinutes = null,
            ShippingAuthoringPreparationProcessingMode? preparationMode = null)
            => throw new NotSupportedException();

        public Task<Result<ShippingAuthoringDraft>> ReplaceTypeAssignmentAsync(
            ShippingAuthoringOwnerScope owner,
            Guid draftId,
            long expectedRevision,
            Guid? shippingTypeId,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Result<ShippingAuthoringDraft>> ReplaceEditorDocumentAsync(
            ShippingAuthoringOwnerScope owner,
            Guid draftId,
            long expectedRevision,
            ShippingAuthoringDraft.EditorDocumentInput input,
            IReadOnlySet<Guid> activeLanguageIds,
            string trustedCurrencyCode,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Result<ShippingAuthoringRevisionSnapshot>> CaptureRevisionAsync(
            ShippingAuthoringOwnerScope owner,
            Guid draftId,
            long expectedRevision,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class StubEdition(ToobaEdition edition) : ICurrentEdition
    {
        public EditionContext? Current => new(edition, "test");
    }

    private sealed class StubTenant(string? tenantId) : ICurrentTenant
    {
        public TenantContext? Current => tenantId is null
            ? null
            : new TenantContext(
                new TenantId(tenantId),
                TenantStatus.Active,
                new ConnectionReference("test"),
                null,
                null,
                null,
                "localhost",
                null);
    }

    private sealed class StubCommerceContext : ICurrentCommerceContext
    {
        public CommerceContext? Current => null;
    }

    private sealed class StubConnectionResolver : IDatabaseConnectionResolver
    {
        public string Resolve(ConnectionReference reference) => "Host=127.0.0.1;Database=unused";
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "Tooba.Shipping.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
