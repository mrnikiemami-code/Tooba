using Tooba.Offer.Application.Validation;
using MediatR;
using Tooba.BuildingBlocks;
using Tooba.StoreContext.Contracts.Current;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Offer.Application.Offers.History;
using Tooba.Offer.Application.Offers.Policies;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Application.Offers.ReadModels;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Errors;
using Tooba.Offer.Contracts.ReturnPolicy;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Party.Contracts.Ports;
using System.Security.Cryptography;
using System.Text;
using DomainChannel = Tooba.Offer.Domain.ValueObjects.SalesChannel;

namespace Tooba.Offer.Application.Offers.Commands.CreateOffer;

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
    decimal? MaximumOrderQuantity = null,
    string? RequestId = null,
    Guid? ActorUserId = null,
    bool AdminSurface = false) : IRequest<Result<SellerOfferDetailPage>>;

internal sealed class CreateOfferHandler(
    IOfferStore store,
    ICatalogVariantLookup catalog,
    IPartyLookup parties,
    IReturnPolicyResolver returnPolicies,
    IClock clock,
    IIdGenerator ids,
    ICurrentStoreCommerceContext storeCommerce,
    IOfferHistoryWriter history,
    OfferReadModelComposer readModels) : IRequestHandler<CreateOfferCommand, Result<SellerOfferDetailPage>>
{
    public async Task<Result<SellerOfferDetailPage>> Handle(CreateOfferCommand request, CancellationToken cancellationToken)
    {
        if (!OfferChannelPolicy.IsAllowed(storeCommerce.Current?.SalesChannel, request.Channel))
            return Result.Failure<SellerOfferDetailPage>(new SemanticError(OfferErrorCodes.ChannelNotAllowed));
        var variant = await catalog.FindVariantAsync(request.CatalogVariantId, cancellationToken);
        if (variant is null)
            return Result.Failure<SellerOfferDetailPage>(new SemanticError(OfferErrorCodes.CatalogVariantMissing));
        var seller = await parties.FindByIdAsync(request.SellerPartyId, cancellationToken);
        if (seller is null)
            return Result.Failure<SellerOfferDetailPage>(new SemanticError(OfferErrorCodes.SellerMissing));
        if (!string.Equals(seller.Kind, "Organization", StringComparison.Ordinal))
            return Result.Failure<SellerOfferDetailPage>(new SemanticError(OfferErrorCodes.SellerNotOrganization));
        var channel = (DomainChannel)(int)request.Channel;
        Guid? requestedId = string.IsNullOrWhiteSpace(request.RequestId)
            ? null
            : DeterministicOfferId(request.SellerPartyId, request.CatalogVariantId, request.RequestId.Trim());
        string? intentFingerprint = null;
        if (requestedId is Guid stableId)
        {
            // TB-287: the requestId identifies the immutable original creation intent, not the current
            // mutable listing state. Compare the durable fingerprint recorded at create time.
            intentFingerprint = CreationIntentFingerprint.Compute(request, channel, request.RequestId!);
            var existing = await store.GetByIdAsync(stableId, cancellationToken);
            if (existing is not null)
            {
                // A historical row with no recorded intent is never inferred from mutable state; fail closed.
                if (existing.CreationIntentFingerprint is null
                    || !string.Equals(existing.CreationIntentFingerprint, intentFingerprint, StringComparison.Ordinal))
                {
                    return Result.Failure<SellerOfferDetailPage>(new SemanticError(OfferErrorCodes.CreateIntentConflict));
                }
                return Result.Success(await readModels.DetailAsync(existing, cancellationToken, adminSurface: request.AdminSurface));
            }
        }
        if (await store.ExistsActiveListingAsync(request.SellerPartyId, request.CatalogVariantId, channel, cancellationToken))
            return Result.Failure<SellerOfferDetailPage>(new SemanticError(OfferErrorCodes.DuplicateActiveListing));
        var sku = string.IsNullOrWhiteSpace(request.SellerSku) ? null : request.SellerSku.Trim();
        if (!string.IsNullOrWhiteSpace(sku)
            && await store.ExistsSellerSkuAsync(request.SellerPartyId, sku, null, cancellationToken))
            return Result.Failure<SellerOfferDetailPage>(new SemanticError(OfferErrorCodes.DuplicateSellerSku));

        var offer = SellerOffer.Create(requestedId ?? ids.NewId(), request.CatalogVariantId, request.SellerPartyId, channel, sku, clock.UtcNow);
        if (intentFingerprint is not null)
        {
            // Persisted atomically with the Offer insert so the original intent survives later edits.
            offer.SetCreationIntentFingerprint(intentFingerprint);
        }
        if (request.ReturnPolicyChoice is not null || request.CustomReturnWindowDays is not null)
        {
            var choice = request.ReturnPolicyChoice ?? "Default";
            var policy = returnPolicies.ValidateOfferChoice(choice, request.CustomReturnWindowDays);
            if (policy.IsFailure)
                return Result.Failure<SellerOfferDetailPage>(policy.Errors);
            offer.SetReturnPolicy(choice, request.CustomReturnWindowDays, clock.UtcNow);
        }
        var quantityError = OfferQuantityPolicy.Validate(request.MinimumOrderQuantity, request.MaximumOrderQuantity, variant);
        if (quantityError is not null)
            return Result.Failure<SellerOfferDetailPage>(quantityError);
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

        // Staged on the same Offer unit of work; PersistNewAsync commits the Offer and this entry together.
        await history.AppendAsync(
            OfferHistorySummaries.CreateEntry(
                ids.NewId(),
                offer.OfferId,
                offer.CreatedAt,
                OfferHistorySummaries.Created,
                OfferHistorySummaries.Created,
                null,
                OfferHistorySummaries.StatusChannel(offer),
                request.ActorUserId),
            cancellationToken);

        var persisted = await store.PersistNewAsync(offer, cancellationToken);
        if (persisted is not null)
        {
            // Race branch: another request won the insert for this identity. Verify its durable intent.
            if (persisted.CreationIntentFingerprint is null
                || !string.Equals(persisted.CreationIntentFingerprint, intentFingerprint, StringComparison.Ordinal))
            {
                return Result.Failure<SellerOfferDetailPage>(new SemanticError(OfferErrorCodes.CreateIntentConflict));
            }
            return Result.Success(await readModels.DetailAsync(persisted, cancellationToken, adminSurface: request.AdminSurface));
        }
        return Result.Success(await readModels.DetailAsync(offer, cancellationToken, adminSurface: request.AdminSurface));
    }

    private static Guid DeterministicOfferId(Guid sellerPartyId, Guid catalogVariantId, string requestId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{sellerPartyId:N}|{catalogVariantId:N}|{requestId}"));
        return new Guid(bytes.AsSpan(0, 16));
    }
}




