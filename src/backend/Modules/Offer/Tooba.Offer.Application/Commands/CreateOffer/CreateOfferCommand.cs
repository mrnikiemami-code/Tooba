using MediatR;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Contracts;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Application.ReadModels;
using Tooba.Offer.Contracts;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Party.Contracts;
using DomainChannel = Tooba.Offer.Domain.ValueObjects.SalesChannel;

namespace Tooba.Offer.Application.Commands.CreateOffer;

/// <summary>Creates a seller offer and applies its complete initial Offer-owned state.</summary>
public sealed record CreateOfferCommand(
    Guid CatalogVariantId,
    Guid SellerPartyId,
    SalesChannel Channel,
    string? SellerSku,
    string? Status = null,
    string? ReturnPolicyChoice = null,
    int? CustomReturnWindowDays = null,
    decimal? MinimumOrderQuantity = null,
    decimal? MaximumOrderQuantity = null) : IRequest<SellerOfferDetailPage>;

internal sealed class CreateOfferHandler(
    IOfferStore store,
    ICatalogVariantLookup catalog,
    IPartyLookup parties,
    IReturnPolicyResolver returnPolicies,
    IClock clock,
    IIdGenerator ids,
    OfferReadModelComposer readModels) : IRequestHandler<CreateOfferCommand, SellerOfferDetailPage>
{
    public async Task<SellerOfferDetailPage> Handle(CreateOfferCommand request, CancellationToken cancellationToken)
    {
        if (await catalog.FindVariantAsync(request.CatalogVariantId, cancellationToken) is null)
            throw new SemanticException(new SemanticError(OfferErrorCodes.CatalogVariantMissing));
        var seller = await parties.FindByIdAsync(request.SellerPartyId, cancellationToken)
            ?? throw new SemanticException(new SemanticError(OfferErrorCodes.SellerMissing));
        if (!string.Equals(seller.Kind, "Organization", StringComparison.Ordinal))
            throw new SemanticException(new SemanticError(OfferErrorCodes.SellerNotOrganization));
        var channel = (DomainChannel)(int)request.Channel;
        if (await store.ExistsActiveListingAsync(request.SellerPartyId, request.CatalogVariantId, channel, cancellationToken))
            throw new SemanticException(new SemanticError(OfferErrorCodes.DuplicateActiveListing));
        var sku = request.SellerSku?.Trim();
        if (!string.IsNullOrWhiteSpace(sku)
            && await store.ExistsSellerSkuAsync(request.SellerPartyId, sku, null, cancellationToken))
            throw new SemanticException(new SemanticError(OfferErrorCodes.DuplicateSellerSku));

        var offer = SellerOffer.Create(ids.NewId(), request.CatalogVariantId, request.SellerPartyId, channel, sku, clock.UtcNow);
        if (request.ReturnPolicyChoice is not null || request.CustomReturnWindowDays is not null)
        {
            var choice = request.ReturnPolicyChoice ?? "Default";
            returnPolicies.ValidateOfferChoice(choice, request.CustomReturnWindowDays);
            offer.SetReturnPolicy(choice, request.CustomReturnWindowDays, clock.UtcNow);
        }
        if (request.MinimumOrderQuantity is not null || request.MaximumOrderQuantity is not null)
            offer.SetOrderQuantityLimits(request.MinimumOrderQuantity, request.MaximumOrderQuantity, clock.UtcNow);
        if (string.Equals(request.Status, nameof(OfferStatus.Active), StringComparison.OrdinalIgnoreCase))
            offer.Activate(clock.UtcNow);
        else if (!string.IsNullOrWhiteSpace(request.Status)
                 && !string.Equals(request.Status, nameof(OfferStatus.Draft), StringComparison.OrdinalIgnoreCase))
            throw new SemanticException(new SemanticError(OfferErrorCodes.StatusUnsupported));

        await store.AddAsync(offer, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);
        return await readModels.DetailAsync(offer, cancellationToken);
    }
}
