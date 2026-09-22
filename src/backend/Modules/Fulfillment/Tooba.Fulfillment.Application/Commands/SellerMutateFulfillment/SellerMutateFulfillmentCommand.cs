using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Errors;
using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Application.Ports;
using Tooba.Fulfillment.Contracts;
using Tooba.Fulfillment.Contracts.Errors;
namespace Tooba.Fulfillment.Application.Commands.SellerMutateFulfillment;

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
    string? ProviderMetadataJson = null) : IRequest<Result<FulfillmentSnapshot>>;

/// <summary>نوع جهش فروشنده.</summary>
public enum SellerFulfillmentMutationKind
{
    MarkProcessing,
    MarkPacked,
    CreateShipment,
    AssignTracking,
    Dispatch,
    Deliver,
}

/// <summary>Handler جهش فروشنده — STABLE_CODES_ONLY via FulfillmentExceptionMapper.</summary>
public sealed class SellerMutateFulfillmentHandler
    : IRequestHandler<SellerMutateFulfillmentCommand, Result<FulfillmentSnapshot>>
{
    private readonly IFulfillmentDirectory _fulfillment;
    private readonly ISellerFulfillmentAuthorizer _authorizer;

    public SellerMutateFulfillmentHandler(
        IFulfillmentDirectory fulfillment,
        ISellerFulfillmentAuthorizer authorizer)
    {
        _fulfillment = fulfillment;
        _authorizer = authorizer;
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

        return await FulfillmentExceptionMapper.TryAsync(async () =>
        {
            return request.Kind switch
            {
                SellerFulfillmentMutationKind.MarkProcessing =>
                    await _fulfillment.MarkProcessingAsync(request.FulfillmentId, request.ActorUserId, cancellationToken),
                SellerFulfillmentMutationKind.MarkPacked =>
                    await _fulfillment.MarkPackedAsync(request.FulfillmentId, request.ActorUserId, cancellationToken),
                SellerFulfillmentMutationKind.CreateShipment =>
                    await _fulfillment.CreateShipmentAsync(
                        request.FulfillmentId,
                        request.ActorUserId,
                        request.CarrierDisplayName ?? string.Empty,
                        request.ShipmentLines ?? [],
                        cancellationToken,
                        request.ShippingMethodCode,
                        request.ProviderMetadataJson),
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
                _ => throw new InvalidOperationException(FulfillmentErrorCodes.Rejected),
            };
        });
    }
}

