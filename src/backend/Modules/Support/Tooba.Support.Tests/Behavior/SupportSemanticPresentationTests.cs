using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Composition;
using Tooba.Support.Contracts.Errors;
using Xunit;

namespace Tooba.Support.Tests.Behavior;

/// <summary>
/// شواهد رفتار درز خطای نوع‌دار Support: ناورداهای اعلام‌شدهٔ دامنه/دایرکتوری به کد نتیجهٔ عمومی
/// پایدار همان عملیات نگاشت می‌شوند، کد ناشناخته هرگز پنهان نمی‌شود و هیچ‌گاه بر پایهٔ متن پیام
/// تصمیم‌گیری نمی‌شود.
/// </summary>
public sealed class SupportSemanticPresentationTests
{
    [Fact]
    public async Task Declared_invariants_map_to_the_stable_public_outcome_code()
    {
        var rejected = await SupportOperation.ExecuteAsync<int>(
            () => throw new ContractOperationException(SupportErrorCodes.CategoryInvalid),
            SupportErrorCodes.Rejected);
        Assert.True(rejected.IsFailure);
        Assert.Equal(SupportErrorCodes.Rejected, rejected.Errors[0].Code);

        var reply = await SupportOperation.ExecuteAsync<int>(
            () => throw new ContractOperationException(SupportErrorCodes.ReplyClosed),
            SupportErrorCodes.ReplyRejected);
        Assert.Equal(SupportErrorCodes.ReplyRejected, reply.Errors[0].Code);

        var action = await SupportOperation.ExecuteAsync<int>(
            () => throw new ContractOperationException(SupportErrorCodes.CloseNotAllowed),
            SupportErrorCodes.ActionRejected);
        Assert.Equal(SupportErrorCodes.ActionRejected, action.Errors[0].Code);

        var patch = await SupportOperation.ExecuteAsync<int>(
            () => throw new ContractOperationException(SupportErrorCodes.StatusInvalid),
            SupportErrorCodes.PatchRejected);
        Assert.Equal(SupportErrorCodes.PatchRejected, patch.Errors[0].Code);
    }

    [Fact]
    public async Task Client_reachable_codes_are_reflected_without_a_public_override()
    {
        var missing = await SupportOperation.ExecuteAsync<int>(
            () => throw new ContractOperationException(SupportErrorCodes.TicketNotFound),
            SupportErrorCodes.Missing);
        Assert.Equal(SupportErrorCodes.Missing, missing.Errors[0].Code);

        var demo = await SupportOperation.ExecuteAsync<int>(
            () => throw new ContractOperationException(SupportErrorCodes.DemoNotReady),
            SupportErrorCodes.Rejected);
        Assert.Equal(SupportErrorCodes.DemoNotReady, demo.Errors[0].Code);
    }

    [Fact]
    public async Task Unknown_contract_codes_and_prose_are_never_swallowed()
    {
        var unknown = new ContractOperationException("support.unknown.future_code");
        await Assert.ThrowsAsync<ContractOperationException>(() =>
            SupportOperation.ExecuteAsync<int>(() => throw unknown, SupportErrorCodes.Rejected));

        var prose = new ContractOperationException("تیکت پیدا نشد.");
        await Assert.ThrowsAsync<ContractOperationException>(() =>
            SupportOperation.ExecuteAsync<int>(() => throw prose, SupportErrorCodes.Rejected));

        var unexpected = new InvalidOperationException("support.ticket_not_found");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SupportOperation.ExecuteAsync<int>(() => throw unexpected, SupportErrorCodes.Rejected));
    }

    [Fact]
    public void ToSemanticError_uses_the_typed_code_and_rethrows_unknowns()
    {
        var mapped = SupportOperation.ToSemanticError(
            new ContractOperationException(SupportErrorCodes.TicketNotFound),
            SupportErrorCodes.Missing);
        Assert.Equal(SupportErrorCodes.Missing, mapped.Code);

        Assert.Throws<ContractOperationException>(() =>
            SupportOperation.ToSemanticError(
                new ContractOperationException("support.unknown.future_code"),
                SupportErrorCodes.Rejected));
    }

    [Fact]
    public async Task Semantic_exceptions_map_through_their_own_error()
    {
        var result = await SupportOperation.ExecuteAsync<int>(
            () => throw new SemanticException(new SemanticError(SupportErrorCodes.Rejected)));
        Assert.True(result.IsFailure);
        Assert.Equal(SupportErrorCodes.Rejected, result.Errors[0].Code);
    }
}
