using Tooba.Catalog.Application;
using Tooba.OperatorProfile.Contracts.Ports;

namespace Tooba.Catalog.Endpoints.Admin;

/// <summary>
/// Module-owned Catalog actor binding for product-history mutations.
/// Does not copy Host CatalogActorHttpBinding; uses Contracts-only OperatorProfile lookup.
/// </summary>
internal static class CatalogActorRequestBinding
{
    /// <summary>Binds ActorUserId / DisplayName onto scoped <see cref="ICatalogActorContext"/>.</summary>
    public static async Task BindAsync(
        HttpContext http,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(http);
        var actor = http.RequestServices.GetRequiredService<ICatalogActorContext>();
        actor.ActorUserId = actorUserId;

        var displays = http.RequestServices.GetService<IActorDisplayLookup>();
        if (displays is null)
        {
            actor.ActorDisplayName = "اپراتور";
            return;
        }

        var map = await displays.GetActorDisplaysAsync([actorUserId], cancellationToken);
        if (map.TryGetValue(actorUserId, out var projection)
            && !string.IsNullOrWhiteSpace(projection.DisplayName))
        {
            actor.ActorDisplayName = projection.DisplayName.Trim();
            return;
        }

        actor.ActorDisplayName = "اپراتور";
    }
}
