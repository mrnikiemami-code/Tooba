using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Application.Shipping;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Infrastructure.Adapters;
using Tooba.Fulfillment.Infrastructure.Directories;
using Tooba.Fulfillment.Infrastructure.Observability;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Domain.Aggregates;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Domain.Enums;
using Tooba.Order.Infrastructure.Integrations.Shipping;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Carriers;
using Tooba.Shipping.Application.Carriers.Commands;
using Tooba.Shipping.Application.Carriers.Ports;
using Tooba.Shipping.Contracts.Carriers;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Domain.Carriers;
using Tooba.Shipping.Infrastructure.Carriers;
using Tooba.Shipping.Infrastructure.Carriers.Tipax;
using Tooba.Shipping.Infrastructure.Persistence;
using Xunit;
using AppShipmentLine = Tooba.Fulfillment.Application.Fulfillments.Models.ShipmentLineCommand;

namespace Tooba.Shipping.Tests.Carriers;

/// <summary>
/// TB-249-R4 mandatory disposable PostgreSQL package-execution matrix.
/// Real Order + Shipping + Fulfillment schemas; only the carrier HTTP provider is CONTROLLED.
/// </summary>
[Collection(ShippingCarrierPackageExecutionRelationalCollection.Name)]
public sealed class ShippingCarrierPackageExecutionRelationalTests
{
    private static readonly SemaphoreSlim MigrationGate = new(1, 1);
    private static bool _migrated;
    private static bool _liveVerified;

    [Fact]
    [Trait("Category", "TB249R4")]
    [Trait("Case", "1")]
    public async Task Case01_accepted_group_package_sender_then_exact_package_execution_preserves_finance()
    {
        await using var fx = await Fixture.CreateAsync(lineQuantity: 2m);
        var beforeFreight = await fx.ReadAcceptedFreightAsync();

        var package = await fx.Directory.SavePackageCompletionAsync(
            fx.Owner,
            new ShippingCarrierShipmentPackageCompletionRequest(
                fx.CheckoutId, fx.GroupId, 1800m, 100m, 22m, 18m, 12m),
            CancellationToken.None);
        Assert.True(package.IsSuccess, package.IsFailure ? package.Errors[0].Code : null);

        await fx.PrepareSenderAsync();
        var created = await fx.Directory.CreateAsync(
            fx.Owner, fx.ActorUserId, new ShippingCarrierShipmentExecuteRequest(fx.CheckoutId, fx.GroupId), CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
        Assert.Equal(1, fx.Provider.Creates);
        Assert.Equal("TRK-1", created.Value.ProviderTrackingCode);
        Assert.Equal(beforeFreight, await fx.ReadAcceptedFreightAsync());
        Assert.NotNull(fx.Provider.LastRequest);
        Assert.Equal(fx.Destination, fx.Provider.LastRequest!.Recipient.LocationCode);
        Assert.Equal(1, fx.Recorder.CreatedCount);
        Assert.Equal(fx.ActorUserId, fx.Recorder.LastActorUserId);
    }

    [Fact]
    [Trait("Category", "TB249R4")]
    [Trait("Case", "2")]
    public async Task Case02_exhausted_cancelled_and_foreign_owner_stay_denied()
    {
        await using var exhausted = await Fixture.CreateAsync(lineQuantity: 1m);
        await exhausted.PrepareSenderAsync();
        Assert.True((await exhausted.Directory.CreateAsync(
            exhausted.Owner, exhausted.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(exhausted.CheckoutId, exhausted.GroupId), CancellationToken.None)).IsSuccess);

        var replay = await exhausted.Directory.CreateAsync(
            exhausted.Owner, exhausted.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(exhausted.CheckoutId, exhausted.GroupId), CancellationToken.None);
        Assert.Equal(ShippingErrorCodes.CarrierShipmentNotEligible, replay.FirstError.Code);
        Assert.Equal(1, exhausted.Provider.Creates);

        var foreign = await exhausted.Directory.CreateAsync(
            ShippingAuthoringOwnerScope.ForStore("other-store"),
            exhausted.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(exhausted.CheckoutId, exhausted.GroupId),
            CancellationToken.None);
        Assert.Equal(ShippingErrorCodes.CarrierShipmentNotEligible, foreign.FirstError.Code);

        await using var cancelled = await Fixture.CreateAsync(lineQuantity: 1m, cancelSeller: true);
        await cancelled.PrepareSenderAsync();
        var denied = await cancelled.Directory.CreateAsync(
            cancelled.Owner, cancelled.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(cancelled.CheckoutId, cancelled.GroupId), CancellationToken.None);
        Assert.Equal(ShippingErrorCodes.CarrierShipmentNotEligible, denied.FirstError.Code);
        Assert.Equal(0, cancelled.Provider.Creates);
    }

    [Fact]
    [Trait("Category", "TB249R4")]
    [Trait("Case", "3")]
    public async Task Case03_two_packages_same_group_each_own_payload_once()
    {
        await using var fx = await Fixture.CreateAsync(lineQuantity: 4m);
        await fx.PrepareSenderAsync();

        var first = await fx.Directory.CreateAsync(
            fx.Owner, fx.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(
                fx.CheckoutId, fx.GroupId,
                new[] { new ShippingCarrierShipmentLineQuantity(fx.OrderLineId, 1m) }),
            CancellationToken.None);
        Assert.True(first.IsSuccess, first.IsFailure ? first.Errors[0].Code : null);
        var firstDeclared = fx.Provider.LastRequest!.Package.DeclaredValue;

        var second = await fx.Directory.CreateAsync(
            fx.Owner, fx.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(
                fx.CheckoutId, fx.GroupId,
                new[] { new ShippingCarrierShipmentLineQuantity(fx.OrderLineId, 3m) }),
            CancellationToken.None);
        Assert.True(second.IsSuccess, second.IsFailure ? second.Errors[0].Code : null);
        Assert.Equal(2, fx.Provider.Creates);
        Assert.Equal(2, fx.Recorder.CreatedCount);
        Assert.NotEqual(first.Value.OperationId, second.Value.OperationId);
        Assert.Equal(firstDeclared * 3m, fx.Provider.LastRequest!.Package.DeclaredValue);

        var exhausted = await fx.Directory.CreateAsync(
            fx.Owner, fx.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(fx.CheckoutId, fx.GroupId), CancellationToken.None);
        Assert.Equal(ShippingErrorCodes.CarrierShipmentNotEligible, exhausted.FirstError.Code);
        Assert.Equal(2, fx.Provider.Creates);
    }

    [Fact]
    [Trait("Category", "TB249R4")]
    [Trait("Case", "4")]
    public async Task Case04_concurrent_create_and_competing_manual_do_not_overship()
    {
        // Concurrent same-intent: two independent contexts against one DB (EF contexts are not thread-safe).
        await using var fxA = await Fixture.CreateAsync(lineQuantity: 2m);
        await fxA.PrepareSenderAsync();
        await using var fxB = await Fixture.AttachSiblingAsync(fxA);
        var results = await Task.WhenAll(
            fxA.Directory.CreateAsync(fxA.Owner, fxA.ActorUserId, new ShippingCarrierShipmentExecuteRequest(fxA.CheckoutId, fxA.GroupId), CancellationToken.None),
            fxB.Directory.CreateAsync(fxB.Owner, fxB.ActorUserId, new ShippingCarrierShipmentExecuteRequest(fxB.CheckoutId, fxB.GroupId), CancellationToken.None));
        Assert.Contains(results, x => x.IsSuccess);
        Assert.True(fxA.Provider.Creates + fxB.Provider.Creates <= 2);
        var shippedConcurrent = await fxA.FulfillmentDb.ShipmentItems.AsNoTracking().SumAsync(x => (decimal?)x.Quantity) ?? 0m;
        Assert.True(shippedConcurrent <= 2m);

        // Competing manual vs carrier against remaining qty=1.
        await using var competeA = await Fixture.CreateAsync(lineQuantity: 1m);
        await competeA.PrepareSenderAsync();
        await using var competeB = await Fixture.AttachSiblingAsync(competeA);
        var manualTask = competeA.FulfillmentDirectory.CreateShipmentAsync(
            competeA.FulfillmentId, competeA.ActorUserId, "Manual",
            new[] { new AppShipmentLine(competeA.OrderLineId, 1m) },
            CancellationToken.None, shippingGroupId: competeA.GroupId);
        var carrierTask = competeB.Directory.CreateAsync(
            competeB.Owner, competeB.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(competeB.CheckoutId, competeB.GroupId), CancellationToken.None);
        await Task.WhenAll(
            manualTask.ContinueWith(t => t.IsFaulted ? 1 : 0),
            carrierTask);
        var shipped = await competeA.FulfillmentDb.ShipmentItems.AsNoTracking().SumAsync(x => (decimal?)x.Quantity) ?? 0m;
        Assert.True(shipped <= 1m);
    }

    [Fact]
    [Trait("Category", "TB249R4")]
    [Trait("Case", "5")]
    public async Task Case05_provider_callback_sees_pending_snapshot_on_separate_context_before_http()
    {
        await using var fx = await Fixture.CreateAsync(lineQuantity: 1m);
        await fx.PrepareSenderAsync();
        fx.Provider.OnCreate = async request =>
        {
            await using var probe = Fixture.CreateShippingDb(fx.ConnectionString);
            var pending = await probe.CarrierShipmentOperations.AsNoTracking()
                .SingleAsync(x => x.OperationKey == request.OperationKey);
            Assert.Equal(ShippingCarrierShipmentState.Pending, pending.State);
            Assert.False(string.IsNullOrWhiteSpace(pending.ExecutionInputSnapshotJson));
            using var doc = JsonDocument.Parse(pending.ExecutionInputSnapshotJson!);
            Assert.True(doc.RootElement.TryGetProperty("PackageDispatchId", out _));
            Assert.True(doc.RootElement.TryGetProperty("Sender", out _));
            Assert.True(doc.RootElement.TryGetProperty("Recipient", out _));
            Assert.True(doc.RootElement.TryGetProperty("Package", out _));
        };

        var created = await fx.Directory.CreateAsync(
            fx.Owner, fx.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(fx.CheckoutId, fx.GroupId), CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Errors[0].Code : null);
        Assert.Equal(1, fx.Provider.Creates);
    }

    [Fact]
    [Trait("Category", "TB249R4")]
    [Trait("Case", "6")]
    public async Task Case06_local_failure_boundaries_recover_same_package_without_extra_http()
    {
        await using var beforeAlloc = await Fixture.CreateAsync(lineQuantity: 1m);
        await beforeAlloc.PrepareSenderAsync();
        beforeAlloc.Recorder.FailNextCreate = true;
        Assert.True((await beforeAlloc.Directory.CreateAsync(
            beforeAlloc.Owner, beforeAlloc.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(beforeAlloc.CheckoutId, beforeAlloc.GroupId), CancellationToken.None)).IsFailure);
        Assert.Equal(0, beforeAlloc.Provider.Creates);
        beforeAlloc.Recorder.FailNextCreate = false;
        var recovered = await beforeAlloc.Directory.CreateAsync(
            beforeAlloc.Owner, beforeAlloc.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(beforeAlloc.CheckoutId, beforeAlloc.GroupId), CancellationToken.None);
        Assert.True(recovered.IsSuccess, recovered.IsFailure ? recovered.Errors[0].Code : null);
        Assert.Equal(1, beforeAlloc.Provider.Creates);
        Assert.Equal(1, beforeAlloc.Recorder.CreatedCount);

        await using var beforeProvider = await Fixture.CreateAsync(lineQuantity: 1m);
        await beforeProvider.PrepareSenderAsync();
        beforeProvider.Provider.ThrowOnCreate = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => beforeProvider.Directory.CreateAsync(
            beforeProvider.Owner, beforeProvider.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(beforeProvider.CheckoutId, beforeProvider.GroupId), CancellationToken.None));
        Assert.Equal(1, beforeProvider.Provider.Creates);
        beforeProvider.Provider.ThrowOnCreate = false;
        var beforeCreates = beforeProvider.Provider.Creates;
        var resume = await beforeProvider.Directory.CreateAsync(
            beforeProvider.Owner, beforeProvider.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(beforeProvider.CheckoutId, beforeProvider.GroupId), CancellationToken.None);
        Assert.True(resume.IsSuccess, resume.IsFailure ? resume.Errors[0].Code : null);
        Assert.Equal(beforeCreates + 1, beforeProvider.Provider.Creates);
        Assert.Equal(1, beforeProvider.Recorder.CreatedCount);
    }

    [Fact]
    [Trait("Category", "TB249R4")]
    [Trait("Case", "7")]
    public async Task Case07_timeout_unknown_replay_no_second_create_track_reconciles()
    {
        await using var fx = await Fixture.CreateAsync(lineQuantity: 1m);
        await fx.PrepareSenderAsync();
        fx.Provider.Outcome = CarrierShipmentOutcome.Unknown;
        var first = await fx.Directory.CreateAsync(
            fx.Owner, fx.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(fx.CheckoutId, fx.GroupId), CancellationToken.None);
        Assert.True(first.IsSuccess);
        Assert.Equal(ShippingCarrierShipmentStateView.Unknown, first.Value.State);
        Assert.Equal(1, fx.Provider.Creates);

        var second = await fx.Directory.CreateAsync(
            fx.Owner, fx.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(fx.CheckoutId, fx.GroupId), CancellationToken.None);
        Assert.Equal(first.Value.OperationId, second.Value.OperationId);
        Assert.Equal(1, fx.Provider.Creates);

        fx.Provider.Outcome = CarrierShipmentOutcome.Succeeded;
        var tracked = await fx.Directory.TrackAsync(fx.Owner, first.Value.OperationId, CancellationToken.None);
        Assert.True(tracked.IsSuccess, tracked.IsFailure ? tracked.Errors[0].Code : null);
        Assert.Equal(1, fx.Provider.Tracks);
    }

    [Fact]
    [Trait("Category", "TB249R4")]
    [Trait("Case", "8")]
    public async Task Case08_wrong_ids_and_mismatched_quantity_fail_closed()
    {
        await using var fx = await Fixture.CreateAsync(lineQuantity: 2m);
        await fx.PrepareSenderAsync();

        Assert.Equal(ShippingErrorCodes.CarrierShipmentNotEligible,
            (await fx.Directory.CreateAsync(fx.Owner, fx.ActorUserId,
                new ShippingCarrierShipmentExecuteRequest(fx.CheckoutId, Guid.NewGuid()), CancellationToken.None)).FirstError.Code);

        Assert.Equal(ShippingErrorCodes.CarrierShipmentNotEligible,
            (await fx.Directory.CreateAsync(fx.Owner, fx.ActorUserId,
                new ShippingCarrierShipmentExecuteRequest(fx.CheckoutId, fx.GroupId,
                    new[] { new ShippingCarrierShipmentLineQuantity(fx.OrderLineId, 99m) }), CancellationToken.None)).FirstError.Code);

        Assert.Equal(ShippingErrorCodes.CarrierShipmentNotEligible,
            (await fx.Directory.CreateAsync(fx.Owner, fx.ActorUserId,
                new ShippingCarrierShipmentExecuteRequest(fx.CheckoutId, fx.GroupId,
                    new[] { new ShippingCarrierShipmentLineQuantity(Guid.NewGuid(), 1m) }), CancellationToken.None)).FirstError.Code);
        Assert.Equal(0, fx.Provider.Creates);
    }

    [Fact]
    [Trait("Category", "TB249R4")]
    [Trait("Case", "9")]
    public async Task Case09_handler_propagates_authenticated_actor_into_fulfillment()
    {
        await using var fx = await Fixture.CreateAsync(lineQuantity: 1m);
        await fx.PrepareSenderAsync();
        var handler = new CreateShippingCarrierShipmentHandler(fx.Directory);
        var actor = Guid.Parse("aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee");
        var result = await handler.Handle(
            new CreateShippingCarrierShipmentCommand(
                fx.Owner, actor, new ShippingCarrierShipmentExecuteRequest(fx.CheckoutId, fx.GroupId)),
            CancellationToken.None);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Errors[0].Code : null);
        Assert.Equal(actor, fx.Recorder.LastActorUserId);
        Assert.NotEqual(Guid.Empty, fx.Recorder.LastActorUserId);
    }

    [Fact]
    [Trait("Category", "TB249R4")]
    [Trait("Case", "10")]
    public async Task Case10_unauthorized_disabled_account_causes_zero_external_calls()
    {
        await using var disabled = await Fixture.CreateAsync(lineQuantity: 1m, accountActive: false);
        await disabled.PrepareSenderAsync();
        Assert.Equal(ShippingErrorCodes.CarrierUnconfigured,
            (await disabled.Directory.CreateAsync(disabled.Owner, disabled.ActorUserId,
                new ShippingCarrierShipmentExecuteRequest(disabled.CheckoutId, disabled.GroupId), CancellationToken.None)).FirstError.Code);
        Assert.Equal(0, disabled.Provider.Creates);

        await using var missing = await Fixture.CreateAsync(lineQuantity: 1m, accountConfigured: false);
        await missing.PrepareSenderAsync();
        Assert.Equal(ShippingErrorCodes.CarrierUnconfigured,
            (await missing.Directory.CreateAsync(missing.Owner, missing.ActorUserId,
                new ShippingCarrierShipmentExecuteRequest(missing.CheckoutId, missing.GroupId), CancellationToken.None)).FirstError.Code);
        Assert.Equal(0, missing.Provider.Creates);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private Fixture(
            string connectionString,
            OrderDbContext orderDb,
            ShippingDbContext shippingDb,
            FulfillmentDbContext fulfillmentDb,
            FulfillmentDirectory fulfillmentDirectory,
            RecordingFulfillmentAdmin recorder,
            ShippingCarrierShipmentDirectory directory,
            RecordingShipmentProvider provider,
            Guid checkoutId,
            Guid groupId,
            Guid orderLineId,
            Guid sellerOrderId,
            Guid fulfillmentId,
            Guid actorUserId,
            string destination)
        {
            ConnectionString = connectionString;
            OrderDb = orderDb;
            ShippingDb = shippingDb;
            FulfillmentDb = fulfillmentDb;
            FulfillmentDirectory = fulfillmentDirectory;
            Recorder = recorder;
            Directory = directory;
            Provider = provider;
            CheckoutId = checkoutId;
            GroupId = groupId;
            OrderLineId = orderLineId;
            SellerOrderId = sellerOrderId;
            FulfillmentId = fulfillmentId;
            ActorUserId = actorUserId;
            Destination = destination;
            Owner = ShippingAuthoringOwnerScope.ForStore("store-alpha");
        }

        public string ConnectionString { get; }
        public OrderDbContext OrderDb { get; }
        public ShippingDbContext ShippingDb { get; }
        public FulfillmentDbContext FulfillmentDb { get; }
        public FulfillmentDirectory FulfillmentDirectory { get; }
        public RecordingFulfillmentAdmin Recorder { get; }
        public ShippingCarrierShipmentDirectory Directory { get; }
        public RecordingShipmentProvider Provider { get; }
        public Guid CheckoutId { get; }
        public Guid GroupId { get; }
        public Guid OrderLineId { get; }
        public Guid SellerOrderId { get; }
        public Guid FulfillmentId { get; }
        public Guid ActorUserId { get; }
        public string Destination { get; }
        public ShippingAuthoringOwnerScope Owner { get; }

        public static async Task<Fixture> CreateAsync(
            decimal lineQuantity,
            bool cancelSeller = false,
            bool accountActive = true,
            bool accountConfigured = true)
        {
            var target = ShippingCarrierPackageExecutionRelationalTestTarget.FromEnvironment();
            await EnsureMigratedAsync(target);

            var checkoutId = Guid.NewGuid();
            var groupId = Guid.NewGuid();
            var sellerOrderId = Guid.NewGuid();
            var sellerPartyId = Guid.Parse("44444444-4444-4444-8444-444444444444");
            var cartLineId = Guid.NewGuid();
            var accountId = Guid.Parse("33333333-3333-4333-8333-333333333333");
            var actor = Guid.Parse("88888888-8888-4888-8888-888888888888");
            var buyer = Guid.Parse("99999999-9999-4999-8999-999999999999");
            const string origin = "101";
            const string destination = "202";
            var now = DateTimeOffset.Parse("2026-11-03T00:00:00Z");

            await WipeAsync(target.ConnectionString);

            var orderDb = CreateOrderDb(target.ConnectionString);
            var line = OrderLine.FromCheckout(
                sellerOrderId, Guid.NewGuid(), Guid.NewGuid(), sellerPartyId,
                lineQuantity, 100_000m, "IRR", true, Guid.NewGuid(), null, "Taxable", 0.09m, 9_000m, 109_000m, null);
            var orderLineId = line.LineId;
            var sellerOrder = SellerOrder.Open(
                checkoutId, sellerPartyId, $"SO-{sellerOrderId:N}"[..20],
                OrderMode.OnlinePurchase, "IRR", [line]);
            sellerOrder.RecordVerifiedPayment();
            if (cancelSeller)
            {
                sellerOrder.CancelPaidBeforeShipment();
            }

            var checkout = CheckoutGroup.Submit(
                checkoutId, $"idem-{checkoutId:N}", Guid.NewGuid(), OrderMode.OnlinePurchase,
                buyer, actor, "IR", "IRR", SalesChannel.Marketplace, [sellerOrder], now,
                "گیرنده", "09121111111", "تهران", "تهران", "نشانی", "1111111111", "tipax", "Tipax");
            orderDb.Checkouts.Add(checkout);

            var quote = ShippingGroupAcceptedCarrierQuote.Create(
                "tipax", accountId, 1, "7", new string('a', 64), "IRR",
                137_500m, 120_000m, 12_500m, 5_000m, 0m, 0m,
                now, now.AddMinutes(30),
                origin, destination, 1500m, 2_000_000m, 20m, 15m, 10m, null);
            var evidence = ShippingGroupAcceptedEvidence.Create(
                Guid.NewGuid(), Guid.NewGuid(), 1, ShippingGroupAcceptedEvidence.SupportedFormatVersion, 1,
                ShippingGroupOwner.Store("store-alpha"),
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                132_500m, 5_000m, "IRR", 137_500m, 0m,
                new DateOnly(2026, 11, 4),
                9 * TimeSpan.TicksPerHour,
                17 * TimeSpan.TicksPerHour,
                "Asia/Tehran", 1, 3, quote);

            var materialized = OrderShippingGroupMaterializer.Materialize(
                [new OrderShippingGroupPlan(
                    groupId, sellerPartyId, "tipax", null, null, "Tipax", null,
                    137_500m, "IRR", new DateOnly(2026, 11, 4), new DateOnly(2026, 11, 4), "09:00-17:00", 1,
                    [cartLineId], evidence)],
                [(orderLineId, sellerPartyId)],
                new Dictionary<Guid, Guid> { [sellerPartyId] = sellerOrderId },
                new Dictionary<Guid, Guid> { [cartLineId] = orderLineId },
                checkoutId,
                now);
            orderDb.AddRange(materialized);
            await orderDb.SaveChangesAsync();

            var fulfillmentDb = CreateFulfillmentDb(target.ConnectionString);
            var handoff = new OrderFulfillmentHandoffSnapshot(
                sellerOrderId, checkoutId, sellerPartyId, Guid.NewGuid(), true,
                "گیرنده", "09121111111", "تهران", "تهران", "نشانی", "1111111111",
                "tipax", "Tipax",
                [new OrderFulfillmentLineSnapshot(orderLineId, lineQuantity, null)],
                [new OrderFulfillmentShippingGroupSnapshot(
                    groupId, sellerOrderId, sellerPartyId, "tipax", null, null, "Tipax", null,
                    new DateOnly(2026, 11, 4), null, null, 1, [orderLineId])]);
            var fulfillmentDirectory = new FulfillmentDirectory(
                fulfillmentDb,
                new OpenFulfillmentUseCaseGuard(),
                new FixedOrderReader(handoff),
                new NoopInventory(),
                new FulfillmentInstrumentation(),
                new SystemUtcClock(),
                new UuidV7IdGenerator());

            if (!cancelSeller)
            {
                await fulfillmentDirectory.CreateFromPaidSellerOrdersAsync(
                    Guid.NewGuid(), Guid.NewGuid(), [sellerOrderId], CancellationToken.None);
                var unit = await fulfillmentDirectory.GetBySellerOrderAsync(sellerOrderId, CancellationToken.None)
                           ?? throw new InvalidOperationException("fulfillment missing");
                await fulfillmentDirectory.MarkProcessingAsync(unit.FulfillmentId, actor, CancellationToken.None);
                await fulfillmentDirectory.MarkPackedAsync(unit.FulfillmentId, actor, CancellationToken.None);

                var shippingDb = CreateShippingDb(target.ConnectionString);
                var provider = new RecordingShipmentProvider();
                var adapter = new FulfillmentAdminOperationsAdapter(
                    fulfillmentDirectory,
                    new FulfillmentShippedQuantityReader(fulfillmentDb),
                    new ShippingMethodsOptions());
                var recorder = new RecordingFulfillmentAdmin(adapter);
                var tipax = new StaticOptions(new TipaxV4Options
                {
                    SuccessCallbackUrl = "https://store.test/ok",
                    FailureCallbackUrl = "https://store.test/fail",
                });
                var accounts = new StubAccounts(accountId, accountConfigured, accountActive, origin);
                var readiness = new ShippingCarrierExecutionReadiness(
                    shippingDb, accounts, new StubRegistry(provider), tipax);
                var facts = new OrderCommittedCarrierShipmentFactsReader(orderDb, recorder);
                var directory = new ShippingCarrierShipmentDirectory(
                    shippingDb, accounts, new StubRegistry(provider), facts, readiness, recorder, tipax);

                return new Fixture(
                    target.ConnectionString, orderDb, shippingDb, fulfillmentDb, fulfillmentDirectory,
                    recorder, directory, provider, checkoutId, groupId, orderLineId, sellerOrderId,
                    unit.FulfillmentId, actor, destination);
            }
            else
            {
                // Cancelled seller: still migrate/wire directory against empty fulfillment eligibility.
                var shippingDb = CreateShippingDb(target.ConnectionString);
                var provider = new RecordingShipmentProvider();
                var adapter = new FulfillmentAdminOperationsAdapter(
                    fulfillmentDirectory,
                    new FulfillmentShippedQuantityReader(fulfillmentDb),
                    new ShippingMethodsOptions());
                var recorder = new RecordingFulfillmentAdmin(adapter);
                var tipax = new StaticOptions(new TipaxV4Options
                {
                    SuccessCallbackUrl = "https://store.test/ok",
                    FailureCallbackUrl = "https://store.test/fail",
                });
                var accounts = new StubAccounts(accountId, accountConfigured, accountActive, origin);
                var readiness = new ShippingCarrierExecutionReadiness(
                    shippingDb, accounts, new StubRegistry(provider), tipax);
                var facts = new OrderCommittedCarrierShipmentFactsReader(orderDb, recorder);
                var directory = new ShippingCarrierShipmentDirectory(
                    shippingDb, accounts, new StubRegistry(provider), facts, readiness, recorder, tipax);
                return new Fixture(
                    target.ConnectionString, orderDb, shippingDb, fulfillmentDb, fulfillmentDirectory,
                    recorder, directory, provider, checkoutId, groupId, orderLineId, sellerOrderId,
                    Guid.Empty, actor, destination);
            }
        }

        public async Task PrepareSenderAsync()
        {
            var saved = await Directory.SaveSenderCompletionAsync(
                Owner,
                new ShippingCarrierShipmentSenderCompletionRequest(
                    CheckoutId, GroupId, "فرستنده", "09120000000", "مبدأ", "1111111111"),
                CancellationToken.None);
            Assert.True(saved.IsSuccess, saved.IsFailure ? saved.Errors[0].Code : null);
        }

        /// <summary>
        /// Second independent context graph against the same seeded identities (for concurrent matrix paths).
        /// </summary>
        public static Task<Fixture> AttachSiblingAsync(Fixture seed)
        {
            var orderDb = CreateOrderDb(seed.ConnectionString);
            var shippingDb = CreateShippingDb(seed.ConnectionString);
            var fulfillmentDb = CreateFulfillmentDb(seed.ConnectionString);
            var handoff = new OrderFulfillmentHandoffSnapshot(
                seed.SellerOrderId,
                seed.CheckoutId,
                Guid.Parse("44444444-4444-4444-8444-444444444444"),
                Guid.NewGuid(),
                true,
                "گیرنده", "09121111111", "تهران", "تهران", "نشانی", "1111111111",
                "tipax", "Tipax",
                [new OrderFulfillmentLineSnapshot(seed.OrderLineId, 1m, null)],
                [new OrderFulfillmentShippingGroupSnapshot(
                    seed.GroupId, seed.SellerOrderId,
                    Guid.Parse("44444444-4444-4444-8444-444444444444"),
                    "tipax", null, null, "Tipax", null,
                    new DateOnly(2026, 11, 4), null, null, 1, [seed.OrderLineId])]);
            var fulfillmentDirectory = new FulfillmentDirectory(
                fulfillmentDb,
                new OpenFulfillmentUseCaseGuard(),
                new FixedOrderReader(handoff),
                new NoopInventory(),
                new FulfillmentInstrumentation(),
                new SystemUtcClock(),
                new UuidV7IdGenerator());
            var provider = new RecordingShipmentProvider();
            var adapter = new FulfillmentAdminOperationsAdapter(
                fulfillmentDirectory,
                new FulfillmentShippedQuantityReader(fulfillmentDb),
                new ShippingMethodsOptions());
            var recorder = new RecordingFulfillmentAdmin(adapter);
            var tipax = new StaticOptions(new TipaxV4Options
            {
                SuccessCallbackUrl = "https://store.test/ok",
                FailureCallbackUrl = "https://store.test/fail",
            });
            var accounts = new StubAccounts(
                Guid.Parse("33333333-3333-4333-8333-333333333333"), true, true, "101");
            var readiness = new ShippingCarrierExecutionReadiness(
                shippingDb, accounts, new StubRegistry(provider), tipax);
            var facts = new OrderCommittedCarrierShipmentFactsReader(orderDb, recorder);
            var directory = new ShippingCarrierShipmentDirectory(
                shippingDb, accounts, new StubRegistry(provider), facts, readiness, recorder, tipax);
            return Task.FromResult(new Fixture(
                seed.ConnectionString, orderDb, shippingDb, fulfillmentDb, fulfillmentDirectory,
                recorder, directory, provider, seed.CheckoutId, seed.GroupId, seed.OrderLineId, seed.SellerOrderId,
                seed.FulfillmentId, seed.ActorUserId, seed.Destination));
        }

        public async Task<decimal> ReadAcceptedFreightAsync()
        {
            OrderDb.ChangeTracker.Clear();
            var group = await OrderDb.OrderShippingGroups.AsNoTracking()
                .SingleAsync(x => x.ShippingGroupId == GroupId);
            return group.AcceptedEvidence!.FreightAmount;
        }

        public static ShippingDbContext CreateShippingDb(string connectionString)
        {
            var builder = new DbContextOptionsBuilder<ShippingDbContext>();
            ToobaNpgsql.ConfigureModuleContext(builder, connectionString, ShippingDbContext.Schema, typeof(ShippingDbContext));
            return new ShippingDbContext(builder.Options);
        }

        private static OrderDbContext CreateOrderDb(string connectionString)
        {
            var builder = new DbContextOptionsBuilder<OrderDbContext>();
            ToobaNpgsql.ConfigureModuleContext(builder, connectionString, OrderDbContext.Schema, typeof(OrderDbContext));
            return new OrderDbContext(builder.Options);
        }

        private static FulfillmentDbContext CreateFulfillmentDb(string connectionString)
        {
            var builder = new DbContextOptionsBuilder<FulfillmentDbContext>();
            ToobaNpgsql.ConfigureModuleContext(builder, connectionString, FulfillmentDbContext.Schema, typeof(FulfillmentDbContext));
            return new FulfillmentDbContext(builder.Options);
        }

        private static async Task WipeAsync(string connectionString)
        {
            await using var wipeOrder = CreateOrderDb(connectionString);
            await using var wipeShip = CreateShippingDb(connectionString);
            await using var wipeFul = CreateFulfillmentDb(connectionString);
            await wipeShip.Database.ExecuteSqlRawAsync(
                """
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='shipping' AND table_name='carrier_shipment_operations') THEN
                    DELETE FROM shipping.carrier_shipment_operations;
                    DELETE FROM shipping.carrier_shipment_package_completions;
                    DELETE FROM shipping.carrier_shipment_sender_completions;
                  END IF;
                END $$;
                """);
            await wipeFul.Database.ExecuteSqlRawAsync(
                """
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='fulfillment' AND table_name='shipments') THEN
                    DELETE FROM fulfillment.shipment_items;
                    DELETE FROM fulfillment.shipments;
                    DELETE FROM fulfillment.items;
                    DELETE FROM fulfillment.shipping_groups;
                    DELETE FROM fulfillment.payment_inbox;
                    DELETE FROM fulfillment.fulfillments;
                  END IF;
                END $$;
                """);
            await wipeOrder.Database.ExecuteSqlRawAsync(
                """
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='order' AND table_name='order_shipping_groups') THEN
                    DELETE FROM "order".order_shipping_group_lines;
                    DELETE FROM "order".order_shipping_groups;
                    DELETE FROM "order".order_lines;
                    DELETE FROM "order".seller_orders;
                    DELETE FROM "order".checkouts;
                  END IF;
                END $$;
                """);
        }

        private static async Task EnsureMigratedAsync(ShippingCarrierPackageExecutionRelationalTestTarget target)
        {
            await MigrationGate.WaitAsync();
            try
            {
                await using var shipping = CreateShippingDb(target.ConnectionString);
                if (!_liveVerified)
                {
                    var facts = await shipping.Database
                        .SqlQueryRaw<string>("SELECT (current_database() || '|' || current_user) AS \"Value\"")
                        .SingleAsync();
                    var parts = facts.Split('|');
                    Assert.Equal(ShippingCarrierPackageExecutionRelationalTestTarget.ExpectedDatabase, parts[0]);
                    Assert.Equal(ShippingCarrierPackageExecutionRelationalTestTarget.ExpectedUser, parts[1]);
                    _liveVerified = true;
                }

                if (!_migrated)
                {
                    await using var order = CreateOrderDb(target.ConnectionString);
                    await using var fulfillment = CreateFulfillmentDb(target.ConnectionString);
                    await order.Database.MigrateAsync();
                    await shipping.GetService<IMigrator>().MigrateAsync();
                    await fulfillment.Database.MigrateAsync();
                    _migrated = true;
                }
            }
            finally
            {
                MigrationGate.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await OrderDb.DisposeAsync();
            await ShippingDb.DisposeAsync();
            await FulfillmentDb.DisposeAsync();
        }
    }
}

[CollectionDefinition(ShippingCarrierPackageExecutionRelationalCollection.Name, DisableParallelization = true)]
public sealed class ShippingCarrierPackageExecutionRelationalCollection
{
    public const string Name = "TB249R4-ShippingCarrierPackageExecution";
}

internal sealed class RecordingShipmentProvider : ICarrierShipmentProvider
{
    public string ProviderCode => "tipax";
    public int Creates { get; private set; }
    public int Tracks { get; private set; }
    public CarrierShipmentOutcome Outcome { get; set; } = CarrierShipmentOutcome.Succeeded;
    public bool ThrowOnCreate { get; set; }
    public Func<CarrierShipmentCreateRequest, Task>? OnCreate { get; set; }
    public CarrierShipmentCreateRequest? LastRequest { get; private set; }
    public ConcurrentBag<CarrierShipmentCreateRequest> Requests { get; } = new();
    public CarrierShipmentCapabilities Capabilities { get; } = new("tipax", true, true, true, false);

    public async Task<CarrierShipmentResult> CreateAsync(CarrierShipmentCreateRequest request, CancellationToken cancellationToken)
    {
        Creates++;
        LastRequest = request;
        Requests.Add(request);
        if (OnCreate is not null)
        {
            await OnCreate(request);
        }

        if (ThrowOnCreate)
        {
            throw new InvalidOperationException("tb249r4: injected provider failure before HTTP completion");
        }

        return Outcome == CarrierShipmentOutcome.Succeeded
            ? new CarrierShipmentResult(Outcome, TrackingCode: "TRK-1", ProviderOrderId: "ord-1")
            : CarrierShipmentResult.Failure(Outcome, ShippingErrorCodes.CarrierShipmentUnknown);
    }

    public Task<CarrierShipmentResult> TrackAsync(CarrierShipmentTrackRequest request, CancellationToken cancellationToken)
    {
        Tracks++;
        return Task.FromResult(new CarrierShipmentResult(CarrierShipmentOutcome.Succeeded, TrackingCode: "TRK-1", ProviderStatus: "x"));
    }

    public Task<CarrierShipmentResult> CancelAsync(CarrierShipmentCancelRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new CarrierShipmentResult(CarrierShipmentOutcome.Succeeded, ProviderStatus: "cancelled"));
}

internal sealed class StubRegistry(ICarrierShipmentProvider provider) : ICarrierShipmentProviderRegistry
{
    public bool TryResolve(string providerCode, out ICarrierShipmentProvider? resolved)
    {
        resolved = provider;
        return string.Equals(providerCode, provider.ProviderCode, StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class StubAccounts(Guid accountId, bool configured, bool active, string origin) : IShippingCarrierAccountDirectory
{
    private readonly Guid _accountId = accountId;
    private readonly bool _configured = configured;
    private readonly bool _active = active;
    private readonly string _origin = origin;

    public Task<Result<IReadOnlyList<ShippingCarrierAccountView>>> ListAccountsAsync(ShippingAuthoringOwnerScope owner, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<Result<ShippingCarrierAccountView>> GetAccountAsync(ShippingAuthoringOwnerScope owner, Guid accountId, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<Result<ShippingCarrierAccountView>> UpsertAccountAsync(ShippingAuthoringOwnerScope owner, Guid? accountId, ShippingCarrierAccountUpsertRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<Result<ShippingCarrierConnectionTestOutcome>> TestConnectionAsync(ShippingAuthoringOwnerScope owner, Guid accountId, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<Result<IReadOnlyList<ShippingCarrierReferenceValueView>>> SyncReferencesAsync(ShippingAuthoringOwnerScope owner, Guid accountId, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<Result<IReadOnlyList<ShippingCarrierReferenceValueView>>> ListReferencesAsync(ShippingAuthoringOwnerScope owner, Guid accountId, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<Result<IReadOnlyList<ShippingCarrierDestinationMappingView>>> ListDestinationMappingsAsync(ShippingAuthoringOwnerScope owner, Guid accountId, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<Result<ShippingCarrierDestinationMappingView>> UpsertDestinationMappingAsync(ShippingAuthoringOwnerScope owner, Guid accountId, ShippingCarrierDestinationMappingUpsertRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<Result<ShippingCarrierMethodBindingView?>> GetMethodBindingAsync(ShippingAuthoringOwnerScope owner, Guid draftId, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<Result<ShippingCarrierMethodBindingView>> ReplaceMethodBindingAsync(ShippingAuthoringOwnerScope owner, Guid draftId, ShippingCarrierMethodBindingReplaceRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<string?> TryResolveDestinationExternalCodeAsync(Guid accountId, Guid geographyCityId, CancellationToken cancellationToken) => throw new NotSupportedException();
    public Task<CarrierBindingExecution?> GetMethodBindingForDraftAsync(Guid draftId, ShippingDraftOwnerKind ownerKind, string? ownerTenantId, Guid? ownerSellerPartyId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<ShippingCarrierAccountExecutionContext?> TryResolveExecutionContextAsync(
        ShippingDraftOwnerKind ownerKind, string? ownerTenantId, Guid? ownerSellerPartyId, Guid accountId, CancellationToken cancellationToken)
    {
        if (!_configured || accountId != _accountId)
        {
            return Task.FromResult<ShippingCarrierAccountExecutionContext?>(null);
        }

        return Task.FromResult<ShippingCarrierAccountExecutionContext?>(new ShippingCarrierAccountExecutionContext(
            _accountId, "tipax", "Tipax", _active, 1,
            "https://api.test/", "https://tokens.test/Token", "IRR", "user", "pass",
            new ShippingCarrierMethodBindingView(
                Guid.NewGuid(), Guid.NewGuid(), _accountId, "tipax", "7",
                "0", "0", "0", "0", "0", "0",
                _origin, "مبدأ", null, null, 20m, 15m, 10m, 1, DateTimeOffset.UtcNow)));
    }
}

internal sealed class StaticOptions(TipaxV4Options value) : IOptionsMonitor<TipaxV4Options>
{
    public TipaxV4Options CurrentValue => value;
    public TipaxV4Options Get(string? name) => value;
    public IDisposable? OnChange(Action<TipaxV4Options, string?> listener) => null;
}

internal sealed class NoopInventory : IFulfillmentInventoryGateway
{
    public Task ConsumeReservationAsync(Guid reservationId, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class FixedOrderReader(OrderFulfillmentHandoffSnapshot snapshot) : IOrderFulfillmentReader
{
    public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffAsync(Guid sellerOrderId, CancellationToken cancellationToken) =>
        Task.FromResult(snapshot.SellerOrderId == sellerOrderId ? snapshot : null);

    public Task<OrderFulfillmentHandoffSnapshot?> GetHandoffForCheckoutAsync(Guid checkoutId, Guid actorUserId, CancellationToken cancellationToken) =>
        Task.FromResult(snapshot.CheckoutId == checkoutId ? snapshot : null);
}

/// <summary>Decorator over the real Fulfillment admin adapter; records create actors/quantities and can inject refusal.</summary>
internal sealed class RecordingFulfillmentAdmin(IFulfillmentAdminOperations inner) : IFulfillmentAdminOperations
{
    public bool FailNextCreate { get; set; }
    public Guid LastActorUserId { get; private set; }
    public int CreatedCount { get; private set; }
    public decimal AllocatedQuantityTotal { get; private set; }

    public Task<FulfillmentSnapshot?> GetAsync(Guid fulfillmentId, CancellationToken cancellationToken) => inner.GetAsync(fulfillmentId, cancellationToken);
    public Task<IReadOnlyList<FulfillmentSnapshot>> ListForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) => inner.ListForCheckoutAsync(checkoutId, cancellationToken);
    public Task<IReadOnlyList<ConsolidatedPackageSnapshot>> GetPackagesForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) => inner.GetPackagesForCheckoutAsync(checkoutId, cancellationToken);
    public Task<IReadOnlyList<ActivePackageMembershipSnapshot>> GetActiveMembershipByShipmentIdsAsync(IReadOnlyList<Guid> shipmentIds, CancellationToken cancellationToken) => inner.GetActiveMembershipByShipmentIdsAsync(shipmentIds, cancellationToken);
    public IReadOnlyList<ShippingMethodOption> ListEnabledShippingMethods() => inner.ListEnabledShippingMethods();
    public string ResolveShippingMethodLabel(string? code, string? fallbackDisplayName) => inner.ResolveShippingMethodLabel(code, fallbackDisplayName);
    public Task<FulfillmentSnapshot> MarkProcessingAsync(Guid fulfillmentId, Guid actorUserId, CancellationToken cancellationToken) => inner.MarkProcessingAsync(fulfillmentId, actorUserId, cancellationToken);
    public Task<FulfillmentSnapshot> ProcessSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) => inner.ProcessSelectionsAsync(fulfillmentId, actorUserId, selections, cancellationToken);
    public Task<FulfillmentSnapshot> UnprocessSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) => inner.UnprocessSelectionsAsync(fulfillmentId, actorUserId, selections, cancellationToken);
    public Task<FulfillmentSnapshot> PackSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) => inner.PackSelectionsAsync(fulfillmentId, actorUserId, selections, cancellationToken);
    public Task<FulfillmentSnapshot> UnpackSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) => inner.UnpackSelectionsAsync(fulfillmentId, actorUserId, selections, cancellationToken);

    public async Task<FulfillmentShipmentCreateResult> CreateShipmentAsync(
        Guid fulfillmentId, Guid actorUserId, string carrierDisplayName, IReadOnlyList<ShipmentLineCommand> items,
        string? shippingMethodCode, string? providerMetadataJson, CancellationToken cancellationToken, Guid? shippingGroupId = null)
    {
        LastActorUserId = actorUserId;
        if (FailNextCreate)
        {
            FailNextCreate = false;
            throw new ContractOperationException("fulfillment.injected.failure");
        }

        var created = await inner.CreateShipmentAsync(
            fulfillmentId, actorUserId, carrierDisplayName, items, shippingMethodCode, providerMetadataJson, cancellationToken, shippingGroupId);
        CreatedCount++;
        AllocatedQuantityTotal += items.Sum(x => x.Quantity);
        return created;
    }

    public Task<FulfillmentSnapshot> CancelShipmentAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) => inner.CancelShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
    public Task<FulfillmentSnapshot> AssignTrackingAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, string trackingReference, CancellationToken cancellationToken) => inner.AssignTrackingAsync(fulfillmentId, shipmentId, actorUserId, trackingReference, cancellationToken);
    public Task<FulfillmentSnapshot> CorrectTrackingAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, string trackingReference, CancellationToken cancellationToken) => inner.CorrectTrackingAsync(fulfillmentId, shipmentId, actorUserId, trackingReference, cancellationToken);
    public Task<FulfillmentSnapshot> DispatchShipmentAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) => inner.DispatchShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
    public Task<FulfillmentSnapshot> DeliverShipmentAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) => inner.DeliverShipmentAsync(fulfillmentId, shipmentId, actorUserId, cancellationToken);
    public Task<ConsolidatedPackageSnapshot> CreateConsolidatedPackageAsync(Guid checkoutId, IReadOnlyList<Guid> shipmentIds, string? shippingMethodCode, string? trackingReference, string? note, Guid actorUserId, CancellationToken cancellationToken) => inner.CreateConsolidatedPackageAsync(checkoutId, shipmentIds, shippingMethodCode, trackingReference, note, actorUserId, cancellationToken);
    public Task<ConsolidatedPackageSnapshot> CancelConsolidatedPackageAsync(Guid consolidatedPackageId, Guid actorUserId, CancellationToken cancellationToken) => inner.CancelConsolidatedPackageAsync(consolidatedPackageId, actorUserId, cancellationToken);
    public Task<ConsolidatedPackageSnapshot> AssignConsolidatedPackageTrackingAsync(Guid consolidatedPackageId, string trackingReference, Guid actorUserId, CancellationToken cancellationToken) => inner.AssignConsolidatedPackageTrackingAsync(consolidatedPackageId, trackingReference, actorUserId, cancellationToken);
    public Task<ConsolidatedPackageSnapshot> DispatchConsolidatedPackageAsync(Guid consolidatedPackageId, Guid actorUserId, CancellationToken cancellationToken) => inner.DispatchConsolidatedPackageAsync(consolidatedPackageId, actorUserId, cancellationToken);
    public Task<ConsolidatedPackageSnapshot> DeliverConsolidatedPackageAsync(Guid consolidatedPackageId, Guid actorUserId, CancellationToken cancellationToken) => inner.DeliverConsolidatedPackageAsync(consolidatedPackageId, actorUserId, cancellationToken);
    public Task AbortForCheckoutCancelAsync(Guid checkoutId, CancellationToken cancellationToken) => inner.AbortForCheckoutCancelAsync(checkoutId, cancellationToken);
    public Task ReactivateAfterOrderRestoreAsync(Guid checkoutId, CancellationToken cancellationToken) => inner.ReactivateAfterOrderRestoreAsync(checkoutId, cancellationToken);
    public Task EnsureCreatedForPaidCheckoutAsync(Guid checkoutId, IReadOnlyList<Guid> sellerOrderIds, CancellationToken cancellationToken) => inner.EnsureCreatedForPaidCheckoutAsync(checkoutId, sellerOrderIds, cancellationToken);
    public Task VoidUnstartedForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) => inner.VoidUnstartedForCheckoutAsync(checkoutId, cancellationToken);
    public Task RebindActiveReservationsFromOrderAsync(Guid checkoutId, CancellationToken cancellationToken) => inner.RebindActiveReservationsFromOrderAsync(checkoutId, cancellationToken);
    public Task<IReadOnlyDictionary<Guid, decimal>> GetShippedByOrderLineIdsForCheckoutsAsync(IReadOnlyList<Guid> checkoutIds, CancellationToken cancellationToken) => inner.GetShippedByOrderLineIdsForCheckoutsAsync(checkoutIds, cancellationToken);
}
