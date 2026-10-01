using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.Catalog.Infrastructure.Development;
using Tooba.Host.Admin.Development;
using Tooba.OperatorProfile.Infrastructure.Development;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Party.Infrastructure.Development;
using Tooba.UserPreference.Infrastructure.Development;

namespace Tooba.Host.Composition;

/// <summary>
/// Thin Host composition for settings-related Development seeds.
/// Binds demo seller display name / guest / admin actors, then delegates to owning modules.
/// Not a Settings HTTP or business owner; Host/Settings folder must stay ABSENT.
/// </summary>
internal static class SettingsFoundationDevelopmentSeedHost
{
    /// <summary>
    /// دانه‌های Party / UserPreference / OperatorProfile را فقط در Development اعمال می‌کند.
    /// </summary>
    public static async Task ApplyAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        var environment = services.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
        {
            return;
        }

        await PartyOrganizationProfileDevelopmentSeed.ApplyAsync(
            services,
            WorkspaceDemoMarketplaceSeed.SellerADisplayName,
            cancellationToken);

        var adminId = AdminDevActorBootstrap.Snapshot?.ActorUserId;
        await UserPreferenceDevelopmentSeed.ApplyAsync(
            services,
            StorefrontGuestActor.ActorId,
            adminId,
            cancellationToken);

        if (adminId is Guid operatorId)
        {
            await OperatorProfileDevelopmentSeed.ApplyAsync(services, operatorId, cancellationToken);
        }
    }
}
