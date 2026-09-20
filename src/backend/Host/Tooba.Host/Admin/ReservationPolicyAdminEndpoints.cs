using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Application;

namespace Tooba.Host.Admin;

/// <summary>Admin و Seller read-only برای سیاست رزرو Store/Category/Offer.</summary>
public static class ReservationPolicyAdminEndpoints
{
    /// <summary>مسیرها را ثبت می‌کند.</summary>
    public static void MapReservationPolicyAdminEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/v1/admin/settings/reservation-policy");
        admin.MapGet("/store", GetStoreAsync);
        admin.MapPut("/store", PutStoreAsync);
        admin.MapGet("/categories/{categoryId:guid}", GetCategoryAsync);
        admin.MapPut("/categories/{categoryId:guid}", PutCategoryAsync);
        admin.MapGet("/offers", GetOffersBatchAsync);
        admin.MapGet("/offers/{offerId:guid}", GetOfferAsync);
        admin.MapPut("/offers/{offerId:guid}", PutOfferAsync);
        admin.MapGet("/audit", GetAuditAsync);

        var seller = app.MapGroup("/v1/seller/settings/reservation-policy");
        seller.MapGet("/offers/{offerId:guid}", SellerGetOfferAsync);
        seller.MapPut("/offers/{offerId:guid}", SellerPutOfferAsync);
    }

    private static async Task<IResult> GetStoreAsync(
        IReservationCyclePolicyResolver resolver,
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
            var preview = await resolver.PreviewAsync(null, null, cancellationToken);
            return Results.Json(ReservationPolicyAdminComposer.ForStore(preview, true));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> PutStoreAsync(
        ReservationPolicyWriteRequest body,
        CatalogDbContext catalog,
        IReservationCyclePolicyResolver resolver,
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
            var now = DateTimeOffset.UtcNow;
            var store = await catalog.StoreHoldPolicySettings
                .SingleOrDefaultAsync(x => x.SettingsId == Tooba.Catalog.Domain.StoreHoldPolicySettings.SingletonId, cancellationToken);
            if (store is null)
            {
                store = Tooba.Catalog.Domain.StoreHoldPolicySettings.CreateDefault(now);
                catalog.StoreHoldPolicySettings.Add(store);
            }

            ReservationPolicyAdminComposer.ReplaceStore(store, body, actor, now, catalog);
            await catalog.SaveChangesAsync(cancellationToken);
            var preview = await resolver.PreviewAsync(null, null, cancellationToken);
            return Results.Json(ReservationPolicyAdminComposer.ForStore(preview, true));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> GetCategoryAsync(
        Guid categoryId,
        IReservationCyclePolicyResolver resolver,
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
            var preview = await resolver.PreviewAsync(null, categoryId, cancellationToken);
            return Results.Json(ReservationPolicyAdminComposer.ForCategory(categoryId, preview, true));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> PutCategoryAsync(
        Guid categoryId,
        ReservationPolicyWriteRequest body,
        CatalogDbContext catalog,
        IReservationCyclePolicyResolver resolver,
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
            await ReservationPolicyAdminComposer.ReplaceOverrideAsync(
                catalog,
                Tooba.Catalog.Domain.ReservationCyclePolicyOverride.CategoryScope,
                categoryId,
                body,
                actor,
                DateTimeOffset.UtcNow,
                cancellationToken);
            var preview = await resolver.PreviewAsync(null, categoryId, cancellationToken);
            return Results.Json(ReservationPolicyAdminComposer.ForCategory(categoryId, preview, true));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> GetOfferAsync(
        Guid offerId,
        IReservationCyclePolicyResolver resolver,
        IOfferQueryGateway offers,
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
            var categoryId = await ReservationPolicyAdminComposer.ResolveOfferCategoryAsync(
                offers, catalog, offerId, cancellationToken);
            var preview = await resolver.PreviewAsync(offerId, categoryId, cancellationToken);
            return Results.Json(ReservationPolicyAdminComposer.ForOffer(offerId, preview, true));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> GetOffersBatchAsync(
        string? offerIds,
        IReservationCyclePolicyResolver resolver,
        IOfferQueryGateway offers,
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
            var ids = ParseIds(offerIds);
            var categories = await ReservationPolicyAdminComposer.ResolveOfferCategoriesAsync(
                offers, catalog, ids, cancellationToken);
            var lines = ids.Select(id => (id, categories.GetValueOrDefault(id))).ToList();

            var previews = resolver is Tooba.Host.ReservationCyclePolicyResolver concrete
                ? await concrete.PreviewManyAsync(lines, cancellationToken)
                : await Task.WhenAll(lines.Select(x => resolver.PreviewAsync(x.Item1, x.Item2, cancellationToken)));
            var items = ids.Select((offerId, index) =>
                    ReservationPolicyAdminComposer.ForOffer(offerId, previews[index], true))
                .ToList();
            return Results.Json(new { items });
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> PutOfferAsync(
        Guid offerId,
        ReservationPolicyWriteRequest body,
        CatalogDbContext catalog,
        IOfferQueryGateway offers,
        IReservationCyclePolicyResolver resolver,
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
            await ReservationPolicyAdminComposer.ReplaceOverrideAsync(
                catalog,
                Tooba.Catalog.Domain.ReservationCyclePolicyOverride.OfferScope,
                offerId,
                body,
                actor,
                DateTimeOffset.UtcNow,
                cancellationToken);
            var categoryId = await ReservationPolicyAdminComposer.ResolveOfferCategoryAsync(
                offers, catalog, offerId, cancellationToken);
            var preview = await resolver.PreviewAsync(offerId, categoryId, cancellationToken);
            return Results.Json(ReservationPolicyAdminComposer.ForOffer(offerId, preview, true));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> GetAuditAsync(
        int? take,
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
            var limit = Math.Clamp(take ?? 50, 1, 200);
            var rows = await catalog.ReservationPolicyAuditEvents.AsNoTracking()
                .OrderByDescending(x => x.OccurredAt)
                .Take(limit)
                .Select(x => new ReservationPolicyAuditView(
                    x.EventId,
                    x.Level,
                    x.ScopeId,
                    x.Field,
                    x.OldOverride,
                    x.NewOverride,
                    x.ActorUserId,
                    x.OccurredAt))
                .ToListAsync(cancellationToken);
            return Results.Json(new { items = rows });
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> SellerGetOfferAsync(
        Guid offerId,
        IReservationCyclePolicyResolver resolver,
        IOfferQueryGateway offers,
        CatalogDbContext catalog,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await Tooba.Host.Seller.SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            var categoryId = await ReservationPolicyAdminComposer.ResolveOfferCategoryAsync(
                offers, catalog, offerId, cancellationToken);
            var preview = await resolver.PreviewAsync(offerId, categoryId, cancellationToken);
            return Results.Json(ReservationPolicyAdminComposer.ForOffer(offerId, preview, false));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> SellerPutOfferAsync(
        Guid offerId,
        ReservationPolicyWriteRequest body,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await Tooba.Host.Seller.SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            _ = offerId;
            _ = body;
            _ = ReservationPolicyAdminComposer.SellerMutatePermission;
            throw new PlatformHttpException(
                403,
                "فروشنده مجوز تغییر سیاست رزرو ندارد.",
                "reservation.policy.seller.denied");
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static List<Guid> ParseIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => Guid.TryParse(x, out var id) ? id : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .Take(50)
            .ToList();
    }
}
