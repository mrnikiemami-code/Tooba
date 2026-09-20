using MediatR;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application;

/// <summary>Gets an offer by identifier.</summary>
public sealed record GetOfferQuery(Guid OfferId) : IRequest<OfferReference?>;

/// <summary>Lists offers owned by a seller.</summary>
public sealed record ListSellerOffersQuery(Guid SellerPartyId) : IRequest<IReadOnlyList<OfferReference>>;
