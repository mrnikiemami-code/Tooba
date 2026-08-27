using Tooba.BuildingBlocks;
using Tooba.Host.Seller;

namespace Tooba.Host;

/// <summary>
/// نگاشت Actor↔Seller ثابت Development برای Marketplace (بدون وابستگی به seed SingleStore).
/// </summary>
internal static class MarketplaceSellerDevBootstrap
{
    internal static readonly Guid DefaultSellerParty = Guid.Parse("01a030d1-40cb-7000-8abe-6d31739956c5");
    internal static readonly Guid DefaultSellerActor = Guid.Parse("01a03628-3f68-7000-844d-99f1cadb54b0");

    public static async Task EnsureAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var tuples = provider.GetRequiredService<IAuthorizationTupleWriter>();
        try
        {
            await tuples.WriteAsync(
                new AuthorizationRelationshipWrite
                {
                    Subject = AuthorizationSubject.ForUser(DefaultSellerActor),
                    Resource = new AuthorizationResource
                    {
                        Type = AuthorizationObjectTypes.Party,
                        Id = DefaultSellerParty.ToString("D"),
                    },
                    Relation = AuthorizationRelations.Member,
                },
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return;
        }

        SellerDevActorBootstrap.PublishSnapshot(
            DefaultSellerActor,
            "اپراتور marketplace",
            DefaultSellerParty,
            "فروشگاه marketplace");
    }
}
