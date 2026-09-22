using Tooba.BuildingBlocks.Grid;
using Tooba.Fulfillment.Application.Errors;
using Tooba.Fulfillment.Application.Queries.QueryAdminFulfillmentWorkQueue;
using Tooba.Fulfillment.Contracts.Errors;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

public sealed class FulfillmentErrorAndGridTests
{
    [Fact]
    public void Exception_mapper_maps_exact_stable_codes_only()
    {
        Assert.Equal(
            FulfillmentErrorCodes.Missing,
            FulfillmentExceptionMapper.ToSemanticError(new InvalidOperationException(FulfillmentErrorCodes.Missing)).Code);
        Assert.Equal(
            FulfillmentErrorCodes.ShippingServiceNotFound,
            FulfillmentExceptionMapper.ToSemanticError(new InvalidOperationException(FulfillmentErrorCodes.ShippingServiceNotFound)).Code);
        Assert.Equal(
            "fulfillment.tracking.required",
            FulfillmentExceptionMapper.ToSemanticError(new InvalidOperationException("fulfillment.tracking.required")).Code);
        Assert.Equal(
            "shipping_service_option.code.required",
            FulfillmentExceptionMapper.ToSemanticError(new InvalidOperationException("shipping_service_option.code.required")).Code);
    }

    [Fact]
    public void Exception_mapper_does_not_parse_localized_or_prose_messages()
    {
        Assert.False(FulfillmentExceptionMapper.TryMapExact("پیدا نشد", out _));
        Assert.False(FulfillmentExceptionMapper.TryMapExact("Shipment failed somehow", out _));
        Assert.False(FulfillmentExceptionMapper.TryMapExact("fulfillment.unknown.future_code", out _));
        Assert.Throws<InvalidOperationException>(() =>
            FulfillmentExceptionMapper.ToSemanticError(new InvalidOperationException("پیدا نشد")));
    }

    [Fact]
    public async Task Exception_mapper_propagates_unknown_InvalidOperationException()
    {
        var unknown = new InvalidOperationException("fulfillment.pricing.unexpected");
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            FulfillmentExceptionMapper.TryAsync<int>(() => throw unknown));
        Assert.Same(unknown, thrown);
        Assert.Equal("fulfillment.pricing.unexpected", thrown.Message);
    }

    [Fact]
    public void Fulfillment_grid_policy_preserves_default_sort_and_field_whitelist()
    {
        var normalized = AdminFulfillmentGridQueryPolicy.Instance.Normalize(
            new GridQueryRequest(1, 20, null, [], [], null));
        Assert.Equal("updatedAt", Assert.Single(normalized.Sort).Field);
        Assert.Equal("desc", normalized.Sort[0].Direction);

        Assert.Throws<GridQueryValidationException>(() =>
            AdminFulfillmentGridQueryPolicy.Instance.Normalize(
                new GridQueryRequest(
                    1,
                    20,
                    null,
                    [],
                    [new GridFilterRequest("not-a-field", "equals", "x", null, null)],
                    null)));
    }
}
