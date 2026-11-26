using Tooba.Offer.Application.Validation;
using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Offer.Application.Offers.Commands.CreateOffer;
using Tooba.Offer.Application.Offers.History;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Application.Offers.ReadModels;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Errors;
using Tooba.Offer.Contracts.ReturnPolicy;

namespace Tooba.Offer.Application.Offers.Commands.UpdateOffer;

/// <summary>Atomically applies the supported seller patch to an owned offer.</summary>
public sealed record UpdateOfferCommand(
    Guid OfferId,
    Guid SellerPartyId,
    string? SellerSku,
    string? Status,
    string? ReturnPolicyChoice = null,
    int? CustomReturnWindowDays = null,
    decimal? MinimumOrderQuantity = null,
    decimal? MaximumOrderQuantity = null,
    bool ClearOrderQuantityLimits = false,
    string? ExpectedVersion = null,
    Guid? ActorUserId = null) : IRequest<Result<SellerOfferDetailPage>>;

internal sealed class UpdateOfferHandler(
    IOfferStore store,
    ICatalogVariantLookup catalog,
    IReturnPolicyResolver returnPolicies,
    IClock clock,
    IIdGenerator ids,
    IOfferHistoryWriter history,
    OfferReadModelComposer readModels) : IRequestHandler<UpdateOfferCommand, Result<SellerOfferDetailPage>>
{
    public async Task<Result<SellerOfferDetailPage>> Handle(UpdateOfferCommand request, CancellationToken cancellationToken)
    {
        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null || offer.SellerPartyId != request.SellerPartyId)
            return OfferReadModelComposer.NotFound<SellerOfferDetailPage>();
        var versionError = OfferVersionGuard.Check(offer.UpdatedAt, request.ExpectedVersion);
        if (versionError is not null)
            return Result.Failure<SellerOfferDetailPage>(versionError);
        var previousCommercial = OfferHistorySummaries.Commercial(offer);
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
            else if (request.Status.Equals(nameof(OfferStatus.Draft), StringComparison.OrdinalIgnoreCase)
                && offer.Status.ToString().Equals(nameof(OfferStatus.Draft), StringComparison.OrdinalIgnoreCase))
            {
                // Draft is already the current lifecycle state; a SKU-only edit must not require a transition.
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
            var variant = await catalog.FindVariantAsync(offer.CatalogVariantId, cancellationToken);
            var quantityError = OfferQuantityPolicy.Validate(request.MinimumOrderQuantity, request.MaximumOrderQuantity, variant);
            if (quantityError is not null)
                return Result.Failure<SellerOfferDetailPage>(quantityError);
        }
        if (request.ClearOrderQuantityLimits || request.MinimumOrderQuantity is not null || request.MaximumOrderQuantity is not null)
        {
            var qty = offer.SetOrderQuantityLimits(request.MinimumOrderQuantity, request.MaximumOrderQuantity, clock.UtcNow);
            if (qty.IsFailure)
                return Result.Failure<SellerOfferDetailPage>(qty.Errors);
        }
        await history.AppendAsync(
            OfferHistorySummaries.CreateEntry(
                ids.NewId(),
                offer.OfferId,
                clock.UtcNow,
                OfferHistorySummaries.CommercialChanged,
                "Offer commercial fields changed",
                previousCommercial,
                OfferHistorySummaries.Commercial(offer),
                request.ActorUserId),
            cancellationToken);
        await store.SaveChangesAsync(cancellationToken);
        return Result.Success(await readModels.DetailAsync(offer, cancellationToken));
    }
}
