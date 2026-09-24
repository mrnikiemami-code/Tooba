using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Application.ReadModels;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Errors;
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
    decimal? MaximumOrderQuantity = null) : IRequest<Result<SellerOfferDetailPage>>;

internal sealed class CreateOfferHandler(
    IOfferStore store,
    ICatalogVariantLookup catalog,
    IPartyLookup parties,
    IReturnPolicyResolver returnPolicies,
    IClock clock,
    IIdGenerator ids,
    OfferReadModelComposer readModels) : IRequestHandler<CreateOfferCommand, Result<SellerOfferDetailPage>>
{
    public async Task<Result<SellerOfferDetailPage>> Handle(CreateOfferCommand request, CancellationToken cancellationToken)
    {
        if (await catalog.FindVariantAsync(request.CatalogVariantId, cancellationToken) is null)
            return Result.Failure<SellerOfferDetailPage>(new SemanticError(OfferErrorCodes.CatalogVariantMissing));
        var seller = await parties.FindByIdAsync(request.SellerPartyId, cancellationToken);
        if (seller is null)
            return Result.Failure<SellerOfferDetailPage>(new SemanticError(OfferErrorCodes.SellerMissing));
        if (!string.Equals(seller.Kind, "Organization", StringComparison.Ordinal))
            return Result.Failure<SellerOfferDetailPage>(new SemanticError(OfferErrorCodes.SellerNotOrganization));
        var channel = (DomainChannel)(int)request.Channel;
        if (await store.ExistsActiveListingAsync(request.SellerPartyId, request.CatalogVariantId, channel, cancellationToken))
            return Result.Failure<SellerOfferDetailPage>(new SemanticError(OfferErrorCodes.DuplicateActiveListing));
        var sku = request.SellerSku?.Trim();
        if (!string.IsNullOrWhiteSpace(sku)
            && await store.ExistsSellerSkuAsync(request.SellerPartyId, sku, null, cancellationToken))
            return Result.Failure<SellerOfferDetailPage>(new SemanticError(OfferErrorCodes.DuplicateSellerSku));

        var offer = SellerOffer.Create(ids.NewId(), request.CatalogVariantId, request.SellerPartyId, channel, sku, clock.UtcNow);
        if (request.ReturnPolicyChoice is not null || request.CustomReturnWindowDays is not null)
        {
            var choice = request.ReturnPolicyChoice ?? "Default";
            var policy = returnPolicies.ValidateOfferChoice(choice, request.CustomReturnWindowDays);
            if (policy.IsFailure)
                return Result.Failure<SellerOfferDetailPage>(policy.Errors);
            offer.SetReturnPolicy(choice, request.CustomReturnWindowDays, clock.UtcNow);
        }
        if (request.MinimumOrderQuantity is not null || request.MaximumOrderQuantity is not null)
        {
            var qty = offer.SetOrderQuantityLimits(request.MinimumOrderQuantity, request.MaximumOrderQuantity, clock.UtcNow);
            if (qty.IsFailure)
                return Result.Failure<SellerOfferDetailPage>(qty.Errors);
        }
        if (string.Equals(request.Status, nameof(OfferStatus.Active), StringComparison.OrdinalIgnoreCase))
        {
            var activated = offer.Activate(clock.UtcNow);
            if (activated.IsFailure)
                return Result.Failure<SellerOfferDetailPage>(activated.Errors);
        }
        else if (!string.IsNullOrWhiteSpace(request.Status)
                 && !string.Equals(request.Status, nameof(OfferStatus.Draft), StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<SellerOfferDetailPage>(new SemanticError(OfferErrorCodes.StatusUnsupported));
        }

        await store.AddAsync(offer, cancellationToken);
        await store.SaveChangesAsync(cancellationToken);
        return Result.Success(await readModels.DetailAsync(offer, cancellationToken));
    }
}
