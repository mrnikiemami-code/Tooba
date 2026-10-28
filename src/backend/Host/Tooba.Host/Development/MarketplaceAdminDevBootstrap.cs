using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Host.Admin.Access;
using Tooba.Host.Configuration;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;

namespace Tooba.Host;

/// <summary>
/// Actor مدیر پلتفرم Development برای Marketplace (tenant synthetic).
/// </summary>
internal static class MarketplaceAdminDevBootstrap
{
    internal static readonly Guid DefaultAdminActor = Guid.Parse("cccccccc-cccc-4ccc-8ccc-000000000001");

    public static async Task EnsureAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var tuples = provider.GetRequiredService<IAuthorizationTupleWriter>();
        try
        {
            await tuples.WriteAsync(
                new AuthorizationRelationshipWrite
                {
                    Subject = AuthorizationSubject.ForUser(DefaultAdminActor),
                    Resource = new AuthorizationResource
                    {
                        Type = AuthorizationObjectTypes.Tenant,
                        Id = HostAdminPanelAccess.MarketplacePlatformTenantId,
                    },
                    Relation = AuthorizationRelations.Member,
                },
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // ignore when authorization writer unavailable
        }
    }

    /// <summary>
    /// Opt-in commissioning seam: binds ONE normally registered Development administrator account to the
    /// SAME existing Marketplace platform tenant membership used by <see cref="EnsureAsync"/>.
    /// The principal is resolved from the real Identity directory — it is never invented, promoted by
    /// position, or created here, and no credential is read, written or reset.
    /// Inert unless Development + Marketplace + explicit opt-in + a resolvable identifier.
    /// </summary>
    public static async Task ProvisionConfiguredAsync(
        IServiceProvider provider,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        var registry = provider.GetRequiredService<ControlPlaneRegistry>();
        if (registry.Edition != ToobaEdition.Marketplace || registry.MarketplaceConnectionReference is null)
        {
            return;
        }

        var options = provider.GetRequiredService<IOptions<MarketplaceAdminDevOptions>>().Value;
        if (!options.Enabled)
        {
            return;
        }

        var identifier = (options.Identifier ?? string.Empty).Trim();
        if (identifier.Length == 0)
        {
            return;
        }

        var actor = await provider.GetRequiredService<IIdentityAuthenticationService>()
            .FindUserIdByIdentifierAsync(LoginIdentifierKind.Email, identifier, cancellationToken);
        if (actor is null)
        {
            // Unknown/blank identifier refuses provisioning; never falls back to a fixed or arbitrary actor.
            return;
        }

        await provider.GetRequiredService<IAuthorizationTupleWriter>().WriteAsync(
            new AuthorizationRelationshipWrite
            {
                Subject = AuthorizationSubject.ForUser(actor.Value),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Tenant,
                    Id = HostAdminPanelAccess.MarketplacePlatformTenantId,
                },
                Relation = AuthorizationRelations.Member,
            },
            cancellationToken);
    }
}
