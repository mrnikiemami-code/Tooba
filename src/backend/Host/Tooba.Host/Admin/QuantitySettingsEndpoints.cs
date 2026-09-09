using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Host.Admin;

/// <summary>یک حالت گرد کردن سراسری مقدار کالا.</summary>
public sealed record StoreQuantitySettingsView(
    string GlobalRoundingMode,
    string LabelFa,
    string LabelEn);

/// <summary>بدنهٔ ذخیرهٔ گرد کردن سراسری.</summary>
public sealed record StoreQuantitySettingsWriteRequest(string GlobalRoundingMode);

/// <summary>
/// تنظیم سراسری گرد کردن مقدار — یک ردیف store_quantity_settings، بدون سیستم موازی.
/// </summary>
public static class QuantitySettingsEndpoints
{
    /// <summary>مسیرهای Admin تنظیم گرد کردن مقدار را ثبت می‌کند.</summary>
    public static void MapQuantitySettingsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin/settings/quantity-rounding");
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
            var row = await catalog.StoreQuantitySettings.AsNoTracking()
                .SingleOrDefaultAsync(x => x.SettingsId == StoreQuantitySettings.SingletonId, cancellationToken);
            var mode = row?.RoundingMode ?? QuantityRoundingMode.Nearest;
            return Results.Json(ToView(mode));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> PutAsync(
        StoreQuantitySettingsWriteRequest body,
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
            if (!Enum.TryParse<QuantityRoundingMode>(body.GlobalRoundingMode, ignoreCase: true, out var mode)
                || mode is not (QuantityRoundingMode.Floor or QuantityRoundingMode.Ceiling or QuantityRoundingMode.Nearest))
            {
                throw new PlatformHttpException(400, "حالت گرد کردن نامعتبر است.", "quantity.rounding.invalid");
            }

            var now = DateTimeOffset.UtcNow;
            var row = await catalog.StoreQuantitySettings
                .SingleOrDefaultAsync(x => x.SettingsId == StoreQuantitySettings.SingletonId, cancellationToken);
            if (row is null)
            {
                row = StoreQuantitySettings.CreateDefault(now);
                catalog.StoreQuantitySettings.Add(row);
            }

            row.SetRoundingMode(mode, now);
            await catalog.SaveChangesAsync(cancellationToken);
            return Results.Json(ToView(mode));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static StoreQuantitySettingsView ToView(QuantityRoundingMode mode) => mode switch
    {
        QuantityRoundingMode.Floor => new("Floor", "رو به پایین", "Floor"),
        QuantityRoundingMode.Ceiling => new("Ceiling", "رو به بالا", "Ceiling"),
        _ => new("Nearest", "نزدیک‌ترین مقدار", "Nearest"),
    };
}
