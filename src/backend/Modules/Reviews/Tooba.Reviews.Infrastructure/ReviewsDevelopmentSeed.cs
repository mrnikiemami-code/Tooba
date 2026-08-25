using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Catalog.Application;
using Tooba.Reviews.Domain;
using Tooba.Reviews.Infrastructure.Persistence;

namespace Tooba.Reviews.Infrastructure;

/// <summary>دانهٔ توسعهٔ قطعی و idempotent؛ خرید تأییدشدهٔ جعلی تولید نمی‌کند.</summary>
public static class ReviewsDevelopmentSeed
{
    /// <summary>چند امتیاز Published و یک Pending را برای محصول نمایشی موجود درج می‌کند.</summary>
    public static async Task ApplyAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<ReviewsDbContext>();
        var catalog = services.GetRequiredService<ICatalogLookupGateway>();
        var product = await catalog.FindReviewableProductBySlugAsync("workspace-live-shirt", cancellationToken);
        if (product is null) return;

        var rows = new[]
        {
            (Guid.Parse("12000000-0000-4000-8000-000000000001"), "مریم", 5, "کیفیت خوب", "کیفیت پارچه و دوخت رضایت‌بخش بود.", true),
            (Guid.Parse("12000000-0000-4000-8000-000000000002"), "علی", 4, "خرید مناسب", "اندازه مطابق توضیحات محصول بود.", true),
            (Guid.Parse("12000000-0000-4000-8000-000000000003"), "سارا", 3, null, "در مجموع محصول قابل قبولی است.", true),
            (Guid.Parse("12000000-0000-4000-8000-000000000004"), "کاربر تازه", 5, "در انتظار بررسی", "این متن هنوز باید توسط مدیر بررسی شود.", false),
        };
        var now = new DateTimeOffset(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);
        foreach (var row in rows)
        {
            if (await db.Reviews.AnyAsync(x => x.ProductId == product.ProductId && x.AuthorUserId == row.Item1, cancellationToken)) continue;
            var review = ProductReview.Create(product.ProductId, row.Item1, row.Item2, row.Item3, row.Item4, row.Item5, false, null, now);
            if (row.Item6) review.Publish(Guid.Parse("12000000-0000-4000-8000-000000000099"), now);
            db.Reviews.Add(review);
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
