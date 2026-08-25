using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Order.Application;
using Tooba.Reviews.Application;
using Tooba.Reviews.Domain;
using Tooba.Reviews.Infrastructure.Persistence;

namespace Tooba.Reviews.Infrastructure;

/// <summary>دایرکتوری Reviews با خواندن فقط از قرارداد Catalog/Order و schema خودش.</summary>
public sealed class ReviewDirectory : IReviewDirectory
{
    private readonly ReviewsDbContext _db;
    private readonly ICatalogLookupGateway _catalog;
    private readonly IOrderPurchaseVerificationGateway _orders;

    /// <summary>وابستگی‌های مالک را تزریق می‌کند؛ DbContext خارجی پذیرفته نمی‌شود.</summary>
    public ReviewDirectory(ReviewsDbContext db, ICatalogLookupGateway catalog, IOrderPurchaseVerificationGateway orders)
    {
        _db = db; _catalog = catalog; _orders = orders;
    }

    /// <inheritdoc />
    public async Task<Guid> SubmitAsync(Guid actorUserId, SubmitProductReview request, CancellationToken cancellationToken)
    {
        var product = await _catalog.FindReviewableProductByIdAsync(request.ProductId, cancellationToken);
        if (product is null || product.Status != CatalogPublicationStatus.Published)
            throw new InvalidOperationException("محصول منتشرشده پیدا نشد.");
        if (await _db.Reviews.AnyAsync(x => x.ProductId == product.ProductId && x.AuthorUserId == actorUserId, cancellationToken))
            throw new InvalidOperationException("برای این محصول قبلاً بررسی ثبت شده است.");

        var proof = await _orders.VerifyPaidPurchaseAsync(actorUserId, product.VariantIds, cancellationToken);
        var review = ProductReview.Create(product.ProductId, actorUserId, "مشتری توبا", request.Rating,
            request.Title, request.Body, proof.IsVerified, proof.SellerOrderId, DateTimeOffset.UtcNow);
        _db.Reviews.Add(review);
        try { await _db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            _db.Entry(review).State = EntityState.Detached;
            if (await _db.Reviews.AnyAsync(x => x.ProductId == product.ProductId && x.AuthorUserId == actorUserId, cancellationToken))
                throw new InvalidOperationException("برای این محصول قبلاً بررسی ثبت شده است.");
            throw;
        }
        return review.ReviewId;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, ProductReviewSummary>> GetPublishedSummariesAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0) return new Dictionary<Guid, ProductReviewSummary>();
        return await _db.Reviews.AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId) && x.Status == ReviewStatus.Published)
            .GroupBy(x => x.ProductId)
            .Select(x => new ProductReviewSummary(x.Key, x.LongCount(), x.Average(y => (decimal)y.Rating)))
            .ToDictionaryAsync(x => x.ProductId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PublishedReviewPage?> GetPublishedAsync(string productSlug, int page, int pageSize, CancellationToken cancellationToken)
    {
        var product = await _catalog.FindReviewableProductBySlugAsync(productSlug, cancellationToken);
        if (product is null || product.Status != CatalogPublicationStatus.Published) return null;
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Reviews.AsNoTracking().Where(x => x.ProductId == product.ProductId && x.Status == ReviewStatus.Published);
        var ratings = await query.GroupBy(x => x.Rating).Select(x => new { Rating = x.Key, Count = x.LongCount() }).ToListAsync(cancellationToken);
        var summary = ReviewSummaryCalculator.Calculate(ratings.Select(x => (x.Rating, x.Count)));
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.ReviewId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new PublishedReview(x.ReviewId, x.AuthorDisplayName, x.Rating, x.Title, x.Body, x.IsVerifiedPurchase, x.CreatedAt))
            .ToListAsync(cancellationToken);
        return new PublishedReviewPage(summary, items, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<ModerationReviewPage> GetPendingAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Reviews.AsNoTracking().Where(x => x.Status == ReviewStatus.Pending);
        var totalCount = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.ReviewId).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ModerationReview(x.ReviewId, x.ProductId, x.AuthorDisplayName, x.Rating, x.Title, x.Body, x.IsVerifiedPurchase, x.CreatedAt, x.Status))
            .ToListAsync(cancellationToken);
        return new ModerationReviewPage(items, page, pageSize, totalCount);
    }

    /// <inheritdoc />
    public async Task PublishAsync(Guid reviewId, Guid moderatorUserId, CancellationToken cancellationToken)
    {
        var review = await _db.Reviews.SingleOrDefaultAsync(x => x.ReviewId == reviewId, cancellationToken)
            ?? throw new InvalidOperationException("بررسی پیدا نشد.");
        review.Publish(moderatorUserId, DateTimeOffset.UtcNow); await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RejectAsync(Guid reviewId, Guid moderatorUserId, string reason, CancellationToken cancellationToken)
    {
        var review = await _db.Reviews.SingleOrDefaultAsync(x => x.ReviewId == reviewId, cancellationToken)
            ?? throw new InvalidOperationException("بررسی پیدا نشد.");
        review.Reject(moderatorUserId, reason, DateTimeOffset.UtcNow); await _db.SaveChangesAsync(cancellationToken);
    }
}
