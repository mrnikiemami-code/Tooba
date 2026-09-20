using MediatR;
using Tooba.BuildingBlocks;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Application.ReadModels;
using Tooba.Offer.Contracts;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;

namespace Tooba.Offer.Application.Commands.UpdateOffer;

/// <summary>Atomically applies the supported seller patch to an owned offer.</summary>
public sealed record UpdateOfferCommand(
    Guid OfferId,
    Guid SellerPartyId,
    string? SellerSku,
    string? Status,
    string? ReturnPolicyChoice = null,
    int? CustomReturnWindowDays = null,
    decimal? MinimumOrderQuantity = null,
    decimal? MaximumOrderQuantity = null) : IRequest<SellerOfferDetailPage>;

internal sealed class UpdateOfferHandler(
    IOfferStore store,
    IReturnPolicyResolver returnPolicies,
    IClock clock,
    OfferReadModelComposer readModels) : IRequestHandler<UpdateOfferCommand, SellerOfferDetailPage>
{
    public async Task<SellerOfferDetailPage> Handle(UpdateOfferCommand request, CancellationToken cancellationToken)
    {
        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId)
            throw OfferReadModelComposer.NotFound();
        if (request.SellerSku is not null)
        {
            var sku = request.SellerSku.Trim();
            if (sku.Length > 0 && await store.ExistsSellerSkuAsync(request.SellerPartyId, sku, request.OfferId, cancellationToken))
                throw new SemanticException(new SemanticError(OfferErrorCodes.DuplicateSellerSku));
            offer.UpdateSellerSku(sku, clock.UtcNow);
        }
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (request.Status.Equals(nameof(OfferStatus.Active), StringComparison.OrdinalIgnoreCase)) offer.Activate(clock.UtcNow);
            else if (request.Status.Equals(nameof(OfferStatus.Suspended), StringComparison.OrdinalIgnoreCase)) offer.Suspend(clock.UtcNow);
            else if (request.Status.Equals(nameof(OfferStatus.Archived), StringComparison.OrdinalIgnoreCase)) offer.Archive(clock.UtcNow);
            else throw new SemanticException(new SemanticError(OfferErrorCodes.StatusUnsupported));
        }
        if (request.ReturnPolicyChoice is not null || request.CustomReturnWindowDays is not null)
        {
            var choice = request.ReturnPolicyChoice ?? "Default";
            returnPolicies.ValidateOfferChoice(choice, request.CustomReturnWindowDays);
            offer.SetReturnPolicy(choice, request.CustomReturnWindowDays, clock.UtcNow);
        }
        if (request.MinimumOrderQuantity is not null || request.MaximumOrderQuantity is not null)
            offer.SetOrderQuantityLimits(request.MinimumOrderQuantity, request.MaximumOrderQuantity, clock.UtcNow);
        await store.SaveChangesAsync(cancellationToken);
        return await readModels.DetailAsync(offer, cancellationToken);
    }
}
