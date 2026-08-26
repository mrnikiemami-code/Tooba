using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Catalog.Application;
using Tooba.Reviews.Domain;
using Tooba.Reviews.Infrastructure.Persistence;

namespace Tooba.Reviews.Infrastructure;

/// <summary>دانهٔ توسعهٔ قطعی و idempotent؛ خرید تأییدشدهٔ جعلی تولید نمی‌کند.</summary>
public static class ReviewsDevelopmentSeed
{
    /// <summary>چند امتیاز Published و یک Pending را برای محصولات نمایشی موجود درج می‌کند.</summary>
    public static async Task ApplyAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<ReviewsDbContext>();
        var catalog = services.GetRequiredService<ICatalogLookupGateway>();
        var now = new DateTimeOffset(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);
        var moderator = Guid.Parse("12000000-0000-4000-8000-000000000099");

        var targets = new (string Slug, (Guid AuthorId, string Name, int Rating, string? Title, string Body, bool Publish)[] Rows)[]
        {
            ("workspace-live-shirt", new (Guid, string, int, string?, string, bool)[]
            {
                (Guid.Parse("12000000-0000-4000-8000-000000000001"), "مریم", 5, "کیفیت خوب", "کیفیت پارچه و دوخت رضایت‌بخش بود.", true),
                (Guid.Parse("12000000-0000-4000-8000-000000000002"), "علی", 4, "خرید مناسب", "اندازه مطابق توضیحات محصول بود.", true),
                (Guid.Parse("12000000-0000-4000-8000-000000000003"), "سارا", 3, null, "در مجموع محصول قابل قبولی است.", true),
                (Guid.Parse("12000000-0000-4000-8000-000000000004"), "کاربر تازه", 5, "در انتظار بررسی", "این متن هنوز باید توسط مدیر بررسی شود.", false),
            }),
            ("demo-mobile-1", new (Guid, string, int, string?, string, bool)[]
            {
                (Guid.Parse("12000000-0000-4000-8000-000000000011"), "رضا نوری", 5, "عالی", "ارسال سریع و بسته‌بندی مرتب بود.", true),
                (Guid.Parse("12000000-0000-4000-8000-000000000012"), "فاطمه", 4, null, "محصول مطابق توضیحات فروشنده رسید.", true),
            }),
            ("demo-mobile-2", new (Guid, string, int, string?, string, bool)[]
            {
                (Guid.Parse("12000000-0000-4000-8000-000000000021"), "حسین", 5, "پیشنهاد می‌کنم", "برای استفادهٔ روزمره انتخاب خوبی بود.", true),
            }),
            ("demo-laptop-1", new (Guid, string, int, string?, string, bool)[]
            {
                (Guid.Parse("12000000-0000-4000-8000-000000000031"), "نگار", 4, "راضی", "عملکرد کلی دستگاه مناسب است.", true),
            }),
        };

        foreach (var target in targets)
        {
            var product = await catalog.FindReviewableProductBySlugAsync(target.Slug, cancellationToken);
            if (product is null) continue;
            foreach (var row in target.Rows)
            {
                if (await db.Reviews.AnyAsync(x => x.ProductId == product.ProductId && x.AuthorUserId == row.AuthorId, cancellationToken)) continue;
                var review = ProductReview.Create(product.ProductId, row.AuthorId, row.Name, row.Rating, row.Title, row.Body, false, null, now);
                if (row.Publish) review.Publish(moderator, now);
                db.Reviews.Add(review);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
