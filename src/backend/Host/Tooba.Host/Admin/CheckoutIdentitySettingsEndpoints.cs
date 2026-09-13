using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Host.Storefront;

namespace Tooba.Host.Admin;

/// <summary>نمایهٔ تنظیم هویت خرید.</summary>
public sealed record CheckoutIdentitySettingsView(
    string Policy,
    string LabelFa,
    string LabelEn,
    string? WarningFa);

/// <summary>بدنهٔ ذخیره.</summary>
public sealed record CheckoutIdentitySettingsWriteRequest(string? Policy);

/// <summary>تنظیم هویت مشتری در فرایند خرید روی Admin Settings موجود.</summary>
public static class CheckoutIdentitySettingsEndpoints
{
    /// <summary>مسیرهای Admin را ثبت می‌کند.</summary>
    public static void MapCheckoutIdentitySettingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin/settings/checkout-identity");
        group.MapGet("/", GetAsync);
        group.MapPut("/", PutAsync);
    }

    private static async Task<IResult> GetAsync(
        CatalogDbContext catalog,
        CheckoutIdentityGate gate,
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
            return Results.Json(ToView(await gate.GetEffectiveAsync(cancellationToken)));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> PutAsync(
        CheckoutIdentitySettingsWriteRequest body,
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
            var policy = ParsePolicy(body.Policy);
            var now = DateTimeOffset.UtcNow;
            var row = await catalog.StoreCheckoutIdentitySettings
                .SingleOrDefaultAsync(x => x.SettingsId == StoreCheckoutIdentitySettings.SingletonId, cancellationToken);
            if (row is null)
            {
                row = StoreCheckoutIdentitySettings.CreateDefault(now);
                catalog.StoreCheckoutIdentitySettings.Add(row);
            }

            row.Replace(policy, now);
            await catalog.SaveChangesAsync(cancellationToken);
            return Results.Json(ToView(row.Policy));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static CheckoutIdentityPolicyKind ParsePolicy(string? raw) =>
        string.Equals(raw, nameof(CheckoutIdentityPolicyKind.GuestAllowed), StringComparison.OrdinalIgnoreCase)
            ? CheckoutIdentityPolicyKind.GuestAllowed
            : CheckoutIdentityPolicyKind.AuthenticatedOnly;

    private static CheckoutIdentitySettingsView ToView(CheckoutIdentityPolicyKind policy) =>
        policy == CheckoutIdentityPolicyKind.GuestAllowed
            ? new(
                nameof(CheckoutIdentityPolicyKind.GuestAllowed),
                "خرید مهمان مجاز",
                "Guest checkout allowed",
                "خرید مهمان می‌تواند کنترل سفارش‌های پرداخت‌نشده و محدودیت‌های رزرو موجودی را کاهش دهد.")
            : new(
                nameof(CheckoutIdentityPolicyKind.AuthenticatedOnly),
                "ورود الزامی",
                "Login required",
                null);
}
