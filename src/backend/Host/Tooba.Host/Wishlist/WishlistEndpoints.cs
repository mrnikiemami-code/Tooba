using Tooba.Host.Storefront;
using Tooba.Wishlist.Application;

namespace Tooba.Host.Wishlist;

/// <summary>مرز HTTP خصوصی Wishlist که Actor را فقط از نشست یا seam کنترل‌شدهٔ توسعه می‌گیرد.</summary>
public static class WishlistEndpoints
{
    private const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <summary>مسیرهای list/add/remove/membership را زیر مرز مشتری ثبت می‌کند.</summary>
    public static void MapWishlistEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/customer/wishlist");
        group.MapGet("", ListAsync);
        group.MapPost("/{productId:guid}", AddAsync);
        group.MapDelete("/{productId:guid}", RemoveAsync);
        group.MapPost("/membership", MembershipAsync);
    }

    private static async Task<IResult> ListAsync(HttpRequest request, CurrentAuthenticatedSession session,
        IHostEnvironment environment, WishlistComposer composer, CancellationToken cancellationToken)
    {
        var actor = ResolveActor(request, session, environment);
        return actor is null ? Unauthorized() : Results.Json(await composer.ListAsync(actor.Value, cancellationToken));
    }

    private static async Task<IResult> AddAsync(Guid productId, HttpRequest request, CurrentAuthenticatedSession session,
        IHostEnvironment environment, IWishlistDirectory wishlist, CancellationToken cancellationToken)
    {
        var actor = ResolveActor(request, session, environment);
        if (actor is null) return Unauthorized();
        var result = await wishlist.AddAsync(actor.Value, productId, cancellationToken);
        return Results.Json(result, statusCode: result.Created ? StatusCodes.Status201Created : StatusCodes.Status200OK);
    }

    private static async Task<IResult> RemoveAsync(Guid productId, HttpRequest request, CurrentAuthenticatedSession session,
        IHostEnvironment environment, IWishlistDirectory wishlist, CancellationToken cancellationToken)
    {
        var actor = ResolveActor(request, session, environment);
        if (actor is null) return Unauthorized();
        await wishlist.RemoveAsync(actor.Value, productId, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> MembershipAsync(WishlistMembershipRequest body, HttpRequest request,
        CurrentAuthenticatedSession session, IHostEnvironment environment, IWishlistDirectory wishlist,
        CancellationToken cancellationToken)
    {
        var actor = ResolveActor(request, session, environment);
        if (actor is null) return Unauthorized();
        var membership = await wishlist.GetMembershipAsync(actor.Value, body.ProductIds.Distinct().Take(500).ToArray(), cancellationToken);
        return Results.Json(new WishlistMembershipResponse(membership));
    }

    private static Guid? ResolveActor(HttpRequest request, CurrentAuthenticatedSession session, IHostEnvironment environment)
    {
        if (session.IsAuthenticated) return session.UserId;
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing")) return null;
        if (request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var actor) && actor != Guid.Empty) return actor;
        return StorefrontCheckoutComposer.StorefrontGuestActorId;
    }

    private static IResult Unauthorized() => Results.Json(
        new { title = "Unauthorized", errorCode = "customer.session.required" },
        statusCode: StatusCodes.Status401Unauthorized);
}

/// <summary>درخواست گروهی عضویت که هیچ شناسهٔ مالک دریافت نمی‌کند.</summary>
public sealed record WishlistMembershipRequest(IReadOnlyList<Guid> ProductIds);
/// <summary>پاسخ مجموعهٔ Productهای عضو Wishlist کاربر جاری.</summary>
public sealed record WishlistMembershipResponse(IReadOnlySet<Guid> ProductIds);
