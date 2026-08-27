using Tooba.BuildingBlocks;
using Tooba.Host.Storefront;
using Tooba.Notification.Application;
using Tooba.Notification.Domain;

namespace Tooba.Host.Notifications;

/// <summary>
/// مرز HTTP اعلان‌های مشتری و فروشنده. هویت از نشست/Dev Actor می‌آید نه از payload.
/// </summary>
public static class NotificationEndpoints
{
    private const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <summary>مسیرهای /v1/customer/notifications و /v1/seller/notifications را ثبت می‌کند.</summary>
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        var customer = app.MapGroup("/v1/customer/notifications");
        customer.MapGet("/", ListCustomerAsync);
        customer.MapGet("/unread-count", CustomerUnreadCountAsync);
        customer.MapPost("/{id:guid}/read", MarkCustomerReadAsync);
        customer.MapPost("/read-all", MarkCustomerAllReadAsync);
        customer.MapDelete("/{id:guid}", DismissCustomerAsync);

        var seller = app.MapGroup("/v1/seller/notifications");
        seller.MapGet("/", ListSellerAsync);
        seller.MapGet("/unread-count", SellerUnreadCountAsync);
        seller.MapPost("/{id:guid}/read", MarkSellerReadAsync);
        seller.MapPost("/read-all", MarkSellerAllReadAsync);
        seller.MapDelete("/{id:guid}", DismissSellerAsync);
    }

    private static async Task<IResult> ListCustomerAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        INotificationDirectory directory,
        CancellationToken cancellationToken,
        int skip = 0,
        int take = 20,
        string? locale = null)
    {
        var actor = ResolveCustomerActor(request, session, environment);
        if (actor is null)
        {
            return CustomerUnauthorized();
        }

        var page = await directory.ListAsync(
            new NotificationRecipientQuery(
                NotificationRecipientKind.Customer,
                actor.Value,
                actor.Value,
                skip,
                take,
                locale ?? "fa"),
            cancellationToken);
        return Results.Json(MapListResponse(page));
    }

    private static async Task<IResult> CustomerUnreadCountAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        INotificationDirectory directory,
        CancellationToken cancellationToken)
    {
        var actor = ResolveCustomerActor(request, session, environment);
        if (actor is null)
        {
            return CustomerUnauthorized();
        }

        var count = await directory.UnreadCountAsync(
            NotificationRecipientKind.Customer,
            actor.Value,
            actor.Value,
            cancellationToken);
        return Results.Json(new { unreadCount = count });
    }

    private static async Task<IResult> MarkCustomerReadAsync(
        Guid id,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        INotificationDirectory directory,
        CancellationToken cancellationToken)
    {
        var actor = ResolveCustomerActor(request, session, environment);
        if (actor is null)
        {
            return CustomerUnauthorized();
        }

        var ok = await directory.MarkReadAsync(
            id,
            NotificationRecipientKind.Customer,
            actor.Value,
            actor.Value,
            cancellationToken);
        return ok ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> MarkCustomerAllReadAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        INotificationDirectory directory,
        CancellationToken cancellationToken)
    {
        var actor = ResolveCustomerActor(request, session, environment);
        if (actor is null)
        {
            return CustomerUnauthorized();
        }

        var changed = await directory.MarkAllReadAsync(
            NotificationRecipientKind.Customer,
            actor.Value,
            actor.Value,
            cancellationToken);
        return Results.Json(new { markedCount = changed });
    }

    private static async Task<IResult> DismissCustomerAsync(
        Guid id,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        INotificationDirectory directory,
        CancellationToken cancellationToken)
    {
        var actor = ResolveCustomerActor(request, session, environment);
        if (actor is null)
        {
            return CustomerUnauthorized();
        }

        var ok = await directory.SoftDeleteAsync(
            id,
            NotificationRecipientKind.Customer,
            actor.Value,
            actor.Value,
            cancellationToken);
        return ok ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> ListSellerAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        INotificationDirectory directory,
        CancellationToken cancellationToken,
        int skip = 0,
        int take = 20,
        string? locale = null)
    {
        try
        {
            var (_, sellerPartyId) = await Seller.SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            var page = await directory.ListAsync(
                new NotificationRecipientQuery(
                    NotificationRecipientKind.Seller,
                    sellerPartyId,
                    null,
                    skip,
                    take,
                    locale ?? "fa"),
                cancellationToken);
            return Results.Json(MapListResponse(page));
        }
        catch (PlatformHttpException ex)
        {
            return SellerError(ex);
        }
    }

    private static async Task<IResult> SellerUnreadCountAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        INotificationDirectory directory,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await Seller.SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            var count = await directory.UnreadCountAsync(
                NotificationRecipientKind.Seller,
                sellerPartyId,
                null,
                cancellationToken);
            return Results.Json(new { unreadCount = count });
        }
        catch (PlatformHttpException ex)
        {
            return SellerError(ex);
        }
    }

    private static async Task<IResult> MarkSellerReadAsync(
        Guid id,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        INotificationDirectory directory,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await Seller.SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            var ok = await directory.MarkReadAsync(
                id,
                NotificationRecipientKind.Seller,
                sellerPartyId,
                null,
                cancellationToken);
            return ok ? Results.NoContent() : Results.NotFound();
        }
        catch (PlatformHttpException ex)
        {
            return SellerError(ex);
        }
    }

    private static async Task<IResult> MarkSellerAllReadAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        INotificationDirectory directory,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await Seller.SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            var changed = await directory.MarkAllReadAsync(
                NotificationRecipientKind.Seller,
                sellerPartyId,
                null,
                cancellationToken);
            return Results.Json(new { markedCount = changed });
        }
        catch (PlatformHttpException ex)
        {
            return SellerError(ex);
        }
    }

    private static async Task<IResult> DismissSellerAsync(
        Guid id,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        INotificationDirectory directory,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await Seller.SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            var ok = await directory.SoftDeleteAsync(
                id,
                NotificationRecipientKind.Seller,
                sellerPartyId,
                null,
                cancellationToken);
            return ok ? Results.NoContent() : Results.NotFound();
        }
        catch (PlatformHttpException ex)
        {
            return SellerError(ex);
        }
    }

    private static object MapListResponse(NotificationListPage page) => new
    {
        items = page.Items.Select(x => new
        {
            notificationId = x.NotificationId,
            type = x.Type,
            category = x.Category,
            title = x.Title,
            body = x.Body,
            targetRoute = x.TargetRoute,
            isRead = x.IsRead,
            createdAt = x.CreatedAt,
        }).ToList(),
        skip = page.Skip,
        take = page.Take,
        totalCount = page.TotalCount,
        unreadCount = page.UnreadCount,
    };

    private static Guid? ResolveCustomerActor(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment)
    {
        if (session.IsAuthenticated)
        {
            return session.UserId;
        }

        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            return null;
        }

        if (request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var devActor)
            && devActor != Guid.Empty)
        {
            return devActor;
        }

        return StorefrontCheckoutComposer.StorefrontGuestActorId;
    }

    private static IResult CustomerUnauthorized() =>
        Results.Json(
            new { title = "Unauthorized", errorCode = "customer.session.required" },
            statusCode: StatusCodes.Status401Unauthorized);

    private static IResult SellerError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
}
