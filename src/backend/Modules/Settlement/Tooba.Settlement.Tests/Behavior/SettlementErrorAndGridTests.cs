using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Results;
using Tooba.Settlement.Application.Composition;
using Tooba.Settlement.Application.Payouts.Queries;
using Tooba.Settlement.Contracts.Errors;
using Xunit;

namespace Tooba.Settlement.Tests.Behavior;

/// <summary>
/// TB-TMAR-SETTLEMENT-AMSC-001-W1 — behavior tests for the canonical typed-fault seam
/// (<see cref="SettlementOperation"/>) that replaced the retired message-text exception mapper.
/// </summary>
public sealed class SettlementErrorAndGridTests
{
    [Fact]
    public async Task Operation_maps_declared_typed_contract_faults_to_Result_failures()
    {
        foreach (var code in SettlementErrorCodes.HttpReachable)
        {
            var value = await SettlementOperation.ExecuteAsync<int>(
                () => throw new ContractOperationException(code));
            Assert.False(value.IsSuccess);
            Assert.Equal(code, value.FirstError.Code);

            var plain = await SettlementOperation.ExecuteAsync(
                () => throw new ContractOperationException(code));
            Assert.False(plain.IsSuccess);
            Assert.Equal(code, plain.FirstError.Code);

            Assert.Equal(code, SettlementOperation.ToSemanticError(new ContractOperationException(code)).Code);
        }
    }

    [Fact]
    public async Task Operation_maps_the_platform_fault_code_without_cataloguing_it()
    {
        Assert.True(SettlementErrorCodes.IsPlatformFault(SettlementErrorCodes.OutboxUnmapped));
        Assert.False(SettlementErrorCodes.IsHttpReachable(SettlementErrorCodes.OutboxUnmapped));

        var value = await SettlementOperation.ExecuteAsync<int>(
            () => throw new ContractOperationException(SettlementErrorCodes.OutboxUnmapped));
        Assert.False(value.IsSuccess);
        Assert.Equal(SettlementErrorCodes.OutboxUnmapped, value.FirstError.Code);
    }

    [Fact]
    public async Task Operation_maps_SemanticException_by_its_typed_error()
    {
        var error = new SemanticError(SettlementErrorCodes.PayoutMissing);
        var value = await SettlementOperation.ExecuteAsync<int>(() => throw new SemanticException(error));
        Assert.False(value.IsSuccess);
        Assert.Equal(SettlementErrorCodes.PayoutMissing, value.FirstError.Code);

        var plain = await SettlementOperation.ExecuteAsync(() => throw new SemanticException(error));
        Assert.False(plain.IsSuccess);
        Assert.Equal(SettlementErrorCodes.PayoutMissing, plain.FirstError.Code);
    }

    [Fact]
    public async Task Operation_never_classifies_by_message_text()
    {
        // Localized prose, English prose and a foreign/unknown stable code must all propagate
        // untouched to the canonical global exception boundary.
        foreach (var message in new[] { "پیدا نشد", "Payout failed somehow", "settlement.unknown.future_code" })
        {
            var unknown = new ContractOperationException(message);
            var thrown = await Assert.ThrowsAsync<ContractOperationException>(
                () => SettlementOperation.ExecuteAsync<int>(() => throw unknown));
            Assert.Same(unknown, thrown);
        }

        var unexpected = new InvalidOperationException("settlement.pricing.unexpected");
        var propagated = await Assert.ThrowsAsync<InvalidOperationException>(
            () => SettlementOperation.ExecuteAsync<int>(() => throw unexpected));
        Assert.Same(unexpected, propagated);

        var foreign = new ContractOperationException("pricing.unknown.future_code");
        Assert.Throws<ContractOperationException>(() => SettlementOperation.ToSemanticError(foreign));
    }

    [Fact]
    public async Task Operation_returns_success_for_a_clean_action()
    {
        Assert.True((await SettlementOperation.ExecuteAsync(() => Task.FromResult(7))).IsSuccess);
        Assert.True((await SettlementOperation.ExecuteAsync(() => Task.CompletedTask)).IsSuccess);
    }

    [Fact]
    public void Payout_grid_policy_preserves_default_sort_and_field_whitelist()
    {
        var normalized = AdminPayoutGridQueryPolicy.Instance.Normalize(
            new GridQueryRequest(1, 20, null, [], [], null));
        Assert.Equal("created", Assert.Single(normalized.Sort).Field);
        Assert.Equal("desc", normalized.Sort[0].Direction);

        Assert.Throws<GridQueryValidationException>(() =>
            AdminPayoutGridQueryPolicy.Instance.Normalize(
                new GridQueryRequest(
                    1,
                    20,
                    null,
                    [],
                    [new GridFilterRequest("not-a-field", "equals", "x", null, null)],
                    null)));
    }
}
