using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Operations;
using Tooba.Order.Contracts.Shipping;
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

namespace Tooba.Shipping.Tests.Carriers;

public sealed class ShippingCarrierShipmentDirectoryTests
{
    private static readonly Guid Checkout = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid Group = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid Account = Guid.Parse("33333333-3333-4333-8333-333333333333");
    private static readonly Guid SellerParty = Guid.Parse("44444444-4444-4444-8444-444444444444");
    private static readonly Guid SellerOrder = Guid.Parse("55555555-5555-4555-8555-555555555555");
    private static readonly Guid Fulfillment = Guid.Parse("66666666-6666-4666-8666-666666666666");
    private static readonly Guid LineA = Guid.Parse("77777777-7777-4777-8777-777777777777");
    private static readonly Guid Actor = Guid.Parse("88888888-8888-4888-8888-888888888888");
    private const string Origin = "101";
    private const string Destination = "202";

    [Fact]
    public async Task Unconfigured_account_makes_no_provider_call()
    {
        var provider = new RecordingShipmentProvider();
        var directory = Create(provider, configured: false, facts: ValidFacts());

        var result = await directory.CreateAsync(Owner(), Actor, ValidExecuteRequest(), CancellationToken.None);

        Assert.Equal(ShippingErrorCodes.CarrierUnconfigured, result.FirstError.Code);
        Assert.Equal(0, provider.Creates);
    }

    [Fact]
    public async Task Missing_committed_order_is_not_eligible_without_network()
    {
        var provider = new RecordingShipmentProvider();
        var directory = Create(provider, configured: true, facts: null);

        var result = await directory.CreateAsync(Owner(), Actor, ValidExecuteRequest(), CancellationToken.None);

        Assert.Equal(ShippingErrorCodes.CarrierShipmentNotEligible, result.FirstError.Code);
        Assert.Equal(0, provider.Creates);
    }

    [Fact]
    public async Task Ineligible_facts_refuse_create_without_provider_call()
    {
        var provider = new RecordingShipmentProvider();
        var directory = Create(provider, configured: true, facts: ValidFacts(canExecute: false, reason: "payment_locked"));

        var result = await directory.CreateAsync(Owner(), Actor, ValidExecuteRequest(), CancellationToken.None);

        Assert.Equal(ShippingErrorCodes.CarrierShipmentNotEligible, result.FirstError.Code);
        Assert.Equal(0, provider.Creates);
    }

    [Fact]
    public async Task Owner_scope_mismatch_is_not_eligible()
    {
        var provider = new RecordingShipmentProvider();
        var directory = Create(provider, configured: true, facts: ValidFacts());

        var result = await directory.CreateAsync(ShippingAuthoringOwnerScope.ForStore("other-store"), Actor, ValidExecuteRequest(), CancellationToken.None);

        Assert.Equal(ShippingErrorCodes.CarrierShipmentNotEligible, result.FirstError.Code);
        Assert.Equal(0, provider.Creates);
    }

    [Fact]
    public async Task Configuration_version_mismatch_is_not_eligible()
    {
        var provider = new RecordingShipmentProvider();
        var directory = Create(provider, configured: true, facts: ValidFacts(configurationVersion: 2));

        var result = await directory.CreateAsync(Owner(), Actor, ValidExecuteRequest(), CancellationToken.None);

        Assert.Equal(ShippingErrorCodes.CarrierShipmentNotEligible, result.FirstError.Code);
        Assert.Equal(0, provider.Creates);
    }

    [Fact]
    public async Task Provider_crash_after_external_claim_marks_unknown_and_resume_does_not_resend()
    {
        var provider = new RecordingShipmentProvider { ThrowOnCreate = true };
        var directory = Create(provider, configured: true, facts: ValidFacts());
        await PrepareSenderAsync(directory);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            directory.CreateAsync(Owner(), Actor, ValidExecuteRequest(), CancellationToken.None));
        Assert.Equal(1, provider.Creates);

        provider.ThrowOnCreate = false;
        var resume = await directory.CreateAsync(Owner(), Actor, ValidExecuteRequest(), CancellationToken.None);
        Assert.True(resume.IsSuccess);
        Assert.Equal(ShippingCarrierShipmentStateView.Unknown, resume.Value.State);
        Assert.Equal(1, provider.Creates);
    }

    [Fact]
    public async Task Same_occurrence_replays_succeeded_operation_without_second_provider_call()
    {
        var provider = new RecordingShipmentProvider();
        var directory = Create(provider, configured: true, facts: ValidFacts());
        await PrepareSenderAsync(directory);

        var first = await directory.CreateAsync(Owner(), Actor, ValidExecuteRequest(), CancellationToken.None);
        Assert.True(first.IsSuccess);
        Assert.NotNull(first.Value.PackageDispatchId);
        Assert.Equal(1, provider.Creates);

        var second = await directory.CreateAsync(
            Owner(),
            Actor,
            new ShippingCarrierShipmentExecuteRequest(Checkout, Group, PackageDispatchId: first.Value.PackageDispatchId),
            CancellationToken.None);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.OperationId, second.Value.OperationId);
        Assert.Equal(first.Value.PackageDispatchId, second.Value.PackageDispatchId);
        Assert.Equal(1, provider.Creates);
    }

    [Fact]
    [Trait("Category", "TB249R7")]
    public async Task Client_prebound_occurrence_on_first_create_is_bound_and_replayed()
    {
        var provider = new RecordingShipmentProvider();
        var directory = Create(provider, configured: true, facts: ValidFacts());
        await PrepareSenderAsync(directory);
        var prebound = UuidV7.New();

        var first = await directory.CreateAsync(
            Owner(),
            Actor,
            new ShippingCarrierShipmentExecuteRequest(Checkout, Group, PackageDispatchId: prebound),
            CancellationToken.None);
        Assert.True(first.IsSuccess, first.IsFailure ? first.FirstError.Code : null);
        Assert.Equal(prebound, first.Value.PackageDispatchId);
        Assert.Equal(1, provider.Creates);

        var replay = await directory.CreateAsync(
            Owner(),
            Actor,
            new ShippingCarrierShipmentExecuteRequest(Checkout, Group, PackageDispatchId: prebound),
            CancellationToken.None);
        Assert.True(replay.IsSuccess);
        Assert.Equal(first.Value.OperationId, replay.Value.OperationId);
        Assert.Equal(prebound, replay.Value.PackageDispatchId);
        Assert.Equal(1, provider.Creates);
    }

    [Fact]
    public async Task Identical_payload_new_occurrence_allocates_second_parcel()
    {
        var provider = new RecordingShipmentProvider();
        var directory = Create(provider, configured: true, facts: ValidFacts(lineQuantity: 2m));
        await PrepareSenderAsync(directory);
        var pkg = new ShippingCarrierShipmentDispatchPackageInput(500m, 10m, 10m, 10m);

        var first = await directory.CreateAsync(
            Owner(),
            Actor,
            new ShippingCarrierShipmentExecuteRequest(
                Checkout,
                Group,
                new[] { new ShippingCarrierShipmentLineQuantity(LineA, 1m) },
                pkg),
            CancellationToken.None);
        Assert.True(first.IsSuccess, first.IsFailure ? first.FirstError.Code : null);
        Assert.NotNull(first.Value.PackageDispatchId);

        var second = await directory.CreateAsync(
            Owner(),
            Actor,
            new ShippingCarrierShipmentExecuteRequest(
                Checkout,
                Group,
                new[] { new ShippingCarrierShipmentLineQuantity(LineA, 1m) },
                pkg),
            CancellationToken.None);
        Assert.True(second.IsSuccess, second.IsFailure ? second.FirstError.Code : null);
        Assert.NotEqual(first.Value.OperationId, second.Value.OperationId);
        Assert.NotEqual(first.Value.PackageDispatchId, second.Value.PackageDispatchId);
        Assert.Equal(2, provider.Creates);

        var replay = await directory.CreateAsync(
            Owner(),
            Actor,
            new ShippingCarrierShipmentExecuteRequest(
                Checkout,
                Group,
                new[] { new ShippingCarrierShipmentLineQuantity(LineA, 1m) },
                pkg,
                first.Value.PackageDispatchId),
            CancellationToken.None);
        Assert.True(replay.IsSuccess);
        Assert.Equal(first.Value.OperationId, replay.Value.OperationId);
        Assert.Equal(2, provider.Creates);
    }

    [Fact]
    public async Task Same_occurrence_conflicting_payload_is_typed_conflict()
    {
        var provider = new RecordingShipmentProvider();
        var directory = Create(provider, configured: true, facts: ValidFacts(lineQuantity: 2m));
        await PrepareSenderAsync(directory);

        var first = await directory.CreateAsync(
            Owner(),
            Actor,
            new ShippingCarrierShipmentExecuteRequest(
                Checkout,
                Group,
                new[] { new ShippingCarrierShipmentLineQuantity(LineA, 1m) },
                new ShippingCarrierShipmentDispatchPackageInput(500m, 10m, 10m, 10m)),
            CancellationToken.None);
        Assert.True(first.IsSuccess);

        var conflict = await directory.CreateAsync(
            Owner(),
            Actor,
            new ShippingCarrierShipmentExecuteRequest(
                Checkout,
                Group,
                new[] { new ShippingCarrierShipmentLineQuantity(LineA, 1m) },
                new ShippingCarrierShipmentDispatchPackageInput(900m, 10m, 10m, 10m),
                first.Value.PackageDispatchId),
            CancellationToken.None);
        Assert.Equal(ShippingErrorCodes.CarrierShipmentIntentConflict, conflict.FirstError.Code);
        Assert.Equal(1, provider.Creates);
    }

    [Fact]
    public async Task Partial_dispatch_without_own_package_facts_is_refused()
    {
        var provider = new RecordingShipmentProvider();
        var facts = ValidFacts(lineQuantity: 4m, unitPrice: 100m);
        var directory = Create(provider, configured: true, facts: facts);
        await PrepareSenderAsync(directory);

        var result = await directory.CreateAsync(
            Owner(),
            Actor,
            new ShippingCarrierShipmentExecuteRequest(
                Checkout,
                Group,
                new[] { new ShippingCarrierShipmentLineQuantity(LineA, 1m) }),
            CancellationToken.None);

        Assert.Equal(ShippingErrorCodes.CarrierShipmentNotEligible, result.FirstError.Code);
        Assert.Equal(0, provider.Creates);
    }

    [Fact]
    public async Task Mixed_line_values_allocate_declared_from_unit_prices_not_quantity_share()
    {
        var provider = new RecordingShipmentProvider();
        var lineB = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1");
        var facts = new OrderCommittedCarrierShipmentFacts(
            Checkout,
            Group,
            SellerParty,
            SellerOrder,
            "tipax:standard",
            "Tipax",
            "CarrierApi",
            0,
            "store-alpha",
            null,
            "گیرنده",
            "09121111111",
            "نشانی",
            "1111111111",
            "تهران",
            "تهران",
            "IRR",
            137_500m,
            1_000m,
            true,
            Account,
            1,
            "tipax",
            "7",
            "input-1",
            Origin,
            Destination,
            1500m,
            0m,
            20m,
            15m,
            10m,
            "Paid",
            false,
            false,
            true,
            null,
            new[] { LineA, lineB },
            new[]
            {
                new OrderCommittedCarrierExecutableLine(LineA, 1m, 100m, 1m),
                new OrderCommittedCarrierExecutableLine(lineB, 1m, 900m, 1m),
            },
            Fulfillment,
            2m,
            new[]
            {
                new OrderCommittedCarrierLineValueFact(LineA, 1m, 100m),
                new OrderCommittedCarrierLineValueFact(lineB, 1m, 900m),
            });
        var directory = Create(provider, configured: true, facts: facts);
        await PrepareSenderAsync(directory);

        var cheap = await directory.CreateAsync(
            Owner(),
            Actor,
            new ShippingCarrierShipmentExecuteRequest(
                Checkout,
                Group,
                new[] { new ShippingCarrierShipmentLineQuantity(LineA, 1m) },
                new ShippingCarrierShipmentDispatchPackageInput(500m, 10m, 10m, 10m)),
            CancellationToken.None);
        Assert.True(cheap.IsSuccess, cheap.IsFailure ? cheap.FirstError.Code : null);
        Assert.Equal(100m, provider.LastPackage!.DeclaredValue);
        Assert.Equal(100m, provider.LastPackage.CollectAmount);
    }

    [Fact]
    public async Task Foreign_owner_cannot_read_the_operation()
    {
        var provider = new RecordingShipmentProvider();
        var directory = Create(provider, configured: true, facts: ValidFacts());
        await PrepareSenderAsync(directory);
        var created = await directory.CreateAsync(Owner(), Actor, ValidExecuteRequest(), CancellationToken.None);

        var foreign = await directory.GetAsync(
            ShippingAuthoringOwnerScope.ForStore("other-store"),
            created.Value.OperationId,
            CancellationToken.None);

        Assert.Equal(ShippingErrorCodes.CarrierShipmentNotFound, foreign.FirstError.Code);
    }

    [Fact]
    public async Task Ambiguous_create_stays_unknown_and_is_not_retried()
    {
        var provider = new RecordingShipmentProvider { Outcome = CarrierShipmentOutcome.Unknown };
        var directory = Create(provider, configured: true, facts: ValidFacts());

        await PrepareSenderAsync(directory);
        var first = await directory.CreateAsync(Owner(), Actor, ValidExecuteRequest(), CancellationToken.None);
        // Same payload without locator converges onto open Unknown (no-resend); occurrence also works.
        var second = await directory.CreateAsync(Owner(), Actor, ValidExecuteRequest(), CancellationToken.None);

        Assert.Equal(ShippingCarrierShipmentStateView.Unknown, first.Value.State);
        Assert.Equal(first.Value.OperationId, second.Value.OperationId);
        Assert.Equal(1, provider.Creates);
    }

    [Fact]
    public async Task Label_is_explicitly_unsupported()
    {
        var directory = Create(new RecordingShipmentProvider(), configured: true, facts: ValidFacts());
        await PrepareSenderAsync(directory);
        var created = await directory.CreateAsync(Owner(), Actor, ValidExecuteRequest(), CancellationToken.None);

        var label = await directory.LabelAsync(Owner(), created.Value.OperationId, CancellationToken.None);

        Assert.Equal(ShippingErrorCodes.CarrierShipmentUnsupported, label.FirstError.Code);
    }

    [Fact]
    public async Task Package_completion_then_sender_unlocks_create_using_saved_package()
    {
        var provider = new RecordingShipmentProvider();
        var facts = IncompleteProvenanceFacts();
        var directory = Create(provider, configured: true, facts: facts);

        var before = await directory.CreateAsync(Owner(), Actor, ValidExecuteRequest(), CancellationToken.None);
        Assert.Equal(ShippingErrorCodes.CarrierShipmentNotEligible, before.FirstError.Code);
        Assert.Equal(0, provider.Creates);

        var savedPackage = await directory.SavePackageCompletionAsync(
            Owner(),
            new ShippingCarrierShipmentPackageCompletionRequest(Checkout, Group, 1800m, 100m, 22m, 18m, 12m),
            CancellationToken.None);
        Assert.True(savedPackage.IsSuccess);
        Assert.False(savedPackage.Value.CanCreate);
        Assert.DoesNotContain("package_completion_required", savedPackage.Value.BlockReasonCodes);

        await PrepareSenderAsync(directory);
        var created = await directory.CreateAsync(Owner(), Actor, ValidExecuteRequest(), CancellationToken.None);
        Assert.True(created.IsSuccess);
        Assert.Equal(1, provider.Creates);
        Assert.Equal("TRK-1", created.Value.ProviderTrackingCode);
    }

    [Fact]
    public async Task Inactive_account_track_is_refused_without_provider_call()
    {
        var provider = new RecordingShipmentProvider();
        var (directory, db) = CreateWithDb(provider, configured: true, facts: ValidFacts(), active: false);
        var operation = ShippingCarrierShipmentOperation.Begin(
            Guid.NewGuid(),
            $"create:{Checkout:N}:{Group:N}",
            ShippingCarrierShipmentKind.Create,
            ShippingDraftOwnerKind.Store,
            "store-alpha",
            null,
            Checkout,
            Group,
            "tipax",
            Account,
            1,
            DateTimeOffset.UtcNow);
        operation.Succeed(DateTimeOffset.UtcNow, "TRK-1", "ord-1", "ok");
        db.CarrierShipmentOperations.Add(operation);
        await db.SaveChangesAsync();

        var track = await directory.TrackAsync(Owner(), operation.OperationId, CancellationToken.None);

        Assert.Equal(ShippingErrorCodes.CarrierUnconfigured, track.FirstError.Code);
        Assert.Equal(0, provider.Tracks);
    }

    private static ShippingCarrierShipmentDirectory Create(
        RecordingShipmentProvider provider,
        bool configured,
        OrderCommittedCarrierShipmentFacts? facts,
        bool active = true)
        => CreateWithDb(provider, configured, facts, active).Directory;

    private static (ShippingCarrierShipmentDirectory Directory, ShippingDbContext Db) CreateWithDb(
        RecordingShipmentProvider provider,
        bool configured,
        OrderCommittedCarrierShipmentFacts? facts,
        bool active = true)
    {
        var options = new DbContextOptionsBuilder<ShippingDbContext>()
            .UseInMemoryDatabase($"tb249-ship-{Guid.NewGuid():N}")
            .Options;
        var db = new ShippingDbContext(options);
        var tipax = new StaticOptions(new TipaxV4Options
        {
            SuccessCallbackUrl = "https://store.test/ok",
            FailureCallbackUrl = "https://store.test/fail",
        });
        var accounts = new StubAccounts(configured, active);
        var readiness = new ShippingCarrierExecutionReadiness(
            db,
            accounts,
            new StubRegistry(provider),
            tipax);
        var directory = new ShippingCarrierShipmentDirectory(
            db,
            accounts,
            new StubRegistry(provider),
            new StubFacts(facts),
            readiness,
            new StubFulfillment(),
            tipax);
        return (directory, db);
    }

    private static async Task PrepareSenderAsync(ShippingCarrierShipmentDirectory directory)
    {
        var saved = await directory.SaveSenderCompletionAsync(
            Owner(),
            new ShippingCarrierShipmentSenderCompletionRequest(
                Checkout,
                Group,
                "فرستنده",
                "09120000000",
                "مبدأ",
                "1111111111"),
            CancellationToken.None);
        Assert.True(saved.IsSuccess);
    }

    private static ShippingAuthoringOwnerScope Owner() => ShippingAuthoringOwnerScope.ForStore("store-alpha");

    private static OrderCommittedCarrierShipmentFacts ValidFacts(
        bool canExecute = true,
        string? reason = null,
        int configurationVersion = 1,
        decimal lineQuantity = 2m,
        decimal unitPrice = 1_000_000m) =>
        new(
            Checkout,
            Group,
            SellerParty,
            SellerOrder,
            "tipax:standard",
            "Tipax",
            "CarrierApi",
            0,
            "store-alpha",
            null,
            "گیرنده",
            "09121111111",
            "نشانی",
            "1111111111",
            "تهران",
            "تهران",
            "IRR",
            137_500m,
            0m,
            true,
            Account,
            configurationVersion,
            "tipax",
            "7",
            "input-1",
            Origin,
            Destination,
            1500m,
            2_000_000m,
            20m,
            15m,
            10m,
            canExecute ? "Paid" : "PendingPayment",
            false,
            !canExecute,
            canExecute,
            reason,
            new[] { LineA },
            canExecute
                ? new[] { new OrderCommittedCarrierExecutableLine(LineA, lineQuantity, unitPrice, lineQuantity) }
                : Array.Empty<OrderCommittedCarrierExecutableLine>(),
            Fulfillment,
            lineQuantity,
            canExecute
                ? new[] { new OrderCommittedCarrierLineValueFact(LineA, lineQuantity, unitPrice) }
                : Array.Empty<OrderCommittedCarrierLineValueFact>());

    private static ShippingCarrierShipmentExecuteRequest ValidExecuteRequest()
        => new(Checkout, Group);

    private static OrderCommittedCarrierShipmentFacts IncompleteProvenanceFacts() =>
        new(
            Checkout,
            Group,
            SellerParty,
            SellerOrder,
            "tipax:standard",
            "Tipax",
            "CarrierApi",
            0,
            "store-alpha",
            null,
            "گیرنده",
            "09121111111",
            "نشانی",
            "1111111111",
            "تهران",
            "تهران",
            "IRR",
            137_500m,
            0m,
            true,
            Account,
            1,
            "tipax",
            "7",
            "input-1",
            Origin,
            Destination,
            null,
            2_000_000m,
            null,
            null,
            null,
            "Paid",
            false,
            false,
            false,
            "package_completion_required",
            new[] { LineA },
            new[] { new OrderCommittedCarrierExecutableLine(LineA, 2m) },
            Fulfillment,
            2m);

    private sealed class RecordingShipmentProvider : ICarrierShipmentProvider
    {
        public string ProviderCode => "tipax";
        public int Creates { get; private set; }
        public int Tracks { get; private set; }
        public bool ThrowOnCreate { get; set; }
        public CarrierShipmentPackage? LastPackage { get; private set; }
        public CarrierShipmentOutcome Outcome { get; set; } = CarrierShipmentOutcome.Succeeded;
        public CarrierShipmentCapabilities Capabilities { get; } = new("tipax", true, true, true, false);

        public Task<CarrierShipmentResult> CreateAsync(CarrierShipmentCreateRequest request, CancellationToken cancellationToken)
        {
            Creates++;
            LastPackage = request.Package;
            Assert.Equal(Destination, request.Recipient.LocationCode);
            Assert.True(request.Package.WeightGrams > 0m);
            if (ThrowOnCreate)
            {
                throw new InvalidOperationException("provider-crash");
            }

            return Task.FromResult(Outcome == CarrierShipmentOutcome.Succeeded
                ? new CarrierShipmentResult(Outcome, TrackingCode: "TRK-1", ProviderOrderId: "ord-1")
                : CarrierShipmentResult.Failure(Outcome, ShippingErrorCodes.CarrierShipmentUnknown));
        }

        public Task<CarrierShipmentResult> TrackAsync(CarrierShipmentTrackRequest request, CancellationToken cancellationToken)
        {
            Tracks++;
            return Task.FromResult(new CarrierShipmentResult(CarrierShipmentOutcome.Succeeded, TrackingCode: "TRK-1", ProviderStatus: "x"));
        }

        public Task<CarrierShipmentResult> CancelAsync(CarrierShipmentCancelRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new CarrierShipmentResult(CarrierShipmentOutcome.Succeeded, ProviderStatus: "cancelled"));
    }

    private sealed class StubRegistry(ICarrierShipmentProvider provider) : ICarrierShipmentProviderRegistry
    {
        public bool TryResolve(string providerCode, out ICarrierShipmentProvider? resolved)
        {
            resolved = provider;
            return string.Equals(providerCode, provider.ProviderCode, StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class StubFacts(OrderCommittedCarrierShipmentFacts? facts) : IOrderCommittedCarrierShipmentFactsReader
    {
        public Task<OrderCommittedCarrierShipmentFacts?> GetAsync(
            Guid checkoutId,
            Guid shippingGroupId,
            CancellationToken cancellationToken)
            => Task.FromResult(facts);

        public Task<IReadOnlyList<OrderCommittedCarrierShipmentFacts>> ListByCheckoutAsync(
            Guid checkoutId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<OrderCommittedCarrierShipmentFacts>>(
                facts is null ? Array.Empty<OrderCommittedCarrierShipmentFacts>() : new[] { facts });
    }

    private sealed class StubFulfillment : IFulfillmentAdminOperations
    {
        public Task<FulfillmentSnapshot?> GetAsync(Guid fulfillmentId, CancellationToken cancellationToken)
            => Task.FromResult<FulfillmentSnapshot?>(null);
        public Task<IReadOnlyList<FulfillmentSnapshot>> ListForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<FulfillmentSnapshot>>(Array.Empty<FulfillmentSnapshot>());
        public Task<IReadOnlyList<ConsolidatedPackageSnapshot>> GetPackagesForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
        public Task<IReadOnlyList<ActivePackageMembershipSnapshot>> GetActiveMembershipByShipmentIdsAsync(IReadOnlyList<Guid> shipmentIds, CancellationToken cancellationToken)
            => throw new NotSupportedException();
        public IReadOnlyList<ShippingMethodOption> ListEnabledShippingMethods() => Array.Empty<ShippingMethodOption>();
        public string ResolveShippingMethodLabel(string? code, string? fallbackDisplayName) => fallbackDisplayName ?? code ?? "";
        public Task<FulfillmentSnapshot> MarkProcessingAsync(Guid fulfillmentId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> ProcessSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> UnprocessSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> PackSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> UnpackSelectionsAsync(Guid fulfillmentId, Guid actorUserId, IReadOnlyList<FulfillmentSelectionCommand> selections, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentShipmentCreateResult> CreateShipmentAsync(
            Guid fulfillmentId,
            Guid actorUserId,
            string carrierDisplayName,
            IReadOnlyList<ShipmentLineCommand> items,
            string? shippingMethodCode,
            string? providerMetadataJson,
            CancellationToken cancellationToken,
            Guid? shippingGroupId = null,
            string? shippingDispatchKey = null)
        {
            Assert.NotEqual(Guid.Empty, actorUserId);
            _ = shippingDispatchKey;
            var shipmentId = Guid.Parse("99999999-9999-4999-8999-999999999999");
            var shipment = new ShipmentSnapshot(
                shipmentId,
                ShipmentOperationStatus.Created,
                carrierDisplayName,
                null,
                null,
                null,
                items.Select(x => new ShipmentLineSnapshot(x.OrderLineId, x.Quantity)).ToArray(),
                DateTimeOffset.UtcNow,
                shippingMethodCode ?? "",
                carrierDisplayName,
                providerMetadataJson,
                0,
                null,
                shippingGroupId ?? Group);
            var snapshot = new FulfillmentSnapshot(
                Fulfillment,
                SellerOrder,
                Checkout,
                SellerParty,
                FulfillmentOperationStatus.Packed,
                "r",
                "0",
                "p",
                "c",
                "a",
                "1",
                shippingMethodCode ?? "",
                carrierDisplayName,
                items.Select(x => new FulfillmentItemSnapshot(Guid.NewGuid(), x.OrderLineId, x.Quantity, 0, null, x.Quantity, 0)).ToArray(),
                new[] { shipment },
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow);
            return Task.FromResult(new FulfillmentShipmentCreateResult(snapshot, shipmentId));
        }
        public Task<FulfillmentSnapshot> CancelShipmentAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> AssignTrackingAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, string trackingReference, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> AssignAutomaticTrackingAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> CorrectTrackingAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, string trackingReference, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> DispatchShipmentAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FulfillmentSnapshot> DeliverShipmentAsync(Guid fulfillmentId, Guid shipmentId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ConsolidatedPackageSnapshot> CreateConsolidatedPackageAsync(Guid checkoutId, IReadOnlyList<Guid> shipmentIds, string? shippingMethodCode, string? trackingReference, string? note, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ConsolidatedPackageSnapshot> CancelConsolidatedPackageAsync(Guid consolidatedPackageId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ConsolidatedPackageSnapshot> AssignConsolidatedPackageTrackingAsync(Guid consolidatedPackageId, string trackingReference, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ConsolidatedPackageSnapshot> DispatchConsolidatedPackageAsync(Guid consolidatedPackageId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ConsolidatedPackageSnapshot> DeliverConsolidatedPackageAsync(Guid consolidatedPackageId, Guid actorUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AbortForCheckoutCancelAsync(Guid checkoutId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ReactivateAfterOrderRestoreAsync(Guid checkoutId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task EnsureCreatedForPaidCheckoutAsync(Guid checkoutId, IReadOnlyList<Guid> sellerOrderIds, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task VoidUnstartedForCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RebindActiveReservationsFromOrderAsync(Guid checkoutId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyDictionary<Guid, decimal>> GetShippedByOrderLineIdsForCheckoutsAsync(IReadOnlyList<Guid> checkoutIds, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(new Dictionary<Guid, decimal>());
    }

    private sealed class StubAccounts(bool configured, bool active = true) : IShippingCarrierAccountDirectory
    {
        public Task<Result<IReadOnlyList<ShippingCarrierAccountView>>> ListAccountsAsync(ShippingAuthoringOwnerScope owner, CancellationToken cancellationToken)
            => throw new NotSupportedException();
        public Task<Result<ShippingCarrierAccountView>> GetAccountAsync(ShippingAuthoringOwnerScope owner, Guid accountId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
        public Task<Result<ShippingCarrierAccountView>> UpsertAccountAsync(ShippingAuthoringOwnerScope owner, Guid? accountId, ShippingCarrierAccountUpsertRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();
        public Task<Result<ShippingCarrierConnectionTestOutcome>> TestConnectionAsync(ShippingAuthoringOwnerScope owner, Guid accountId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<ShippingCarrierReferenceValueView>>> SyncReferencesAsync(ShippingAuthoringOwnerScope owner, Guid accountId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<ShippingCarrierReferenceValueView>>> ListReferencesAsync(ShippingAuthoringOwnerScope owner, Guid accountId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<ShippingCarrierDestinationMappingView>>> ListDestinationMappingsAsync(ShippingAuthoringOwnerScope owner, Guid accountId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
        public Task<Result<ShippingCarrierDestinationMappingView>> UpsertDestinationMappingAsync(ShippingAuthoringOwnerScope owner, Guid accountId, ShippingCarrierDestinationMappingUpsertRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();
        public Task<Result<ShippingCarrierMethodBindingView?>> GetMethodBindingAsync(ShippingAuthoringOwnerScope owner, Guid draftId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
        public Task<Result<ShippingCarrierMethodBindingView>> ReplaceMethodBindingAsync(ShippingAuthoringOwnerScope owner, Guid draftId, ShippingCarrierMethodBindingReplaceRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();
        public Task<string?> TryResolveDestinationExternalCodeAsync(Guid accountId, Guid geographyCityId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
        public Task<CarrierBindingExecution?> GetMethodBindingForDraftAsync(Guid draftId, ShippingDraftOwnerKind ownerKind, string? ownerTenantId, Guid? ownerSellerPartyId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<ShippingCarrierAccountExecutionContext?> TryResolveExecutionContextAsync(
            ShippingDraftOwnerKind ownerKind,
            string? ownerTenantId,
            Guid? ownerSellerPartyId,
            Guid accountId,
            CancellationToken cancellationToken)
        {
            if (!configured)
            {
                return Task.FromResult<ShippingCarrierAccountExecutionContext?>(null);
            }

            return Task.FromResult<ShippingCarrierAccountExecutionContext?>(new ShippingCarrierAccountExecutionContext(
                Account,
                "tipax",
                "Tipax",
                active,
                1,
                "https://api.test/",
                "https://tokens.test/Token",
                "IRR",
                "user",
                "pass",
                new ShippingCarrierMethodBindingView(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Account,
                    "tipax",
                    "7",
                    "0",
                    "0",
                    "0",
                    "0",
                    "0",
                    "0",
                    Origin,
                    "مبدأ",
                    null,
                    null,
                    20m,
                    15m,
                    10m,
                    1,
                    DateTimeOffset.UtcNow)));
        }
    }

    private sealed class StaticOptions(TipaxV4Options value) : IOptionsMonitor<TipaxV4Options>
    {
        public TipaxV4Options CurrentValue => value;
        public TipaxV4Options Get(string? name) => value;
        public IDisposable? OnChange(Action<TipaxV4Options, string?> listener) => null;
    }
}
