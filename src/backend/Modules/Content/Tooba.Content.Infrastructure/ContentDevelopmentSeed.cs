using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Content.Domain;
using Tooba.Content.Infrastructure.Persistence;

namespace Tooba.Content.Infrastructure;

/// <summary>دانهٔ توسعهٔ قطعی و idempotent برای مقالات Published خانه.</summary>
public static class ContentDevelopmentSeed
{
    /// <summary>چند مقالهٔ Published فارسی برای ریل خانه درج می‌کند.</summary>
    public static async Task ApplyAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<ContentDbContext>();
        var now = new DateTimeOffset(2026, 8, 20, 10, 0, 0, TimeSpan.Zero);
        var rows = new[]
        {
            (
                "guide-online-shopping",
                "راهنمای خرید آنلاین هوشمند",
                "نکات عملی برای انتخاب کالا، مقایسهٔ قیمت و خرید امن در فروشگاه‌های آنلاین.",
                Guid.Parse("d0d0d0d0-0001-4000-8000-000000000001"),
                "تحریریه توبا",
                new[] { "راهنما", "خرید" },
                true,
                now.AddDays(-2)),
            (
                "mobile-buying-tips",
                "چطور گوشی مناسب انتخاب کنیم؟",
                "معیارهای مهم انتخاب گوشی هوشمند از بودجه تا پشتیبانی و گارانتی.",
                Guid.Parse("d0d0d0d0-0002-4000-8000-000000000002"),
                "مریم احمدی",
                new[] { "موبایل", "راهنما" },
                false,
                now.AddDays(-5)),
            (
                "home-appliance-care",
                "نگهداری لوازم خانگی",
                "راهکارهای ساده برای افزایش عمر مفید لوازم خانگی پرکاربرد.",
                Guid.Parse("d0d0d0d0-0003-4000-8000-000000000003"),
                "علی رضایی",
                new[] { "لوازم خانگی" },
                false,
                now.AddDays(-8)),
            (
                "seasonal-offers",
                "پیشنهادهای فصلی را از دست ندهید",
                "چگونه پیشنهادهای واقعی را از تخفیف‌های نمایشی تشخیص دهیم.",
                Guid.Parse("d0d0d0d0-0004-4000-8000-000000000004"),
                "تحریریه توبا",
                new[] { "پیشنهاد", "خرید" },
                true,
                now.AddDays(-11)),
        };

        foreach (var row in rows)
        {
            if (await db.Articles.AnyAsync(article => article.Slug == row.Item1, cancellationToken)) continue;
            var article = ContentArticle.Create(
                row.Item1,
                row.Item2,
                row.Item3,
                row.Item4,
                row.Item5,
                row.Item6,
                row.Item7,
                row.Item8,
                now);
            article.Publish(now);
            db.Articles.Add(article);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
