using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Content.Domain;
using Tooba.Content.Infrastructure.Persistence;

namespace Tooba.Content.Infrastructure;

/// <summary>دانهٔ توسعهٔ قطعی و idempotent برای مقالات Published خانه و بلاگ.</summary>
public static class ContentDevelopmentSeed
{
    /// <summary>چند مقالهٔ Published فارسی برای ریل خانه و بلاگ درج/تکمیل می‌کند.</summary>
    public static async Task ApplyAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<ContentDbContext>();
        var now = new DateTimeOffset(2026, 8, 20, 10, 0, 0, TimeSpan.Zero);
        var categoryIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var name in new[] { "راهنما", "موبایل", "لوازم خانگی", "پیشنهاد" })
        {
            var slug = ContentCategory.NormalizeSlug(name);
            var existingCategory = await db.Categories
                .SingleOrDefaultAsync(c => c.LanguageCode == ContentArticle.DefaultLocale && c.Slug == slug, cancellationToken);
            if (existingCategory is null)
            {
                var created = ContentCategory.Create(
                    ContentArticle.DefaultLocale,
                    null,
                    name,
                    slug,
                    null,
                    null,
                    categoryIds.Count,
                    null,
                    null,
                    null,
                    now);
                db.Categories.Add(created);
                categoryIds[name] = created.CategoryId;
            }
            else
            {
                categoryIds[name] = existingCategory.CategoryId;
            }
        }

        var authorIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var (displayName, slug) in new[]
        {
            ("تحریریه توبا", "tooba-editorial"),
            ("مریم احمدی", "maryam-ahmadi"),
            ("علی رضایی", "ali-rezaei"),
        })
        {
            var normalizedSlug = ContentAuthor.NormalizeSlug(slug);
            var existingAuthor = await db.Authors
                .SingleOrDefaultAsync(a => a.Slug == normalizedSlug, cancellationToken);
            if (existingAuthor is null)
            {
                var created = ContentAuthor.Create(
                    displayName,
                    slug,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    now);
                db.Authors.Add(created);
                authorIds[displayName] = created.AuthorId;
            }
            else
            {
                authorIds[displayName] = existingAuthor.AuthorId;
            }
        }

        var rows = new[]
        {
            (
                "guide-online-shopping",
                "راهنمای خرید آنلاین هوشمند",
                "نکات عملی برای انتخاب کالا، مقایسهٔ قیمت و خرید امن در فروشگاه‌های آنلاین.",
                "راهنما",
                Guid.Parse("d0d0d0d0-0001-4000-8000-000000000001"),
                "تحریریه توبا",
                new[] { "راهنما", "خرید" },
                true,
                now.AddDays(-2)),
            (
                "mobile-buying-tips",
                "چطور گوشی مناسب انتخاب کنیم؟",
                "معیارهای مهم انتخاب گوشی هوشمند از بودجه تا پشتیبانی و گارانتی.",
                "موبایل",
                Guid.Parse("d0d0d0d0-0002-4000-8000-000000000002"),
                "مریم احمدی",
                new[] { "موبایل", "راهنما" },
                false,
                now.AddDays(-5)),
            (
                "home-appliance-care",
                "نگهداری لوازم خانگی",
                "راهکارهای ساده برای افزایش عمر مفید لوازم خانگی پرکاربرد.",
                "لوازم خانگی",
                Guid.Parse("d0d0d0d0-0003-4000-8000-000000000003"),
                "علی رضایی",
                new[] { "لوازم خانگی" },
                false,
                now.AddDays(-8)),
            (
                "seasonal-offers",
                "پیشنهادهای فصلی را از دست ندهید",
                "چگونه پیشنهادهای واقعی را از تخفیف‌های نمایشی تشخیص دهیم.",
                "پیشنهاد",
                Guid.Parse("d0d0d0d0-0004-4000-8000-000000000004"),
                "تحریریه توبا",
                new[] { "پیشنهاد", "خرید" },
                true,
                now.AddDays(-11)),
        };

        foreach (var row in rows)
        {
            var excerpt = row.Item3;
            var body = $"{excerpt}\n\n{excerpt}\n\nبرای مطالعهٔ بیشتر، این راهنما را تا انتها دنبال کنید.";
            var authorId = authorIds.GetValueOrDefault(row.Item6);
            var existing = await db.Articles.SingleOrDefaultAsync(article => article.Slug == row.Item1, cancellationToken);
            if (existing is null)
            {
                var article = ContentArticle.Create(
                    row.Item1,
                    row.Item2,
                    excerpt,
                    body,
                    row.Item5,
                    authorId,
                    row.Item6,
                    row.Item7,
                    row.Item8,
                    row.Item9,
                    now,
                    ContentArticle.DefaultLocale,
                    row.Item2,
                    excerpt,
                    row.Item4,
                    categoryIds.GetValueOrDefault(row.Item4));
                article.Publish(now);
                db.Articles.Add(article);
                continue;
            }

            if (string.IsNullOrWhiteSpace(existing.Body)
                || string.IsNullOrWhiteSpace(existing.Category)
                || string.IsNullOrWhiteSpace(existing.SeoTitle)
                || existing.AuthorId is null)
            {
                existing.Update(
                    row.Item2,
                    excerpt,
                    body,
                    row.Item2,
                    excerpt,
                    row.Item4,
                    categoryIds.GetValueOrDefault(row.Item4),
                    row.Item5,
                    authorId,
                    row.Item6,
                    row.Item7,
                    row.Item8,
                    now,
                    ContentArticle.DefaultLocale);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
