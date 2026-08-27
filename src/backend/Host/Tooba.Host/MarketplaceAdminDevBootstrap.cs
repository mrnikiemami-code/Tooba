using Tooba.BuildingBlocks;
using Tooba.Host.Settlement;

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
                        Id = SettlementAdminAccess.MarketplacePlatformTenantId,
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
}
