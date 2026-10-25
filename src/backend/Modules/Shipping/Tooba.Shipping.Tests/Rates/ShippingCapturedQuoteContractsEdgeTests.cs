using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Domain.Rates;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.Shipping.Infrastructure.DependencyInjection;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.Shipping.Infrastructure.Rates;
using Xunit;

namespace Tooba.Shipping.Tests.Rates;

/// <summary>
/// TB-216 — the Contracts edge over the EXISTING captured-revision quote authority: exact owner/draft/
/// revision read, verified Geography path, one engine call, explicit Domain→Contracts mapping.
/// </summary>
/// <remarks>
/// The real module adapter runs against the real Shipping directory (EF InMemory, query-only) and a
/// controlled Geography Contracts data source. No transaction/schema/provider claim is made here and no
/// customer activation is implied by a monetary answer.
/// </remarks>
[Collection(nameof(Tooba.Shipping.Tests.Observability.ShippingModuleCallTracingCollection))]
public sealed class ShippingCapturedQuoteContractsEdgeTests
{
    private static readonly Guid Country = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid RootArea = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid DeepArea = Guid.Parse("22222222-2222-4222-8222-222222222223");
    private static readonly Guid OtherCountry = Guid.Parse("11111111-1111-4111-8111-111111111119");
    private static readonly Guid CityId = Guid.Parse("33333333-3333-4333-8333-333333333333");
    private static readonly Guid TypeId = Guid.Parse("66666666-6666-4666-8666-666666666666");
    private const string Service = "post";
    private const string Tenant = "tb216-store";
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void The_real_module_registers_exactly_one_scoped_captured_quote_lookup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentEdition>(new StubEdition(ToobaEdition.SingleStore));
        services.AddSingleton<ICurrentCommerceContext>(new StubCommerceContext());
        services.AddSingleton<IDatabaseConnectionResolver>(new StubConnectionResolver());
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<ICurrentTenant>(new StubTenant(null));
        services.AddSingleton<IModuleCallTracer>(new ModuleCallTracer());
        services.AddScoped<IGeographyPlaceSnapshotReader>(_ => new ControlledGeography());

        new ShippingModule().AddServices(
            services,
            new ConfigurationBuilder().Build(),
            new StubHostEnvironment());

        // ONLY the provider boundary is replaced; the module keeps its own directory registration.
        services.AddSingleton(new DbContextOptionsBuilder<ShippingDbContext>()
            .UseInMemoryDatabase($"tb216-di-{Guid.NewGuid():N}")
            .Options);

        var descriptors = services
            .Where(descriptor => descriptor.ServiceType == typeof(IShippingCapturedRevisionQuoteLookup))
            .ToArray();
        var descriptor = Assert.Single(descriptors);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(ShippingCapturedRevisionQuoteLookup), descriptor.ImplementationType);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var a = first.ServiceProvider.GetRequiredService<IShippingCapturedRevisionQuoteLookup>();
        var b = first.ServiceProvider.GetRequiredService<IShippingCapturedRevisionQuoteLookup>();
        var c = second.ServiceProvider.GetRequiredService<IShippingCapturedRevisionQuoteLookup>();
        Assert.IsType<ShippingCapturedRevisionQuoteLookup>(a);
        Assert.Same(a, b);
        Assert.NotSame(a, c);
    }

    [Fact]
    public async Task An_exact_captured_read_returns_the_declared_row_money_and_full_provenance()
    {
        var snapshot = Snapshot(CityAreaGroup(), Rows());
        using var database = new Database(snapshot);
        var geography = new ControlledGeography().WithCountry(Country).WithCity(CityId, Country, DeepArea)
            .WithArea(RootArea, Country, null).WithArea(DeepArea, Country, RootArea);

        var result = await Lookup(database, geography).QuoteAsync(
            Request(snapshot.DraftId, snapshot.SourceRevision, new DestinationIdentifiers(Country, null, CityId)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var quote = result.Value;
        Assert.True(quote.IsQuoted, quote.ReasonCode);
        Assert.Equal(ShippingCapturedQuoteVerdict.Quoted, quote.Verdict);
        Assert.Equal("IRT", quote.CurrencyCode);
        Assert.Equal(120_000.00m, quote.FreightAmount);
        Assert.Equal(5_000.75m, quote.PackagingAmount);
        Assert.Equal(125_000.75m, quote.TotalExpenseAmount);
        Assert.Equal(125_000.75m, quote.PayableAtCheckoutAmount);
        Assert.Equal(0m, quote.CarrierCollectAmount);

        Assert.Equal(snapshot.SnapshotId, quote.SnapshotId);
        Assert.Equal(snapshot.DraftId, quote.DraftId);
        Assert.Equal(snapshot.SourceRevision, quote.SourceRevision);
        Assert.Equal(snapshot.FormatVersion, quote.FormatVersion);
        Assert.Equal(TypeId, quote.ShippingTypeId);
        Assert.NotNull(quote.GroupId);
        Assert.NotNull(quote.RateRowId);
        Assert.Equal(2, quote.MinDeliveryDays);
        Assert.Equal(5, quote.MaxDeliveryDays);
        Assert.Equal(ShippingFreightPayerMode.Prepaid, quote.Settings!.FreightPaymentMode);
        Assert.Equal(ShippingAuthoringCalculationMode.Manual, quote.Settings.CalculationMode);
        Assert.Equal("Asia/Tehran", quote.ReceivingWeek!.TimeZoneId);
        Assert.False(quote.ReceivingWeek.Days.Single(day => day.Weekday == 2).IsWorking);

        // A monetary answer is never a customer-eligibility/activation claim.
        Assert.Equal(ShippingCapturedQuoteCustomerEligibility.NotEvaluated, quote.Eligibility);

        // Read-only: the query never attaches or mutates the stored row.
        Assert.Empty(database.Context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task A_missing_foreign_or_mismatched_exact_revision_refuses_with_zero_geography_reads()
    {
        var snapshot = Snapshot(CityAreaGroup(), Rows());
        using var database = new Database(snapshot);

        var missingRevision = new ControlledGeography();
        var missing = await Lookup(database, missingRevision).QuoteAsync(
            Request(snapshot.DraftId, snapshot.SourceRevision + 7, new DestinationIdentifiers(Country, null, CityId)),
            CancellationToken.None);
        Assert.False(missing.Value.IsQuoted);
        Assert.Equal(ShippingCapturedQuoteVerdict.CapturedRevisionUnavailable, missing.Value.Verdict);
        Assert.Equal(0, missingRevision.Reads);

        var storeEdition = new ControlledGeography();
        var mismatched = await new ShippingCapturedRevisionQuoteLookup(
                new ShippingAuthoringDraftDirectory(database.Context),
                storeEdition,
                new StubEdition(ToobaEdition.SingleStore),
                new StubTenant(null),
                new ModuleCallTracer(),
                NullLogger<ShippingCapturedRevisionQuoteLookup>.Instance)
            .QuoteAsync(
                Request(snapshot.DraftId, snapshot.SourceRevision, new DestinationIdentifiers(Country, null, CityId)),
                CancellationToken.None);
        Assert.Equal(ShippingCapturedQuoteVerdict.OwnerScopeUnavailable, mismatched.Value.Verdict);
        Assert.Equal(0, storeEdition.Reads);

        var marketplaceEdition = new ControlledGeography();
        var wrongOwner = await new ShippingCapturedRevisionQuoteLookup(
                new ShippingAuthoringDraftDirectory(database.Context),
                marketplaceEdition,
                new StubEdition(ToobaEdition.Marketplace),
                new StubTenant(null),
                new ModuleCallTracer(),
                NullLogger<ShippingCapturedRevisionQuoteLookup>.Instance)
            .QuoteAsync(
                new ShippingCapturedQuoteLookupRequest(
                    ShippingCapturedQuoteOwnerKind.Seller,
                    null,
                    Guid.NewGuid(),
                    snapshot.DraftId,
                    snapshot.SourceRevision,
                    new DestinationIdentifiers(Country, null, CityId)),
                CancellationToken.None);
        Assert.Equal(ShippingCapturedQuoteVerdict.CapturedRevisionUnavailable, wrongOwner.Value.Verdict);
        Assert.Equal(0, marketplaceEdition.Reads);
    }

    [Fact]
    public async Task A_root_administrative_coverage_covers_a_deep_city_through_the_verified_chain()
    {
        var snapshot = Snapshot(
            [new ShippingDestinationNode(ShippingDestinationNodeKind.AdministrativeArea, RootArea)],
            Rows());
        using var database = new Database(snapshot);
        var geography = new ControlledGeography().WithCountry(Country).WithCity(CityId, Country, DeepArea)
            .WithArea(RootArea, Country, null).WithArea(DeepArea, Country, RootArea);

        var quoted = await Lookup(database, geography).QuoteAsync(
            Request(snapshot.DraftId, snapshot.SourceRevision, new DestinationIdentifiers(Country, null, CityId)),
            CancellationToken.None);

        Assert.True(quoted.Value.IsQuoted, quoted.Value.ReasonCode);
        Assert.Equal(120_000.00m, quoted.Value.FreightAmount);

        // The verified chain is Geometry-proof, never the request's guess: a foreign parent breaks it.
        var brokenChain = new ControlledGeography().WithCountry(Country).WithCity(CityId, Country, DeepArea)
            .WithArea(RootArea, OtherCountry, null).WithArea(DeepArea, Country, RootArea);
        var unresolved = await Lookup(database, brokenChain).QuoteAsync(
            Request(snapshot.DraftId, snapshot.SourceRevision, new DestinationIdentifiers(Country, null, CityId)),
            CancellationToken.None);
        Assert.Equal(ShippingCapturedQuoteVerdict.DestinationUnresolved, unresolved.Value.Verdict);
        AssertNoMoney(unresolved.Value);
    }

    [Fact]
    public async Task Contradictory_inactive_or_foreign_geography_answers_fail_closed()
    {
        var snapshot = Snapshot(CityAreaGroup(), Rows());
        using var database = new Database(snapshot);

        // A city whose real parent contradicts the declared area is unresolved, never a guess.
        var contradiction = new ControlledGeography().WithCountry(Country).WithCity(CityId, Country, DeepArea)
            .WithArea(RootArea, Country, null).WithArea(DeepArea, Country, RootArea);
        var mismatchedParent = await Lookup(database, contradiction).QuoteAsync(
            Request(snapshot.DraftId, snapshot.SourceRevision, new DestinationIdentifiers(Country, RootArea, CityId)),
            CancellationToken.None);
        Assert.Equal(ShippingCapturedQuoteVerdict.DestinationUnresolved, mismatchedParent.Value.Verdict);

        // A city without an administrative parent stays LAWFUL and still quotes.
        var lawful = new ControlledGeography().WithCountry(Country).WithCity(CityId, Country, null);
        var quoted = await Lookup(database, lawful).QuoteAsync(
            Request(snapshot.DraftId, snapshot.SourceRevision, new DestinationIdentifiers(Country, null, CityId)),
            CancellationToken.None);
        Assert.True(quoted.Value.IsQuoted, quoted.Value.ReasonCode);

        // An inactive country, a duplicate row and a foreign row are all refusals with no money.
        var inactive = new ControlledGeography().WithCountry(Country, isActive: false);
        var inactiveQuote = await Lookup(database, inactive).QuoteAsync(
            Request(snapshot.DraftId, snapshot.SourceRevision, new DestinationIdentifiers(Country)),
            CancellationToken.None);
        Assert.Equal(ShippingCapturedQuoteVerdict.DestinationUnresolved, inactiveQuote.Value.Verdict);

        var duplicate = new ControlledGeography().WithCountry(Country).WithDuplicateCountry(Country);
        var duplicateQuote = await Lookup(database, duplicate).QuoteAsync(
            Request(snapshot.DraftId, snapshot.SourceRevision, new DestinationIdentifiers(Country)),
            CancellationToken.None);
        Assert.Equal(ShippingCapturedQuoteVerdict.DestinationUnresolved, duplicateQuote.Value.Verdict);
        AssertNoMoney(duplicateQuote.Value);

        var foreignRow = new ControlledGeography().WithCountry(OtherCountry);
        var foreignQuote = await Lookup(database, foreignRow).QuoteAsync(
            Request(snapshot.DraftId, snapshot.SourceRevision, new DestinationIdentifiers(Country)),
            CancellationToken.None);
        Assert.Equal(ShippingCapturedQuoteVerdict.DestinationUnresolved, foreignQuote.Value.Verdict);
    }

    [Fact]
    public async Task A_carrier_collect_row_splits_checkout_and_carrier_amounts_without_guessing()
    {
        var snapshot = Snapshot(
            CityAreaGroup(),
            Rows(),
            payerMode: ShippingFreightPayerMode.CollectOnDelivery);
        using var database = new Database(snapshot);
        var geography = new ControlledGeography().WithCountry(Country).WithCity(CityId, Country, DeepArea)
            .WithArea(RootArea, Country, null).WithArea(DeepArea, Country, RootArea);

        var result = await Lookup(database, geography).QuoteAsync(
            Request(snapshot.DraftId, snapshot.SourceRevision, new DestinationIdentifiers(Country, null, CityId)),
            CancellationToken.None);

        Assert.True(result.Value.IsQuoted, result.Value.ReasonCode);
        // Packaging is always paid at checkout; only freight can be collected by the carrier.
        Assert.Equal(5_000.75m, result.Value.PayableAtCheckoutAmount);
        Assert.Equal(120_000.00m, result.Value.CarrierCollectAmount);
        Assert.Equal(ShippingFreightPayerMode.CollectOnDelivery, result.Value.Settings!.FreightPaymentMode);
    }

    [Fact]
    public async Task An_unconfigured_or_non_manual_capture_is_explicitly_unavailable_not_free()
    {
        var unconfigured = Snapshot(CityAreaGroup(), Rows(), withSettings: false);
        using var noSettingsDb = new Database(unconfigured);
        var geography = ExtraGeography();
        var notConfigured = await Lookup(noSettingsDb, geography).QuoteAsync(
            Request(unconfigured.DraftId, unconfigured.SourceRevision, new DestinationIdentifiers(Country, null, CityId)),
            CancellationToken.None);
        Assert.Equal(ShippingCapturedQuoteVerdict.NotConfigured, notConfigured.Value.Verdict);
        AssertNoMoney(notConfigured.Value);

        var free = Snapshot(CityAreaGroup(), Rows(), calculationMode: ShippingAuthoringCalculationMode.Free);
        using var freeDb = new Database(free);
        var freeQuote = await Lookup(freeDb, ExtraGeography()).QuoteAsync(
            Request(free.DraftId, free.SourceRevision, new DestinationIdentifiers(Country, null, CityId)),
            CancellationToken.None);
        Assert.Equal(ShippingCapturedQuoteVerdict.CalculationModeUnavailable, freeQuote.Value.Verdict);
        AssertNoMoney(freeQuote.Value);
    }

    [Fact]
    public void An_unknown_alternative_requires_the_measurement_but_a_known_outside_row_never_blocks()
    {
        var payload = Payload(
            [
                CapturedRow(null, 0, 5000, null, null, null, null, 10_000.00m, 1_000.00m),
                CapturedRow(null, 6000, null, null, null, null, null, 20_000.00m, 2_000.00m),
            ]);

        // One row matches on weight; the OTHER row is undetermined (weight 6000 is outside 10kg? no:
        // 6001 is outside its [6000,∞)? It is inside. Use 4kg: row2 needs >=6000 so it is KNOWN outside.)
        var knownOutside = Quote(payload, 4_000);
        Assert.True(knownOutside.IsQuoted, knownOutside.ReasonCode);
        Assert.Equal(10_000.00m, knownOutside.FreightAmount);

        // A row whose only condition is a COUNT makes weight-only matching undetermined.
        var withCount = Payload(
            [
                CapturedRow(null, 0, 5000, null, null, null, null, 10_000.00m, 1_000.00m),
                CapturedRow(null, null, null, 5, 10, null, null, 20_000.00m, 2_000.00m),
            ]);

        var undetermined = Quote(withCount, 4_000);
        Assert.False(undetermined.IsQuoted);
        Assert.Equal(ShippingCapturedQuoteOutcome.MeasurementRequired, undetermined.Outcome);

        // Supplying the measurement resolves it into exactly ONE row, with that row's own packaging.
        var resolved = Quote(withCount, 4_000, 2);
        Assert.True(resolved.IsQuoted, resolved.ReasonCode);
        Assert.Equal(10_000.00m, resolved.FreightAmount);
        Assert.Equal(1_000.00m, resolved.PackagingAmount);
    }

    [Fact]
    public async Task Provider_failure_keeps_its_own_identity_the_caller_token_and_one_child_span()
    {
        var snapshot = Snapshot(CityAreaGroup(), Rows());
        using var database = new Database(snapshot);
        var sentinel = new InvalidOperationException("geography-sentinel");
        var geography = new ControlledGeography { Failure = sentinel };

        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.DisplayName.StartsWith("module.Shipping.Geography.", StringComparison.Ordinal))
                {
                    spans.Add(activity);
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        var correlation = Guid.NewGuid().ToString("N");
        using var correlationScope = CorrelationIdContext.BeginScope(correlation);
        using var parent = new ActivitySource("Tooba.Shipping.Tests.Parent").StartActivity("parent.quote");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Lookup(database, geography).QuoteAsync(
                Request(snapshot.DraftId, snapshot.SourceRevision, new DestinationIdentifiers(Country, null, CityId)),
                CancellationToken.None));
        Assert.Same(sentinel, thrown);

        var failed = Assert.Single(spans);
        Assert.Equal(parent!.SpanId, failed.ParentSpanId);
        Assert.Equal(ActivityStatusCode.Error, failed.Status);
        Assert.Equal(correlation, failed.GetTagItem(TracingTagNames.CorrelationId));
        var values = string.Join("|", failed.TagObjects.Select(tag => Convert.ToString(tag.Value)));
        Assert.DoesNotContain(Country.ToString(), values, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(sentinel.Message, values, StringComparison.Ordinal);

        // The caller token reaches the foreign seam unchanged.
        var cancelling = new ControlledGeography { Failure = null, CancelWith = new CancellationTokenSource() };
        var token = cancelling.CancelWith!.Token;
        var cancelled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Lookup(database, cancelling).QuoteAsync(
                Request(snapshot.DraftId, snapshot.SourceRevision, new DestinationIdentifiers(Country, null, CityId)),
                token));
        Assert.Equal(token, cancelled.CancellationToken);
        Assert.Equal(token, cancelling.LastToken);
        listener.Dispose();
    }

    // ---------------------------------------------------------------- fixtures

    private static ShippingCapturedRevisionQuoteLookup Lookup(Database database, IGeographyPlaceSnapshotReader geography)
        => new(
            new ShippingAuthoringDraftDirectory(database.Context),
            geography,
            new StubEdition(ToobaEdition.SingleStore),
            new StubTenant(Tenant),
            new ModuleCallTracer(),
            NullLogger<ShippingCapturedRevisionQuoteLookup>.Instance);

    private static ControlledGeography ExtraGeography()
        => new ControlledGeography().WithCountry(Country).WithCity(CityId, Country, DeepArea)
            .WithArea(RootArea, Country, null).WithArea(DeepArea, Country, RootArea);

    private static ShippingCapturedQuoteLookupRequest Request(Guid draftId, long revision, DestinationIdentifiers destination)
        => new(
            ShippingCapturedQuoteOwnerKind.Store,
            Tenant,
            null,
            draftId,
            revision,
            destination,
            GroupWeightGrams: 3_000,
            GroupItemCount: 3,
            FinalGroupGoodsAmountAfterDiscountAndTax: 200_000.00m,
            CurrencyCode: "IRT");

    private static Guid _draftId;

    private static Guid DraftIdOf(DestinationIdentifiers _) => _draftId;

    private static ShippingDestinationNode[] CityAreaGroup()
        => [new ShippingDestinationNode(ShippingDestinationNodeKind.City, CityId)];

    private static ShippingAuthoringDraft.ShippingAuthoringRateRowInput[] Rows()
        => [new(null, 0, 5000, 1, 10, null, null, 120_000.00m, 5_000.75m, 2, 5)];

    private static ShippingAuthoringRevisionRateRow CapturedRow(
        Guid? id,
        long? minWeight,
        long? maxWeight,
        int? minCount,
        int? maxCount,
        decimal? minAmount,
        decimal? maxAmount,
        decimal freight,
        decimal packaging)
        => new(
            id ?? Guid.NewGuid(),
            minWeight,
            maxWeight,
            minCount,
            maxCount,
            minAmount,
            maxAmount,
            freight,
            packaging,
            1,
            3);

    private static ShippingAuthoringRevisionPayload Payload(
        IReadOnlyList<ShippingAuthoringRevisionRateRow> rows)
    {
        var draft = Draft(
            [CityAreaGroup()],
            [
                new ShippingAuthoringDraft.ShippingAuthoringRateRowInput[]
                {
                    new(null, 0, null, null, null, null, null, 1.00m, 1.00m, 1, 3),
                },
            ]);
        var captured = ShippingAuthoringRevisionSnapshot.Capture(draft, Now.AddHours(1)).ReadPayload();
        var groupId = captured.Groups[0].GroupId;
        return captured with
        {
            Groups = captured.Groups
                .Select(group => group.GroupId == groupId ? group with { RateRows = rows.ToArray() } : group)
                .ToArray(),
        };
    }

    private static ShippingCapturedRevisionQuote Quote(
        ShippingAuthoringRevisionPayload payload,
        long? weightGrams,
        int? itemCount = null)
        => ShippingRatePolicyEngine.QuoteCapturedRevision(
            new ShippingCapturedRevisionQuoteRequest(
                new ShippingCapturedRevisionIdentity(
                    payload.Owner.Kind,
                    payload.Owner.TenantId,
                    payload.Owner.SellerPartyId,
                    payload.DraftId,
                    payload.SourceRevision),
                payload,
                ShippingDestinationScope.ForCity(Country, null, CityId),
                weightGrams,
                itemCount,
                null,
                "IRT"));

    private static ShippingAuthoringRevisionSnapshot Snapshot(
        ShippingDestinationNode[] areas,
        ShippingAuthoringDraft.ShippingAuthoringRateRowInput[] rows,
        ShippingFreightPayerMode payerMode = ShippingFreightPayerMode.Prepaid,
        ShippingAuthoringCalculationMode calculationMode = ShippingAuthoringCalculationMode.Manual,
        bool withSettings = true)
    {
        var draft = Draft([areas], [rows], payerMode, calculationMode, withSettings);
        return ShippingAuthoringRevisionSnapshot.Capture(draft, Now.AddHours(1));
    }

    private static ShippingAuthoringDraft Draft(
        IReadOnlyList<ShippingDestinationNode[]> areaGroups,
        IReadOnlyList<ShippingAuthoringDraft.ShippingAuthoringRateRowInput[]>? rowsPerGroup = null,
        ShippingFreightPayerMode payerMode = ShippingFreightPayerMode.Prepaid,
        ShippingAuthoringCalculationMode calculationMode = ShippingAuthoringCalculationMode.Manual,
        bool withSettings = true)
    {
        var draft = ShippingAuthoringDraft.Create(
            ShippingDraftOwner.ForStore(Tenant),
            Service,
            null,
            areaGroups
                .Select(nodes => new ShippingAuthoringRegionGroupInput(null, nodes))
                .ToArray(),
            Now);

        var groups = draft.RegionGroups.OrderBy(group => group.Order).ToArray();
        if (rowsPerGroup is not null)
        {
            for (var index = 0; index < rowsPerGroup.Count && index < groups.Length; index++)
            {
                draft.ReplaceGroupRateRows(groups[index].GroupId, rowsPerGroup[index], "IRT", Now.AddMinutes(1 + index));
            }
        }

        draft.ApplyWeeklySchedule(
            draft.PreflightWeeklySchedule(
                "Asia/Tehran",
                [
                    new ShippingAuthoringDraft.ScheduleDayInput(1, true, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                    new ShippingAuthoringDraft.ScheduleDayInput(2, false, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                    new ShippingAuthoringDraft.ScheduleDayInput(3, true, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                    new ShippingAuthoringDraft.ScheduleDayInput(4, true, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                    new ShippingAuthoringDraft.ScheduleDayInput(5, true, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                    new ShippingAuthoringDraft.ScheduleDayInput(6, true, new TimeOnly(9, 0), new TimeOnly(13, 0)),
                    new ShippingAuthoringDraft.ScheduleDayInput(7, false, null, null),
                ]),
            Now.AddMinutes(8));

        if (withSettings)
        {
            draft.ApplyMethodSettings(draft.PreflightMethodSettings(payerMode, calculationMode), Now.AddMinutes(9));
        }

        draft.ApplyTypeAssignment(draft.PreflightTypeAssignment(TypeId), Now.AddMinutes(10));
        return draft;
    }

    private static void AssertNoMoney(ShippingCapturedQuoteResult quote)
    {
        Assert.False(quote.IsQuoted);
        Assert.Null(quote.CurrencyCode);
        Assert.Null(quote.FreightAmount);
        Assert.Null(quote.PackagingAmount);
        Assert.Null(quote.TotalExpenseAmount);
        Assert.Null(quote.PayableAtCheckoutAmount);
        Assert.Null(quote.CarrierCollectAmount);
        Assert.Null(quote.Settings);
        Assert.Null(quote.RateRowId);
    }

    private sealed class Database : IDisposable
    {
        public Database(ShippingAuthoringRevisionSnapshot snapshot)
        {
            Context = new ShippingDbContext(new DbContextOptionsBuilder<ShippingDbContext>()
                .UseInMemoryDatabase($"tb216-{Guid.NewGuid():N}")
                .Options);
            Context.AuthoringRevisionSnapshots.Add(snapshot);
            Context.SaveChanges();
            Context.ChangeTracker.Clear();
        }

        public ShippingDbContext Context { get; }

        public void Dispose() => Context.Dispose();
    }

    private sealed class ControlledGeography : IGeographyPlaceSnapshotReader
    {
        private readonly Dictionary<Guid, CountrySnapshot> _countries = [];
        private readonly Dictionary<Guid, AdministrativeAreaSnapshot> _areas = [];
        private readonly Dictionary<Guid, CitySnapshot> _cities = [];

        public int Reads { get; private set; }

        public Exception? Failure { get; init; }

        public CancellationTokenSource? CancelWith { get; init; }

        public CancellationToken LastToken { get; private set; }

        public ControlledGeography WithCountry(Guid id, bool isActive = true)
        {
            _countries[id] = new CountrySnapshot(id, "IR", isActive);
            return this;
        }

        public ControlledGeography WithDuplicateCountry(Guid id)
        {
            _duplicates.Add(id);
            return this;
        }

        public ControlledGeography WithArea(Guid id, Guid countryId, Guid? parentId)
        {
            _areas[id] = new AdministrativeAreaSnapshot(id, countryId, parentId, "A", true);
            return this;
        }

        public ControlledGeography WithCity(Guid id, Guid countryId, Guid? areaId)
        {
            _cities[id] = new CitySnapshot(id, countryId, areaId, "C", true);
            return this;
        }

        private readonly HashSet<Guid> _duplicates = [];

        public Task<IReadOnlyList<CountrySnapshot>> GetCountriesAsync(
            IReadOnlyCollection<Guid> countryIds,
            CancellationToken cancellationToken)
            => Read<CountrySnapshot>(
                countryIds,
                id => _countries.GetValueOrDefault(id) is { } row ? [row] : [],
                cancellationToken);

        public Task<IReadOnlyList<AdministrativeAreaSnapshot>> GetAdministrativeAreasAsync(
            IReadOnlyCollection<Guid> administrativeAreaIds,
            CancellationToken cancellationToken)
            => Read<AdministrativeAreaSnapshot>(administrativeAreaIds, id => _areas.GetValueOrDefault(id) is { } row ? [row] : [], cancellationToken);

        public Task<IReadOnlyList<CitySnapshot>> GetCitiesAsync(
            IReadOnlyCollection<Guid> cityIds,
            CancellationToken cancellationToken)
            => Read<CitySnapshot>(cityIds, id => _cities.GetValueOrDefault(id) is { } row ? [row] : [], cancellationToken);

        public Task<IReadOnlyList<ServiceAreaSnapshot>> GetServiceAreasAsync(
            IReadOnlyCollection<Guid> serviceAreaIds,
            CancellationToken cancellationToken)
            => Read<ServiceAreaSnapshot>(serviceAreaIds, _ => [], cancellationToken);

        private Task<IReadOnlyList<T>> Read<T>(
            IReadOnlyCollection<Guid> ids,
            Func<Guid, IReadOnlyList<T>> resolve,
            CancellationToken cancellationToken)
        {
            LastToken = cancellationToken;
            Reads++;

            if (Failure is not null)
            {
                throw Failure;
            }

            if (CancelWith is not null)
            {
                throw new OperationCanceledException(CancelWith.Token);
            }

            var rows = new List<T>();
            foreach (var id in ids)
            {
                rows.AddRange(resolve(id));
                if (typeof(T) == typeof(CountrySnapshot) && _duplicates.Contains(id))
                {
                    rows.AddRange(resolve(id));
                }
            }

            return Task.FromResult<IReadOnlyList<T>>(rows);
        }
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
