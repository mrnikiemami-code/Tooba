using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Contracts.Shipping;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Domain.Carriers;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Carriers;
using Tooba.Shipping.Application.Carriers.Ports;
using Tooba.Shipping.Contracts.Carriers;
using Tooba.Shipping.Infrastructure.Carriers;
using Tooba.Shipping.Infrastructure.Carriers.Tipax;
using Tooba.Shipping.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Shipping.Tests.Carriers;

public sealed class ShippingCarrierExecutionReadinessTests
{
    private static readonly Guid Checkout = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid Group = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid Account = Guid.Parse("33333333-3333-4333-8333-333333333333");
    private static readonly Guid SellerParty = Guid.Parse("44444444-4444-4444-8444-444444444444");
    private static readonly Guid SellerOrder = Guid.Parse("55555555-5555-4555-8555-555555555555");
    private const string Origin = "101";
    private const string Destination = "202";

    [Fact]
    public async Task Payment_locked_blocks_create_on_capability()
    {
        var readiness = CreateReadiness(configured: true, facts: ValidFacts(canExecute: false, reason: "payment_locked"));
        var view = await readiness.BuildCapabilityAsync(Owner(), ValidFacts(canExecute: false, reason: "payment_locked"), CancellationToken.None);
        Assert.False(view.CanCreate);
        Assert.Contains("payment_locked", view.BlockReasonCodes);
    }

    [Fact]
    public async Task Unknown_operation_allows_track_when_provider_supports_it()
    {
        var db = NewDb();
        var op = ShippingCarrierShipmentOperation.Begin(
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
        op.MarkUnknown(DateTimeOffset.UtcNow, "shipping.carrier.shipment.unknown");
        db.CarrierShipmentOperations.Add(op);
        await db.SaveChangesAsync();

        var readiness = CreateReadiness(db, configured: true, facts: ValidFacts());
        var view = await readiness.BuildCapabilityAsync(Owner(), ValidFacts(), CancellationToken.None);
        Assert.False(view.CanCreate);
        Assert.True(view.CanTrack);
    }

    [Fact]
    public async Task Config_version_mismatch_surfaces_reconciliation_code()
    {
        var readiness = CreateReadiness(configured: true, facts: ValidFacts(configurationVersion: 2));
        var view = await readiness.BuildCapabilityAsync(Owner(), ValidFacts(configurationVersion: 2), CancellationToken.None);
        Assert.Contains("config_reconciliation_required", view.BlockReasonCodes);
        Assert.False(view.CanCreate);
    }

    [Fact]
    public async Task Stored_package_completion_clears_package_completion_required_and_allows_create_when_sender_complete()
    {
        var db = NewDb();
        db.CarrierShipmentPackageCompletions.Add(
            ShippingCarrierShipmentPackageCompletion.Create(
                Guid.NewGuid(),
                ShippingDraftOwnerKind.Store,
                "store-alpha",
                null,
                Checkout,
                Group,
                1800m,
                100m,
                22m,
                18m,
                12m,
                DateTimeOffset.UtcNow));
        db.CarrierShipmentSenderCompletions.Add(
            ShippingCarrierShipmentSenderCompletion.Create(
                Guid.NewGuid(),
                ShippingDraftOwnerKind.Store,
                "store-alpha",
                null,
                Checkout,
                Group,
                "فرستنده",
                "09120000000",
                "مبدأ",
                "1111111111",
                Origin,
                DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var facts = IncompleteProvenanceFacts();
        var readiness = CreateReadiness(db, configured: true, facts: facts);
        var view = await readiness.BuildCapabilityAsync(Owner(), facts, CancellationToken.None);
        Assert.True(view.CanCreate);
        Assert.DoesNotContain("package_completion_required", view.BlockReasonCodes);
    }

    [Fact]
    public async Task Existing_create_operation_disallows_second_create()
    {
        var db = NewDb();
        var pending = ShippingCarrierShipmentOperation.Begin(
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
        db.CarrierShipmentOperations.Add(pending);
        await db.SaveChangesAsync();

        var readiness = CreateReadiness(db, configured: true, facts: ValidFacts());
        var view = await readiness.BuildCapabilityAsync(Owner(), ValidFacts(), CancellationToken.None);
        Assert.False(view.CanCreate);
    }

    private static ShippingCarrierExecutionReadiness CreateReadiness(bool configured, OrderCommittedCarrierShipmentFacts facts)
        => CreateReadiness(NewDb(), configured, facts);

    private static ShippingDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<ShippingDbContext>()
            .UseInMemoryDatabase($"tb249r1-ready-{Guid.NewGuid():N}")
            .Options;
        return new ShippingDbContext(options);
    }

    private static ShippingCarrierExecutionReadiness CreateReadiness(
        ShippingDbContext db,
        bool configured,
        OrderCommittedCarrierShipmentFacts facts)
    {
        var provider = new RecordingShipmentProvider();
        var accounts = new ShippingCarrierShipmentDirectoryTestsStubAccounts(configured);
        return new ShippingCarrierExecutionReadiness(
            db,
            accounts,
            new ShippingCarrierShipmentDirectoryTestsStubRegistry(provider),
            new StaticTipaxOptions(new TipaxV4Options
            {
                SuccessCallbackUrl = "https://store.test/ok",
                FailureCallbackUrl = "https://store.test/fail",
            }));
    }

    private static ShippingAuthoringOwnerScope Owner() => ShippingAuthoringOwnerScope.ForStore("store-alpha");

    private static OrderCommittedCarrierShipmentFacts ValidFacts(
        bool canExecute = true,
        string? reason = null,
        int configurationVersion = 1) =>
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
            Array.Empty<Guid>());

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
            true,
            "package_completion_required",
            Array.Empty<Guid>());

    private sealed class RecordingShipmentProvider : ICarrierShipmentProvider
    {
        public string ProviderCode => "tipax";
        public CarrierShipmentCapabilities Capabilities { get; } = new("tipax", true, true, true, false);
        public Task<CarrierShipmentResult> CreateAsync(CarrierShipmentCreateRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new CarrierShipmentResult(CarrierShipmentOutcome.Succeeded, TrackingCode: "TRK-1"));
        public Task<CarrierShipmentResult> TrackAsync(CarrierShipmentTrackRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new CarrierShipmentResult(CarrierShipmentOutcome.Succeeded, TrackingCode: "TRK-1"));
        public Task<CarrierShipmentResult> CancelAsync(CarrierShipmentCancelRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new CarrierShipmentResult(CarrierShipmentOutcome.Succeeded));
    }

    private sealed class StaticTipaxOptions(TipaxV4Options value) : IOptionsMonitor<TipaxV4Options>
    {
        public TipaxV4Options CurrentValue => value;
        public TipaxV4Options Get(string? name) => value;
        public IDisposable? OnChange(Action<TipaxV4Options, string?> listener) => null;
    }
}

/// <summary>Exposed for readiness tests (same behavior as directory stub).</summary>
public sealed class ShippingCarrierShipmentDirectoryTestsStubAccounts(bool configured) : IShippingCarrierAccountDirectory
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
            true,
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
                "101",
                "مبدأ",
                null,
                null,
                20m,
                15m,
                10m,
                1,
                DateTimeOffset.UtcNow)));
    }

    private static readonly Guid Account = Guid.Parse("33333333-3333-4333-8333-333333333333");
}

public sealed class ShippingCarrierShipmentDirectoryTestsStubRegistry(ICarrierShipmentProvider provider) : ICarrierShipmentProviderRegistry
{
    public bool TryResolve(string providerCode, out ICarrierShipmentProvider? resolved)
    {
        resolved = provider;
        return string.Equals(providerCode, provider.ProviderCode, StringComparison.OrdinalIgnoreCase);
    }
}
