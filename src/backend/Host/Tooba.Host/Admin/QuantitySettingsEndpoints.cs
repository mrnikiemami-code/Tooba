using MediatR;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;

namespace Tooba.Host.Admin;

/// <summary>یک حالت گرد کردن سراسری مقدار کالا.</summary>
public sealed record StoreQuantitySettingsView(
    string GlobalRoundingMode,
    string LabelFa,
    string LabelEn);

/// <summary>بدنهٔ ذخیرهٔ گرد کردن سراسری.</summary>
public sealed record StoreQuantitySettingsWriteRequest(string GlobalRoundingMode);

/// <summary>
/// تنظیم سراسری گرد کردن مقدار — خواندن از Catalog lookup؛ نوشتن از طریق CQRS.
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
        ICatalogLookupGateway catalogLookup,
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
            var mode = await catalogLookup.GetGlobalRoundingModeAsync(cancellationToken);
            return Results.Json(ToView(mode));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> PutAsync(
        StoreQuantitySettingsWriteRequest body,
        ISender sender,
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
            var mode = await sender.Send(new SaveStoreQuantitySettingsCommand(body.GlobalRoundingMode), cancellationToken);
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
