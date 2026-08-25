using Microsoft.EntityFrameworkCore;
using Tooba.CustomerProfile.Domain;
using Tooba.CustomerProfile.Infrastructure.Persistence;
using Tooba.Host.Storefront;

namespace Tooba.Host.CustomerProfile;

/// <summary>دانهٔ قطعی Development برای پروفایل مشتری نمایشی فروشگاه.</summary>
public static class CustomerProfileDevelopmentSeed
{
    /// <summary>
    /// پروفایل ساختگی غیرشخصی را برای <see cref="StorefrontCheckoutComposer.StorefrontGuestActorId"/>
    /// به‌صورت idempotent درج می‌کند؛ در Production صدا زده نمی‌شود.
    /// </summary>
    public static async Task ApplyAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<CustomerProfileDbContext>();
        var actor = StorefrontCheckoutComposer.StorefrontGuestActorId;
        if (await db.Profiles.AnyAsync(x => x.OwnerUserId == actor, cancellationToken))
        {
            return;
        }

        var createdAt = new DateTimeOffset(2026, 8, 25, 16, 0, 0, TimeSpan.Zero);
        db.Profiles.Add(Tooba.CustomerProfile.Domain.CustomerProfile.Create(
            actor,
            "مشتری نمایشی توبا",
            "مشتری",
            "نمایشی توبا",
            "1403/06/04",
            "پروفایل آزمایشی Development برای اتصال UI Shopeiva.",
            createdAt));
        await db.SaveChangesAsync(cancellationToken);
    }
}
