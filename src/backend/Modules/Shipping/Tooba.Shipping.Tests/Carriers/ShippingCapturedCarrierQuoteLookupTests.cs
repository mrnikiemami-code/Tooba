using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Results;
using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Carriers;
using Tooba.Shipping.Application.Carriers.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Carriers;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.Shipping.Infrastructure.Rates;
using Xunit;

namespace Tooba.Shipping.Tests.Carriers;

[Collection(nameof(Tooba.Shipping.Tests.Observability.ShippingModuleCallTracingCollection))]
public sealed class ShippingCapturedCarrierQuoteLookupTests
{
    private static readonly Guid Country = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid CityId = Guid.Parse("33333333-3333-4333-8333-333333333333");
    private static readonly Guid AreaId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid TypeId = Guid.Parse("66666666-6666-4666-8666-666666666666");
    private static readonly Guid BindingId = Guid.Parse("77777777-7777-4777-8777-777777777777");
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
    private const string Tenant = "tb248-store";

    [Fact]
    public async Task Carrier_api_quote_maps_provider_money_and_keeps_packaging_separate()
    {
        var snapshot = CaptureCarrierDraft();
        using var context = new ShippingDbContext(new DbContextOptionsBuilder<ShippingDbContext>()
            .UseInMemoryDatabase($"tb248-quote-{Guid.NewGuid():N}")
            .Options);
        context.AuthoringRevisionSnapshots.Add(snapshot);
        context.SaveChanges();

        var provider = new FixedCarrierProvider();
        var lookup = new ShippingCapturedRevisionQuoteLookup(
            new ShippingAuthoringDraftDirectory(context),
            Geography(),
            new BoundCarrierAccounts(),
            new FixedRegistry(provider),
            new StubEdition(ToobaEdition.SingleStore),
            new StubTenant(Tenant),
            new ModuleCallTracer(),
            NullLogger<ShippingCapturedRevisionQuoteLookup>.Instance);

        var result = await lookup.QuoteAsync(
            new ShippingCapturedQuoteLookupRequest(
                ShippingCapturedQuoteOwnerKind.Store,
                Tenant,
                null,
                snapshot.DraftId,
                snapshot.SourceRevision,
                new DestinationIdentifiers(Country, null, CityId),
                GroupWeightGrams: 1500,
                GroupItemCount: 1,
                FinalGroupGoodsAmountAfterDiscountAndTax: 2_000_000m,
                CurrencyCode: "IRT"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ShippingCapturedQuoteVerdict.Quoted, result.Value.Verdict);
        Assert.Equal(132_500m, result.Value.FreightAmount);
        Assert.Equal(5_000m, result.Value.PackagingAmount);
        Assert.Equal(137_500m, result.Value.TotalExpenseAmount);
        Assert.Equal(BindingId, result.Value.RateRowId);
        Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task Unauthorized_carrier_is_typed_and_has_no_money()
    {
        var snapshot = CaptureCarrierDraft();
        using var context = new ShippingDbContext(new DbContextOptionsBuilder<ShippingDbContext>()
            .UseInMemoryDatabase($"tb248-unauth-{Guid.NewGuid():N}")
            .Options);
        context.AuthoringRevisionSnapshots.Add(snapshot);
        context.SaveChanges();

        var lookup = new ShippingCapturedRevisionQuoteLookup(
            new ShippingAuthoringDraftDirectory(context),
            Geography(),
            new BoundCarrierAccounts(),
            new FixedRegistry(new FixedCarrierProvider(CarrierRateOutcome.Unauthorized)),
            new StubEdition(ToobaEdition.SingleStore),
            new StubTenant(Tenant),
            new ModuleCallTracer(),
            NullLogger<ShippingCapturedRevisionQuoteLookup>.Instance);

        var result = await lookup.QuoteAsync(
            new ShippingCapturedQuoteLookupRequest(
                ShippingCapturedQuoteOwnerKind.Store,
                Tenant,
                null,
                snapshot.DraftId,
                snapshot.SourceRevision,
                new DestinationIdentifiers(Country, null, CityId),
                GroupWeightGrams: 1500,
                GroupItemCount: 1,
                FinalGroupGoodsAmountAfterDiscountAndTax: 2_000_000m,
                CurrencyCode: "IRT"),
            CancellationToken.None);

        Assert.Equal(ShippingCapturedQuoteVerdict.CarrierUnauthorized, result.Value.Verdict);
        Assert.Null(result.Value.FreightAmount);
        Assert.False(result.Value.IsQuoted);
    }

    private static ShippingAuthoringRevisionSnapshot CaptureCarrierDraft()
    {
        var draft = ShippingAuthoringDraft.Create(
            ShippingDraftOwner.ForStore(Tenant),
            "post",
            null,
            [new ShippingAuthoringRegionGroupInput(null, [new ShippingDestinationNode(ShippingDestinationNodeKind.City, CityId)])],
            Now);
        draft.ReplaceGroupRateRows(
            draft.RegionGroups.Single().GroupId,
            [new ShippingAuthoringDraft.ShippingAuthoringRateRowInput(null, null, null, null, null, null, null, 10_000m, 1_000m, 1, 2)],
            "IRT",
            Now);
        draft.ApplyMethodSettings(
            draft.PreflightMethodSettings(ShippingFreightPayerMode.Prepaid, ShippingAuthoringCalculationMode.CarrierApi),
            Now);
        draft.ApplyTypeAssignment(draft.PreflightTypeAssignment(TypeId), Now);
        return ShippingAuthoringRevisionSnapshot.Capture(draft, Now.AddHours(1));
    }

    private static ControlledGeography Geography()
        => new ControlledGeography()
            .WithCountry(Country)
            .WithArea(AreaId, Country, null)
            .WithCity(CityId, Country, AreaId);

    private sealed class BoundCarrierAccounts : IShippingCarrierAccountDirectory
    {
        public Task<Result<IReadOnlyList<ShippingCarrierAccountView>>> ListAccountsAsync(
            ShippingAuthoringOwnerScope owner,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Result<ShippingCarrierAccountView>> GetAccountAsync(
            ShippingAuthoringOwnerScope owner,
            Guid accountId,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Result<ShippingCarrierAccountView>> UpsertAccountAsync(
            ShippingAuthoringOwnerScope owner,
            Guid? accountId,
            ShippingCarrierAccountUpsertRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Result<ShippingCarrierConnectionTestOutcome>> TestConnectionAsync(
            ShippingAuthoringOwnerScope owner,
            Guid accountId,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Result<IReadOnlyList<ShippingCarrierReferenceValueView>>> SyncReferencesAsync(
            ShippingAuthoringOwnerScope owner,
            Guid accountId,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Result<IReadOnlyList<ShippingCarrierReferenceValueView>>> ListReferencesAsync(
            ShippingAuthoringOwnerScope owner,
            Guid accountId,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Result<IReadOnlyList<ShippingCarrierDestinationMappingView>>> ListDestinationMappingsAsync(
            ShippingAuthoringOwnerScope owner,
            Guid accountId,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Result<ShippingCarrierDestinationMappingView>> UpsertDestinationMappingAsync(
            ShippingAuthoringOwnerScope owner,
            Guid accountId,
            ShippingCarrierDestinationMappingUpsertRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Result<ShippingCarrierMethodBindingView?>> GetMethodBindingAsync(
            ShippingAuthoringOwnerScope owner,
            Guid draftId,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Result<ShippingCarrierMethodBindingView>> ReplaceMethodBindingAsync(
            ShippingAuthoringOwnerScope owner,
            Guid draftId,
            ShippingCarrierMethodBindingReplaceRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<ShippingCarrierAccountExecutionContext?> TryResolveExecutionContextAsync(
            ShippingDraftOwnerKind ownerKind,
            string? ownerTenantId,
            Guid? ownerSellerPartyId,
            Guid accountId,
            CancellationToken cancellationToken)
            => Task.FromResult<ShippingCarrierAccountExecutionContext?>(null);

        public Task<string?> TryResolveDestinationExternalCodeAsync(
            Guid accountId,
            Guid geographyCityId,
            CancellationToken cancellationToken)
            => Task.FromResult<string?>("202");

        public Task<CarrierBindingExecution?> GetMethodBindingForDraftAsync(
            Guid draftId,
            ShippingDraftOwnerKind ownerKind,
            string? ownerTenantId,
            Guid? ownerSellerPartyId,
            CancellationToken cancellationToken)
            => Task.FromResult<CarrierBindingExecution?>(new CarrierBindingExecution(
                BindingId,
                Guid.Parse("88888888-8888-4888-8888-888888888888"),
                "tipax",
                "7",
                "101",
                20m,
                15m,
                10m,
                true,
                new CarrierAccountExecution(
                    Guid.Parse("88888888-8888-4888-8888-888888888888"),
                    1,
                    "user",
                    "pass",
                    "IRT",
                    "11",
                    "21",
                    "31",
                    "1",
                    "2",
                    "3")));
    }

    private sealed class FixedRegistry(ICarrierRateProvider provider) : ICarrierRateProviderRegistry
    {
        public bool TryResolve(string providerCode, out ICarrierRateProvider? resolved)
        {
            resolved = provider;
            return string.Equals(providerCode, provider.ProviderCode, StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class FixedCarrierProvider(CarrierRateOutcome outcome = CarrierRateOutcome.Quoted) : ICarrierRateProvider
    {
        public string ProviderCode => "tipax";

        public int Calls { get; private set; }

        public Task<CarrierRateResult> QuoteAsync(CarrierRateRequest request, CancellationToken cancellationToken)
        {
            Calls++;
            Assert.Equal("101", request.OriginLocationCode);
            Assert.Equal("202", request.DestinationLocationCode);
            Assert.NotNull(request.Account);
            if (outcome != CarrierRateOutcome.Quoted)
            {
                return Task.FromResult(CarrierRateResult.Failure(outcome, "carrier.test"));
            }

            return Task.FromResult(new CarrierRateResult(
                CarrierRateOutcome.Quoted,
                [
                    new CarrierRateOption(
                        "tipax",
                        "7",
                        "اکسپرس",
                        "IRT",
                        137_500m,
                        120_000m,
                        12_500m,
                        5_000m,
                        0m,
                        0m,
                        "tipax:quote:1:7")
                ]));
        }
    }

    private sealed class ControlledGeography : IGeographyPlaceSnapshotReader
    {
        private readonly Dictionary<Guid, CountrySnapshot> _countries = [];
        private readonly Dictionary<Guid, AdministrativeAreaSnapshot> _areas = [];
        private readonly Dictionary<Guid, CitySnapshot> _cities = [];

        public ControlledGeography WithCountry(Guid id)
        {
            _countries[id] = new CountrySnapshot(id, "IR", true);
            return this;
        }

        public ControlledGeography WithArea(Guid id, Guid countryId, Guid? parentId)
        {
            _areas[id] = new AdministrativeAreaSnapshot(id, countryId, parentId, "A", true);
            return this;
        }

        public ControlledGeography WithCity(Guid id, Guid countryId, Guid areaId)
        {
            _cities[id] = new CitySnapshot(id, countryId, areaId, "C", true);
            return this;
        }

        public Task<IReadOnlyList<CountrySnapshot>> GetCountriesAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<CountrySnapshot>>(ids.Select(id => _countries[id]).ToArray());

        public Task<IReadOnlyList<AdministrativeAreaSnapshot>> GetAdministrativeAreasAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<AdministrativeAreaSnapshot>>(ids.Select(id => _areas[id]).ToArray());

        public Task<IReadOnlyList<CitySnapshot>> GetCitiesAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<CitySnapshot>>(ids.Select(id => _cities[id]).ToArray());

        public Task<IReadOnlyList<ServiceAreaSnapshot>> GetServiceAreasAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ServiceAreaSnapshot>>([]);
    }

    private sealed class StubEdition(ToobaEdition edition) : ICurrentEdition
    {
        public EditionContext? Current => new(edition, "test");
    }

    private sealed class StubTenant(string tenantId) : ICurrentTenant
    {
        public TenantContext? Current => new(
            new TenantId(tenantId),
            TenantStatus.Active,
            new ConnectionReference("test"),
            null,
            null,
            null,
            "localhost",
            null);
    }
}
