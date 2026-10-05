using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Composition;
using Tooba.Fulfillment.Application.Errors;
using Tooba.Fulfillment.Application.WorkQueue.Queries;
using Tooba.Fulfillment.Contracts.Errors;
using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

public sealed class FulfillmentErrorAndGridTests
{
    [Fact]
    public void Typed_fault_helper_accepts_only_declared_stable_codes()
    {
        Assert.Equal(FulfillmentErrorCodes.Missing, FulfillmentErrors.RequireKnown(FulfillmentErrorCodes.Missing));
        Assert.Equal(FulfillmentErrorCodes.ShippingServiceNotFound,
            FulfillmentErrors.RequireKnown(FulfillmentErrorCodes.ShippingServiceNotFound));

        var fault = FulfillmentErrors.SemanticFault(FulfillmentErrorCodes.ShippingServiceOptionCodeRequired);
        Assert.Equal(FulfillmentErrorCodes.ShippingServiceOptionCodeRequired, fault.Error.Code);

        // A bare/undeclared string is rejected at the throw site — never classified as prose.
        Assert.Throws<InvalidOperationException>(() => FulfillmentErrors.RequireKnown("پیدا نشد"));
    }

    [Fact]
    public void Typed_fault_helper_does_not_parse_localized_or_prose_messages()
    {
        Assert.False(FulfillmentErrors.IsKnown("پیدا نشد"));
        Assert.False(FulfillmentErrors.IsKnown("Shipment failed somehow"));
        Assert.False(FulfillmentErrors.IsKnown("fulfillment.unknown.future_code"));
    }

    [Fact]
    public async Task Fulfillment_operation_maps_semantic_fault_and_propagates_unknowns()
    {
        var mapped = await FulfillmentOperation.ExecuteAsync<int>(() =>
            throw FulfillmentErrors.SemanticFault(FulfillmentErrorCodes.Missing));
        Assert.True(mapped.IsFailure);
        Assert.Equal(FulfillmentErrorCodes.Missing, mapped.Errors[0].Code);

        var unknown = new InvalidOperationException("fulfillment.pricing.unexpected");
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            FulfillmentOperation.ExecuteAsync<int>(() => throw unknown));
        Assert.Same(unknown, thrown);
        Assert.Equal("fulfillment.pricing.unexpected", thrown.Message);
    }

    [Fact]
    public async Task Fulfillment_operation_maps_domain_contract_faults_by_stable_code()
    {
        // The Domain aggregates and the Infrastructure directory raise ContractOperationException with a
        // declared stable code; that expected failure must stay mapped (never a bare platform.unexpected).
        var mapped = await FulfillmentOperation.ExecuteAsync<int>(() =>
            throw new ContractOperationException(FulfillmentErrorCodes.PackQuantityExceeds));
        Assert.True(mapped.IsFailure);
        Assert.Equal(FulfillmentErrorCodes.PackQuantityExceeds, mapped.Errors[0].Code);

        var voidMapped = await FulfillmentOperation.ExecuteAsync(() =>
            throw new ContractOperationException(FulfillmentErrorCodes.OrderNotPaid));
        Assert.True(voidMapped.IsFailure);
        Assert.Equal(FulfillmentErrorCodes.OrderNotPaid, voidMapped.Errors[0].Code);

        // A contract fault carrying a non-Fulfillment code is not classified and must fail loud.
        var foreign = new ContractOperationException("catalog.category.not_found");
        var thrown = await Assert.ThrowsAsync<ContractOperationException>(() =>
            FulfillmentOperation.ExecuteAsync<int>(() => throw foreign));
        Assert.Same(foreign, thrown);
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
