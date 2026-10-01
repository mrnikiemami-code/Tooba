using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.Party.Application;
using Tooba.Party.Contracts;

namespace Tooba.Party.Infrastructure.Development;

/// <summary>
/// دانهٔ Development پروفایل سازمانی فروشندهٔ دمو. مالکیت Party؛ Host فقط orchestrate می‌کند.
/// </summary>
public static class PartyOrganizationProfileDevelopmentSeed
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
    /// پروفایل سازمانی فروشندهٔ دمو را idempotent پر می‌کند؛ فقط Development.
    /// </summary>
    public static async Task ApplyAsync(
        IServiceProvider services,
        string sellerDisplayName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (string.IsNullOrWhiteSpace(sellerDisplayName))
        {
            throw new ArgumentException("Seller display name is required.", nameof(sellerDisplayName));
        }

        var environment = services.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
        {
            return;
        }

        var gateway = services.GetRequiredService<IPartyDevelopmentSeedGateway>();
        var partyId = await gateway.FindDevelopmentOrganizationByDisplayNameAsync(
            sellerDisplayName.Trim(),
            cancellationToken);
        if (partyId is null)
        {
            return;
        }

        var parties = services.GetRequiredService<IPartyDirectory>();
        var existing = await parties.GetOrganizationProfileAsync(partyId.Value, cancellationToken);
        if (existing is not null
            && !string.IsNullOrWhiteSpace(existing.Description)
            && !string.IsNullOrWhiteSpace(existing.SupportPhone))
        {
            return;
        }

        await parties.UpdateOrganizationProfileAsync(
            partyId.Value,
            new OrganizationProfileWrite(
                existing?.DisplayName ?? sellerDisplayName.Trim(),
                existing?.LegalName,
                SellerADescription,
                SellerASupportPhone,
                SellerASupportEmail,
                SellerAAddressLine),
            cancellationToken);
    }
}
