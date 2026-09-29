using Tooba.BuildingBlocks;
using Tooba.Host.Security.Seller;

namespace Tooba.Host.Seller;

/// <summary>
/// مسیرهای باقی‌ماندهٔ پنل فروشنده. مجوز از Actor احرازشده و SpiceDB/موتور مجوز می‌آید؛ هدر Seller فقط زمینه است.
/// مسیر داشبورد به Order.Endpoints، مسیرهای /orders* به Order.Endpoints، مسیرهای Catalog فروشنده به Catalog.Endpoints
/// و Seller settings به Party.Endpoints منتقل شده‌اند. تنها مسیر باقی‌مانده: GET /v1/seller/dev-contexts.
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
    /// مسیر باقی‌ماندهٔ Seller Panel را ثبت می‌کند (فقط /dev-contexts).
    /// </summary>
    public static void MapSellerPanelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/seller");
        // Dashboard has moved to Tooba.Order.Endpoints.
        // Offer HTTP routes live in Tooba.Offer.Endpoints.
        // The three Seller Catalog routes live in Tooba.Catalog.Endpoints.
        // Seller settings routes live in Tooba.Party.Endpoints.
        group.MapGet("/dev-contexts", GetDevContexts);
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
}
