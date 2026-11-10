using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Composition;
using Tooba.Fulfillment.Application.Errors;
using Tooba.Fulfillment.Application.Fulfillments.Models;
using Tooba.Fulfillment.Application.Fulfillments.Ports;
using Tooba.Fulfillment.Contracts;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Order.Contracts.Shipping;

namespace Tooba.Fulfillment.Application.Fulfillments.Commands;

/// <summary>Snapshot مجوز که Host از AccessControl می‌سازد.</summary>
public sealed record SellerHandlePermissionInput(
    bool HasGlobalWithinOwner,
    IReadOnlyList<Guid> AllowedCategoryIds);

/// <summary>جهش فروشنده با احراز order.handle.</summary>
public sealed record SellerMutateFulfillmentCommand(
    Guid FulfillmentId,
    Guid ActorUserId,
    Guid SellerPartyId,
    SellerHandlePermissionInput Permission,
    SellerFulfillmentMutationKind Kind,
    string? CarrierDisplayName = null,
    IReadOnlyList<ShipmentLineCommand>? ShipmentLines = null,
    Guid? ShipmentId = null,
    string? TrackingReference = null,
    string? ShippingMethodCode = null,
    string? ProviderMetadataJson = null,
    Guid? ShippingGroupId = null,
    /// <summary>TB-258: server-owned non-delivery reason code.</summary>
    string? NonDeliveryReason = null,
    /// <summary>TB-258: optional operator note for the non-delivery attempt or the origin receipt.</summary>
    string? Note = null,
    /// <summary>TB-258: physically received lines of one origin receipt, with condition per line.</summary>
    IReadOnlyList<ReturnReceiptLineCommand>? ReturnReceiptLines = null) : IRequest<Result<FulfillmentSnapshot>>;

/// <summary>نوع جهش فروشنده.</summary>
public enum SellerFulfillmentMutationKind
{
    MarkProcessing,
    MarkPacked,
    CreateShipment,
    AssignTracking,
    Dispatch,
    Deliver,
    /// <summary>TB-258: record a post-dispatch non-delivery attempt.</summary>
    RecordNonDelivery,
    /// <summary>TB-258: the carrier started returning the failed parcel.</summary>
    StartReturn,
    /// <summary>TB-258: confirm the physical origin receipt.</summary>
    RecordReturnReceipt,
    /// <summary>TB-258: allocate a new resend parcel for usable returned goods.</summary>
    CreateResendShipment,
}

/// <summary>Handler جهش فروشنده — STABLE_CODES_ONLY via FulfillmentOperation.</summary>
public sealed class SellerMutateFulfillmentHandler
    : IRequestHandler<SellerMutateFulfillmentCommand, Result<FulfillmentSnapshot>>
{
    private readonly IFulfillmentDirectory _fulfillment;
    private readonly ISellerFulfillmentAuthorizer _authorizer;
    private readonly IOrderCommittedCarrierShipmentFactsReader _committedShippingGroups;

    public SellerMutateFulfillmentHandler(
        IFulfillmentDirectory fulfillment,
        ISellerFulfillmentAuthorizer authorizer,
        IOrderCommittedCarrierShipmentFactsReader committedShippingGroups)
    {
        _fulfillment = fulfillment;
        _authorizer = authorizer;
        _committedShippingGroups = committedShippingGroups;
    }

    public async Task<Result<FulfillmentSnapshot>> Handle(
        SellerMutateFulfillmentCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _fulfillment.GetAsync(request.FulfillmentId, cancellationToken);
        if (existing is null || existing.SellerPartyId != request.SellerPartyId)
        {
            return Result.Failure<FulfillmentSnapshot>(new SemanticError(FulfillmentErrorCodes.Missing));
        }

        var auth = await _authorizer.EnsureCanMutateAsync(
            request.SellerPartyId,
            existing.SellerOrderId,
            new SellerOrderHandlePermissionSnapshot(
                request.Permission.HasGlobalWithinOwner,
                request.Permission.AllowedCategoryIds),
            cancellationToken);
        if (auth.IsFailure)
        {
            return Result.Failure<FulfillmentSnapshot>(auth.Errors);
        }

        if (request.Kind == SellerFulfillmentMutationKind.CreateShipment)
        {
            AcceptedGroupParcelCreateAuthority.BoundCreate bound;
            try
            {
                var checkoutGroups = await _committedShippingGroups.ListByCheckoutAsync(
                    existing.CheckoutId,
                    cancellationToken);
                var selections = request.ShipmentLines is { Count: > 0 }
                    ? request.ShipmentLines
                        .Select(x => new AcceptedGroupParcelCreateAuthority.LineQuantity(x.OrderLineId, x.Quantity))
                        .ToArray()
                    : null;

                bound = AcceptedGroupParcelCreateAuthority.Bind(
                    existing.CheckoutId,
                    existing.SellerOrderId,
                    existing.SellerPartyId,
                    existing.FulfillmentId,
                    request.ShippingGroupId,
                    request.ShippingMethodCode,
                    clientServiceCode: null,
                    clientOptionCode: null,
                    request.ProviderMetadataJson,
                    selections,
                    checkoutGroups);
            }
            catch (AcceptedGroupParcelCreateException ex)
            {
                // Order-owned refusal codes — surface as Result, not Fulfillment catalog fault.
                return Result.Failure<FulfillmentSnapshot>(new SemanticError(ex.Code));
            }

            var registryCode = MapAcceptedMethodToRegistry(
                bound.AcceptedMethodCode,
                bound.AcceptedServiceCode,
                bound.AcceptedOptionCode);
            var lines = bound.Lines
                .Select(x => new ShipmentLineCommand(x.OrderLineId, x.Quantity))
                .ToArray();

            string? trustedMetadata;
            try
            {
                var facts = (await _committedShippingGroups.ListByCheckoutAsync(
                    existing.CheckoutId,
                    cancellationToken)).First(g => g.ShippingGroupId == bound.ShippingGroupId);
                trustedMetadata = AcceptedGroupTrustedProviderMetadata.BuildFromFactsWithFallback(
                    registryCode,
                    facts,
                    existing.RecipientName,
                    existing.ContactMobile,
                    existing.PostalAddress,
                    existing.PostalCode,
                    existing.ProvinceName,
                    existing.CityName);
            }
            catch (AcceptedGroupParcelCreateException ex)
            {
                return Result.Failure<FulfillmentSnapshot>(new SemanticError(ex.Code));
            }

            return await FulfillmentOperation.ExecuteAsync(async () =>
                (await _fulfillment.CreateShipmentAsync(
                    request.FulfillmentId,
                    request.ActorUserId,
                    bound.CarrierDisplayName,
                    lines,
                    cancellationToken,
                    registryCode,
                    providerMetadataJson: trustedMetadata,
                    bound.ShippingGroupId)).Snapshot);
        }

        if (request.Kind == SellerFulfillmentMutationKind.RecordNonDelivery
            && !Tooba.Fulfillment.Contracts.Operations.FulfillmentNonDeliveryReasons.IsKnown(request.NonDeliveryReason))
        {
            return Result.Failure<FulfillmentSnapshot>(new SemanticError(FulfillmentErrorCodes.Rejected));
        }

        return await FulfillmentOperation.ExecuteAsync(async () =>
        {
            return request.Kind switch
            {
                SellerFulfillmentMutationKind.MarkProcessing =>
                    await _fulfillment.MarkProcessingAsync(request.FulfillmentId, request.ActorUserId, cancellationToken),
                SellerFulfillmentMutationKind.MarkPacked =>
                    await _fulfillment.MarkPackedAsync(request.FulfillmentId, request.ActorUserId, cancellationToken),
                SellerFulfillmentMutationKind.AssignTracking =>
                    await _fulfillment.AssignTrackingAsync(
                        request.FulfillmentId,
                        request.ShipmentId ?? Guid.Empty,
                        request.ActorUserId,
                        request.TrackingReference ?? string.Empty,
                        cancellationToken),
                SellerFulfillmentMutationKind.Dispatch =>
                    await _fulfillment.DispatchShipmentAsync(
                        request.FulfillmentId,
                        request.ShipmentId ?? Guid.Empty,
                        request.ActorUserId,
                        cancellationToken),
                SellerFulfillmentMutationKind.Deliver =>
                    await _fulfillment.DeliverShipmentAsync(
                        request.FulfillmentId,
                        request.ShipmentId ?? Guid.Empty,
                        request.ActorUserId,
                        cancellationToken),
                SellerFulfillmentMutationKind.RecordNonDelivery =>
                    await _fulfillment.RecordNonDeliveryAsync(
                        request.FulfillmentId,
                        request.ShipmentId ?? Guid.Empty,
                        request.ActorUserId,
                        request.NonDeliveryReason ?? string.Empty,
                        request.Note,
                        cancellationToken),
                SellerFulfillmentMutationKind.StartReturn =>
                    await _fulfillment.StartReturnAsync(
                        request.FulfillmentId,
                        request.ShipmentId ?? Guid.Empty,
                        request.ActorUserId,
                        cancellationToken),
                SellerFulfillmentMutationKind.RecordReturnReceipt =>
                    await _fulfillment.RecordReturnReceiptAsync(
                        request.FulfillmentId,
                        request.ShipmentId ?? Guid.Empty,
                        request.ActorUserId,
                        request.ReturnReceiptLines ?? [],
                        request.Note,
                        cancellationToken),
                SellerFulfillmentMutationKind.CreateResendShipment =>
                    (await _fulfillment.CreateResendShipmentAsync(
                        request.FulfillmentId,
                        request.ActorUserId,
                        request.ShipmentId ?? Guid.Empty,
                        request.ShipmentLines ?? [],
                        cancellationToken)).Snapshot,
                _ => throw FulfillmentErrors.SemanticFault(FulfillmentErrorCodes.Rejected),
            };
        });
    }

    private static string? MapAcceptedMethodToRegistry(
        string acceptedMethodCode,
        string? acceptedServiceCode,
        string? acceptedOptionCode)
    {
        if (string.IsNullOrWhiteSpace(acceptedMethodCode))
        {
            return null;
        }

        var method = acceptedMethodCode.Trim().Split('\u001f', 2)[0].Trim();
        var service = string.IsNullOrWhiteSpace(acceptedServiceCode)
            ? null
            : acceptedServiceCode.Trim().Split('\u001f', 2)[0].Trim();
        var option = string.IsNullOrWhiteSpace(acceptedOptionCode)
            ? null
            : acceptedOptionCode.Trim().Split('\u001f', 2)[0].Trim();

        string[] candidates =
        [
            string.IsNullOrEmpty(service) || string.IsNullOrEmpty(option) ? string.Empty : $"{method}:{service}:{option}",
            string.IsNullOrEmpty(option) ? string.Empty : $"{method}:{option}",
            string.IsNullOrEmpty(service) ? string.Empty : $"{method}:{service}",
            method,
        ];

        foreach (var candidate in candidates)
        {
            if (candidate.Length == 0)
            {
                continue;
            }

            if (ShippingMethodRegistry.Find(candidate) is { } exact)
            {
                return exact.Code;
            }
        }

        return method;
    }
}
