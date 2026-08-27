using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.PageComposition.Domain;
using Tooba.PageComposition.Infrastructure.Persistence;

namespace Tooba.PageComposition.Infrastructure;

/// <summary>دانهٔ توسعهٔ idempotent برای ترکیب پیش‌فرض خانه به‌ازای Tenant.</summary>
public static class PageCompositionDevelopmentSeed
{
    /// <summary>ترکیب home پیش‌فرض را برای Tenantهای dev درج می‌کند.</summary>
    public static async Task ApplyAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<PageCompositionDbContext>();
        var now = new DateTimeOffset(2026, 8, 27, 6, 0, 0, TimeSpan.Zero);
        await EnsureHomeAsync(db, PageCompositionTenantIds.StoreAlpha, now, cancellationToken);
    }

    /// <summary>ترکیب home پیش‌فرض را برای Tenant مشخص idempotent می‌سازد.</summary>
    public static async Task EnsureHomeAsync(
        PageCompositionDbContext db,
        Guid tenantId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.PageDefinitions.AnyAsync(
            definition => definition.TenantId == tenantId
                && definition.PageKey == PageKeys.Home
                && definition.Locale == null,
            cancellationToken);
        if (exists)
            return;

        var definition = PageDefinition.CreateDefaultHome(tenantId, locale: null, now);
        db.PageDefinitions.Add(definition);
        foreach (var section in definition.Sections)
            db.PageSections.Add(section);
        await db.SaveChangesAsync(cancellationToken);
    }
}
