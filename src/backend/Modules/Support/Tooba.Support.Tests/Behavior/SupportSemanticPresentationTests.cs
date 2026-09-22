using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Errors;
using Xunit;

namespace Tooba.Support.Tests.Behavior;

public sealed class SupportSemanticPresentationTests
{
    [Fact]
    public void Stable_directory_codes_map_to_public_outcome()
    {
        var rejected = SupportExceptionMapper.ToSemanticError(
            new InvalidOperationException("support.category_invalid"),
            SupportErrorCodes.Rejected);
        Assert.Equal(SupportErrorCodes.Rejected, rejected.Code);

        var reply = SupportExceptionMapper.ToSemanticError(
            new InvalidOperationException("support.reply_closed"),
            SupportErrorCodes.ReplyRejected);
        Assert.Equal(SupportErrorCodes.ReplyRejected, reply.Code);

        var action = SupportExceptionMapper.ToSemanticError(
            new InvalidOperationException("support.close_not_allowed"),
            SupportErrorCodes.ActionRejected);
        Assert.Equal(SupportErrorCodes.ActionRejected, action.Code);

        var patch = SupportExceptionMapper.ToSemanticError(
            new InvalidOperationException("support.status_invalid"),
            SupportErrorCodes.PatchRejected);
        Assert.Equal(SupportErrorCodes.PatchRejected, patch.Code);
    }

    [Fact]
    public void Prose_and_unknown_codes_are_not_swallowed()
    {
        Assert.False(SupportExceptionMapper.TryMapExact("تیکت پیدا نشد.", SupportErrorCodes.Rejected, out _));
        Assert.False(SupportExceptionMapper.TryMapExact("support.unknown.future_code", SupportErrorCodes.Rejected, out _));
        Assert.Throws<InvalidOperationException>(() =>
            SupportExceptionMapper.ToSemanticError(
                new InvalidOperationException("پیدا نشد"),
                SupportErrorCodes.Rejected));
    }

    [Fact]
    public async Task Unexpected_InvalidOperationException_propagates_from_TryAsync()
    {
        var unknown = new InvalidOperationException("support.unknown.future_code");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SupportExceptionMapper.TryAsync<int>(() => throw unknown, SupportErrorCodes.Rejected));
    }

    [Fact]
    public async Task Known_InvalidOperationException_maps_in_TryAsync()
    {
        var result = await SupportExceptionMapper.TryAsync<int>(
            () => throw new InvalidOperationException("support.ticket_not_found"),
            SupportErrorCodes.ReplyRejected);
        Assert.True(result.IsFailure);
        Assert.Equal(SupportErrorCodes.ReplyRejected, result.Errors[0].Code);
    }
}
