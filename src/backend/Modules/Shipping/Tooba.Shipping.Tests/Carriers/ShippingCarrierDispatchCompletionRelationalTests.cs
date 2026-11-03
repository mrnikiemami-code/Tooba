using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Infrastructure.Adapters;
using Tooba.Fulfillment.Infrastructure.Directories;
using Tooba.Fulfillment.Infrastructure.Observability;
using Tooba.Fulfillment.Infrastructure.Persistence;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Order.Domain.Aggregates;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Domain.Enums;
using Tooba.Order.Infrastructure.Integrations.Shipping;
using Tooba.Order.Infrastructure.Persistence;
using Tooba.Persistence;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Carriers;
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
/// TB-249-R5 disposable PostgreSQL dispatch-completion matrix (fork of R4 fixture).
/// Cases a–e: barrier concurrency, provider-accept crash→UNKNOWN, post-allocate link resume,
/// multi-package money/dims, snapshot rotation refusal. Helpers reused from R4 file.
/// </summary>
[Collection(ShippingCarrierDispatchCompletionRelationalCollection.Name)]
public sealed class ShippingCarrierDispatchCompletionRelationalTests
{
    private static readonly SemaphoreSlim MigrationGate = new(1, 1);
    private static bool _migrated;
    private static bool _liveVerified;

    [Fact]
    [Trait("Category", "TB249R5")]
    [Trait("Case", "a")]
    public async Task CaseA_barrier_concurrent_same_dispatch_one_allocation_one_provider()
    {
        await using var fxA = await Fixture.CreateAsync(lineQuantity: 2m);
        await fxA.PrepareSenderAsync();
        // Separate fulfillment graphs; shared controlled provider so Creates is authoritative.
        await using var fxB = await Fixture.AttachSiblingAsync(fxA, shareProvider: true, shareRecorder: false);

        using var startBarrier = new Barrier(2);
        var providerGateHits = 0;
        fxA.Provider.OnCreate = _ =>
        {
            Interlocked.Increment(ref providerGateHits);
            return Task.CompletedTask;
        };

        var results = await Task.WhenAll(
            Task.Run(async () =>
            {
                startBarrier.SignalAndWait(TimeSpan.FromSeconds(15));
                return await fxA.Directory.CreateAsync(
                    fxA.Owner, fxA.ActorUserId,
                    new ShippingCarrierShipmentExecuteRequest(fxA.CheckoutId, fxA.GroupId), CancellationToken.None);
            }),
            Task.Run(async () =>
            {
                startBarrier.SignalAndWait(TimeSpan.FromSeconds(15));
                return await fxB.Directory.CreateAsync(
                    fxB.Owner, fxB.ActorUserId,
                    new ShippingCarrierShipmentExecuteRequest(fxB.CheckoutId, fxB.GroupId), CancellationToken.None);
            }));

        Assert.Contains(results, x => x.IsSuccess);
        Assert.Equal(1, fxA.Provider.Creates);
        Assert.Equal(1, providerGateHits);
        var shipmentCount = await fxA.FulfillmentDb.Shipments.AsNoTracking().CountAsync();
        Assert.Equal(1, shipmentCount);
        var shipped = await fxA.FulfillmentDb.ShipmentItems.AsNoTracking().SumAsync(x => (decimal?)x.Quantity) ?? 0m;
        Assert.True(shipped <= 2m);
        // Ledger: startBarrier participants=2, providerCreates=1, allocations=1.
        Assert.Equal(2, startBarrier.ParticipantCount);
    }

    [Fact]
    [Trait("Category", "TB249R5")]
    [Trait("Case", "b")]
    public async Task CaseB_provider_accepts_then_local_cancel_before_result_save_replay_unknown_no_resend()
    {
        await using var fx = await Fixture.CreateAsync(lineQuantity: 1m);
        await fx.PrepareSenderAsync();
        fx.Provider.OnCreate = _ => throw new OperationCanceledException("tb249r5: cancel after provider accept before result save");

        await Assert.ThrowsAsync<OperationCanceledException>(() => fx.Directory.CreateAsync(
            fx.Owner, fx.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(fx.CheckoutId, fx.GroupId), CancellationToken.None));
        Assert.Equal(1, fx.Provider.Creates);

        fx.Provider.OnCreate = null;
        fx.Provider.ThrowOnCreate = false;
        var before = fx.Provider.Creates;
        var replay = await fx.Directory.CreateAsync(
            fx.Owner, fx.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(fx.CheckoutId, fx.GroupId), CancellationToken.None);
        Assert.True(replay.IsSuccess, replay.IsFailure ? replay.Errors[0].Code : null);
        Assert.Equal(ShippingCarrierShipmentStateView.Unknown, replay.Value.State);
        Assert.Equal(before, fx.Provider.Creates);
        Assert.Equal(1, fx.Recorder.CreatedCount);
    }

    [Fact]
    [Trait("Category", "TB249R5")]
    [Trait("Case", "c")]
    public async Task CaseC_failure_after_allocation_before_shipping_link_resumes_same_allocation()
    {
        await using var fx = await Fixture.CreateAsync(lineQuantity: 2m);
        await fx.PrepareSenderAsync();
        fx.Recorder.FailAfterAllocate = true;

        var first = await fx.Directory.CreateAsync(
            fx.Owner, fx.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(fx.CheckoutId, fx.GroupId), CancellationToken.None);
        Assert.True(first.IsFailure);
        Assert.Equal(0, fx.Provider.Creates);
        Assert.Equal(1, fx.Recorder.AllocateAttempts);
        var firstShipmentId = fx.Recorder.LastCreatedShipmentId;
        Assert.NotNull(firstShipmentId);
        var qtyAfterFail = await fx.FulfillmentDb.ShipmentItems.AsNoTracking().SumAsync(x => (decimal?)x.Quantity) ?? 0m;
        Assert.Equal(2m, qtyAfterFail);

        var resume = await fx.Directory.CreateAsync(
            fx.Owner, fx.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(fx.CheckoutId, fx.GroupId), CancellationToken.None);
        Assert.True(resume.IsSuccess, resume.IsFailure ? resume.Errors[0].Code : null);
        Assert.Equal(1, fx.Provider.Creates);
        Assert.Equal(2, fx.Recorder.AllocateAttempts);
        Assert.Equal(firstShipmentId, fx.Recorder.LastCreatedShipmentId);
        var qtyAfterResume = await fx.FulfillmentDb.ShipmentItems.AsNoTracking().SumAsync(x => (decimal?)x.Quantity) ?? 0m;
        Assert.Equal(2m, qtyAfterResume);
        Assert.Equal(1, await fx.FulfillmentDb.Shipments.AsNoTracking().CountAsync());
    }

    [Fact]
    [Trait("Category", "TB249R5")]
    [Trait("Case", "d")]
    public async Task CaseD_different_physical_packages_own_qty_dims_declared_collect_residual()
    {
        await using var fx = await Fixture.CreateMixedValueAsync(unitPriceA: 100m, unitPriceB: 900m, collectAmount: 10_000m);
        await fx.PrepareSenderAsync();

        var first = await fx.Directory.CreateAsync(
            fx.Owner, fx.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(
                fx.CheckoutId, fx.GroupId,
                new[] { new ShippingCarrierShipmentLineQuantity(fx.OrderLineId, 1m) },
                new ShippingCarrierShipmentDispatchPackageInput(400m, 11m, 12m, 13m)),
            CancellationToken.None);
        Assert.True(first.IsSuccess, first.IsFailure ? first.Errors[0].Code : null);
        Assert.Equal(100m, fx.Provider.LastRequest!.Package.DeclaredValue);
        Assert.Equal(400m, fx.Provider.LastRequest.Package.WeightGrams);
        Assert.Equal(11m, fx.Provider.LastRequest.Package.LengthCentimeters);
        var firstCollect = fx.Provider.LastRequest.Package.CollectAmount;
        Assert.Equal(1000m, firstCollect); // 10000 * (100/1000) IRR scale0

        var second = await fx.Directory.CreateAsync(
            fx.Owner, fx.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(
                fx.CheckoutId, fx.GroupId,
                new[] { new ShippingCarrierShipmentLineQuantity(fx.OrderLineIdB, 1m) },
                new ShippingCarrierShipmentDispatchPackageInput(800m, 21m, 22m, 23m)),
            CancellationToken.None);
        Assert.True(second.IsSuccess, second.IsFailure ? second.Errors[0].Code : null);
        Assert.Equal(900m, fx.Provider.LastRequest!.Package.DeclaredValue);
        Assert.Equal(800m, fx.Provider.LastRequest.Package.WeightGrams);
        Assert.Equal(21m, fx.Provider.LastRequest.Package.LengthCentimeters);
        Assert.Equal(22m, fx.Provider.LastRequest.Package.WidthCentimeters);
        Assert.Equal(23m, fx.Provider.LastRequest.Package.HeightCentimeters);
        // Final remaining collect residual: 10000 - 1000 = 9000
        Assert.Equal(9000m, fx.Provider.LastRequest.Package.CollectAmount);
        Assert.Equal(2, fx.Provider.Creates);
        Assert.Equal(10_000m, firstCollect + fx.Provider.LastRequest.Package.CollectAmount);
    }

    [Fact]
    [Trait("Category", "TB249R5")]
    [Trait("Case", "e")]
    public async Task CaseE_snapshot_account_rotation_disable_config_refused_without_substitution()
    {
        await using var beforeProvider = await Fixture.CreateAsync(lineQuantity: 1m);
        await beforeProvider.PrepareSenderAsync();
        beforeProvider.Provider.ThrowOnCreate = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => beforeProvider.Directory.CreateAsync(
            beforeProvider.Owner, beforeProvider.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(beforeProvider.CheckoutId, beforeProvider.GroupId),
            CancellationToken.None));
        Assert.Equal(1, beforeProvider.Provider.Creates);

        // Rotation / disable after ExternalDispatchAttempted: replay stays UNKNOWN (no silent substitute).
        beforeProvider.Accounts.RotateConfiguration(99);
        beforeProvider.Provider.ThrowOnCreate = false;
        var rotated = await beforeProvider.Directory.CreateAsync(
            beforeProvider.Owner, beforeProvider.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(beforeProvider.CheckoutId, beforeProvider.GroupId),
            CancellationToken.None);
        Assert.True(rotated.IsSuccess);
        Assert.Equal(ShippingCarrierShipmentStateView.Unknown, rotated.Value.State);
        Assert.Equal(1, beforeProvider.Provider.Creates);

        await using var pending = await Fixture.CreateAsync(lineQuantity: 1m);
        await pending.PrepareSenderAsync();
        pending.Recorder.FailNextCreate = true;
        Assert.True((await pending.Directory.CreateAsync(
            pending.Owner, pending.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(pending.CheckoutId, pending.GroupId), CancellationToken.None)).IsFailure);
        Assert.Equal(0, pending.Provider.Creates);
        pending.Accounts.Disable();
        var disabled = await pending.Directory.CreateAsync(
            pending.Owner, pending.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(pending.CheckoutId, pending.GroupId), CancellationToken.None);
        Assert.Equal(ShippingErrorCodes.CarrierUnconfigured, disabled.FirstError.Code);
        Assert.Equal(0, pending.Provider.Creates);

        await using var configChange = await Fixture.CreateAsync(lineQuantity: 1m);
        await configChange.PrepareSenderAsync();
        configChange.Recorder.FailNextCreate = true;
        Assert.True((await configChange.Directory.CreateAsync(
            configChange.Owner, configChange.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(configChange.CheckoutId, configChange.GroupId), CancellationToken.None)).IsFailure);
        configChange.Accounts.RotateConfiguration(7);
        var refused = await configChange.Directory.CreateAsync(
            configChange.Owner, configChange.ActorUserId,
            new ShippingCarrierShipmentExecuteRequest(configChange.CheckoutId, configChange.GroupId), CancellationToken.None);
        Assert.Equal(ShippingErrorCodes.CarrierShipmentNotEligible, refused.FirstError.Code);
        Assert.Equal(0, configChange.Provider.Creates);
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
            StubAccounts accounts,
            Guid checkoutId,
            Guid groupId,
            Guid orderLineId,
            Guid orderLineIdB,
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
            Accounts = accounts;
            CheckoutId = checkoutId;
            GroupId = groupId;
            OrderLineId = orderLineId;
            OrderLineIdB = orderLineIdB;
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
        public StubAccounts Accounts { get; }
        public Guid CheckoutId { get; }
        public Guid GroupId { get; }
        public Guid OrderLineId { get; }
        public Guid OrderLineIdB { get; }
        public Guid SellerOrderId { get; }
        public Guid FulfillmentId { get; }
        public Guid ActorUserId { get; }
        public string Destination { get; }
        public ShippingAuthoringOwnerScope Owner { get; }

        public static Task<Fixture> CreateAsync(
            decimal lineQuantity,
            bool cancelSeller = false,
            bool accountActive = true,
            bool accountConfigured = true)
            => CreateCoreAsync(
                [(Guid.NewGuid(), lineQuantity, 100_000m)],
                cancelSeller,
                accountActive,
                accountConfigured,
                collectAmount: 0m);

        public static Task<Fixture> CreateMixedValueAsync(decimal unitPriceA, decimal unitPriceB, decimal collectAmount)
            => CreateCoreAsync(
                [(Guid.NewGuid(), 1m, unitPriceA), (Guid.NewGuid(), 1m, unitPriceB)],
                cancelSeller: false,
                accountActive: true,
                accountConfigured: true,
                collectAmount);

        private static async Task<Fixture> CreateCoreAsync(
            (Guid LineSeed, decimal Qty, decimal UnitPrice)[] linesSpec,
            bool cancelSeller,
            bool accountActive,
            bool accountConfigured,
            decimal collectAmount)
        {
            var target = ShippingCarrierDispatchCompletionRelationalTestTarget.FromEnvironment();
            await EnsureMigratedAsync(target);

            var checkoutId = Guid.NewGuid();
            var groupId = Guid.NewGuid();
            var sellerOrderId = Guid.NewGuid();
            var sellerPartyId = Guid.Parse("44444444-4444-4444-8444-444444444444");
            var accountId = Guid.Parse("33333333-3333-4333-8333-333333333333");
            var actor = Guid.Parse("88888888-8888-4888-8888-888888888888");
            var buyer = Guid.Parse("99999999-9999-4999-8999-999999999999");
            const string origin = "101";
            const string destination = "202";
            var now = DateTimeOffset.Parse("2026-11-03T00:00:00Z");

            await WipeAsync(target.ConnectionString);

            var orderDb = CreateOrderDb(target.ConnectionString);
            var orderLines = new List<OrderLine>();
            var cartLineIds = new List<Guid>();
            foreach (var (seed, qty, unit) in linesSpec)
            {
                var cartLineId = Guid.NewGuid();
                cartLineIds.Add(cartLineId);
                var tax = decimal.Round(unit * qty * 0.09m, 2, MidpointRounding.ToZero);
                var line = OrderLine.FromCheckout(
                    sellerOrderId, seed, Guid.NewGuid(), sellerPartyId,
                    qty, unit, "IRR", true, Guid.NewGuid(), null, "Taxable", 0.09m, tax, (unit * qty) + tax, null);
                orderLines.Add(line);
            }

            var sellerOrder = SellerOrder.Open(
                checkoutId, sellerPartyId, $"SO-{sellerOrderId:N}"[..20],
                OrderMode.OnlinePurchase, "IRR", orderLines);
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

            // Quote money is independent of checkout collect split; keep R4-valid components.
            var quote = ShippingGroupAcceptedCarrierQuote.Create(
                "tipax", accountId, 1, "7", new string('a', 64), "IRR",
                137_500m, 120_000m, 12_500m, 5_000m, 0m, 0m,
                now, now.AddMinutes(30),
                origin, destination, 1500m, 2_000_000m, 20m, 15m, 10m, null);
            // Evidence conservation: freight + packaging = payable + collect.
            decimal freight;
            decimal packaging;
            decimal payable;
            if (collectAmount == 0m)
            {
                freight = 132_500m;
                packaging = 5_000m;
                payable = 137_500m;
            }
            else
            {
                // Quote FinalAmount is 137500; evidence freight+packaging must match that exactly.
                freight = 132_500m;
                packaging = 5_000m;
                payable = freight + packaging - collectAmount;
            }

            var evidence = ShippingGroupAcceptedEvidence.Create(
                Guid.NewGuid(), Guid.NewGuid(), 1, ShippingGroupAcceptedEvidence.SupportedFormatVersion, 1,
                ShippingGroupOwner.Store("store-alpha"),
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                freight, packaging, "IRR", payable, collectAmount,
                new DateOnly(2026, 11, 4),
                9 * TimeSpan.TicksPerHour,
                17 * TimeSpan.TicksPerHour,
                "Asia/Tehran", 1, 3, quote);

            var lineIdPairs = orderLines.Select(l => (l.LineId, sellerPartyId)).ToArray();
            var cartToOrder = cartLineIds.Zip(orderLines, (c, l) => (c, l.LineId))
                .ToDictionary(x => x.c, x => x.LineId);
            var materialized = OrderShippingGroupMaterializer.Materialize(
                [new OrderShippingGroupPlan(
                    groupId, sellerPartyId, "tipax", null, null, "Tipax", null,
                    payable, "IRR", new DateOnly(2026, 11, 4), new DateOnly(2026, 11, 4), "09:00-17:00", 1,
                    cartLineIds, evidence)],
                lineIdPairs,
                new Dictionary<Guid, Guid> { [sellerPartyId] = sellerOrderId },
                cartToOrder,
                checkoutId,
                now);
            orderDb.AddRange(materialized);
            await orderDb.SaveChangesAsync();

            var fulfillmentDb = CreateFulfillmentDb(target.ConnectionString);
            var handoff = new OrderFulfillmentHandoffSnapshot(
                sellerOrderId, checkoutId, sellerPartyId, Guid.NewGuid(), true,
                "گیرنده", "09121111111", "تهران", "تهران", "نشانی", "1111111111",
                "tipax", "Tipax",
                orderLines.Select(l => new OrderFulfillmentLineSnapshot(l.LineId, l.Quantity, null)).ToArray(),
                [new OrderFulfillmentShippingGroupSnapshot(
                    groupId, sellerOrderId, sellerPartyId, "tipax", null, null, "Tipax", null,
                    new DateOnly(2026, 11, 4), null, null, 1, orderLines.Select(l => l.LineId).ToArray())]);
            var fulfillmentDirectory = new FulfillmentDirectory(
                fulfillmentDb,
                new OpenFulfillmentUseCaseGuard(),
                new FixedOrderReader(handoff),
                new NoopInventory(),
                new FulfillmentInstrumentation(),
                new SystemUtcClock(),
                new UuidV7IdGenerator());

            Guid fulfillmentId = Guid.Empty;
            if (!cancelSeller)
            {
                await fulfillmentDirectory.CreateFromPaidSellerOrdersAsync(
                    Guid.NewGuid(), Guid.NewGuid(), [sellerOrderId], CancellationToken.None);
                var unit = await fulfillmentDirectory.GetBySellerOrderAsync(sellerOrderId, CancellationToken.None)
                           ?? throw new InvalidOperationException("fulfillment missing");
                await fulfillmentDirectory.MarkProcessingAsync(unit.FulfillmentId, actor, CancellationToken.None);
                await fulfillmentDirectory.MarkPackedAsync(unit.FulfillmentId, actor, CancellationToken.None);
                fulfillmentId = unit.FulfillmentId;
            }

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
                recorder, directory, provider, accounts, checkoutId, groupId,
                orderLines[0].LineId,
                orderLines.Count > 1 ? orderLines[1].LineId : Guid.Empty,
                sellerOrderId, fulfillmentId, actor, destination);
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

        public static async Task<Fixture> AttachSiblingAsync(
            Fixture seed,
            bool shareProvider = false,
            bool shareRecorder = false)
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
            var provider = shareProvider ? seed.Provider : new RecordingShipmentProvider();
            var adapter = new FulfillmentAdminOperationsAdapter(
                fulfillmentDirectory,
                new FulfillmentShippedQuantityReader(fulfillmentDb),
                new ShippingMethodsOptions());
            var recorder = shareRecorder ? seed.Recorder : new RecordingFulfillmentAdmin(adapter);
            var tipax = new StaticOptions(new TipaxV4Options
            {
                SuccessCallbackUrl = "https://store.test/ok",
                FailureCallbackUrl = "https://store.test/fail",
            });
            var accounts = seed.Accounts;
            var readiness = new ShippingCarrierExecutionReadiness(
                shippingDb, accounts, new StubRegistry(provider), tipax);
            var facts = new OrderCommittedCarrierShipmentFactsReader(orderDb, recorder);
            var directory = new ShippingCarrierShipmentDirectory(
                shippingDb, accounts, new StubRegistry(provider), facts, readiness, recorder, tipax);
            return new Fixture(
                seed.ConnectionString, orderDb, shippingDb, fulfillmentDb, fulfillmentDirectory,
                recorder, directory, provider, accounts, seed.CheckoutId, seed.GroupId, seed.OrderLineId, seed.OrderLineIdB,
                seed.SellerOrderId, seed.FulfillmentId, seed.ActorUserId, seed.Destination);
        }

        public static ShippingCarrierShipmentDirectory RebuildDirectory(
            Fixture seed,
            ShippingDbContext shippingDb,
            RecordingShipmentProvider provider,
            RecordingFulfillmentAdmin recorder,
            StubAccounts accounts)
        {
            var tipax = new StaticOptions(new TipaxV4Options
            {
                SuccessCallbackUrl = "https://store.test/ok",
                FailureCallbackUrl = "https://store.test/fail",
            });
            var readiness = new ShippingCarrierExecutionReadiness(
                shippingDb, accounts, new StubRegistry(provider), tipax);
            var facts = new OrderCommittedCarrierShipmentFactsReader(seed.OrderDb, recorder);
            return new ShippingCarrierShipmentDirectory(
                shippingDb, accounts, new StubRegistry(provider), facts, readiness, recorder, tipax);
        }

        public static ShippingDbContext CreateShippingDb(string connectionString, IInterceptor? interceptor = null)
        {
            var builder = new DbContextOptionsBuilder<ShippingDbContext>();
            ToobaNpgsql.ConfigureModuleContext(builder, connectionString, ShippingDbContext.Schema, typeof(ShippingDbContext));
            if (interceptor is not null)
            {
                builder.AddInterceptors(interceptor);
            }

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

        private static async Task EnsureMigratedAsync(ShippingCarrierDispatchCompletionRelationalTestTarget target)
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
                    Assert.Equal(ShippingCarrierDispatchCompletionRelationalTestTarget.ExpectedDatabase, parts[0]);
                    Assert.Equal(ShippingCarrierDispatchCompletionRelationalTestTarget.ExpectedUser, parts[1]);
                    _liveVerified = true;
                }

                if (!_migrated)
                {
                    await using var order = CreateOrderDb(target.ConnectionString);
                    await using var fulfillment = CreateFulfillmentDb(target.ConnectionString);
                    var catalogBuilder = new DbContextOptionsBuilder<Tooba.Catalog.Infrastructure.Persistence.CatalogDbContext>();
                    ToobaNpgsql.ConfigureModuleContext(
                        catalogBuilder,
                        target.ConnectionString,
                        Tooba.Catalog.Infrastructure.Persistence.CatalogDbContext.Schema,
                        typeof(Tooba.Catalog.Infrastructure.Persistence.CatalogDbContext));
                    await using var catalog = new Tooba.Catalog.Infrastructure.Persistence.CatalogDbContext(catalogBuilder.Options);
                    await order.Database.MigrateAsync();
                    await shipping.GetService<IMigrator>().MigrateAsync();
                    await fulfillment.Database.MigrateAsync();
                    await catalog.GetService<IMigrator>().MigrateAsync();
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

    private sealed class PendingInsertBarrierInterceptor(Barrier barrier, Action onHit) : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context?.ChangeTracker.Entries<ShippingCarrierShipmentOperation>()
                    .Any(e => e.State == EntityState.Added) == true)
            {
                onHit();
                barrier.SignalAndWait(TimeSpan.FromSeconds(15));
            }

            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context?.ChangeTracker.Entries<ShippingCarrierShipmentOperation>()
                    .Any(e => e.State == EntityState.Added) == true)
            {
                onHit();
                barrier.SignalAndWait(TimeSpan.FromSeconds(15));
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}

[CollectionDefinition(ShippingCarrierDispatchCompletionRelationalCollection.Name, DisableParallelization = true)]
public sealed class ShippingCarrierDispatchCompletionRelationalCollection
{
    public const string Name = "TB249R5-ShippingCarrierDispatchCompletion";
}
