using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Content.Domain;
using Tooba.Content.Infrastructure.Persistence;

namespace Tooba.Content.Infrastructure;

/// <summary>دانهٔ توسعهٔ قطعی و idempotent برای مقالات Published خانه و بلاگ (fa/en).</summary>
public static class ContentDevelopmentSeed
{
    private const string EnLocale = "en-US";

    /// <summary>
    /// دسته‌ها، نویسندگان و مقالات دمو را درج/تکمیل می‌کند.
    /// Idempotent: دسته با (LanguageCode+Slug)، نویسنده با Slug، مقاله با (Locale+Slug).
    /// </summary>
    public static async Task ApplyAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<ContentDbContext>();
        var now = new DateTimeOffset(2026, 8, 20, 10, 0, 0, TimeSpan.Zero);
        // تاریخ انتشار آیندهٔ ثابت تا lookup عمومی Scheduled را پایدار نگه دارد.
        var scheduledPublishDate = new DateTimeOffset(2027, 12, 1, 9, 0, 0, TimeSpan.Zero);

        var categoryIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        await EnsureCategoriesAsync(db, categoryIds, now, cancellationToken);
        var authorIds = await EnsureAuthorsAsync(db, now, cancellationToken);

        var cover1 = Guid.Parse("d0d0d0d0-0001-4000-8000-000000000001");
        var cover2 = Guid.Parse("d0d0d0d0-0002-4000-8000-000000000002");
        var cover3 = Guid.Parse("d0d0d0d0-0003-4000-8000-000000000003");
        var cover4 = Guid.Parse("d0d0d0d0-0004-4000-8000-000000000004");

        var rows = new SeedArticle[]
        {
            // --- fa-IR Published (موجود + ارتقا) ---
            new(
                ContentArticle.DefaultLocale,
                "guide-online-shopping",
                "راهنمای خرید آنلاین هوشمند",
                "نکات عملی برای انتخاب کالا، مقایسهٔ قیمت و خرید امن در فروشگاه‌های آنلاین.",
                "راهنما",
                cover1,
                "تحریریه توبا",
                ["راهنما", "خرید"],
                Featured: true,
                PublishDate: now.AddDays(-2),
                Kind: SeedKind.Published),
            new(
                ContentArticle.DefaultLocale,
                "mobile-buying-tips",
                "چطور گوشی مناسب انتخاب کنیم؟",
                "معیارهای مهم انتخاب گوشی هوشمند از بودجه تا پشتیبانی و گارانتی.",
                "موبایل",
                cover2,
                "مریم احمدی",
                ["موبایل", "راهنما"],
                Featured: false,
                PublishDate: now.AddDays(-5),
                Kind: SeedKind.Published),
            new(
                ContentArticle.DefaultLocale,
                "home-appliance-care",
                "نگهداری لوازم خانگی",
                "راهکارهای ساده برای افزایش عمر مفید لوازم خانگی پرکاربرد.",
                "لوازم خانگی",
                cover3,
                "علی رضایی",
                ["لوازم خانگی"],
                Featured: false,
                PublishDate: now.AddDays(-8),
                Kind: SeedKind.Published),
            new(
                ContentArticle.DefaultLocale,
                "seasonal-offers",
                "پیشنهادهای فصلی را از دست ندهید",
                "چگونه پیشنهادهای واقعی را از تخفیف‌های نمایشی تشخیص دهیم.",
                "پیشنهاد",
                cover4,
                "تحریریه توبا",
                ["پیشنهاد", "خرید"],
                Featured: true,
                PublishDate: now.AddDays(-11),
                Kind: SeedKind.Published),

            // --- en-US Published (مستقل؛ یک slug مشترک با FA برای اثبات Locale+Slug) ---
            new(
                EnLocale,
                "guide-online-shopping",
                "Smart online shopping guide",
                "Practical tips for comparing prices and buying safely in online stores.",
                "Guides",
                cover1,
                "Jordan Blake",
                ["guides", "shopping"],
                Featured: true,
                PublishDate: now.AddDays(-3),
                Kind: SeedKind.Published,
                SeoTitle: "Smart online shopping guide | Tooba",
                SeoDescription: "Compare prices and shop safely with Tooba's practical online shopping guide."),
            new(
                EnLocale,
                "choosing-a-smartphone",
                "How to choose the right smartphone",
                "Budget, support, and warranty criteria for picking a phone that fits.",
                "Mobile",
                cover2,
                "مریم احمدی",
                ["mobile", "guides"],
                Featured: false,
                PublishDate: now.AddDays(-6),
                Kind: SeedKind.Published,
                SeoTitle: "Choose the right smartphone | Tooba",
                SeoDescription: "A clear checklist for budget, support, and warranty when buying a phone."),
            new(
                EnLocale,
                "home-essentials-checklist",
                "Home essentials checklist",
                "A short list of must-have appliances and care tips for a new home.",
                "Home",
                cover3,
                "علی رضایی",
                ["home", "checklist"],
                Featured: false,
                PublishDate: now.AddDays(-9),
                Kind: SeedKind.Published,
                SeoTitle: "Home essentials checklist | Tooba",
                SeoDescription: "Must-have appliances and simple care tips for a new home setup."),
            new(
                EnLocale,
                "weekend-deals-decoded",
                "Weekend deals decoded",
                "How to tell a real promotion from a display-only discount.",
                "Offers",
                cover4,
                "تحریریه توبا",
                ["offers", "deals"],
                Featured: true,
                PublishDate: now.AddDays(-12),
                Kind: SeedKind.Published,
                SeoTitle: "Weekend deals decoded | Tooba",
                SeoDescription: "Spot real promotions and skip display-only discounts this weekend."),

            // --- Drafts (منتشر نشوند) ---
            new(
                ContentArticle.DefaultLocale,
                "draft-fa-shopping-notes",
                "یادداشت‌های خرید (پیش‌نویس)",
                "یادداشت داخلی برای تکمیل راهنمای خرید فارسی.",
                "راهنما",
                cover1,
                "تحریریه توبا",
                ["پیش‌نویس", "راهنما"],
                Featured: false,
                PublishDate: now,
                Kind: SeedKind.Draft),
            new(
                EnLocale,
                "draft-en-buying-checklist",
                "Buying checklist (draft)",
                "Internal notes for an upcoming English buying checklist.",
                "Guides",
                cover2,
                "Jordan Blake",
                ["draft", "guides"],
                Featured: false,
                PublishDate: now,
                Kind: SeedKind.Draft,
                SeoTitle: "Buying checklist draft | Tooba",
                SeoDescription: "Draft English checklist for upcoming publish."),

            // --- Scheduled Published (fa-IR؛ تاریخ آینده → عمومی نیست) ---
            new(
                ContentArticle.DefaultLocale,
                "scheduled-fa-festival-guide",
                "راهنمای جشنوارهٔ آینده",
                "این مقاله برای انتشار در آینده زمان‌بندی شده است.",
                "پیشنهاد",
                cover3,
                "تحریریه توبا",
                ["جشنواره", "زمان‌بندی"],
                Featured: true,
                PublishDate: scheduledPublishDate,
                Kind: SeedKind.ScheduledPublished),
        };

        foreach (var row in rows)
        {
            await UpsertArticleAsync(db, row, categoryIds, authorIds, now, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureCategoriesAsync(
        ContentDbContext db,
        Dictionary<string, Guid> categoryIds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var faCategories = new[]
        {
            ("راهنما", "راهنما"),
            ("موبایل", "موبایل"),
            ("لوازم خانگی", "لوازم خانگی"),
            ("پیشنهاد", "پیشنهاد"),
        };
        var sort = 0;
        foreach (var (name, slugSource) in faCategories)
        {
            var slug = ContentCategory.NormalizeSlug(slugSource);
            var existing = await db.Categories
                .SingleOrDefaultAsync(
                    c => c.LanguageCode == ContentArticle.DefaultLocale && c.Slug == slug,
                    cancellationToken);
            if (existing is null)
            {
                var created = ContentCategory.Create(
                    ContentArticle.DefaultLocale,
                    null,
                    name,
                    slug,
                    null,
                    null,
                    sort,
                    null,
                    null,
                    null,
                    now);
                db.Categories.Add(created);
                categoryIds[name] = created.CategoryId;
            }
            else
            {
                categoryIds[name] = existing.CategoryId;
            }

            sort++;
        }

        var enCategories = new[]
        {
            ("Guides", "guides"),
            ("Mobile", "mobile"),
            ("Home", "home"),
            ("Offers", "offers"),
        };
        sort = 0;
        foreach (var (name, slugSource) in enCategories)
        {
            var slug = ContentCategory.NormalizeSlug(slugSource);
            var existing = await db.Categories
                .SingleOrDefaultAsync(c => c.LanguageCode == EnLocale && c.Slug == slug, cancellationToken);
            if (existing is null)
            {
                var created = ContentCategory.Create(
                    EnLocale,
                    null,
                    name,
                    slug,
                    null,
                    null,
                    sort,
                    $"{name} | Tooba",
                    $"Browse {name.ToLowerInvariant()} articles on Tooba.",
                    null,
                    now);
                db.Categories.Add(created);
                categoryIds[name] = created.CategoryId;
            }
            else
            {
                categoryIds[name] = existing.CategoryId;
            }

            sort++;
        }
    }

    private static async Task<Dictionary<string, Guid>> EnsureAuthorsAsync(
        ContentDbContext db,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var authorIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var (displayName, slug) in new[]
        {
            ("تحریریه توبا", "tooba-editorial"),
            ("مریم احمدی", "maryam-ahmadi"),
            ("علی رضایی", "ali-rezaei"),
            ("Jordan Blake", "jordan-blake"),
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

        return authorIds;
    }

    private static async Task UpsertArticleAsync(
        ContentDbContext db,
        SeedArticle row,
        Dictionary<string, Guid> categoryIds,
        Dictionary<string, Guid> authorIds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var excerpt = row.Excerpt;
        var body = $"{excerpt}\n\n{excerpt}\n\nبرای مطالعهٔ بیشتر، این راهنما را تا انتها دنبال کنید.";
        if (string.Equals(row.Locale, EnLocale, StringComparison.Ordinal))
        {
            body = $"{excerpt}\n\n{excerpt}\n\nRead through this guide for the full checklist.";
        }

        var authorId = authorIds.GetValueOrDefault(row.AuthorDisplayName);
        var categoryId = categoryIds.GetValueOrDefault(row.CategoryName);
        var seoTitle = row.SeoTitle ?? row.Title;
        var seoDescription = row.SeoDescription ?? excerpt;
        var normalizedSlug = row.Slug.Trim().ToLowerInvariant();

        var existing = await db.Articles.SingleOrDefaultAsync(
            article => article.Slug == normalizedSlug && article.Locale == row.Locale,
            cancellationToken);

        if (existing is null)
        {
            var article = ContentArticle.Create(
                row.Slug,
                row.Title,
                excerpt,
                body,
                row.CoverMediaAssetId,
                authorId == Guid.Empty ? null : authorId,
                row.AuthorDisplayName,
                row.Tags,
                row.Featured,
                row.PublishDate,
                now,
                row.Locale,
                seoTitle,
                seoDescription,
                row.CategoryName,
                categoryId == Guid.Empty ? null : categoryId);

            if (row.Kind is SeedKind.Published or SeedKind.ScheduledPublished)
            {
                article.Publish(now);
            }

            db.Articles.Add(article);
            return;
        }

        var needsUpgrade = string.IsNullOrWhiteSpace(existing.Body)
            || string.IsNullOrWhiteSpace(existing.Category)
            || string.IsNullOrWhiteSpace(existing.SeoTitle)
            || existing.AuthorId is null
            || existing.CategoryId is null
            || existing.CoverMediaAssetId is null
            || (row.Kind is SeedKind.Published or SeedKind.ScheduledPublished
                && existing.Status != ContentPublicationStatus.Published)
            || (row.Kind == SeedKind.Draft && existing.Status == ContentPublicationStatus.Published)
            || (row.Kind == SeedKind.ScheduledPublished && existing.PublishDate <= now);

        if (!needsUpgrade)
        {
            return;
        }

        existing.Update(
            row.Title,
            excerpt,
            body,
            seoTitle,
            seoDescription,
            row.CategoryName,
            categoryId == Guid.Empty ? null : categoryId,
            row.CoverMediaAssetId,
            authorId == Guid.Empty ? null : authorId,
            row.AuthorDisplayName,
            row.Tags,
            row.Featured,
            now,
            row.Locale,
            row.PublishDate);

        switch (row.Kind)
        {
            case SeedKind.Published:
            case SeedKind.ScheduledPublished:
                if (existing.Status != ContentPublicationStatus.Published)
                {
                    existing.Publish(now);
                }

                break;
            case SeedKind.Draft:
                if (existing.Status == ContentPublicationStatus.Published)
                {
                    existing.Unpublish(now);
                }

                break;
        }
    }

    private enum SeedKind
    {
        Published,
        Draft,
        ScheduledPublished,
    }

    private sealed record SeedArticle(
        string Locale,
        string Slug,
        string Title,
        string Excerpt,
        string CategoryName,
        Guid CoverMediaAssetId,
        string AuthorDisplayName,
        string[] Tags,
        bool Featured,
        DateTimeOffset PublishDate,
        SeedKind Kind,
        string? SeoTitle = null,
        string? SeoDescription = null);
}
