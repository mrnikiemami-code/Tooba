using Microsoft.EntityFrameworkCore;
using Tooba.Shipping.Application.Scheduling.Commands;
using Tooba.Shipping.Application.Validation;
using Tooba.Shipping.Contracts.Admin;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Scheduling;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.Shipping.Infrastructure.Scheduling;
using Xunit;

namespace Tooba.Shipping.Tests.Scheduling;

/// <summary>
/// TB-SHIPPING-FULL-OPERATIONS-071 — authorized admin authoring of the weekly operating schedule:
/// real persistence roundtrip, stable refusals, idempotent upsert and fail-closed empty read.
/// </summary>
public sealed class ShippingOperatingScheduleAdminTests
{
    private static ShippingOperatingScheduleDirectory Directory(out ShippingDbContext context)
    {
        var options = new DbContextOptionsBuilder<ShippingDbContext>()
            .UseInMemoryDatabase($"shipping-071-{Guid.NewGuid():N}")
            .Options;
        context = new ShippingDbContext(options);
        return new ShippingOperatingScheduleDirectory(context);
    }

    private static ShippingOperatingScheduleWriteRequest ScheduleRequest(
        IReadOnlyList<ShippingOperatingWindowWrite>? windows = null,
        string? optionCode = "standard",
        string timeZoneId = "Asia/Tehran") =>
        new(
            "tipax",
            timeZoneId,
            windows ??
            [
                new ShippingOperatingWindowWrite(7, new TimeOnly(13, 0), new TimeOnly(14, 0)),
                new ShippingOperatingWindowWrite(7, new TimeOnly(16, 30), new TimeOnly(18, 0), 25),
            ],
            optionCode);

    [Fact]
    public async Task Create_read_update_deactivate_roundtrip_through_the_real_context()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var created = await directory.UpsertAsync("tipax", ScheduleRequest(), default);
        Assert.True(created.IsSuccess);
        Assert.True(created.Value.IsActive);
        Assert.Equal("Asia/Tehran", created.Value.TimeZoneId);
        Assert.Equal("standard", created.Value.OptionCode);
        Assert.Equal(2, created.Value.Windows.Count);
        Assert.Equal(new TimeOnly(13, 0), created.Value.Windows[0].StartTime);
        Assert.Equal(new TimeOnly(14, 0), created.Value.Windows[0].EndTime);
        Assert.Equal(25, created.Value.ConfiguredCapacityTotal);

        var scheduleId = created.Value.ScheduleId;

        var read = await directory.ListAsync("tipax", default);
        Assert.True(read.IsSuccess);
        var only = Assert.Single(read.Value.Schedules);
        Assert.Equal(scheduleId, only.ScheduleId);
        Assert.Equal(2, only.Windows.Count);

        // The same identity upserts in place instead of accumulating a second active schedule.
        var updated = await directory.UpsertAsync(
            "tipax",
            ScheduleRequest([
                new ShippingOperatingWindowWrite(7, new TimeOnly(13, 0), new TimeOnly(14, 0)),
            ]),
            default);
        Assert.True(updated.IsSuccess);
        Assert.Equal(scheduleId, updated.Value.ScheduleId);
        Assert.Single(updated.Value.Windows);
        Assert.Null(updated.Value.ConfiguredCapacityTotal);

        var deactivated = await directory.SetActiveAsync(scheduleId, "tipax", false, default);
        Assert.True(deactivated.IsSuccess);
        Assert.False(deactivated.Value.IsActive);

        var reloaded = await directory.ListAsync("tipax", default);
        Assert.False(Assert.Single(reloaded.Value.Schedules).IsActive);
    }

    [Fact]
    public async Task Replaying_the_same_body_is_idempotent()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var first = await directory.UpsertAsync("tipax", ScheduleRequest(), default);
        var second = await directory.UpsertAsync("tipax", ScheduleRequest(), default);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.ScheduleId, second.Value.ScheduleId);
        Assert.Equal(2, second.Value.Windows.Count);

        var read = await directory.ListAsync("tipax", default);
        Assert.Single(read.Value.Schedules);
        Assert.Equal(2, read.Value.Schedules[0].Windows.Count);
    }

    [Fact]
    public async Task Service_level_and_option_schedules_coexist_without_leaking()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        Assert.True((await directory.UpsertAsync("tipax", ScheduleRequest(optionCode: "standard"), default)).IsSuccess);
        Assert.True((await directory.UpsertAsync("tipax", ScheduleRequest(optionCode: "express"), default)).IsSuccess);
        Assert.True((await directory.UpsertAsync("tipax", ScheduleRequest(optionCode: null), default)).IsSuccess);

        var read = await directory.ListAsync("tipax", default);
        Assert.True(read.IsSuccess);
        Assert.Equal(3, read.Value.Schedules.Count);

        // Upserting the express schedule again must not touch the service-level or standard row.
        var express = read.Value.Schedules.Single(x => x.OptionCode == "express");
        var again = await directory.UpsertAsync("tipax", ScheduleRequest(optionCode: "express"), default);
        Assert.Equal(express.ScheduleId, again.Value.ScheduleId);

        var after = await directory.ListAsync("tipax", default);
        Assert.Equal(3, after.Value.Schedules.Count);
    }

    [Fact]
    public async Task Overlapping_and_reversed_windows_are_denied_with_stable_codes()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var overlap = await directory.UpsertAsync(
            "tipax",
            ScheduleRequest([
                new ShippingOperatingWindowWrite(7, new TimeOnly(13, 0), new TimeOnly(15, 0)),
                new ShippingOperatingWindowWrite(7, new TimeOnly(14, 0), new TimeOnly(16, 0)),
            ]),
            default);
        Assert.True(overlap.IsFailure);
        Assert.Equal(ShippingErrorCodes.ScheduleWindowOverlap, overlap.FirstError.Code);

        var reversed = await directory.UpsertAsync(
            "tipax",
            ScheduleRequest([
                new ShippingOperatingWindowWrite(7, new TimeOnly(14, 0), new TimeOnly(13, 0)),
            ]),
            default);
        Assert.True(reversed.IsFailure);
        Assert.Equal(ShippingErrorCodes.ScheduleWindowRangeInvalid, reversed.FirstError.Code);

        // Nothing partial was written by the refused calls.
        var read = await directory.ListAsync("tipax", default);
        Assert.Empty(read.Value.Schedules);
    }

    [Fact]
    public async Task An_unresolvable_timezone_is_denied_with_a_stable_code()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var result = await directory.UpsertAsync("tipax", ScheduleRequest(timeZoneId: "Nowhere/Fake"), default);
        Assert.True(result.IsFailure);
        Assert.Equal(ShippingErrorCodes.ScheduleTimeZoneInvalid, result.FirstError.Code);
    }

    [Fact]
    public async Task Missing_schedule_and_cross_service_toggle_are_denied_with_stable_codes()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var missing = await directory.SetActiveAsync(Guid.NewGuid(), "tipax", false, default);
        Assert.True(missing.IsFailure);
        Assert.Equal(ShippingErrorCodes.ScheduleNotFound, missing.FirstError.Code);

        var created = await directory.UpsertAsync("tipax", ScheduleRequest(), default);
        Assert.True(created.IsSuccess);

        var foreign = await directory.SetActiveAsync(created.Value.ScheduleId, "post", false, default);
        Assert.True(foreign.IsFailure);
        Assert.Equal(ShippingErrorCodes.ServiceScopeMismatch, foreign.FirstError.Code);

        // The untouched row is still owned by, and still active for, the original service.
        var reloaded = await directory.ListAsync("tipax", default);
        Assert.True(Assert.Single(reloaded.Value.Schedules).IsActive);
    }

    [Fact]
    public async Task An_empty_schedule_set_is_a_real_answer_never_a_default()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var view = await directory.ListAsync("tipax", default);
        Assert.True(view.IsSuccess);
        Assert.Empty(view.Value.Schedules);
    }

    [Fact]
    public async Task Missing_service_code_is_denied_with_a_stable_code()
    {
        var directory = Directory(out var context);
        await using var _ = context;

        var read = await directory.ListAsync("  ", default);
        Assert.True(read.IsFailure);
        Assert.Equal(ShippingErrorCodes.ServiceCodeRequired, read.FirstError.Code);
    }

    [Fact]
    public void Transport_shape_invalid_requests_are_denied_with_stable_codes()
    {
        var validator = new ShippingOperatingScheduleWriteRequestValidator();

        Assert.Contains(
            validator.Validate(ScheduleRequest(windows: [
                new ShippingOperatingWindowWrite(0, new TimeOnly(13, 0), new TimeOnly(14, 0)),
            ])).Errors,
            e => e.ErrorCode == ShippingScheduleValidationCodes.WeekdayOutOfRange);

        Assert.Contains(
            validator.Validate(ScheduleRequest(windows: [
                new ShippingOperatingWindowWrite(7, new TimeOnly(14, 0), new TimeOnly(14, 0)),
            ])).Errors,
            e => e.ErrorCode == ShippingScheduleValidationCodes.WindowRangeInvalid);

        Assert.Contains(
            validator.Validate(ScheduleRequest(windows: [
                new ShippingOperatingWindowWrite(7, new TimeOnly(13, 0), new TimeOnly(14, 0), 0),
            ])).Errors,
            e => e.ErrorCode == ShippingScheduleValidationCodes.WindowCapacityInvalid);

        Assert.Contains(
            validator.Validate(ScheduleRequest(windows: [
                new ShippingOperatingWindowWrite(7, new TimeOnly(13, 0), new TimeOnly(15, 0)),
                new ShippingOperatingWindowWrite(7, new TimeOnly(14, 0), new TimeOnly(16, 0)),
            ])).Errors,
            e => e.ErrorCode == ShippingScheduleValidationCodes.WindowOverlap);

        Assert.Contains(
            validator.Validate(ScheduleRequest(timeZoneId: " ")).Errors,
            e => e.ErrorCode == ShippingScheduleValidationCodes.TimeZoneRequired);

        Assert.Contains(
            validator.Validate(ScheduleRequest() with { ServiceCode = " " }).Errors,
            e => e.ErrorCode == ShippingScheduleValidationCodes.ServiceCodeRequired);

        Assert.Contains(
            validator.Validate(ScheduleRequest() with { Windows = null! }).Errors,
            e => e.ErrorCode == ShippingScheduleValidationCodes.WindowsRequired);
    }

    [Fact]
    public void Body_service_code_cannot_bypass_the_route_service_code()
    {
        var validator = new UpsertShippingOperatingScheduleCommandValidator();

        Assert.True(validator.Validate(
            new UpsertShippingOperatingScheduleCommand("tipax", ScheduleRequest())).IsValid);

        var bodyOverride = new UpsertShippingOperatingScheduleCommand(
            "tipax",
            ScheduleRequest() with { ServiceCode = "post" });
        var failure = validator.Validate(bodyOverride);
        Assert.False(failure.IsValid);
        Assert.Contains(failure.Errors, e => e.ErrorCode == ShippingValidationCodes.BodyServiceCodeMismatch);
    }

    [Fact]
    public async Task Handler_refuses_an_unknown_option_before_any_write()
    {
        var directory = Directory(out var context);
        await using var _ = context;
        var handler = new UpsertShippingOperatingScheduleHandler(directory, new RejectingOptionValidator());

        var result = await handler.Handle(
            new UpsertShippingOperatingScheduleCommand("tipax", ScheduleRequest(optionCode: "drone")),
            default);

        Assert.True(result.IsFailure);
        Assert.Equal(ShippingErrorCodes.ServiceOptionUnknown, result.FirstError.Code);

        // Fail-closed: a rejected method identity must not leave a row behind.
        var read = await directory.ListAsync("tipax", default);
        Assert.Empty(read.Value.Schedules);
    }

    private sealed class RejectingOptionValidator : Application.Rates.Ports.IShippingMethodOptionValidator
    {
        public Task<BuildingBlocks.Results.Result<bool>> ValidateAsync(
            string serviceCode,
            string? optionCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(BuildingBlocks.Results.Result.Failure<bool>(
                new BuildingBlocks.SemanticError(ShippingErrorCodes.ServiceOptionUnknown)));
    }
}
