using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tooba.Shipping.Application.Rates.Commands;
using Tooba.Shipping.Application.Validation;
using Tooba.Shipping.Contracts.Admin;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.Shipping.Infrastructure.Rates;
using Xunit;

namespace Tooba.Shipping.Tests.Admin;

/// <summary>
/// TB-SHIPPING-MVP-RATE-COVERAGE-ADMIN-054 — rate/coverage authoring slice: transport-shape
/// denials, real persistence roundtrip, duplicate-scope denial and fail-closed empty coverage.
/// </summary>
public sealed class ShippingRateCoverageAdminTests
{
    private static readonly Guid CountryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AreaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CityId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static ShippingRateCoverageDirectory Directory(out ShippingDbContext context)
    {
        var options = new DbContextOptionsBuilder<ShippingDbContext>()
            .UseInMemoryDatabase($"shipping-054-{Guid.NewGuid():N}")
            .Options;
        context = new ShippingDbContext(options);
        return new ShippingRateCoverageDirectory(context);
    }

    private static ShippingRatePolicyWriteRequest RateRequest(decimal charge = 25000m) =>
        new(
            "tooba-post",
            new ShippingDestinationScopeWrite(CountryId, AreaId, CityId),
            charge,
            "IRR",
            2,
            3,
            500000m);

    [Fact]
    public async Task Create_update_and_deactivate_rate_policy_roundtrip_through_real_context()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var created = await directory.CreateRateAsync(RateRequest(), default);
        Assert.True(created.IsSuccess);
        Assert.Equal(25000m, created.Value.ChargeAmount);
        Assert.True(created.Value.IsActive);

        var updated = await directory.UpdateRateAsync(
            created.Value.RatePolicyId,
            RateRequest(31000m) with { FreeAboveThreshold = null },
            default);
        Assert.True(updated.IsSuccess);
        Assert.Equal(31000m, updated.Value.ChargeAmount);
        Assert.Null(updated.Value.FreeAboveThreshold);

        var deactivated = await directory.SetRateActiveAsync(created.Value.RatePolicyId, false, default);
        Assert.True(deactivated.IsSuccess);
        Assert.False(deactivated.Value.IsActive);

        var reloaded = await directory.ListAsync("tooba-post", default);
        Assert.True(reloaded.IsSuccess);
        var only = Assert.Single(reloaded.Value.RatePolicies);
        Assert.Equal(31000m, only.ChargeAmount);
        Assert.False(only.IsActive);
    }

    [Fact]
    public async Task Duplicate_scope_for_same_service_is_denied_with_stable_code()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        Assert.True((await directory.CreateRateAsync(RateRequest(), default)).IsSuccess);
        var duplicate = await directory.CreateRateAsync(RateRequest(99999m), default);
        Assert.True(duplicate.IsFailure);
        Assert.Equal(ShippingErrorCodes.RatePolicyScopeDuplicate, duplicate.FirstError.Code);

        var grant = new ShippingCoverageGrantWriteRequest(
            "tooba-post",
            new ShippingDestinationScopeWrite(CountryId, AreaId, CityId));
        Assert.True((await directory.CreateGrantAsync(grant, default)).IsSuccess);
        var duplicateGrant = await directory.CreateGrantAsync(grant, default);
        Assert.True(duplicateGrant.IsFailure);
        Assert.Equal(ShippingErrorCodes.CoverageGrantScopeDuplicate, duplicateGrant.FirstError.Code);
    }

    [Fact]
    public async Task Empty_coverage_set_is_returned_as_no_coverage_never_unrestricted()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var view = await directory.ListAsync("tooba-post", default);
        Assert.True(view.IsSuccess);
        Assert.Empty(view.Value.RatePolicies);
        Assert.Empty(view.Value.CoverageGrants);
    }

    [Fact]
    public async Task Missing_record_is_reported_with_stable_not_found_code()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var missing = await directory.SetRateActiveAsync(Guid.NewGuid(), false, default);
        Assert.True(missing.IsFailure);
        Assert.Equal(ShippingErrorCodes.RatePolicyNotFound, missing.FirstError.Code);

        var missingGrant = await directory.SetGrantActiveAsync(Guid.NewGuid(), false, default);
        Assert.True(missingGrant.IsFailure);
        Assert.Equal(ShippingErrorCodes.CoverageGrantNotFound, missingGrant.FirstError.Code);
    }

    [Fact]
    public void Transport_shape_invalid_requests_are_denied()
    {
        var validator = new ShippingRatePolicyWriteRequestValidator();

        Assert.NotEmpty(validator.Validate(RateRequest(-1m)).Errors);
        Assert.NotEmpty(validator.Validate(RateRequest() with { CurrencyCode = "IR" }).Errors);
        Assert.NotEmpty(validator.Validate(RateRequest() with { PreparationDays = 400 }).Errors);
        Assert.NotEmpty(validator.Validate(RateRequest() with { LeadDays = -1 }).Errors);
        Assert.NotEmpty(validator.Validate(RateRequest() with { FreeAboveThreshold = -5m }).Errors);
        Assert.NotEmpty(validator.Validate(RateRequest() with { ServiceCode = " " }).Errors);
        Assert.NotEmpty(validator.Validate(
            RateRequest() with { Scope = new ShippingDestinationScopeWrite(CountryId, null, null, Guid.NewGuid()) }).Errors);

        var grantValidator = new ShippingCoverageGrantWriteRequestValidator();
        Assert.NotEmpty(grantValidator.Validate(
            new ShippingCoverageGrantWriteRequest("", new ShippingDestinationScopeWrite(CountryId))).Errors);
    }

    [Fact]
    public void Handler_wraps_transport_message_and_returns_directory_result()
    {
        var handler = new GetShippingRatesAndCoverageHandler(new StubDirectory());

        var result = handler.Handle(new GetShippingRatesAndCoverageQuery("tooba-post"), default).GetAwaiter().GetResult();

        Assert.True(result.IsSuccess);
        Assert.Equal("tooba-post", result.Value.ServiceCode);
    }

    private sealed class StubDirectory : Application.Rates.Ports.IShippingRateCoverageDirectory
    {
        public Task<BuildingBlocks.Results.Result<ShippingRatesAndCoverageView>> ListAsync(
            string serviceCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(BuildingBlocks.Results.Result.Success(
                new ShippingRatesAndCoverageView(serviceCode, [], [])));

        public Task<BuildingBlocks.Results.Result<ShippingRatePolicyAdminItem>> CreateRateAsync(
            ShippingRatePolicyWriteRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<BuildingBlocks.Results.Result<ShippingRatePolicyAdminItem>> UpdateRateAsync(
            Guid ratePolicyId,
            ShippingRatePolicyWriteRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<BuildingBlocks.Results.Result<ShippingRatePolicyAdminItem>> SetRateActiveAsync(
            Guid ratePolicyId,
            bool isActive,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<BuildingBlocks.Results.Result<ShippingCoverageGrantAdminItem>> CreateGrantAsync(
            ShippingCoverageGrantWriteRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<BuildingBlocks.Results.Result<ShippingCoverageGrantAdminItem>> SetGrantActiveAsync(
            Guid coverageGrantId,
            bool isActive,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
