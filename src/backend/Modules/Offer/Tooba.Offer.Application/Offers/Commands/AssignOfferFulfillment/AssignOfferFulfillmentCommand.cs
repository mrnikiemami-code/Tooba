using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Order.Contracts.Fulfillment;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Application.Offers.History;
using Tooba.Offer.Application.Offers.Mappings;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Application.Offers.ReadModels;
using Tooba.Offer.Contracts.Errors;
using Tooba.Offer.Domain.Aggregates;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Rates;
using DomainResponsibility = Tooba.Offer.Domain.ValueObjects.FulfillmentResponsibility;
using DomainShippingMode = Tooba.Offer.Domain.ValueObjects.ShippingSelectionMode;

namespace Tooba.Offer.Application.Offers.Commands.AssignOfferFulfillment;

/// <summary>
/// Explicitly assigns (or clears) the fulfillment responsibility of ONE existing offer and, when supplied,
/// its shipping method selection mode and stable method set.
/// </summary>
/// <remarks>
/// <para>
/// <b>No inference, no browser owner.</b> The responsible Seller identity is the aggregate's own
/// <see cref="SellerOffer.SellerPartyId"/> — never a caller-supplied seller id — and the Platform owner is
/// resolved from the trusted edition/tenant context, never from the body.
/// </para>
/// <para>
/// <b>Presence-aware.</b> A legacy caller that supplies only responsibility/type keeps the current shipping
/// restriction. It may not silently widen a Specific restriction: changing responsibility/type while a
/// Specific set exists is rejected until the caller explicitly reconciles the method set.
/// </para>
/// <para>
/// <b>One owning transaction.</b> Assignment, mode, every link row and the history entry are staged on the
/// same Offer unit of work and committed with one <c>SaveChanges</c> behind the Offer revision guard.
/// </para>
/// </remarks>
/// <param name="OfferId">Existing offer identity.</param>
/// <param name="Responsibility">Explicit responsibility, or <c>null</c> to clear the assignment.</param>
/// <param name="ShippingTypeId">Real Fulfillment shipping type id, or <c>null</c>.</param>
/// <param name="ExpectedVersion">Offer revision the caller saw.</param>
/// <param name="ActorUserId">Admin actor identity.</param>
/// <param name="ShippingModeSupplied">Whether the new shipping mode key was present in the request.</param>
/// <param name="ShippingMode">Explicit mode when supplied.</param>
/// <param name="ShippingMethodIds">Explicit stable method identities when supplied.</param>
public sealed record AssignOfferFulfillmentCommand(
    Guid OfferId,
    OfferFulfillmentResponsibility? Responsibility,
    Guid? ShippingTypeId,
    string? ExpectedVersion = null,
    Guid? ActorUserId = null,
    bool ShippingModeSupplied = false,
    OfferShippingSelectionMode? ShippingMode = null,
    IReadOnlyList<Guid>? ShippingMethodIds = null) : IRequest<Result<OfferReference>>;

internal sealed class AssignOfferFulfillmentHandler(
    IOfferStore store,
    IShippingTypeReader shippingTypes,
    IClock clock,
    IIdGenerator ids,
    IOfferHistoryWriter history,
    IOfferFulfillmentChangeGate inventoryGate,
    IOrderOfferCommitmentReader orderCommitments,
    IOfferShippingMethodStore? shippingMethods = null,
    IOfferShippingMethodLookup? methodCatalog = null,
    ICurrentEdition? edition = null,
    ICurrentTenant? tenant = null)
    : IRequestHandler<AssignOfferFulfillmentCommand, Result<OfferReference>>
{
    public async Task<Result<OfferReference>> Handle(
        AssignOfferFulfillmentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.OfferId == Guid.Empty)
        {
            return OfferReadModelComposer.NotFound<OfferReference>();
        }

        var offer = await store.GetByIdAsync(request.OfferId, cancellationToken);
        if (offer is null)
        {
            return OfferReadModelComposer.NotFound<OfferReference>();
        }

        var versionError = OfferVersionGuard.Check(
            offer.UpdatedAt, request.ShippingModeSupplied ? request.ExpectedVersion ?? string.Empty : request.ExpectedVersion);
        if (versionError is not null)
        {
            return Result.Failure<OfferReference>(versionError);
        }

        // A requested type must exist and be ACTIVE in the real catalog; an infrastructure failure is not
        // "unknown type" and stays an exception instead of becoming a silent empty answer.
        if (request.ShippingTypeId is Guid typeId)
        {
            var types = await shippingTypes.GetByIdsAsync([typeId], cancellationToken);
            var type = types.SingleOrDefault(candidate => candidate.ShippingTypeId == typeId);
            if (type is null || !type.IsActive)
            {
                return Result.Failure<OfferReference>(new SemanticError(OfferErrorCodes.FulfillmentTypeUnavailable));
            }
        }

        var requestedResponsibility = request.Responsibility is null
            ? (DomainResponsibility?)null : (DomainResponsibility)(int)request.Responsibility.Value;
        var changesResponsibility = requestedResponsibility != offer.FulfillmentResponsibility;
        var currentMethodIds = shippingMethods is null
            ? (IReadOnlyList<Guid>)[]
            : await shippingMethods.ListMethodIdsAsync(offer.OfferId, cancellationToken);
        var currentMode = offer.ShippingSelectionMode ?? DomainShippingMode.Automatic;

        DomainShippingMode? targetMode = null;
        IReadOnlyList<Guid> targetMethodIds = [];
        if (!request.ShippingModeSupplied && (request.ShippingMode is not null || request.ShippingMethodIds is not null))
        {
            return Result.Failure<OfferReference>(new SemanticError(OfferErrorCodes.FulfillmentBodyMalformed));
        }

        if (request.ShippingModeSupplied)
        {
            if (request.ShippingMode is null || !Enum.IsDefined(request.ShippingMode.Value))
            {
                return Result.Failure<OfferReference>(new SemanticError(OfferErrorCodes.FulfillmentBodyMalformed));
            }

            if (shippingMethods is null)
            {
                return Result.Failure<OfferReference>(new SemanticError(OfferErrorCodes.ShippingMethodUnavailable));
            }

            targetMode = (DomainShippingMode)(int)request.ShippingMode.Value;
            targetMethodIds = (request.ShippingMethodIds ?? []).ToArray();

            if (targetMode == DomainShippingMode.Automatic)
            {
                if (targetMethodIds.Count > 0)
                {
                    return Result.Failure<OfferReference>(new SemanticError(OfferErrorCodes.ShippingMethodInvalid));
                }
            }
            else
            {
                if (targetMethodIds.Count == 0)
                {
                    return Result.Failure<OfferReference>(new SemanticError(OfferErrorCodes.ShippingMethodsRequired));
                }

                if (targetMethodIds.Any(id => id == Guid.Empty))
                {
                    return Result.Failure<OfferReference>(new SemanticError(OfferErrorCodes.ShippingMethodInvalid));
                }

                if (targetMethodIds.Distinct().Count() != targetMethodIds.Count)
                {
                    return Result.Failure<OfferReference>(new SemanticError(OfferErrorCodes.ShippingMethodDuplicate));
                }

                if (requestedResponsibility is null || request.ShippingTypeId is not Guid selectedType)
                {
                    return Result.Failure<OfferReference>(new SemanticError(OfferErrorCodes.ShippingAssignmentRequired));
                }

                var owner = ResolveOwner(requestedResponsibility.Value, offer.SellerPartyId);
                if (owner is null || methodCatalog is null)
                {
                    return Result.Failure<OfferReference>(new SemanticError(OfferErrorCodes.ShippingMethodUnavailable));
                }

                var candidates = await methodCatalog.ListAsync(
                    new OfferShippingMethodQuery(owner.Value.Kind, owner.Value.TenantId, owner.Value.SellerPartyId, selectedType),
                    cancellationToken);
                var eligible = candidates.Select(candidate => candidate.MethodId).ToHashSet();
                if (!targetMethodIds.All(eligible.Contains))
                {
                    return Result.Failure<OfferReference>(new SemanticError(OfferErrorCodes.ShippingMethodUnavailable));
                }

                targetMethodIds = targetMethodIds.OrderBy(id => id).ToArray();
            }
        }
        else if (currentMode == DomainShippingMode.Specific
                 && (changesResponsibility
                     || requestedResponsibility is null
                     || request.ShippingTypeId != offer.FulfillmentShippingTypeId))
        {
            // A legacy caller cannot move a Specific offer to another owner/type while keeping a now-unrelated
            // set: that would widen or silently reinterpret the restriction. Reconciliation is explicit.
            return Result.Failure<OfferReference>(new SemanticError(OfferErrorCodes.ShippingReconciliationRequired));
        }

        // Inventory serializes this check with new holds and explicit allocation writes.
        // Order is the authority for durable obligations even when a hold was already consumed.
        await using var lease = changesResponsibility
            ? await inventoryGate.TryAcquireAsync(offer.OfferId, cancellationToken)
            : null;
        if (changesResponsibility && (lease is null
            || await orderCommitments.HasOrderCommitmentAsync(offer.OfferId, cancellationToken)))
        {
            return Result.Failure<OfferReference>(new SemanticError(OfferErrorCodes.FulfillmentChangeProtected));
        }

        var modeChanged = targetMode is not null && targetMode != offer.ShippingSelectionMode;
        var methodsChanged = targetMode is not null
            ? !targetMethodIds.SequenceEqual(currentMethodIds)
            : false;

        if (!changesResponsibility && request.ShippingTypeId == offer.FulfillmentShippingTypeId
            && !modeChanged && !methodsChanged)
        {
            // Truthful no-op: the exact same intent at the current revision writes nothing and appends no
            // duplicate history entry.
            return Result.Success(offer.ToReference());
        }

        var previous = OfferHistorySummaries.Shipping(
            offer.FulfillmentResponsibility, offer.FulfillmentShippingTypeId,
            currentMode, currentMethodIds);

        // PostgreSQL stores microseconds. Even a link-only change must advance the owning
        // Offer row's revision, so its existing xmin concurrency token participates in SaveChanges.
        var mutationTime = clock.UtcNow;
        if (mutationTime.UtcTicks / 10 <= offer.UpdatedAt.UtcTicks / 10)
        {
            mutationTime = offer.UpdatedAt.AddTicks(10);
        }

        var assignment = offer.AssignFulfillment(
            requestedResponsibility,
            request.ShippingTypeId,
            mutationTime);
        if (assignment.IsFailure)
        {
            return Result.Failure<OfferReference>(assignment.Errors);
        }

        if (targetMode is not null)
        {
            var modeResult = offer.AssignShippingSelectionMode(targetMode, mutationTime);
            if (modeResult.IsFailure)
            {
                return Result.Failure<OfferReference>(modeResult.Errors);
            }

            if (shippingMethods is not null)
            {
                await shippingMethods.ReplaceMethodIdsAsync(offer.OfferId, targetMethodIds, cancellationToken);
            }
        }
        else if (requestedResponsibility is null)
        {
            // Clearing the assignment also clears any selection; no link can outlive its assignment.
            offer.AssignShippingSelectionMode(null, mutationTime);
            if (shippingMethods is not null)
            {
                await shippingMethods.ReplaceMethodIdsAsync(offer.OfferId, [], cancellationToken);
            }
        }

        var resulting = targetMode is not null
            ? targetMethodIds
            : currentMethodIds;
        var resultingMode = targetMode ?? currentMode;

        await history.AppendAsync(
            OfferHistorySummaries.CreateEntry(
                ids.NewId(),
                offer.OfferId,
                mutationTime,
                OfferHistorySummaries.FulfillmentChanged,
                OfferHistorySummaries.FulfillmentChanged,
                previous,
                OfferHistorySummaries.Shipping(
                    offer.FulfillmentResponsibility, offer.FulfillmentShippingTypeId,
                    resultingMode, resulting),
                request.ActorUserId),
            cancellationToken);
        await store.SaveChangesAsync(cancellationToken);
        return Result.Success(offer.ToReference());
    }

    /// <summary>
    /// Trusted owner identity for the eligible-method read. Platform resolves from the edition/tenant context;
    /// a Seller resolves only in Marketplace and only to the offer's own real seller.
    /// </summary>
    private (ShippingCapturedQuoteOwnerKind Kind, string? TenantId, Guid? SellerPartyId)? ResolveOwner(
        DomainResponsibility responsibility,
        Guid sellerPartyId)
    {
        if (edition is null || tenant is null)
        {
            return null;
        }

        return Tooba.Offer.Application.Offers.Shipping.OfferShippingOwnerResolver.Resolve(
            edition,
            tenant,
            responsibility == DomainResponsibility.Platform
                ? OfferFulfillmentResponsibility.Platform
                : OfferFulfillmentResponsibility.Seller,
            sellerPartyId);
    }
}
