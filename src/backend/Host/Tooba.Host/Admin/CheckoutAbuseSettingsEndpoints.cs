using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Host.Admin;

/// <summary>نمایهٔ تنظیم سقف سفارش باز و سهمیه رزرو.</summary>
public sealed record CheckoutAbuseSettingsView(
    int MaxOpenUnpaidOrdersPerCustomer,
    int ReservationCommitWindowMinutes,
    int MaxCheckoutCommitsPerCustomerInWindow,
    int MinOpenUnpaid,
    int MaxOpenUnpaid,
    int MinWindowMinutes,
    int MaxWindowMinutes,
    int MinCommits,
    int MaxCommits);

/// <summary>بدنهٔ ذخیره.</summary>
public sealed record CheckoutAbuseSettingsWriteRequest(
    int? MaxOpenUnpaidOrdersPerCustomer,
    int? ReservationCommitWindowMinutes,
    int? MaxCheckoutCommitsPerCustomerInWindow);

/// <summary>تنظیم کنترل سفارش‌های پرداخت‌نشده و سوءاستفاده از رزرو.</summary>
public static class CheckoutAbuseSettingsEndpoints
{
    /// <summary>مسیرهای Admin را ثبت می‌کند.</summary>
    public static void MapCheckoutAbuseSettingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin/settings/checkout-abuse");
        group.MapGet("/", GetAsync);
        group.MapPut("/", PutAsync);
    }

    private static async Task<IResult> GetAsync(
        CatalogDbContext catalog,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var row = await catalog.StoreCheckoutAbuseSettings.AsNoTracking()
                .SingleOrDefaultAsync(x => x.SettingsId == StoreCheckoutAbuseSettings.SingletonId, cancellationToken);
            return Results.Json(ToView(row));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> PutAsync(
        CheckoutAbuseSettingsWriteRequest body,
        CatalogDbContext catalog,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            if (body.MaxOpenUnpaidOrdersPerCustomer is null
                || body.ReservationCommitWindowMinutes is null
                || body.MaxCheckoutCommitsPerCustomerInWindow is null)
            {
                throw new InvalidOperationException("settings.checkout_abuse.invalid");
            }

            var now = DateTimeOffset.UtcNow;
            var row = await catalog.StoreCheckoutAbuseSettings
                .SingleOrDefaultAsync(x => x.SettingsId == StoreCheckoutAbuseSettings.SingletonId, cancellationToken);
            if (row is null)
            {
                row = StoreCheckoutAbuseSettings.CreateDefault(now);
                catalog.StoreCheckoutAbuseSettings.Add(row);
            }

            var oldOpen = row.MaxOpenUnpaidOrdersPerCustomer;
            var oldWindow = row.ReservationCommitWindowMinutes;
            var oldCommits = row.MaxCheckoutCommitsPerCustomerInWindow;
            row.Replace(
                body.MaxOpenUnpaidOrdersPerCustomer.Value,
                body.ReservationCommitWindowMinutes.Value,
                body.MaxCheckoutCommitsPerCustomerInWindow.Value,
                now);
            RecordField(catalog, "MaxOpenUnpaidOrdersPerCustomer", oldOpen, row.MaxOpenUnpaidOrdersPerCustomer, actor, now);
            RecordField(catalog, "ReservationCommitWindowMinutes", oldWindow, row.ReservationCommitWindowMinutes, actor, now);
            RecordField(catalog, "MaxCheckoutCommitsPerCustomerInWindow", oldCommits, row.MaxCheckoutCommitsPerCustomerInWindow, actor, now);
            await catalog.SaveChangesAsync(cancellationToken);
            return Results.Json(ToView(row));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(
                new { title = "Bad Request", errorCode = ex.Message },
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static void RecordField(
        CatalogDbContext catalog,
        string field,
        int oldValue,
        int newValue,
        Guid actorUserId,
        DateTimeOffset now)
    {
        if (oldValue == newValue)
        {
            return;
        }

        catalog.ReservationPolicyAuditEvents.Add(
            ReservationPolicyAuditEvent.Create("store", null, field, oldValue, newValue, actorUserId, now));
    }

    private static CheckoutAbuseSettingsView ToView(StoreCheckoutAbuseSettings? row) =>
        new(
            row?.MaxOpenUnpaidOrdersPerCustomer ?? StoreCheckoutAbuseSettings.DefaultMaxOpenUnpaid,
            row?.ReservationCommitWindowMinutes ?? StoreCheckoutAbuseSettings.DefaultWindowMinutes,
            row?.MaxCheckoutCommitsPerCustomerInWindow ?? StoreCheckoutAbuseSettings.DefaultMaxCommits,
            StoreCheckoutAbuseSettings.MinValue,
            StoreCheckoutAbuseSettings.MaxOpenUnpaidCap,
            StoreCheckoutAbuseSettings.MinValue,
            StoreCheckoutAbuseSettings.MaxWindowMinutesCap,
            StoreCheckoutAbuseSettings.MinValue,
            StoreCheckoutAbuseSettings.MaxCommitsCap);
}
