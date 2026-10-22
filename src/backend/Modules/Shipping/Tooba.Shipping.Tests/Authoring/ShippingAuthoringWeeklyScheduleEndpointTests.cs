using System.Globalization;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.DependencyInjection;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Results;
using Tooba.BuildingBlocks.Security;
using Tooba.Shipping.Application.Authoring.Commands;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Application.Rates.Ports;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Endpoints;
using Tooba.Shipping.Endpoints.Admin;
using Tooba.Shipping.Infrastructure.Authoring;
using Tooba.Shipping.Infrastructure.Persistence;
using Tooba.StoreContext.Contracts.Current;
using Xunit;

namespace Tooba.Shipping.Tests.Authoring;

/// <summary>
/// TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193 — the owner-scoped per-weekday operating schedule,
/// executed through the REAL HTTP delegates.
/// </summary>
/// <remarks>
/// <para>
/// Every fact composes the production entry (<see cref="ShippingEndpointModule"/>), the real
/// <see cref="ShippingAuthoringOwnerAuthorizer"/>, the real body reader, the real MediatR validation
/// pipeline, the real schedule query/command handlers and the real
/// <see cref="ShippingAuthoringDraftDirectory"/> over the real <see cref="ShippingDbContext"/>. Only the
/// platform authorization eddges and the destination/option seams are controlled fixtures.
/// </para>
/// <para>
/// <b>Dispatch vs execution.</b> <see cref="Harness.SentCount"/> counts real <c>ISender</c> dispatch and
/// <see cref="Harness.HandlerReached"/> counts arrival past validation. A denied request asserts both are
/// zero; a malformed body may dispatch but must never reach a handler.
/// </para>
/// </remarks>
[Collection(nameof(Tooba.Shipping.Tests.Observability.ShippingModuleCallTracingCollection))]
public sealed class ShippingAuthoringWeeklyScheduleEndpointTests
{
    private const string AdminDrafts = "/v1/admin/shipping-services/authoring-drafts";
    private const string SellerDrafts = "/v1/seller/shipping-services/authoring-drafts";
    private static readonly Guid Iran = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Tehran = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Isfahan = Guid.Parse("44444444-4444-4444-4444-444444444444");

    // ISO identity: 1=Monday … 5=Friday, 6=Saturday, 7=Sunday.
    private const int Monday = 1;
    private const int Friday = 5;
    private const int Saturday = 6;
    private const int Sunday = 7;

    [Fact]
    public async Task Unconfigured_draft_returns_no_fabricated_week_and_a_full_week_roundtrips_through_real_admin_routes()
    {
        using var harness = Harness.Create("week-http-a");
        var draftId = await harness.CreateDraftAsync(Tehran);
        var path = $"{AdminDrafts}/{draftId}/weekly-schedule";

        // Absent schedule is explicitly UNCONFIGURED: no timezone, no hours, no seven default days.
        var (initialStatus, _, initial) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.Equal(StatusCodes.Status200OK, initialStatus);
        Assert.False(initial.GetProperty("isConfigured").GetBoolean());
        Assert.Equal(JsonValueKind.Null, initial.GetProperty("timeZoneId").ValueKind);
        Assert.Empty(initial.GetProperty("days").EnumerateArray());
        Assert.Equal(1, initial.GetProperty("revision").GetInt64());

        // Independent Saturday/Sunday hours, Friday closed but remembering an explicit pair.
        var (putStatus, _, configured) = await harness.SendAsync(
            HttpMethod.Put,
            path,
            Week(
                1,
                "Asia/Tehran",
                Day(Monday, true, "09:00:00", "17:00:00"),
                Day(Friday, false, "10:00:00", "12:00:00"),
                Day(Saturday, true, "09:00:00", "13:00:00"),
                Day(Sunday, true, "15:00:00", "18:00:00")));
        Assert.True(putStatus == StatusCodes.Status200OK, configured.ToString());
        Assert.Equal(2, configured.GetProperty("revision").GetInt64());
        Assert.True(configured.GetProperty("isConfigured").GetBoolean());
        Assert.Equal("Asia/Tehran", configured.GetProperty("timeZoneId").GetString());
        Assert.Equal(7, configured.GetProperty("days").GetArrayLength());

        var (_, _, read) = await harness.SendAsync(HttpMethod.Get, path);
        var days = read.GetProperty("days").EnumerateArray()
            .ToDictionary(day => day.GetProperty("weekday").GetInt32());
        Assert.Equal(7, days.Count);
        Assert.True(days[Saturday].GetProperty("isWorking").GetBoolean());
        Assert.Equal("09:00:00", days[Saturday].GetProperty("startTime").GetString());
        Assert.Equal("13:00:00", days[Saturday].GetProperty("endTime").GetString());
        Assert.Equal("15:00:00", days[Sunday].GetProperty("startTime").GetString());
        Assert.False(days[Friday].GetProperty("isWorking").GetBoolean());
        // A closed day still returns its remembered hours truthfully, so the UI can reuse them.
        Assert.Equal("10:00:00", days[Friday].GetProperty("startTime").GetString());
        Assert.Equal("12:00:00", days[Friday].GetProperty("endTime").GetString());
        // A closed day that never had hours stays honestly empty.
        Assert.Equal(JsonValueKind.Null, days[Tuesday].GetProperty("startTime").ValueKind);

        // Re-opening Friday without hours is refused: remembered hours are not an active window.
        var (reopenStatus, _, reopen) = await harness.SendAsync(
            HttpMethod.Put,
            path,
            Week(2, "Asia/Tehran", Day(Friday, true)));
        Assert.Equal(StatusCodes.Status400BadRequest, reopenStatus);
        Assert.Equal("validation.failed", reopen.GetProperty("errorCode").GetString());
        Assert.Contains(
            "shipping.validation.schedule_day_entry_invalid",
            reopen.GetRawText(),
            StringComparison.Ordinal);

        // Editing one day never overwrites another: only Sunday changes and revision moves once.
        var (editStatus, _, edited) = await harness.SendAsync(
            HttpMethod.Put,
            path,
            Week(
                2,
                "Asia/Tehran",
                Day(Monday, true, "09:00:00", "17:00:00"),
                Day(Friday, false, "10:00:00", "12:00:00"),
                Day(Saturday, true, "09:00:00", "13:00:00"),
                Day(Sunday, true, "16:00:00", "19:00:00")));
        Assert.Equal(StatusCodes.Status200OK, editStatus);
        Assert.Equal(3, edited.GetProperty("revision").GetInt64());
        var editedDays = edited.GetProperty("days").EnumerateArray()
            .ToDictionary(day => day.GetProperty("weekday").GetInt32());
        Assert.Equal("16:00:00", editedDays[Sunday].GetProperty("startTime").GetString());
        Assert.Equal("09:00:00", editedDays[Saturday].GetProperty("startTime").GetString());

        // An identical PUT is a truthful no-op: revision and timestamp are preserved.
        var (_, _, stampBefore) = await harness.SendAsync(HttpMethod.Get, $"{AdminDrafts}/{draftId}");
        var noOpJson = Week(
            3,
            "Asia/Tehran",
            Day(Monday, true, "09:00:00", "17:00:00"),
            Day(Friday, false, "10:00:00", "12:00:00"),
            Day(Saturday, true, "09:00:00", "13:00:00"),
            Day(Sunday, true, "16:00:00", "19:00:00"));
        var (noOpStatus, _, noOp) = await harness.SendAsync(HttpMethod.Put, path, noOpJson);
        Assert.Equal(StatusCodes.Status200OK, noOpStatus);
        Assert.Equal(3, noOp.GetProperty("revision").GetInt64());
        var (_, _, stampAfter) = await harness.SendAsync(HttpMethod.Get, $"{AdminDrafts}/{draftId}");
        Assert.Equal(stampBefore.GetProperty("updatedAt").GetString(), stampAfter.GetProperty("updatedAt").GetString());

        // A stale root revision is the canonical conflict and changes nothing.
        var staleJson = Week(
            2,
            "Asia/Tehran",
            Day(Monday, true, "09:00:00", "17:00:00"),
            Day(Friday, false, "10:00:00", "12:00:00"),
            Day(Saturday, true, "09:00:00", "13:00:00"),
            Day(Sunday, true, "16:00:00", "19:00:00"));
        var (staleStatus, _, stale) = await harness.SendAsync(HttpMethod.Put, path, staleJson);
        Assert.Equal(StatusCodes.Status409Conflict, staleStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftRevisionConflict, stale.GetProperty("errorCode").GetString());

        // The draft binding and its region tree are untouched by schedule writes.
        var (_, _, draft) = await harness.SendAsync(HttpMethod.Get, $"{AdminDrafts}/{draftId}");
        Assert.Equal("post", draft.GetProperty("serviceCode").GetString());
        Assert.Single(draft.GetProperty("regionGroups").EnumerateArray());
        Assert.Equal(3, draft.GetProperty("revision").GetInt64());
    }

    [Fact]
    public async Task All_closed_week_is_configuration_and_interface_locale_never_changes_stored_identity()
    {
        using var harness = Harness.Create("week-http-b");
        var draftId = await harness.CreateDraftAsync(Tehran);
        var path = $"{AdminDrafts}/{draftId}/weekly-schedule";

        var allClosed = Week(1, "Asia/Tehran", Day(Monday, false), Day(Friday, false), Day(Saturday, false), Day(Sunday, false));
        var (status, _, configured) = await harness.SendAsync(HttpMethod.Put, path, allClosed);
        Assert.Equal(StatusCodes.Status200OK, status);
        Assert.True(configured.GetProperty("isConfigured").GetBoolean());
        Assert.Equal(7, configured.GetProperty("days").GetArrayLength());
        Assert.All(
            configured.GetProperty("days").EnumerateArray(),
            day => Assert.False(day.GetProperty("isWorking").GetBoolean()));

        // A different interface culture / Accept-Language must not move a weekday, a timezone or an hour.
        var before = await harness.SendAsync(HttpMethod.Get, path);
        harness.AcceptLanguage = "en-US";
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var (_, _, after) = await harness.SendAsync(HttpMethod.Get, path);
            Assert.Equal(
                before.Body.GetProperty("timeZoneId").GetString(),
                after.GetProperty("timeZoneId").GetString());
            Assert.Equal(
                before.Body.GetProperty("days").GetRawText(),
                after.GetProperty("days").GetRawText());
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        // An unresolvable timezone is rejected instead of falling back to a locale-derived default.
        var (badZoneStatus, _, badZone) = await harness.SendAsync(
            HttpMethod.Put,
            path,
            Week(2, "Mars/Olympus", Day(Monday, true, "09:00:00", "17:00:00")));
        Assert.Equal(StatusCodes.Status400BadRequest, badZoneStatus);
        Assert.Equal(ShippingErrorCodes.ScheduleTimeZoneInvalid, badZone.GetProperty("errorCode").GetString());

        // A blank timezone is its own stable error, never an implicit Asia/Tehran.
        var (blankZoneStatus, _, blankZone) = await harness.SendAsync(
            HttpMethod.Put,
            path,
            Week(2, "  ", Day(Monday, true, "09:00:00", "17:00:00")));
        Assert.Equal(StatusCodes.Status400BadRequest, blankZoneStatus);
        Assert.Equal("validation.failed", blankZone.GetProperty("errorCode").GetString());
        Assert.Contains(
            "shipping.validation.schedule_timezone_id_invalid",
            blankZone.GetRawText(),
            StringComparison.Ordinal);

        // Nothing above moved the stored configuration.
        var (_, _, unchanged) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.Equal(2, unchanged.GetProperty("revision").GetInt64());
        Assert.Equal("Asia/Tehran", unchanged.GetProperty("timeZoneId").GetString());
    }

    [Theory]
    [InlineData("missing-collection", "shipping.validation.schedule_days_required")]
    [InlineData("missing-flag", "shipping.validation.schedule_day_entry_invalid")]
    [InlineData("null-element", "shipping.validation.schedule_day_entry_invalid")]
    [InlineData("six-days", "shipping.validation.schedule_day_entry_invalid")]
    [InlineData("duplicate-day", "shipping.validation.schedule_day_entry_invalid")]
    [InlineData("out-of-range", "shipping.validation.schedule_day_entry_invalid")]
    [InlineData("inverted", "shipping.validation.schedule_day_entry_invalid")]
    [InlineData("equal", "shipping.validation.schedule_day_entry_invalid")]
    [InlineData("overnight", "shipping.validation.schedule_day_entry_invalid")]
    [InlineData("one-bound", "shipping.validation.schedule_day_entry_invalid")]
    [InlineData("working-without-times", "shipping.validation.schedule_day_entry_invalid")]
    public async Task Malformed_week_payloads_fail_before_mutation_and_leave_the_draft_untouched(
        string scenario,
        string expectedCode)
    {
        using var harness = Harness.Create("week-http-" + scenario);
        var draftId = await harness.CreateDraftAsync(Tehran);
        var path = $"{AdminDrafts}/{draftId}/weekly-schedule";
        var json = scenario switch
        {
            "missing-collection" => "{\"expectedRevision\":1,\"timeZoneId\":\"Asia/Tehran\"}",
            "missing-flag" => Week(1, "Asia/Tehran", DayWithoutFlag(Monday), Day(Tuesday, true, "09:00:00", "17:00:00")),
            "null-element" => Week(1, "Asia/Tehran", "null", Day(Monday, true, "09:00:00", "17:00:00")),
            "six-days" => "{\"expectedRevision\":1,\"timeZoneId\":\"Asia/Tehran\",\"days\":["
                            + Day(Monday, true, "09:00:00", "17:00:00") + ","
                            + Day(Tuesday, false) + ","
                            + Day(3, false) + ","
                            + Day(4, false) + ","
                            + Day(Friday, false) + ","
                            + Day(Saturday, false) + "]}",
            "duplicate-day" => Week(
                1,
                "Asia/Tehran",
                Day(Monday, true, "09:00:00", "17:00:00"),
                Day(Monday, true, "09:00:00", "17:00:00")),
            "out-of-range" => Week(
                1,
                "Asia/Tehran",
                Day(8, true, "09:00:00", "17:00:00"),
                Day(Monday, true, "09:00:00", "17:00:00")),
            "inverted" => Week(1, "Asia/Tehran", Day(Tuesday, true, "17:00:00", "09:00:00")),
            "equal" => Week(1, "Asia/Tehran", Day(Tuesday, true, "09:00:00", "09:00:00")),
            "overnight" => Week(1, "Asia/Tehran", Day(Tuesday, true, "22:00:00", "02:00:00")),
            "one-bound" => Week(1, "Asia/Tehran", Day(Tuesday, false, "09:00:00")),
            _ => Week(1, "Asia/Tehran", Day(Tuesday, true)),
        };

        var (status, _, body) = await harness.SendAsync(HttpMethod.Put, path, json);
        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.Equal("validation.failed", body.GetProperty("errorCode").GetString());
        Assert.Contains(expectedCode, body.GetRawText(), StringComparison.Ordinal);
        Assert.True(harness.SentCount > 0);
        Assert.Equal(0, harness.HandlerReached);

        // The rejected payload left the same aggregate/context, revision and timestamp unchanged.
        var (_, _, draft) = await harness.SendAsync(HttpMethod.Get, $"{AdminDrafts}/{draftId}");
        Assert.Equal(1, draft.GetProperty("revision").GetInt64());
        var (_, _, read) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.False(read.GetProperty("isConfigured").GetBoolean());
        Assert.Empty(read.GetProperty("days").EnumerateArray());
    }

    [Fact]
    public async Task Invalid_seventh_day_keeps_the_previous_week_and_later_save_publishes_nothing()
    {
        using var harness = Harness.Create("week-http-c");
        var draftId = await harness.CreateDraftAsync(Tehran);
        var path = $"{AdminDrafts}/{draftId}/weekly-schedule";
        var valid = Week(
            1,
            "Asia/Tehran",
            Day(Monday, true, "09:00:00", "17:00:00"),
            Day(Saturday, true, "09:00:00", "13:00:00"));
        Assert.Equal(StatusCodes.Status200OK, (await harness.SendAsync(HttpMethod.Put, path, valid)).Status);
        var (revision, updatedAt) = await harness.ReadDraftStampAsync(draftId);

        // The last day of the array is the invalid one: the whole week must still be refused, atomically.
        var invalid = Week(
            2,
            "Asia/Tehran",
            Day(Monday, true, "08:00:00", "16:00:00"),
            Day(Saturday, true, "08:00:00", "12:00:00"),
            Day(Sunday, true, "20:00:00", "07:00:00"));
        var (status, _, body) = await harness.SendAsync(HttpMethod.Put, path, invalid);
        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.Equal("validation.failed", body.GetProperty("errorCode").GetString());
        Assert.Contains(
            "shipping.validation.schedule_day_entry_invalid",
            body.GetRawText(),
            StringComparison.Ordinal);

        var (_, _, read) = await harness.SendAsync(HttpMethod.Get, path);
        var days = read.GetProperty("days").EnumerateArray()
            .ToDictionary(day => day.GetProperty("weekday").GetInt32());
        Assert.Equal("09:00:00", days[Monday].GetProperty("startTime").GetString());
        Assert.Equal("09:00:00", days[Saturday].GetProperty("startTime").GetString());
        Assert.Equal(2, read.GetProperty("revision").GetInt64());

        // A later unrelated save in the SAME context cannot leak the refused edit.
        await harness.WithContextAsync(async context =>
        {
            var draft = await context.AuthoringDrafts.SingleAsync(item => item.DraftId == draftId);
            draft.ReplaceRegionGroups([], DateTimeOffset.UtcNow);
            await context.SaveChangesAsync();
        });
        var (afterRevision, _) = await harness.ReadDraftStampAsync(draftId);
        Assert.Equal(revision + 1, afterRevision);
        var (_, _, after) = await harness.SendAsync(HttpMethod.Get, path);
        Assert.Equal("09:00:00", after.GetProperty("days").EnumerateArray()
            .Single(day => day.GetProperty("weekday").GetInt32() == Monday)
            .GetProperty("startTime").GetString());
        Assert.True(afterRevision >= revision);
        _ = updatedAt;
    }

    [Fact]
    public async Task View_only_put_is_denied_before_dispatch_and_foreign_or_missing_drafts_share_one_safe_404()
    {
        using var reader = Harness.Create("week-http-d");
        var draftId = await reader.CreateDraftAsync(Tehran);
        var path = $"{AdminDrafts}/{draftId}/weekly-schedule";
        var week = Week(1, "Asia/Tehran", Day(Monday, true, "09:00:00", "17:00:00"));

        using var viewOnly = Harness.Create("week-http-e", adminAllowed: [ShippingAdminPermissions.View]);
        var (deniedStatus, _, denied) = await viewOnly.SendAsync(HttpMethod.Put, path, week);
        Assert.Equal(StatusCodes.Status403Forbidden, deniedStatus);
        Assert.Equal(0, viewOnly.SentCount);
        Assert.Equal(0, viewOnly.HandlerReached);
        Assert.False(string.IsNullOrEmpty(denied.GetProperty("errorCode").GetString()));

        // A foreign owner's draft and an invented id share the exact same safe response.
        using var foreign = Harness.Create(
            "week-http-f",
            tenantId: "store-b");
        var (foreignStatus, _, foreignBody) = await foreign.SendAsync(HttpMethod.Get, path);
        Assert.Equal(StatusCodes.Status404NotFound, foreignStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftNotFound, foreignBody.GetProperty("errorCode").GetString());
        var (foreignPutStatus, _, foreignPut) = await foreign.SendAsync(HttpMethod.Put, path, week);
        Assert.Equal(StatusCodes.Status404NotFound, foreignPutStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftNotFound, foreignPut.GetProperty("errorCode").GetString());
        var (missingStatus, _, missing) = await foreign.SendAsync(
            HttpMethod.Get,
            $"{AdminDrafts}/{Guid.NewGuid()}/weekly-schedule");
        Assert.Equal(StatusCodes.Status404NotFound, missingStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringDraftNotFound, missing.GetProperty("errorCode").GetString());

        // The legitimate owner's week is still unconfigured: a denied/foreign write changed nothing.
        var (_, _, read) = await reader.SendAsync(HttpMethod.Get, path);
        Assert.False(read.GetProperty("isConfigured").GetBoolean());
    }

    [Fact]
    public async Task Marketplace_seller_roundtrips_the_same_use_case_and_single_store_seller_is_unavailable()
    {
        using var seller = Harness.Create(
            "week-http-g",
            edition: ToobaEdition.Marketplace,
            marketplace: true,
            tenantId: null);
        var draftId = await seller.CreateSellerDraftAsync(Tehran);
        var path = $"{SellerDrafts}/{draftId}/weekly-schedule";

        var (putStatus, _, configured) = await seller.SendAsync(
            HttpMethod.Put,
            path,
            Week(1, "Asia/Tehran", Day(Saturday, true, "10:00:00", "14:00:00")));
        Assert.Equal(StatusCodes.Status200OK, putStatus);
        Assert.Equal("Asia/Tehran", configured.GetProperty("timeZoneId").GetString());
        Assert.Equal(
            "10:00:00",
            configured.GetProperty("days").EnumerateArray()
                .Single(day => day.GetProperty("weekday").GetInt32() == Saturday)
                .GetProperty("startTime").GetString());
        Assert.True(seller.SellerCalls > 0);
        Assert.Equal(StatusCodes.Status200OK, (await seller.SendAsync(HttpMethod.Get, path)).Status);

        // A scoped seller grant is refused BEFORE draft resolution or dispatch.
        using var denied = Harness.Create(
            "week-http-h",
            edition: ToobaEdition.Marketplace,
            marketplace: true,
            tenantId: null,
            sellerGrants: [new PlatformPermissionGrant(
                ShippingAdminPermissions.Manage,
                PlatformAccessScopeKind.Category,
                Guid.NewGuid(),
                false)]);
        var (deniedStatus, _, deniedBody) = await denied.SendAsync(
            HttpMethod.Put,
            $"{SellerDrafts}/{Guid.NewGuid()}/weekly-schedule",
            Week(1, "Asia/Tehran", Day(Monday, true, "09:00:00", "17:00:00")));
        Assert.Equal(StatusCodes.Status403Forbidden, deniedStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringOwnerScopeDenied, deniedBody.GetProperty("errorCode").GetString());
        Assert.Equal(0, denied.SentCount);

        // SingleStore seller routes stay canonically unavailable before any seller resolution.
        using var singleStore = Harness.Create("week-http-i");
        var adminDraft = await singleStore.CreateDraftAsync(Tehran);
        var (singleStatus, _, singleBody) = await singleStore.SendAsync(
            HttpMethod.Get,
            $"{SellerDrafts}/{adminDraft}/weekly-schedule");
        Assert.Equal(StatusCodes.Status404NotFound, singleStatus);
        Assert.Equal(ShippingErrorCodes.AuthoringOwnerScopeUnavailable, singleBody.GetProperty("errorCode").GetString());
        Assert.Equal(0, singleStore.SellerCalls);
        Assert.Equal(0, singleStore.SentCount);
    }

    [Fact]
    public async Task Domain_guards_reject_duplicate_missing_and_incoherent_days_even_without_transport_validation()
    {
        using var harness = Harness.Create("week-http-domain");
        var draftId = await harness.CreateDraftAsync(Tehran);
        var owner = ShippingAuthoringOwnerScope.ForStore("store-a");

        var duplicate = await harness.WithContextAsync(async context => await new ReplaceShippingAuthoringWeeklyScheduleHandler(
                new ShippingAuthoringDraftDirectory(context))
            .Handle(
                new ReplaceShippingAuthoringWeeklyScheduleCommand(
                    owner,
                    draftId,
                    new ShippingAuthoringWeeklyScheduleReplaceRequest(
                        1,
                        "Asia/Tehran",
                        [
                            new ShippingAuthoringScheduleDayWrite(Monday, true, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                            new ShippingAuthoringScheduleDayWrite(Monday, true, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                        ])),
                CancellationToken.None));
        Assert.True(duplicate.IsFailure);
        Assert.Equal(ShippingErrorCodes.AuthoringScheduleWeekdaySetInvalid, duplicate.Errors[0].Code);

        var sixDays = await harness.WithContextAsync(async context => await new ReplaceShippingAuthoringWeeklyScheduleHandler(
                new ShippingAuthoringDraftDirectory(context))
            .Handle(
                new ReplaceShippingAuthoringWeeklyScheduleCommand(
                    owner,
                    draftId,
                    new ShippingAuthoringWeeklyScheduleReplaceRequest(
                        1,
                        "Asia/Tehran",
                        [new ShippingAuthoringScheduleDayWrite(Monday, true, new TimeOnly(9, 0), new TimeOnly(17, 0))])),
                CancellationToken.None));
        Assert.Equal(ShippingErrorCodes.AuthoringScheduleWeekdaySetInvalid, sixDays.Errors[0].Code);

        var overnight = await harness.WithContextAsync(async context => await new ReplaceShippingAuthoringWeeklyScheduleHandler(
                new ShippingAuthoringDraftDirectory(context))
            .Handle(
                new ReplaceShippingAuthoringWeeklyScheduleCommand(
                    owner,
                    draftId,
                    new ShippingAuthoringWeeklyScheduleReplaceRequest(
                        1,
                        "Asia/Tehran",
                        Full(monday: (new TimeOnly(22, 0), new TimeOnly(2, 0))))),
                CancellationToken.None));
        Assert.Equal(ShippingErrorCodes.ScheduleWindowRangeInvalid, overnight.Errors[0].Code);

        var oneBound = await harness.WithContextAsync(async context => await new ReplaceShippingAuthoringWeeklyScheduleHandler(
                new ShippingAuthoringDraftDirectory(context))
            .Handle(
                new ReplaceShippingAuthoringWeeklyScheduleCommand(
                    owner,
                    draftId,
                    new ShippingAuthoringWeeklyScheduleReplaceRequest(
                        1,
                        "Asia/Tehran",
                        Full(monday: (new TimeOnly(9, 0), null)))),
                CancellationToken.None));
        Assert.Equal(ShippingErrorCodes.AuthoringScheduleDayInvalid, oneBound.Errors[0].Code);

        // A closed day without hours is legal on first configuration and stores no invented pair.
        var closedWeek = await harness.WithContextAsync(async context => await new ReplaceShippingAuthoringWeeklyScheduleHandler(
                new ShippingAuthoringDraftDirectory(context))
            .Handle(
                new ReplaceShippingAuthoringWeeklyScheduleCommand(
                    owner,
                    draftId,
                    new ShippingAuthoringWeeklyScheduleReplaceRequest(1, "Asia/Tehran", Full())),
                CancellationToken.None));
        Assert.True(closedWeek.IsSuccess, closedWeek.IsFailure ? closedWeek.Errors[0].Code : null);
        Assert.Equal(2, closedWeek.Value.Revision);
        Assert.Equal(7, closedWeek.Value.Days.Count);
        Assert.All(closedWeek.Value.Days, day => Assert.False(day.IsWorking));
        Assert.All(closedWeek.Value.Days, day => Assert.Null(day.StartTime));

        // None of the refused documents changed the revision or the stored week.
        var (revision, _) = await harness.ReadDraftStampAsync(draftId);
        Assert.Equal(2, revision);
    }

    private static IReadOnlyList<ShippingAuthoringScheduleDayWrite> Full(
        (TimeOnly? Start, TimeOnly? End)? monday = null)
        => Enumerable.Range(1, 7)
            .Select(weekday => weekday == Monday && monday is { } hours
                ? new ShippingAuthoringScheduleDayWrite(Monday, true, hours.Start, hours.End)
                : new ShippingAuthoringScheduleDayWrite(weekday, false))
            .ToArray();

    private const int Tuesday = 2;

    private static string Week(int revision, string timeZoneId, params string[] days)
    {
        var week = new List<string>(days);
        for (var weekday = 1; weekday <= 7; weekday++)
        {
            if (week.Any(entry => entry.Contains($"\"weekday\":{weekday},", StringComparison.Ordinal)))
            {
                continue;
            }

            week.Add(Day(weekday, false));
        }

        return string.Concat(
            "{\"expectedRevision\":",
            revision.ToString(CultureInfo.InvariantCulture),
            ",\"timeZoneId\":\"",
            timeZoneId,
            "\",\"days\":[",
            string.Join(',', week),
            "]}");
    }

    private static string Day(int weekday, bool isWorking, string? start = null, string? end = null)
        => DayCore(weekday, isWorking ? "true" : "false", start, end);

    private static string DayWithoutFlag(int weekday)
        => DayCore(weekday, null, null, null);

    private static string DayCore(int weekday, string? isWorking, string? start, string? end)
    {
        var builder = new StringBuilder("{\"weekday\":").Append(weekday.ToString(CultureInfo.InvariantCulture));
        if (isWorking is not null)
        {
            builder.Append(",\"isWorking\":").Append(isWorking);
        }

        if (start is not null)
        {
            builder.Append(",\"startTime\":\"").Append(start).Append('"');
        }

        if (end is not null)
        {
            builder.Append(",\"endTime\":\"").Append(end).Append('"');
        }

        return builder.Append('}').ToString();
    }

    private sealed class Harness : IDisposable
    {
        private readonly WebApplication _app;
        private readonly IServiceScope _scope;
        private readonly Counter _counter;

        private Harness(WebApplication app, IServiceScope scope, Counter counter, int sellerCalls)
        {
            _app = app;
            _scope = scope;
            _counter = counter;
            SellerCalls = sellerCalls;
        }

        public string AcceptLanguage { get; set; } = "fa";

        public int SellerCalls { get; private set; }

        public int SentCount => _counter.Sent;

        public int HandlerReached => _counter.Handler;

        public static Harness Create(
            string database,
            ToobaEdition edition = ToobaEdition.SingleStore,
            IReadOnlyList<string>? adminAllowed = null,
            IReadOnlyList<PlatformPermissionGrant>? sellerGrants = null,
            bool marketplace = false,
            string? tenantId = "store-a")
        {
            var counter = new Counter();
            var tenant = new StubCurrentTenant(marketplace ? null : Tenant(tenantId ?? "store-a"));
            var seller = new RecordingSellerAccess();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Production,
            });
            builder.Logging.ClearProviders();
            builder.Services.AddRouting();
            builder.Services.AddSingleton<IHostEnvironment>(new StubHostEnvironment());
            builder.Services.AddToobaObservabilityFoundation();
            builder.Services.AddShippingEndpointPresentation();
            // Outermost: counts every real Send, before validation.
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(SentCountingBehavior<,>));
            builder.Services.AddToobaCqrsFoundation(typeof(CreateShippingAuthoringDraftCommand).Assembly);
            // Behind validation: only a request that passes it is counted as reaching the handler.
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(HandlerReachedBehavior<,>));
            builder.Services.AddProblemDetails();
            builder.Services.AddSingleton<IExceptionPresentationService, ExceptionPresentationService>();
            builder.Services.AddDbContext<ShippingDbContext>(options => options.UseInMemoryDatabase(database));
            builder.Services.AddScoped<IShippingAuthoringDraftDirectory, ShippingAuthoringDraftDirectory>();
            builder.Services.AddSingleton<IShippingAdminAuthorizer>(
                new RecordingAdminAuthorizer(adminAllowed ?? [ShippingAdminPermissions.View, ShippingAdminPermissions.Manage]));
            builder.Services.AddSingleton<ISellerPanelAccess>(seller);
            builder.Services.AddSingleton<IPlatformEffectiveAccessReader>(
                new RecordingEffectiveAccess(sellerGrants ?? [
                    new PlatformPermissionGrant(ShippingAdminPermissions.View, PlatformAccessScopeKind.GlobalWithinOwner, null, false),
                    new PlatformPermissionGrant(ShippingAdminPermissions.Manage, PlatformAccessScopeKind.GlobalWithinOwner, null, false),
                ]));
            builder.Services.AddSingleton<ICurrentEdition>(
                new StubCurrentEdition(new EditionContext(edition, "deployment-test")));
            builder.Services.AddSingleton<ICurrentTenant>(tenant);
            builder.Services.AddSingleton<IShippingMethodOptionValidator>(new FakeMethodValidator());
            builder.Services.AddSingleton<IShippingAuthoringDestinationResolver>(new FakeResolver());
            builder.Services.AddSingleton(counter);

            var app = builder.Build();
            app.MapShippingEndpoints();
            var scope = app.Services.CreateScope();
            var harness = new Harness(app, scope, counter, 0);
            seller.OnCall = () => harness.SellerCalls++;
            return harness;
        }

        public static TenantContext Tenant(string tenantId) =>
            new(
                new TenantId(tenantId),
                TenantStatus.Active,
                new ConnectionReference("store-db"),
                "Test Store",
                null,
                null,
                "store.test",
                null);

        public async Task<Guid> CreateDraftAsync(params Guid[] areas)
        {
            var (status, _, body) = await SendAsync(HttpMethod.Post, AdminDrafts, CreateBody(areas));
            Assert.Equal(StatusCodes.Status201Created, status);
            return body.GetProperty("draftId").GetGuid();
        }

        public async Task<Guid> CreateSellerDraftAsync(params Guid[] areas)
        {
            var (status, _, body) = await SendAsync(HttpMethod.Post, SellerDrafts, CreateBody(areas));
            Assert.Equal(StatusCodes.Status201Created, status);
            return body.GetProperty("draftId").GetGuid();
        }

        public async Task<(long Revision, DateTimeOffset UpdatedAt)> ReadDraftStampAsync(Guid draftId)
            => await WithContextAsync(async context =>
            {
                var draft = await context.AuthoringDrafts.AsNoTracking().SingleAsync(item => item.DraftId == draftId);
                return (draft.Revision, draft.UpdatedAt);
            });

        public async Task WithContextAsync(Func<ShippingDbContext, Task> action)
            => await action(_scope.ServiceProvider.GetRequiredService<ShippingDbContext>());

        public async Task<T> WithContextAsync<T>(Func<ShippingDbContext, Task<T>> action)
            => await action(_scope.ServiceProvider.GetRequiredService<ShippingDbContext>());

        public async Task<(int Status, string? Location, JsonElement Body)> SendAsync(
            HttpMethod method,
            string path,
            string? json = null)
        {
            var endpoint = FindEndpoint(path, method.Method);
            var context = CreateContext(method, path, json);
            var routeValues = new RouteValueDictionary();
            TemplateMatches(endpoint, path, routeValues);
            context.Request.RouteValues = routeValues;
            try
            {
                await endpoint.RequestDelegate!(context);
            }
            catch (Exception exception)
            {
                await _app.Services.GetRequiredService<IExceptionPresentationService>()
                    .WriteAsync(context, exception, CancellationToken.None);
            }

            return (
                context.Response.StatusCode,
                context.Response.Headers.Location.ToString() is { Length: > 0 } location ? location : null,
                ReadBody(context));
        }

        public void Dispose()
        {
            _scope.Dispose();
            _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        private static string CreateBody(IEnumerable<Guid> areas)
            => string.Concat(
                "{\"serviceCode\":\"post\",\"optionCode\":null,\"regionGroups\":[",
                string.Join(',', areas.Select(area =>
                    $"{{\"groupId\":null,\"areas\":[{{\"kind\":1,\"countryId\":\"{Iran}\",\"administrativeAreaId\":\"{area}\"}}]}}")),
                "]}");

        private RouteEndpoint FindEndpoint(string path, string method) =>
            ((IEndpointRouteBuilder)_app).DataSources
                .SelectMany(dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>()
                .Single(endpoint =>
                    TemplateMatches(endpoint, path, new RouteValueDictionary())
                    && (endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Contains(method) ?? false));

        private static bool TemplateMatches(RouteEndpoint endpoint, string path, RouteValueDictionary values)
        {
            var template = TemplateParser.Parse(endpoint.RoutePattern.RawText!.TrimStart('/'));
            var matcher = new TemplateMatcher(template, new RouteValueDictionary());
            return matcher.TryMatch(new PathString(path), values);
        }

        private DefaultHttpContext CreateContext(HttpMethod method, string path, string? json)
        {
            var context = new DefaultHttpContext { RequestServices = _scope.ServiceProvider };
            context.Request.Method = method.Method;
            context.Request.Path = path;
            context.Request.Headers.AcceptLanguage = AcceptLanguage;
            context.Request.ContentType = "application/json";
            if (json is not null)
            {
                context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
            }

            context.Response.Body = new MemoryStream();
            _app.Services.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
            return context;
        }

        private static JsonElement ReadBody(HttpContext context)
        {
            context.Response.Body.Position = 0;
            if (context.Response.Body.Length == 0)
            {
                return default;
            }

            using var document = JsonDocument.Parse(context.Response.Body);
            return document.RootElement.Clone();
        }
    }

    private sealed class Counter
    {
        public int Sent { get; set; }

        public int Handler { get; set; }
    }

    private sealed class SentCountingBehavior<TRequest, TResponse>(Counter counter)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (IsWeeklySchedule(request))
            {
                counter.Sent++;
            }

            return await next();
        }
    }

    private sealed class HandlerReachedBehavior<TRequest, TResponse>(Counter counter)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (IsWeeklySchedule(request))
            {
                counter.Handler++;
            }

            return await next();
        }
    }

    private static bool IsWeeklySchedule(object request) =>
        request is GetShippingAuthoringWeeklyScheduleQuery or ReplaceShippingAuthoringWeeklyScheduleCommand;

    private sealed class RecordingAdminAuthorizer(IReadOnlyList<string> allowed) : IShippingAdminAuthorizer
    {
        public Task<Guid> RequireAuthorizedAsync(
            HttpContext context,
            string permission,
            CancellationToken cancellationToken)
            => allowed.Contains(permission, StringComparer.Ordinal)
                ? Task.FromResult(Iran)
                : Task.FromException<Guid>(
                    new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationDenied)));
    }

    private sealed class RecordingSellerAccess : ISellerPanelAccess
    {
        public Action? OnCall { get; set; }

        public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
            HttpRequest request,
            CancellationToken cancellationToken)
        {
            OnCall?.Invoke();
            return Task.FromResult((Iran, Tehran));
        }
    }

    private sealed class RecordingEffectiveAccess(IReadOnlyList<PlatformPermissionGrant> grants)
        : IPlatformEffectiveAccessReader
    {
        public Task<IReadOnlyList<PlatformPermissionGrant>> GetEffectivePermissionsAsync(
            Guid userId,
            PlatformAccessOwnerKind ownerKind,
            Guid? ownerScopeId,
            CancellationToken cancellationToken)
            => Task.FromResult(grants);
    }

    private sealed class FakeMethodValidator : IShippingMethodOptionValidator
    {
        public Task<Result<bool>> ValidateAsync(
            string serviceCode,
            string? optionCode,
            CancellationToken cancellationToken)
            => Task.FromResult(string.Equals(serviceCode, "post", StringComparison.Ordinal)
                ? Result<bool>.Success(true)
                : Result<bool>.Failure(new SemanticError(ShippingErrorCodes.ServiceOptionUnknown)));
    }

    private sealed class FakeResolver : IShippingAuthoringDestinationResolver
    {
        public Task<Result<IReadOnlyList<ShippingDestinationSelectionPath>>> ResolveAsync(
            IReadOnlyList<ShippingAuthoringSelectedAreaWrite> selections,
            CancellationToken cancellationToken)
            => Task.FromResult(Result<IReadOnlyList<ShippingDestinationSelectionPath>>.Success(
                selections.Select(selection => ShippingDestinationSelectionPath.FromChain(
                    [
                        new ShippingDestinationNode(ShippingDestinationNodeKind.Country, selection.CountryId),
                        new ShippingDestinationNode(
                            ShippingDestinationNodeKind.AdministrativeArea,
                            selection.AdministrativeAreaId!.Value),
                    ])).ToArray()));
    }

    private sealed class StubCurrentEdition(EditionContext? current) : ICurrentEdition
    {
        public EditionContext? Current { get; } = current;
    }

    private sealed class StubCurrentTenant(TenantContext? current) : ICurrentTenant
    {
        public TenantContext? Current { get; set; } = current;
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Tooba.Shipping.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
