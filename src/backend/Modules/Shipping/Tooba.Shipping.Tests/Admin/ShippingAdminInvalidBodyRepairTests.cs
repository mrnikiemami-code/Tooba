using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Shipping.Application.Validation;
using Tooba.Shipping.Contracts.Admin;
using Xunit;

namespace Tooba.Shipping.Tests.Admin;

/// <summary>
/// TB-SHIPPING-MVP-OPERATIONAL-REPAIR-064, defect F-063-1 — a body the Shipping admin write
/// routes cannot read (syntax-invalid JSON, non-JSON content type, an unparsable/empty GUID in
/// <c>scope</c>, or a wholly absent body) escaped the endpoint as
/// <c>BadHttpRequestException</c> and was mapped by the Host envelope to
/// <c>500 platform.unexpected</c>.
///
/// These are focused in-memory proofs of the transport-to-envelope translation. They are NOT a
/// live HTTP assertion; the real-HTTP 400 is proven separately on an isolated scratch Host.
/// </summary>
public sealed class ShippingAdminInvalidBodyRepairTests
{
    private const string BodyField = "request";

    private static readonly SafeErrorMapper Mapper =
        new(new ErrorDefinitionCatalog([new FoundationErrorCatalogContributor()]));

    private static ValidationException InvalidBody() =>
        new([new ValidationFailure(BodyField, ShippingValidationCodes.BodyInvalid)
        {
            ErrorCode = ShippingValidationCodes.BodyInvalid,
        }]);

    [Fact]
    public void Unparsed_body_reproduces_the_historical_platform_500_defect()
    {
        // The pre-repair shape: the connection boundary threw BadHttpRequestException before the
        // handler ran, and the Host envelope had no client-input classification for it.
        var mapper = Mapper.Map(new BadHttpRequestException("{not-json"));

        Assert.Equal(StatusCodes.Status500InternalServerError, mapper.StatusCode);
        Assert.Equal("platform.unexpected", mapper.ErrorCode);
    }

    [Fact]
    public void Unreadable_body_translates_to_the_canonical_validation_envelope_not_a_platform_error()
    {
        var mapped = Mapper.Map(InvalidBody());

        Assert.Equal(StatusCodes.Status400BadRequest, mapped.StatusCode);
        Assert.Equal("validation.failed", mapped.ErrorCode);
        Assert.NotEqual("platform.unexpected", mapped.ErrorCode);
        Assert.False(string.IsNullOrWhiteSpace(mapped.LocalizationKey));
    }

    [Fact]
    public void Unreadable_body_reports_the_declared_stable_code_under_the_body_field()
    {
        var mapped = Mapper.Map(InvalidBody());

        Assert.NotNull(mapped.ValidationErrors);
        var field = Assert.Single(mapped.ValidationErrors!.Keys);
        Assert.Equal(BodyField, field);
        Assert.Equal(
            [ShippingValidationCodes.BodyInvalid],
            mapped.ValidationErrors[field]);
    }

    [Fact]
    public void Declared_body_code_is_stable_and_not_reused_from_the_scope_guard()
    {
        // A body that cannot be read is not a scope problem; conflating the two would tell the
        // client to add a scope when the actual defect is an unreadable payload.
        Assert.Equal("shipping.validation.body_invalid", ShippingValidationCodes.BodyInvalid);
        Assert.NotEqual(ShippingValidationCodes.ScopeRequired, ShippingValidationCodes.BodyInvalid);
    }

    [Fact]
    public void Valid_json_with_absent_scope_still_reports_the_unchanged_scope_code()
    {
        // The repair must not weaken 063's proven behavior: `{}` deserializes to a real request
        // object and the declared scope guard still owns the rejection.
        var request = new ShippingRatePolicyWriteRequest(null!, null!, 0m, null!, 0, 0);
        var result = new ShippingRatePolicyWriteRequestValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == ShippingValidationCodes.ScopeRequired);
        Assert.DoesNotContain(result.Errors, e => e.ErrorCode == ShippingValidationCodes.BodyInvalid);
    }

    [Fact]
    public void Valid_json_with_absent_scope_still_maps_to_the_declared_400_scope_envelope()
    {
        var exception = new ValidationException(
            new ShippingRatePolicyWriteRequestValidator()
                .Validate(new ShippingRatePolicyWriteRequest(null!, null!, 0m, null!, 0, 0))
                .Errors);

        var mapped = Mapper.Map(exception);

        Assert.Equal(StatusCodes.Status400BadRequest, mapped.StatusCode);
        Assert.Equal("validation.failed", mapped.ErrorCode);
        Assert.Contains(
            mapped.ValidationErrors!.Values.SelectMany(x => x),
            code => code == ShippingValidationCodes.ScopeRequired);
    }

    [Fact]
    public void Coverage_grant_write_shares_the_same_declared_body_and_scope_contract()
    {
        var bodyFailure = Mapper.Map(InvalidBody());
        Assert.Equal(StatusCodes.Status400BadRequest, bodyFailure.StatusCode);

        var scopeFailure = new ShippingCoverageGrantWriteRequestValidator()
            .Validate(new ShippingCoverageGrantWriteRequest(null!, null!));
        Assert.False(scopeFailure.IsValid);
        Assert.Contains(scopeFailure.Errors, e => e.ErrorCode == ShippingValidationCodes.ScopeRequired);
    }
}
