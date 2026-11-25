using MediatR;
using Tooba.StoreContext.Contracts.Current;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Application.Offers.Policies;

namespace Tooba.Offer.Application.Offers.Queries.GetAllowedOfferChannels;

/// <summary>
/// Returns the sales channels the current installation edition allows for Offer creation.
/// The set comes from Offer-owned edition policy, never from client input.
/// </summary>
public sealed record GetAllowedOfferChannelsQuery : IRequest<Result<IReadOnlyList<string>>>;

/// <summary>
/// Reads the edition from the trusted request context and projects the allowed channels as stable names.
/// When the edition is unset or unknown the policy allows none, so this deliberately returns an empty
/// success (fail-closed for creation) instead of inventing a default channel.
/// </summary>
internal sealed class GetAllowedOfferChannelsHandler(ICurrentStoreCommerceContext storeCommerce)
    : IRequestHandler<GetAllowedOfferChannelsQuery, Result<IReadOnlyList<string>>>
{
    public Task<Result<IReadOnlyList<string>>> Handle(
        GetAllowedOfferChannelsQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string> channels = OfferChannelPolicy.Allowed(storeCommerce.Current?.SalesChannel)
            .Select(channel => channel.ToString())
            .ToArray();
        return Task.FromResult(Result.Success(channels));
    }
}
