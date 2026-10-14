using FluentValidation;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Rates.Commands;
using Tooba.Shipping.Application.Validation;
using Tooba.Shipping.Contracts.Admin;
using Xunit;

namespace Tooba.Shipping.Tests.Admin;

/// <summary>
/// TB-SHIPPING-MVP-SAFETY-REPAIR-060 — repair of the reproduced defect where an omitted/null
/// <c>scope</c> in the Shipping rate/coverage write bodies reached the nested validator rules and
/// threw a NullReferenceException, surfacing as a platform 500 instead of a declared 4xx.
///
/// These are focused in-memory proofs. They establish the canonical request-pipeline and
/// presentation-envelope translation (ValidationException -> validation.failed 400). They are NOT a
/// live HTTP assertion and do not claim a live 400 was observed.
/// </summary>
public sealed class ShippingAdminScopeGuardRepairTests
{
    private static readonly Guid CountryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AreaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CityId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly SafeErrorMapper Mapper =
        new(new ErrorDefinitionCatalog([new FoundationErrorCatalogContributor()]));

    private static ShippingRatePolicyWriteRequest RateRequest(ShippingDestinationScopeWrite? scope) =>
        new(
            "tooba-post",
            scope!,
            25000m,
            "IRR",
            2,
            3,
            500000m);

    private static ShippingCoverageGrantWriteRequest GrantRequest(ShippingDestinationScopeWrite? scope) =>
        new("tooba-post", scope!);

    [Fact]
    public void Omitted_scope_on_rate_policy_write_is_rejected_with_declared_code_never_throws()
    {
        var validator = new ShippingRatePolicyWriteRequestValidator();

        var result = validator.Validate(RateRequest(scope: null));

        Assert.False(result.IsValid);
        var failure = Assert.Single(result.Errors, e => e.ErrorCode == ShippingValidationCodes.ScopeRequired);
        Assert.Equal("Scope", failure.PropertyName);
    }

    [Fact]
    public void Omitted_scope_on_coverage_grant_write_is_rejected_with_declared_code_never_throws()
    {
        var validator = new ShippingCoverageGrantWriteRequestValidator();

        var result = validator.Validate(GrantRequest(scope: null));

        Assert.False(result.IsValid);
        var failure = Assert.Single(result.Errors, e => e.ErrorCode == ShippingValidationCodes.ScopeRequired);
        Assert.Equal("Scope", failure.PropertyName);
    }

    [Fact]
    public void Valid_scope_still_passes_both_write_validators_unchanged()
    {
        var scope = new ShippingDestinationScopeWrite(CountryId, AreaId, CityId);

        Assert.True(new ShippingRatePolicyWriteRequestValidator().Validate(RateRequest(scope)).IsValid);
        Assert.True(new ShippingCoverageGrantWriteRequestValidator().Validate(GrantRequest(scope)).IsValid);
    }

    [Fact]
    public void Wrong_nested_scope_data_is_still_denied_with_the_existing_stable_codes()
    {
        // Empty country id must still be refused through the pre-existing country code.
        var emptyCountry = new ShippingRatePolicyWriteRequestValidator()
            .Validate(RateRequest(new ShippingDestinationScopeWrite(Guid.Empty, AreaId, CityId)));
        Assert.False(emptyCountry.IsValid);
        Assert.Contains(emptyCountry.Errors, e => e.ErrorCode == ShippingValidationCodes.CountryRequired);

        // A service-area id without its owning city must still be refused; the guard did not weaken it.
        var areaWithoutCity = new ShippingCoverageGrantWriteRequestValidator()
            .Validate(GrantRequest(new ShippingDestinationScopeWrite(CountryId, AreaId, null, Guid.NewGuid())));
        Assert.False(areaWithoutCity.IsValid);
        Assert.Contains(
            areaWithoutCity.Errors,
            e => e.ErrorCode == ShippingValidationCodes.ServiceAreaRequiresCity);
    }

    [Fact]
    public void Unguarded_nested_scope_rule_reproduces_the_historical_null_reference_defect()
    {
        // Local reproduction of the pre-repair rule shape: the NotNull rule reports one failure but does
        // not stop the following nested-property rule from being evaluated, so a null scope dereferenced
        // inside the property expression and the request escaped as an unhandled exception (500).
        var unguarded = new InlineValidator<ShippingRatePolicyWriteRequest>();
        unguarded.RuleFor(x => x.Scope).NotNull();
        unguarded.RuleFor(x => x.Scope.CountryId).NotEmpty();

        Assert.Throws<NullReferenceException>(() => unguarded.Validate(RateRequest(scope: null)));

        // The repaired shape returns a declared validation failure instead of throwing.
        Assert.False(new ShippingRatePolicyWriteRequestValidator().Validate(RateRequest(scope: null)).IsValid);
    }

    [Fact]
    public async Task Omitted_scope_translates_to_the_canonical_validation_envelope_not_a_platform_error()
    {
        var command = new CreateShippingRatePolicyCommand("tooba-post", RateRequest(scope: null));
        var behavior = new ValidationBehavior<CreateShippingRatePolicyCommand, Result<ShippingRatePolicyAdminItem>>(
            [new CreateShippingRatePolicyCommandValidator()]);

        var handlerRan = false;
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                command,
                _ =>
                {
                    handlerRan = true;
                    return Task.FromResult(Result.Failure<ShippingRatePolicyAdminItem>(
                        new SemanticError("shipping.unexpected")));
                },
                default));

        // The handler must never run for an invalid transport shape.
        Assert.False(handlerRan);

        var mapped = Mapper.Map(exception);
        Assert.Equal(400, mapped.StatusCode);
        Assert.Equal("validation.failed", mapped.ErrorCode);
        Assert.NotEqual("platform.unexpected", mapped.ErrorCode);
        Assert.NotNull(mapped.ValidationErrors);
        var scopeKey = Assert.Single(
            mapped.ValidationErrors!.Keys,
            key => key.EndsWith("Scope", StringComparison.Ordinal));
        Assert.Contains(ShippingValidationCodes.ScopeRequired, mapped.ValidationErrors[scopeKey]);

        // The original defect surfaced here as a NullReferenceException on the nested property access;
        // assert the exception that reached the envelope carries only declared validation failures.
        Assert.All(exception.Errors, failure => Assert.False(string.IsNullOrWhiteSpace(failure.ErrorCode)));
    }

    [Fact]
    public async Task Omitted_scope_translates_to_the_canonical_validation_envelope_for_grant_write()
    {
        var command = new CreateShippingCoverageGrantCommand("tooba-post", GrantRequest(scope: null));
        var behavior = new ValidationBehavior<CreateShippingCoverageGrantCommand, Result<ShippingCoverageGrantAdminItem>>(
            [new CreateShippingCoverageGrantCommandValidator()]);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                command,
                _ => Task.FromResult(Result.Failure<ShippingCoverageGrantAdminItem>(
                    new SemanticError("shipping.unexpected"))),
                default));

        var mapped = Mapper.Map(exception);
        Assert.Equal(400, mapped.StatusCode);
        Assert.Equal("validation.failed", mapped.ErrorCode);
        var scopeKey = Assert.Single(
            mapped.ValidationErrors!.Keys,
            key => key.EndsWith("Scope", StringComparison.Ordinal));
        Assert.Contains(ShippingValidationCodes.ScopeRequired, mapped.ValidationErrors[scopeKey]);
    }
}
