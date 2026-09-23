using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Host.Admin;
using Tooba.Order.Application.Seller.Queries.GetSellerOrderDashboardSummary;

namespace Tooba.Host.Seller;

/// <summary>
/// مسیرهای HTTP پنل فروشنده. مجوز از Actor احرازشده و SpiceDB/موتور مجوز می‌آید؛ هدر Seller فقط زمینه است.
/// مسیرهای /orders* به Order.Endpoints منتقل شده‌اند.
/// </summary>
public static class SellerPanelEndpoints
{
    /// <summary>
    /// هدر زمینهٔ Party فروشنده (مرجع مجوز نیست).
    /// </summary>
    public const string SellerPartyHeader = SellerPanelAccess.SellerPartyHeader;

    /// <summary>
    /// هدر Actor محدود Development.
    /// </summary>
    public const string DevActorHeader = SellerPanelAccess.DevActorHeader;

    /// <summary>
    /// مسیرهای Seller Panel را ثبت می‌کند (بدون /orders*).
    /// </summary>
    public static void MapSellerPanelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/seller");
        group.MapGet("/dashboard", GetDashboardAsync);
        group.MapGet("/catalog-variants", ListCatalogVariantsAsync);
        // Offer HTTP routes live in Tooba.Offer.Endpoints (MapOfferModule).
        group.MapGet("/dev-contexts", GetDevContexts);
        group.MapPut("/products/{productId:guid}/attributes/{definitionId:guid}", SetProductAttributeAsync);
        group.MapPut("/products/{productId:guid}/variant-axes", SetProductVariantAxesAsync);
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static async Task<IResult> GetDashboardAsync(
        SellerPanelComposer composer,
        MediatR.ISender sender,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (actorUserId, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            var (displayName, found) = await composer.GetSellerDisplayAsync(sellerPartyId, cancellationToken);
            if (!found)
            {
                throw new PlatformHttpException(404, "Seller was not found.", "seller.missing");
            }

            var orderSummary = await sender.Send(
                new GetSellerOrderDashboardSummaryQuery(sellerPartyId, actorUserId),
                cancellationToken);
            if (orderSummary.IsFailure)
            {
                return Results.Json(
                    new { title = orderSummary.FirstError.Code, errorCode = orderSummary.FirstError.Code },
                    statusCode: StatusCodes.Status400BadRequest);
            }

            return Results.Json(new SellerDashboardSummary(
                sellerPartyId,
                displayName,
                ActiveOffers: 0,
                orderSummary.Value.OpenOrders,
                orderSummary.Value.PaidOrders));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> ListCatalogVariantsAsync(
        SellerPanelComposer composer,
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
            var items = await composer.ListCatalogVariantsAsync(sellerPartyId, cancellationToken);
            return Results.Json(items);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static IResult GetDevContexts(IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return Results.Json(new { title = "Not Found", errorCode = "seller.dev.unavailable" }, statusCode: StatusCodes.Status404NotFound);
        }

        var snapshot = SellerDevActorBootstrap.Snapshot;
        if (snapshot is null)
        {
            return Results.Json(new { title = "در دسترس نیست", errorCode = "seller.dev.not-ready" }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Json(BuildDevContexts(snapshot));
    }

    private static object BuildDevContexts(SellerDevContextSnapshot snapshot)
    {
        var rows = new List<object>
        {
            new
            {
                actorUserId = snapshot.ActorA.ActorUserId,
                actorLabel = snapshot.ActorA.ActorLabel,
                sellerPartyId = snapshot.ActorA.SellerPartyId,
                sellerLabel = snapshot.ActorA.SellerLabel,
                contextKind = "seller-owner",
            },
            new
            {
                actorUserId = snapshot.ActorB.ActorUserId,
                actorLabel = snapshot.ActorB.ActorLabel,
                sellerPartyId = snapshot.ActorB.SellerPartyId,
                sellerLabel = snapshot.ActorB.SellerLabel,
                contextKind = "seller-owner-alt",
            },
        };
        if (snapshot.ScopedEmployee is { } employee)
        {
            rows.Add(new
            {
                actorUserId = employee.ActorUserId,
                actorLabel = employee.ActorLabel,
                sellerPartyId = employee.SellerPartyId,
                sellerLabel = employee.SellerLabel,
                contextKind = "scoped-employee",
            });
        }

        return new { actors = rows };
    }

    private static async Task<IResult> SetProductAttributeAsync(
        Guid productId,
        Guid definitionId,
        SetProductAttributeRequest body,
        ICatalogDirectory catalog,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            // فروشنده فقط مقدار محصول را می‌نویسد؛ تعریف schema را بازتعریف نمی‌کند.
            await catalog.SetProductAttributeAsync(
                productId,
                definitionId,
                body.RawValue,
                body.EnumOptionId,
                cancellationToken);
            return Results.Json(new { ok = true });
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = "catalog.attribute.invalid" }, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> SetProductVariantAxesAsync(
        Guid productId,
        SetProductVariantAxesRequest body,
        ICatalogDirectory catalog,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            await catalog.SetProductVariantAxesAsync(
                productId,
                body.OrderedDefinitionIds ?? [],
                cancellationToken);
            return Results.Json(new { ok = true });
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = "catalog.variant_axes.invalid" }, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
