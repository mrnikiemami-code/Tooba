using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Offer.Application.Ports;
using Tooba.Offer.Application.ReadModels;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Errors;
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
    decimal? MaximumOrderQuantity = null) : IRequest<Result<SellerOfferDetailPage>>;

internal sealed class UpdateOfferHandler(
    IOfferStore store,
    IReturnPolicyResolver returnPolicies,
    IClock clock,
    OfferReadModelComposer readModels) : IRequestHandler<UpdateOfferCommand, Result<SellerOfferDetailPage>>
{
    public async Task<Result<SellerOfferDetailPage>> Handle(UpdateOfferCommand request, CancellationToken cancellationToken)
    {
        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId)
            return OfferReadModelComposer.NotFound<SellerOfferDetailPage>();
        if (request.SellerSku is not null)
        {
            var sku = request.SellerSku.Trim();
            if (sku.Length > 0 && await store.ExistsSellerSkuAsync(request.SellerPartyId, sku, request.OfferId, cancellationToken))
                return Result.Failure<SellerOfferDetailPage>(new SemanticError(OfferErrorCodes.DuplicateSellerSku));
            offer.UpdateSellerSku(sku, clock.UtcNow);
        }
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (request.Status.Equals(nameof(OfferStatus.Active), StringComparison.OrdinalIgnoreCase))
            {
                var activated = offer.Activate(clock.UtcNow);
                if (activated.IsFailure)
                    return Result.Failure<SellerOfferDetailPage>(activated.Errors);
            }
            else if (request.Status.Equals(nameof(OfferStatus.Suspended), StringComparison.OrdinalIgnoreCase))
            {
                offer.Suspend(clock.UtcNow);
            }
            else if (request.Status.Equals(nameof(OfferStatus.Archived), StringComparison.OrdinalIgnoreCase))
            {
                offer.Archive(clock.UtcNow);
            }
            else
            {
                return Result.Failure<SellerOfferDetailPage>(new SemanticError(OfferErrorCodes.StatusUnsupported));
            }
        }
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
        await store.SaveChangesAsync(cancellationToken);
        return Result.Success(await readModels.DetailAsync(offer, cancellationToken));
    }
}
