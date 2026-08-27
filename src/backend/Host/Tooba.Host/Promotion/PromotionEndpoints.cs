using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Host.Seller;

namespace Tooba.Host.Promotion;

/// <summary>مرزهای HTTP فروشنده و مدیریتی پروموشن/کوپن.</summary>
public static class PromotionEndpoints
{
    /// <summary>مسیرهای پروموشن پنل را ثبت می‌کند.</summary>
    public static void MapPromotionEndpoints(this WebApplication app)
    {
        var seller = app.MapGroup("/v1/seller/promotions");
        seller.MapGet("", SellerListAsync);
        seller.MapPost("", SellerCreateAsync);
        seller.MapGet("/{id:guid}", SellerGetAsync);
        seller.MapPut("/{id:guid}", SellerUpdateAsync);
        seller.MapPost("/{id:guid}/activate", SellerActivateAsync);
        seller.MapPost("/{id:guid}/deactivate", SellerDeactivateAsync);

        var admin = app.MapGroup("/v1/admin/promotions");
        admin.MapGet("", AdminListAsync);
        admin.MapGet("/{id:guid}", AdminGetAsync);
        admin.MapPost("/{id:guid}/deactivate", AdminDeactivateAsync);
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static IResult ToMutationError(InvalidOperationException ex)
    {
        var missing = ex.Message.Contains("یافت نشد", StringComparison.Ordinal);
        var errorCode = missing ? "promotion.missing" : "promotion.mutation.rejected";
        var statusCode = missing ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest;
        return Results.Json(
            new { title = missing ? "Not Found" : "Bad Request", errorCode, detail = ex.Message },
            statusCode: statusCode);
    }

    private static async Task<IResult> SellerListAsync(
        PromotionPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            return Results.Json(await composer.SellerListAsync(sellerPartyId, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> SellerGetAsync(
        Guid id,
        PromotionPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            var row = await composer.SellerGetAsync(sellerPartyId, id, cancellationToken);
            return row is null ? Results.NotFound() : Results.Json(row);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> SellerCreateAsync(
        UpsertSellerPromotionBody body,
        PromotionPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await SellerMutationAsync(
            request,
            session,
            guard,
            environment,
            cancellationToken,
            sellerPartyId => composer.SellerCreateAsync(sellerPartyId, body, cancellationToken),
            successStatusCode: StatusCodes.Status201Created);

    private static async Task<IResult> SellerUpdateAsync(
        Guid id,
        UpsertSellerPromotionBody body,
        PromotionPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await SellerMutationAsync(
            request,
            session,
            guard,
            environment,
            cancellationToken,
            sellerPartyId => composer.SellerUpdateAsync(sellerPartyId, id, body, cancellationToken));

    private static async Task<IResult> SellerActivateAsync(
        Guid id,
        PromotionPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await SellerMutationAsync(
            request,
            session,
            guard,
            environment,
            cancellationToken,
            async sellerPartyId =>
            {
                await composer.SellerActivateAsync(sellerPartyId, id, cancellationToken);
                return await composer.SellerGetAsync(sellerPartyId, id, cancellationToken);
            });

    private static async Task<IResult> SellerDeactivateAsync(
        Guid id,
        PromotionPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        await SellerMutationAsync(
            request,
            session,
            guard,
            environment,
            cancellationToken,
            async sellerPartyId =>
            {
                await composer.SellerDeactivateAsync(sellerPartyId, id, cancellationToken);
                return await composer.SellerGetAsync(sellerPartyId, id, cancellationToken);
            });

    private static async Task<IResult> AdminListAsync(
        PromotionPanelComposer composer,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        Guid? sellerPartyId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await composer.AdminListAsync(sellerPartyId, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> AdminGetAsync(
        Guid id,
        PromotionPanelComposer composer,
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
            var row = await composer.AdminGetAsync(id, cancellationToken);
            return row is null ? Results.NotFound() : Results.Json(row);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> AdminDeactivateAsync(
        Guid id,
        PromotionPanelComposer composer,
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
            await composer.AdminDeactivateAsync(id, cancellationToken);
            var row = await composer.AdminGetAsync(id, cancellationToken);
            return row is null ? Results.NotFound() : Results.Json(row);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return ToMutationError(ex);
        }
    }

    private static async Task<IResult> SellerMutationAsync<T>(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken,
        Func<Guid, Task<T>> action,
        int successStatusCode = StatusCodes.Status200OK)
    {
        try
        {
            var (_, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            var result = await action(sellerPartyId);
            return Results.Json(result, statusCode: successStatusCode);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return ToMutationError(ex);
        }
    }
}
