using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Application.Offers.Mappings;
using Tooba.Offer.Application.Offers.Ports;
using Tooba.Offer.Application.Offers.ReadModels;
using Tooba.Offer.Contracts.Errors;
using Tooba.Offer.Domain.Aggregates;
using DomainResponsibility = Tooba.Offer.Domain.ValueObjects.FulfillmentResponsibility;

namespace Tooba.Offer.Application.Offers.Commands.AssignOfferFulfillment;

/// <summary>
/// Explicitly assigns (or clears) the fulfillment responsibility of ONE existing offer.
/// </summary>
/// <remarks>
/// <para>
/// <b>No inference, no browser owner.</b> The responsible Seller identity is the aggregate's own
/// <see cref="SellerOffer.SellerPartyId"/> — never a caller-supplied seller id — and the Platform owner stays a
/// trusted edition/tenant decision made later by the consumer.
/// </para>
/// <para>
/// <b>Real taxonomy only.</b> A non-null type is checked against the real active Fulfillment catalog through its
/// Contracts reader; only the id is stored, no code/label copy and no cross-module join.
/// </para>
/// <para>
/// <b>Admin surface.</b> This command has no seller authorization path: an admin endpoint is the only caller, so
/// a seller never gains the right to declare Platform responsibility.
/// </para>
/// </remarks>
/// <param name="OfferId">Existing offer identity.</param>
/// <param name="Responsibility">Explicit responsibility, or <c>null</c> to clear the assignment.</param>
/// <param name="ShippingTypeId">Real Fulfillment shipping type id, or <c>null</c>.</param>
public sealed record AssignOfferFulfillmentCommand(
    Guid OfferId,
    OfferFulfillmentResponsibility? Responsibility,
    Guid? ShippingTypeId) : IRequest<Result<OfferReference>>;

internal sealed class AssignOfferFulfillmentHandler(
    IOfferStore store,
    IShippingTypeReader shippingTypes,
    IClock clock)
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

        var assignment = offer.AssignFulfillment(
            request.Responsibility is null ? null : (DomainResponsibility)(int)request.Responsibility.Value,
            request.ShippingTypeId,
            clock.UtcNow);
        if (assignment.IsFailure)
        {
            return Result.Failure<OfferReference>(assignment.Errors);
        }

        await store.SaveChangesAsync(cancellationToken);
        return Result.Success(offer.ToReference());
    }
}

