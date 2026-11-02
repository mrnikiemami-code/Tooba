using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Contracts.Shipping;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Carriers;
using Tooba.Shipping.Application.Carriers.Ports;
using Tooba.Shipping.Contracts.Carriers;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;
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

    [Fact]
    public async Task Unconfigured_account_makes_no_provider_call()
    {
        var provider = new RecordingShipmentProvider();
        var directory = Create(provider, configured: false, facts: ValidFacts());

        var result = await directory.CreateAsync(Owner(), ValidRequest(), CancellationToken.None);

        Assert.Equal(ShippingErrorCodes.CarrierUnconfigured, result.FirstError.Code);
        Assert.Equal(0, provider.Creates);
    }

    [Fact]
    public async Task Missing_committed_order_is_not_eligible_without_network()
    {
        var provider = new RecordingShipmentProvider();
        var directory = Create(provider, configured: true, facts: null);

        var result = await directory.CreateAsync(Owner(), ValidRequest(), CancellationToken.None);

        Assert.Equal(ShippingErrorCodes.CarrierShipmentNotEligible, result.FirstError.Code);
        Assert.Equal(0, provider.Creates);
    }

    [Fact]
    public async Task Second_create_replays_the_durable_operation_without_another_outbound_call()
    {
        var provider = new RecordingShipmentProvider();
        var directory = Create(provider, configured: true, facts: ValidFacts());

        var first = await directory.CreateAsync(Owner(), ValidRequest(), CancellationToken.None);
        var second = await directory.CreateAsync(Owner(), ValidRequest(), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.OperationId, second.Value.OperationId);
        Assert.Equal(1, provider.Creates);
        Assert.Equal("TRK-1", first.Value.ProviderTrackingCode);
    }

    [Fact]
    public async Task Foreign_owner_cannot_read_the_operation()
    {
        var provider = new RecordingShipmentProvider();
        var directory = Create(provider, configured: true, facts: ValidFacts());
        var created = await directory.CreateAsync(Owner(), ValidRequest(), CancellationToken.None);

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

        var first = await directory.CreateAsync(Owner(), ValidRequest(), CancellationToken.None);
        var second = await directory.CreateAsync(Owner(), ValidRequest(), CancellationToken.None);

        Assert.Equal(ShippingCarrierShipmentStateView.Unknown, first.Value.State);
        Assert.Equal(first.Value.OperationId, second.Value.OperationId);
        Assert.Equal(1, provider.Creates);
    }

    [Fact]
    public async Task Label_is_explicitly_unsupported()
    {
        var directory = Create(new RecordingShipmentProvider(), configured: true, facts: ValidFacts());
        var created = await directory.CreateAsync(Owner(), ValidRequest(), CancellationToken.None);

        var label = await directory.LabelAsync(Owner(), created.Value.OperationId, CancellationToken.None);

        Assert.Equal(ShippingErrorCodes.CarrierShipmentUnsupported, label.FirstError.Code);
    }

    private static ShippingCarrierShipmentDirectory Create(
        RecordingShipmentProvider provider,
        bool configured,
        OrderCommittedCarrierShipmentFacts? facts)
    {
        var options = new DbContextOptionsBuilder<ShippingDbContext>()
            .UseInMemoryDatabase($"tb248r1-ship-{Guid.NewGuid():N}")
            .Options;
        var db = new ShippingDbContext(options);
        var tipax = new StaticOptions(new TipaxV4Options
        {
            SuccessCallbackUrl = "https://store.test/ok",
            FailureCallbackUrl = "https://store.test/fail"
        });
        return new ShippingCarrierShipmentDirectory(
            db,
            new StubAccounts(configured),
            new StubRegistry(provider),
            new StubFacts(facts),
            tipax);
    }

    private static ShippingAuthoringOwnerScope Owner() => ShippingAuthoringOwnerScope.ForStore("store-alpha");

    private static OrderCommittedCarrierShipmentFacts ValidFacts() => new(
        Checkout,
        Group,
        "گیرنده",
        "09121111111",
        "نشانی",
        "1111111111",
        "IRR",
        137_500m,
        0m,
        Account,
        "7");

    private static ShippingCarrierShipmentCreateRequest ValidRequest() => new(
        Checkout,
        Group,
        Account,
        "7",
        new ShippingCarrierShipmentPartyInput("فرستنده", "09120000000", "مبدأ", "101", "1111111111"),
        new ShippingCarrierShipmentPartyInput("گیرنده", "09121111111", "مقصد", "202", "2222222222"),
        new ShippingCarrierShipmentPackageInput(1500m, 2_000_000m, 20m, 15m, 10m, 0m));

    private sealed class RecordingShipmentProvider : ICarrierShipmentProvider
    {
        public string ProviderCode => "tipax";
        public int Creates { get; private set; }
        public CarrierShipmentOutcome Outcome { get; set; } = CarrierShipmentOutcome.Succeeded;
        public CarrierShipmentCapabilities Capabilities { get; } = new("tipax", true, true, true, false);

        public Task<CarrierShipmentResult> CreateAsync(CarrierShipmentCreateRequest request, CancellationToken cancellationToken)
        {
            Creates++;
            return Task.FromResult(Outcome == CarrierShipmentOutcome.Succeeded
                ? new CarrierShipmentResult(Outcome, TrackingCode: "TRK-1", ProviderOrderId: "ord-1")
                : CarrierShipmentResult.Failure(Outcome, ShippingErrorCodes.CarrierShipmentUnknown));
        }

        public Task<CarrierShipmentResult> TrackAsync(CarrierShipmentTrackRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new CarrierShipmentResult(CarrierShipmentOutcome.Succeeded, TrackingCode: "TRK-1", ProviderStatus: "x"));

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
    }

    private sealed class StubAccounts(bool configured) : IShippingCarrierAccountDirectory
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
                null));
        }
    }

    private sealed class StaticOptions(TipaxV4Options value) : IOptionsMonitor<TipaxV4Options>
    {
        public TipaxV4Options CurrentValue => value;
        public TipaxV4Options Get(string? name) => value;
        public IDisposable? OnChange(Action<TipaxV4Options, string?> listener) => null;
    }
}
