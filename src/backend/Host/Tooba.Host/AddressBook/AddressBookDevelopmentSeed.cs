using Microsoft.EntityFrameworkCore;
using Tooba.AddressBook.Domain;
using Tooba.AddressBook.Infrastructure.Persistence;
using Tooba.Host.Storefront;

namespace Tooba.Host.AddressBook;

/// <summary>دانهٔ قطعی Development برای دفترچهٔ آدرس مشتری نمایشی فروشگاه.</summary>
public static class AddressBookDevelopmentSeed
{
    /// <summary>شناسهٔ پایدار نشانی پیش‌فرض مشتری نمایشی.</summary>
    public static readonly Guid DefaultAddressId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-0000000000a1");

    /// <summary>شناسهٔ پایدار نشانی جایگزین مشتری نمایشی.</summary>
    public static readonly Guid AlternateAddressId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-0000000000a2");

    /// <summary>
    /// دو نشانی ساختگی غیرشخصی را برای <see cref="StorefrontCheckoutComposer.StorefrontGuestActorId"/>
    /// به‌صورت idempotent درج می‌کند؛ در Production صدا زده نمی‌شود.
    /// </summary>
    public static async Task ApplyAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<AddressBookDbContext>();
        var actor = StorefrontCheckoutComposer.StorefrontGuestActorId;
        var createdAt = new DateTimeOffset(2026, 8, 25, 14, 0, 0, TimeSpan.Zero);
        if (!await db.Addresses.AnyAsync(x => x.AddressId == DefaultAddressId, cancellationToken))
        {
            db.Addresses.Add(CustomerAddress.Create(
                actor,
                "گیرندهٔ نمایشی توبا",
                "+989120000014",
                "IR",
                "تهران",
                "تهران",
                "19199",
                "خیابان نمونه، پلاک ۱۴، دفتر نمایشی فروشگاه",
                "واحد ۱",
                "خانه",
                isDefault: true,
                createdAt,
                DefaultAddressId));
        }

        if (!await db.Addresses.AnyAsync(x => x.AddressId == AlternateAddressId, cancellationToken))
        {
            db.Addresses.Add(CustomerAddress.Create(
                actor,
                "تحویل‌گیرندهٔ جایگزین",
                "+989330000033",
                "IR",
                "اصفهان",
                "اصفهان",
                "81456",
                "خیابان چهارباغ، پلاک ۳۳، انبار نمایشی",
                "طبقه ۲",
                "محل کار",
                isDefault: false,
                createdAt.AddMinutes(5),
                AlternateAddressId));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
