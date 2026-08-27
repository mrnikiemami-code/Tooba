using Microsoft.EntityFrameworkCore;
using Tooba.Host.Admin;
using Tooba.Host.Seller;
using Tooba.Host.Storefront;
using Tooba.OperatorProfile.Application;
using Tooba.Party.Application;
using Tooba.Party.Infrastructure.Persistence;
using Tooba.UserPreference.Application;
using UserPreferenceEntity = Tooba.UserPreference.Domain.UserPreference;

namespace Tooba.Host.Settings;

/// <summary>
/// دانهٔ Development تنظیمات: پروفایل سازمانی فروشنده، locale، و پروفایل اپراتور.
/// </summary>
public static class SettingsFoundationDevelopmentSeed
{
    /// <summary>توضیح نمایشی فروشگاه آرمان.</summary>
    public const string SellerADescription = "فروشگاه نمایشی آرمان برای پیش‌نمایش تنظیمات فروشنده.";

    /// <summary>تلفن پشتیبانی نمایشی.</summary>
    public const string SellerASupportPhone = "02191000000";

    /// <summary>ایمیل پشتیبانی نمایشی.</summary>
    public const string SellerASupportEmail = "support-arman@tooba.local";

    /// <summary>نشانی نمایشی.</summary>
    public const string SellerAAddressLine = "تهران، خیابان نمایشی آرمان، پلاک ۱";

    /// <summary>
    /// دانه‌های idempotent را اعمال می‌کند؛ فقط در Development صدا زده می‌شود.
    /// </summary>
    public static async Task ApplyAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var environment = services.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
        {
            return;
        }

        await SeedSellerOrganizationProfileAsync(services, cancellationToken);
        await SeedUserPreferencesAsync(services, cancellationToken);
        await SeedOperatorProfileAsync(services, cancellationToken);
    }

    private static async Task SeedSellerOrganizationProfileAsync(
        IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        var partyDb = provider.GetRequiredService<PartyDbContext>();
        var parties = provider.GetRequiredService<IPartyDirectory>();
        var seller = await partyDb.Parties.AsNoTracking()
            .SingleOrDefaultAsync(x => x.DisplayName == SellerDevActorBootstrap.SellerADisplayName, cancellationToken);
        if (seller is null)
        {
            return;
        }

        var existing = await parties.GetOrganizationProfileAsync(seller.PartyId, cancellationToken);
        if (existing is not null
            && !string.IsNullOrWhiteSpace(existing.Description)
            && !string.IsNullOrWhiteSpace(existing.SupportPhone))
        {
            return;
        }

        await parties.UpdateOrganizationProfileAsync(
            seller.PartyId,
            new OrganizationProfileWrite(
                seller.DisplayName,
                seller.LegalName,
                SellerADescription,
                SellerASupportPhone,
                SellerASupportEmail,
                SellerAAddressLine),
            cancellationToken);
    }

    private static async Task SeedUserPreferencesAsync(
        IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        var directory = provider.GetRequiredService<IUserPreferenceDirectory>();
        var guest = StorefrontCheckoutComposer.StorefrontGuestActorId;
        if (await directory.GetAsync(guest, cancellationToken) is null)
        {
            await directory.UpsertAsync(
                guest,
                new UserPreferenceWrite(UserPreferenceEntity.LocaleFa),
                cancellationToken);
        }

        var admin = AdminDevActorBootstrap.Snapshot?.ActorUserId;
        if (admin is Guid adminId && await directory.GetAsync(adminId, cancellationToken) is null)
        {
            await directory.UpsertAsync(
                adminId,
                new UserPreferenceWrite(UserPreferenceEntity.LocaleFa),
                cancellationToken);
        }
    }

    private static async Task SeedOperatorProfileAsync(
        IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        var admin = AdminDevActorBootstrap.Snapshot?.ActorUserId;
        if (admin is not Guid adminId)
        {
            return;
        }

        var directory = provider.GetRequiredService<IOperatorProfileDirectory>();
        if (await directory.GetAsync(adminId, cancellationToken) is not null)
        {
            return;
        }

        await directory.UpsertAsync(
            adminId,
            new OperatorProfileWrite(
                "اپراتور نمایشی توبا",
                "اپراتور",
                "نمایشی توبا",
                "پروفایل آزمایشی Development برای پنل Admin."),
            cancellationToken);
    }
}
