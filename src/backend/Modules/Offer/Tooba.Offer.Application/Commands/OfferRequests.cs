using MediatR;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application;

/// <summary>Creates a seller offer.</summary>
public sealed record CreateOfferCommand(Guid CatalogVariantId, Guid SellerPartyId, SalesChannel Channel, string? SellerSku) : IRequest<OfferReference>;
/// <summary>Updates seller-owned mutable offer fields.</summary>
public sealed record UpdateOfferCommand(Guid OfferId, Guid SellerPartyId, string? SellerSku, string? Status) : IRequest<OfferReference>;
/// <summary>Activates an offer.</summary>
public sealed record ActivateOfferCommand(Guid OfferId) : IRequest<OfferReference>;
/// <summary>Suspends an offer.</summary>
public sealed record SuspendOfferCommand(Guid OfferId) : IRequest<OfferReference>;
/// <summary>Archives an offer.</summary>
public sealed record ArchiveOfferCommand(Guid OfferId) : IRequest<OfferReference>;
/// <summary>Sets an offer return policy.</summary>
public sealed record SetReturnPolicyCommand(Guid OfferId, string Choice, int? CustomReturnWindowDays) : IRequest<OfferReference>;
/// <summary>Sets offer order quantity limits.</summary>
public sealed record SetOrderQuantityLimitsCommand(Guid OfferId, decimal? Minimum, decimal? Maximum) : IRequest<OfferReference>;
