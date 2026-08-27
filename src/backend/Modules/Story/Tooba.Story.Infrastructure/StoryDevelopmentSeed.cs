using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Story.Domain;
using Tooba.Story.Infrastructure.Persistence;
using StoryEntity = Tooba.Story.Domain.Story;

namespace Tooba.Story.Infrastructure;

/// <summary>دانهٔ توسعهٔ idempotent برای استوری‌های فعال StoreAlpha.</summary>
public static class StoryDevelopmentSeed
{
    /// <summary>فروشندهٔ نمونهٔ Marketplace برای استوری‌های Seller.</summary>
    public static readonly Guid DevSellerPartyId = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");

    /// <summary>Actor نمونهٔ فروشنده.</summary>
    public static readonly Guid DevSellerActorId = Guid.Parse("01a03628-3f68-7000-844d-99f1cadb54b0");

    /// <summary>استوری‌های نمونه را برای Tenantهای dev درج می‌کند.</summary>
    public static async Task ApplyAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<StoryDbContext>();
        var now = new DateTimeOffset(2026, 8, 27, 6, 0, 0, TimeSpan.Zero);
        await EnsureAsync(db, StoryTenantIds.StoreAlpha, now, cancellationToken);
    }

    /// <summary>حداقل دو استوری Active با آیتم برای Tenant مشخص می‌سازد.</summary>
    public static async Task EnsureAsync(
        StoryDbContext db,
        Guid tenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.Stories.AnyAsync(story => story.TenantId == tenantId, cancellationToken);
        if (!exists)
        {
            var mobile = StoryEntity.CreateDraft(
                tenantId,
                "موبایل",
                displayOrder: 0,
                now,
                locale: "fa",
                coverMediaUrl: "/images/stories/1.jpg",
                ctaType: "internal",
                ctaTarget: "/products");
            mobile.AddItem(
                StoryRules.MediaImage,
                displayOrder: 0,
                now,
                mediaUrl: "/images/stories/1.jpg",
                ctaType: "internal",
                ctaTarget: "/products");
            mobile.AddItem(
                StoryRules.MediaImage,
                displayOrder: 1,
                now,
                mediaUrl: "/images/stories/2.jpg",
                ctaType: "internal",
                ctaTarget: "/products");
            mobile.Activate(now);

            var games = StoryEntity.CreateDraft(
                tenantId,
                "بازی",
                displayOrder: 1,
                now,
                locale: null,
                coverMediaUrl: "/images/stories/video/1.mp4",
                ctaType: "category",
                ctaTarget: "/offers");
            games.AddItem(
                StoryRules.MediaVideo,
                displayOrder: 0,
                now,
                mediaUrl: "/images/stories/video/1.mp4",
                durationMs: 8000,
                ctaType: "category",
                ctaTarget: "/offers");
            games.AddItem(
                StoryRules.MediaImage,
                displayOrder: 1,
                now,
                mediaUrl: "/images/stories/3.jpg",
                ctaType: "internal",
                ctaTarget: "/offers");
            games.Activate(now);

            var english = StoryEntity.CreateDraft(
                tenantId,
                "English rail",
                displayOrder: 2,
                now,
                locale: "en",
                coverMediaUrl: "/images/stories/1.jpg",
                ctaType: "internal",
                ctaTarget: "/products");
            english.AddItem(
                StoryRules.MediaImage,
                displayOrder: 0,
                now,
                mediaUrl: "/images/stories/1.jpg",
                ctaType: "internal",
                ctaTarget: "/products");
            english.Activate(now);

            db.Stories.Add(mobile);
            db.Stories.Add(games);
            db.Stories.Add(english);
            foreach (var item in mobile.Items.Concat(games.Items).Concat(english.Items))
                db.StoryItems.Add(item);
            await db.SaveChangesAsync(cancellationToken);
        }

        await EnsureSellerReviewSamplesAsync(db, tenantId, now, cancellationToken);
    }

    private static async Task EnsureSellerReviewSamplesAsync(
        StoryDbContext db,
        Guid tenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var hasSeller = await db.Stories.AnyAsync(
            story => story.TenantId == tenantId && story.Origin == StoryOrigin.Seller,
            cancellationToken);
        if (hasSeller)
            return;

        var draft = StoryEntity.CreateSellerDraft(
            tenantId,
            DevSellerPartyId,
            DevSellerActorId,
            "پیش‌نویس فروشنده",
            displayOrder: 90,
            now,
            locale: "fa",
            coverMediaUrl: "/images/stories/1.jpg",
            ctaType: "internal",
            ctaTarget: "/products");
        draft.AddItem(
            StoryRules.MediaImage,
            displayOrder: 0,
            now,
            mediaUrl: "/images/stories/1.jpg",
            ctaType: "internal",
            ctaTarget: "/products");

        var submitted = StoryEntity.CreateSellerDraft(
            tenantId,
            DevSellerPartyId,
            DevSellerActorId,
            "در انتظار بازبینی",
            displayOrder: 91,
            now,
            locale: "fa",
            coverMediaUrl: "/images/stories/2.jpg",
            ctaType: "internal",
            ctaTarget: "/products");
        submitted.AddItem(
            StoryRules.MediaImage,
            displayOrder: 0,
            now,
            mediaUrl: "/images/stories/2.jpg",
            ctaType: "internal",
            ctaTarget: "/products");
        submitted.SubmitForReview(DevSellerActorId, now);

        db.Stories.Add(draft);
        db.Stories.Add(submitted);
        foreach (var item in draft.Items.Concat(submitted.Items))
            db.StoryItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
    }
}
