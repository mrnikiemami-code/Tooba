using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Host.Seller;
using Tooba.Reviews.Application;
using Tooba.Reviews.Domain;
using Tooba.Catalog.Application;

namespace Tooba.Host.Reviews;

/// <summary>مرزهای HTTP عمومی، مشتری، فروشنده و مدیر برای Reviews.</summary>
public static class ReviewEndpoints
{
    private const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <summary>مسیرهای Reviews را ثبت می‌کند.</summary>
    public static void MapReviewEndpoints(this WebApplication app)
    {
        app.MapGet("/v1/storefront/products/{slug}/reviews", GetPublishedAsync);
        app.MapPost("/v1/customer/reviews", SubmitAsync);
        app.MapGet("/v1/seller/reviews", SellerListAsync);
        app.MapGet("/v1/admin/reviews", PendingAsync);
        app.MapPost("/v1/admin/reviews/{reviewId:guid}/publish", PublishAsync);
        app.MapPost("/v1/admin/reviews/{reviewId:guid}/reject", RejectAsync);
    }

    private static async Task<IResult> GetPublishedAsync(string slug, IReviewDirectory reviews, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await reviews.GetPublishedAsync(slug, page, pageSize, cancellationToken);
        if (result is null) return Results.NotFound();
        return Results.Json(new PublicReviewsResponse(
            result.Summary.Count == 0 ? null : result.Summary.Average,
            result.Summary.Count,
            result.Summary.Distribution,
            result.Items.Select(x => new PublicReviewItem(
                x.ReviewId, x.AuthorDisplayName, x.Rating, x.Title, x.Body,
                x.IsVerifiedPurchase, x.CreatedAt)).ToList(),
            result.Page,
            result.PageSize,
            result.Summary.Count));
    }

    private static async Task<IResult> SubmitAsync(SubmitProductReview body, HttpRequest request,
        CurrentAuthenticatedSession session, IHostEnvironment environment, IReviewDirectory reviews, CancellationToken cancellationToken)
    {
        var actor = ResolveActor(request, session, environment);
        if (actor is null) return Results.Json(new { title = "Unauthorized", errorCode = "customer.session.required" }, statusCode: 401);
        try
        {
            var id = await reviews.SubmitAsync(actor.Value, body, cancellationToken);
            return Results.Json(new { reviewId = id, status = "Pending" }, statusCode: 201);
        }
        catch (InvalidOperationException ex)
        {
            var duplicate = ex.Message.Contains("قبلاً", StringComparison.Ordinal);
            return Results.Json(new { title = duplicate ? "Conflict" : "Bad Request", errorCode = duplicate ? "reviews.duplicate" : "reviews.rejected" },
                statusCode: duplicate ? 409 : 400);
        }
    }

    /// <summary>
    /// فهرست نظرات محصولات متعلق به Offerهای فروشندهٔ مجاز؛ پاسخ فروشنده در دامنه پشتیبانی نمی‌شود.
    /// </summary>
    private static async Task<IResult> SellerListAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        SellerPanelComposer composer,
        IReviewDirectory reviews,
        ICatalogLookupGateway catalog,
        string? status = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (_, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            var productIds = await composer.ListOwnedProductIdsAsync(sellerPartyId, cancellationToken);
            var statusFilter = ParseSellerStatus(status);
            var scoped = await reviews.ListForProductsAsync(productIds, statusFilter, page, pageSize, cancellationToken);
            var titles = await catalog.GetProductTitlesAsync(
                scoped.Items.Select(x => x.ProductId).Distinct().ToArray(),
                cancellationToken);
            return Results.Json(new SellerReviewsResponse(
                scoped.Items.Select(x => new SellerReviewItem(
                    x.ReviewId,
                    titles.GetValueOrDefault(x.ProductId) ?? "محصول",
                    x.AuthorDisplayName,
                    x.Rating,
                    x.Title,
                    x.Body,
                    MapSellerStatusLabel(x.Status),
                    x.Status.ToString(),
                    x.IsVerifiedPurchase,
                    x.CreatedAt)).ToList(),
                scoped.Page,
                scoped.PageSize,
                scoped.TotalCount,
                scoped.PublishedCount,
                scoped.PendingCount,
                scoped.RejectedCount,
                SellerResponseSupported: false));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> PendingAsync(HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant,
        IAuthorizationGuard guard, IHostEnvironment environment, IReviewDirectory reviews, ICatalogLookupGateway catalog, int page = 1, int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try { await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken); }
        catch (PlatformHttpException ex) { return Results.Json(new { title = ex.Message, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode); }
        var pending = await reviews.GetPendingAsync(page, pageSize, cancellationToken);
        var titles = await catalog.GetProductTitlesAsync(pending.Items.Select(x => x.ProductId).Distinct().ToArray(), cancellationToken);
        return Results.Json(new AdminReviewsResponse(
            pending.Items.Select(x => new AdminReviewItem(
                x.ReviewId,
                titles.GetValueOrDefault(x.ProductId) ?? "محصول",
                x.AuthorDisplayName,
                x.Rating,
                x.Title,
                x.Body,
                x.Status.ToString(),
                x.IsVerifiedPurchase,
                x.CreatedAt)).ToList(),
            pending.Page,
            pending.PageSize,
            pending.TotalCount));
    }

    private static async Task<IResult> PublishAsync(Guid reviewId, HttpRequest request, CurrentAuthenticatedSession session,
        ICurrentTenant tenant, IAuthorizationGuard guard, IHostEnvironment environment, IReviewDirectory reviews, CancellationToken cancellationToken)
        => await ModerateAsync(request, session, tenant, guard, environment, cancellationToken,
            actor => reviews.PublishAsync(reviewId, actor, cancellationToken));

    private static async Task<IResult> RejectAsync(Guid reviewId, HttpRequest request,
        CurrentAuthenticatedSession session, ICurrentTenant tenant, IAuthorizationGuard guard, IHostEnvironment environment,
        IReviewDirectory reviews, CancellationToken cancellationToken, RejectReviewRequest? body = null)
        => await ModerateAsync(request, session, tenant, guard, environment, cancellationToken,
            actor => reviews.RejectAsync(reviewId, actor, body?.Reason ?? "رد توسط مدیر", cancellationToken));

    private static async Task<IResult> ModerateAsync(HttpRequest request, CurrentAuthenticatedSession session, ICurrentTenant tenant,
        IAuthorizationGuard guard, IHostEnvironment environment, CancellationToken cancellationToken, Func<Guid, Task> action)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(request, session, tenant, guard, environment, cancellationToken);
            await action(actor); return Results.NoContent();
        }
        catch (PlatformHttpException ex) { return Results.Json(new { title = ex.Message, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode); }
        catch (InvalidOperationException) { return Results.Json(new { title = "Conflict", errorCode = "reviews.moderation.rejected" }, statusCode: 409); }
    }

    private static Guid? ResolveActor(HttpRequest request, CurrentAuthenticatedSession session, IHostEnvironment environment)
    {
        if (session.IsAuthenticated && session.UserId is { } userId) return userId;
        if (environment.IsDevelopment() && request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var actor) && actor != Guid.Empty) return actor;
        return null;
    }

    private static ReviewStatus? ParseSellerStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(status, "Published", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "تایید شده", StringComparison.Ordinal))
        {
            return ReviewStatus.Published;
        }

        if (string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "در انتظار", StringComparison.Ordinal))
        {
            return ReviewStatus.Pending;
        }

        if (string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "رد شده", StringComparison.Ordinal))
        {
            return ReviewStatus.Rejected;
        }

        return null;
    }

    private static string MapSellerStatusLabel(ReviewStatus status) => status switch
    {
        ReviewStatus.Published => "تایید شده",
        ReviewStatus.Pending => "در انتظار",
        ReviewStatus.Rejected => "رد شده",
        _ => status.ToString(),
    };
}

/// <summary>درخواست رد مدیریتی با دلیل داخلی.</summary>
public sealed record RejectReviewRequest(string Reason);

/// <summary>پاسخ عمومی صریح و سازگار Reviews.</summary>
public sealed record PublicReviewsResponse(
    decimal? AverageRating,
    long ReviewCount,
    IReadOnlyDictionary<int, long> RatingDistribution,
    IReadOnlyList<PublicReviewItem> Reviews,
    int Page,
    int PageSize,
    long TotalCount);

/// <summary>ردیف عمومی بررسی بدون هویت داخلی.</summary>
public sealed record PublicReviewItem(
    Guid ReviewId,
    string AuthorDisplayName,
    int Rating,
    string? Title,
    string Body,
    bool VerifiedPurchase,
    DateTimeOffset CreatedAt);

/// <summary>صف صریح مدیریت Reviews.</summary>
public sealed record AdminReviewsResponse(IReadOnlyList<AdminReviewItem> Reviews, int Page, int PageSize, long TotalCount);

/// <summary>ردیف امن صف مدیریت با عنوان واقعی Product.</summary>
public sealed record AdminReviewItem(
    Guid ReviewId,
    string ProductTitle,
    string AuthorDisplayName,
    int Rating,
    string? Title,
    string Body,
    string Status,
    bool VerifiedPurchase,
    DateTimeOffset CreatedAt);

/// <summary>
/// پاسخ فهرست نظرات فروشنده برای محصولات متعلق به Offerهای خودش؛ بدون پاسخ‌فروشنده و بدون هویت داخلی نویسنده.
/// </summary>
public sealed record SellerReviewsResponse(
    IReadOnlyList<SellerReviewItem> Reviews,
    int Page,
    int PageSize,
    long TotalCount,
    long PublishedCount,
    long PendingCount,
    long RejectedCount,
    bool SellerResponseSupported);

/// <summary>ردیف امن فهرست فروشنده با عنوان محصول واقعی و برچسب وضعیت قابل‌نمایش.</summary>
public sealed record SellerReviewItem(
    Guid ReviewId,
    string ProductTitle,
    string AuthorDisplayName,
    int Rating,
    string? Title,
    string Body,
    string StatusLabel,
    string Status,
    bool VerifiedPurchase,
    DateTimeOffset CreatedAt);
