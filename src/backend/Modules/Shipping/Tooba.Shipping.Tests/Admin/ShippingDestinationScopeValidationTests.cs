using Tooba.BuildingBlocks.Results;
using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Application.Rates.Commands;
using Tooba.Shipping.Application.Rates.Ports;
using Tooba.Shipping.Contracts.Admin;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Infrastructure.Rates;
using Xunit;

namespace Tooba.Shipping.Tests.Admin;

/// <summary>
/// TB-SHIPPING-MVP-GEOGRAPHY-ADMIN-056 — the create write path must validate the submitted destination
/// through the Geography Contracts lookup before any write, so an invented/unknown/inactive/inconsistent
/// scope is refused with a stable code and never reaches persistence.
/// </summary>
public sealed class ShippingDestinationScopeValidationTests
{
    private static readonly Guid CountryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AreaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CityId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task Valid_scope_is_accepted_and_passed_through_unchanged()
    {
        var validator = new GeographyDestinationScopeValidator(new StubGeography(accept: true));

        var result = await validator.ValidateAsync(
            new ShippingDestinationScopeWrite(CountryId, AreaId, null, null),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal(CountryId, result.Value.CountryId);
        Assert.Equal(AreaId, result.Value.AdministrativeAreaId);
    }

    [Fact]
    public async Task Unknown_or_inactive_scope_is_refused_with_stable_code()
    {
        var validator = new GeographyDestinationScopeValidator(new StubGeography(accept: false));

        var result = await validator.ValidateAsync(new ShippingDestinationScopeWrite(CountryId), default);

        Assert.True(result.IsFailure);
        Assert.Equal(ShippingErrorCodes.DestinationScopeInvalid, result.FirstError.Code);
    }

    [Fact]
    public async Task Invalid_scope_stops_the_create_rate_handler_before_any_write()
    {
        var directory = new RecordingDirectory();
        var handler = new CreateShippingRatePolicyHandler(
            directory,
            new GeographyDestinationScopeValidator(new StubGeography(accept: false)),
            new AcceptingMethodOptionValidator());

        var result = await handler.Handle(
            new CreateShippingRatePolicyCommand(
                "tooba-post",
                new ShippingRatePolicyWriteRequest(
                    "tooba-post",
                    new ShippingDestinationScopeWrite(CountryId),
                    1000m,
                    "IRR",
                    1,
                    2)),
            default);

        Assert.True(result.IsFailure);
        Assert.Equal(ShippingErrorCodes.DestinationScopeInvalid, result.FirstError.Code);
        Assert.Equal(0, directory.CreateRateCalls);
    }

    [Fact]
    public async Task Invalid_scope_stops_the_create_grant_handler_before_any_write()
    {
        var directory = new RecordingDirectory();
        var handler = new CreateShippingCoverageGrantHandler(
            directory,
            new GeographyDestinationScopeValidator(new StubGeography(accept: false)));

        var result = await handler.Handle(
            new CreateShippingCoverageGrantCommand(
                "tooba-post",
                new ShippingCoverageGrantWriteRequest(
                    "tooba-post",
                    new ShippingDestinationScopeWrite(CountryId, AreaId, CityId))),
            default);

        Assert.True(result.IsFailure);
        Assert.Equal(ShippingErrorCodes.DestinationScopeInvalid, result.FirstError.Code);
        Assert.Equal(0, directory.CreateGrantCalls);
    }

    [Fact]
    public async Task Valid_scope_reaches_the_directory_exactly_once()
    {
        var directory = new RecordingDirectory();
        var handler = new CreateShippingCoverageGrantHandler(
            directory,
            new GeographyDestinationScopeValidator(new StubGeography(accept: true)));

        var result = await handler.Handle(
            new CreateShippingCoverageGrantCommand(
                "tooba-post",
                new ShippingCoverageGrantWriteRequest(
                    "tooba-post",
                    new ShippingDestinationScopeWrite(CountryId))),
            default);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, directory.CreateGrantCalls);
    }

    private sealed class StubGeography(bool accept) : IGeographyLookup
    {
        public Task<IReadOnlyList<CountrySnapshot>> ListCountriesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CountrySnapshot>>([]);

        public Task<IReadOnlyList<AdministrativeAreaSnapshot>> ListAdministrativeAreasAsync(
            Guid countryId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AdministrativeAreaSnapshot>>([]);

        public Task<IReadOnlyList<CitySnapshot>> ListCitiesAsync(
            Guid countryId,
            Guid? administrativeAreaId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CitySnapshot>>([]);

        public Task<IReadOnlyList<ServiceAreaSnapshot>> ListServiceAreasAsync(
            Guid cityId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ServiceAreaSnapshot>>([]);

        public Task<DestinationIdentifiers?> GetByIdsAsync(
            DestinationIdentifiers identifiers,
            CancellationToken cancellationToken) =>
            Task.FromResult(accept ? identifiers : null);

        public Task<DestinationResolution> ResolveLegacyAsync(
            string? countryCode,
            string? administrativeAreaName,
            string? cityName,
            string? serviceAreaName,
            CancellationToken cancellationToken) =>
            Task.FromResult(DestinationResolution.Unresolved());
    }

    private sealed class RecordingDirectory : IShippingRateCoverageDirectory
    {
        public int CreateRateCalls { get; private set; }

        public int CreateGrantCalls { get; private set; }

        public Task<Result<ShippingRatesAndCoverageView>> ListAsync(
            string serviceCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(new ShippingRatesAndCoverageView(serviceCode, [], [])));

        public Task<Result<ShippingRatePolicyAdminItem>> CreateRateAsync(
            string serviceCode,
            ShippingRatePolicyWriteRequest request,
            CancellationToken cancellationToken)
        {
            CreateRateCalls++;
            return Task.FromResult(Result.Success(new ShippingRatePolicyAdminItem(
                Guid.NewGuid(),
                serviceCode,
                request.Scope.CountryId,
                request.Scope.AdministrativeAreaId,
                request.Scope.CityId,
                request.Scope.ServiceAreaId,
                Contracts.Rates.ShippingDestinationLevel.Country,
                request.ChargeAmount,
                request.CurrencyCode,
                request.PreparationDays,
                request.LeadDays,
                request.FreeAboveThreshold,
                true,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow)));
        }

        public Task<Result<ShippingRatePolicyAdminItem>> UpdateRateAsync(
            Guid ratePolicyId,
            string serviceCode,
            ShippingRatePolicyWriteRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<ShippingRatePolicyAdminItem>> SetRateActiveAsync(
            Guid ratePolicyId,
            string serviceCode,
            bool isActive,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result<ShippingCoverageGrantAdminItem>> CreateGrantAsync(
            string serviceCode,
            ShippingCoverageGrantWriteRequest request,
            CancellationToken cancellationToken)
        {
            CreateGrantCalls++;
            return Task.FromResult(Result.Success(new ShippingCoverageGrantAdminItem(
                Guid.NewGuid(),
                serviceCode,
                request.Scope.CountryId,
                request.Scope.AdministrativeAreaId,
                request.Scope.CityId,
                request.Scope.ServiceAreaId,
                Contracts.Rates.ShippingDestinationLevel.Country,
                true,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow)));
        }

        public Task<Result<ShippingCoverageGrantAdminItem>> SetGrantActiveAsync(
            Guid coverageGrantId,
            string serviceCode,
            bool isActive,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class AcceptingMethodOptionValidator : IShippingMethodOptionValidator
    {
        public Task<Result<bool>> ValidateAsync(
            string serviceCode,
            string? optionCode,
            CancellationToken cancellationToken) => Task.FromResult(Result.Success(true));
    }
}
