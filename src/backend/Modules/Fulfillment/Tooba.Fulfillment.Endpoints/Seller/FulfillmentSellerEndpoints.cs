using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Fulfillment.Application.Commands.SellerMutateFulfillment;
using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Application.Queries.GetSellerFulfillment;
using Tooba.Fulfillment.Application.Queries.ListSellerFulfillments;

namespace Tooba.Fulfillment.Endpoints.Seller;

/// <summary>Wire-only shipment line.</summary>
public sealed record FulfillmentShipmentLineRequest(Guid OrderLineId, decimal Quantity);

/// <summary>Wire-only create shipment.</summary>
public sealed record FulfillmentCreateShipmentRequest(
    string CarrierDisplayName,
    IReadOnlyList<FulfillmentShipmentLineRequest> Items,
    string? ShippingMethodCode = null,
    string? ProviderMetadataJson = null);

/// <summary>Wire-only tracking.</summary>
public sealed record FulfillmentAssignTrackingRequest(string TrackingReference);

/// <summary>Thin seller Fulfillment HTTP routes.</summary>
public static class FulfillmentSellerEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/fulfillments", SellerListAsync);
        group.MapGet("/fulfillments/{fulfillmentId:guid}", SellerGetAsync);
        group.MapPost("/fulfillments/{fulfillmentId:guid}/processing", SellerProcessingAsync);
        group.MapPost("/fulfillments/{fulfillmentId:guid}/packed", SellerPackedAsync);
        group.MapPost("/fulfillments/{fulfillmentId:guid}/shipments", SellerCreateShipmentAsync);
        group.MapPost("/fulfillments/{fulfillmentId:guid}/shipments/{shipmentId:guid}/tracking", SellerTrackingAsync);
        group.MapPost("/fulfillments/{fulfillmentId:guid}/shipments/{shipmentId:guid}/dispatch", SellerDispatchAsync);
        group.MapPost("/fulfillments/{fulfillmentId:guid}/shipments/{shipmentId:guid}/deliver", SellerDeliverAsync);
    }

    private static async Task<IResult> SellerListAsync(
        ISender sender, IFulfillmentSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new ListSellerFulfillmentsQuery(sellerPartyId), cancellationToken));
    }

    private static async Task<IResult> SellerGetAsync(
        Guid fulfillmentId, ISender sender, IFulfillmentSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(new GetSellerFulfillmentQuery(sellerPartyId, fulfillmentId), cancellationToken));
    }

    private static Task<IResult> SellerProcessingAsync(
        Guid fulfillmentId, ISender sender, IFulfillmentSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken) =>
        SellerMutateAsync(fulfillmentId, sender, authorizer, api, context,
            SellerFulfillmentMutationKind.MarkProcessing, null, null, null, null, null, null, cancellationToken);

    private static Task<IResult> SellerPackedAsync(
        Guid fulfillmentId, ISender sender, IFulfillmentSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken) =>
        SellerMutateAsync(fulfillmentId, sender, authorizer, api, context,
            SellerFulfillmentMutationKind.MarkPacked, null, null, null, null, null, null, cancellationToken);

    private static Task<IResult> SellerCreateShipmentAsync(
        Guid fulfillmentId, FulfillmentCreateShipmentRequest body,
        ISender sender, IFulfillmentSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken) =>
        SellerMutateAsync(fulfillmentId, sender, authorizer, api, context,
            SellerFulfillmentMutationKind.CreateShipment,
            body.CarrierDisplayName,
            body.Items.Select(x => new ShipmentLineCommand(x.OrderLineId, x.Quantity)).ToArray(),
            null, null, body.ShippingMethodCode, body.ProviderMetadataJson, cancellationToken);

    private static Task<IResult> SellerTrackingAsync(
        Guid fulfillmentId, Guid shipmentId, FulfillmentAssignTrackingRequest body,
        ISender sender, IFulfillmentSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken) =>
        SellerMutateAsync(fulfillmentId, sender, authorizer, api, context,
            SellerFulfillmentMutationKind.AssignTracking, null, null, shipmentId, body.TrackingReference, null, null, cancellationToken);

    private static Task<IResult> SellerDispatchAsync(
        Guid fulfillmentId, Guid shipmentId,
        ISender sender, IFulfillmentSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken) =>
        SellerMutateAsync(fulfillmentId, sender, authorizer, api, context,
            SellerFulfillmentMutationKind.Dispatch, null, null, shipmentId, null, null, null, cancellationToken);

    private static Task<IResult> SellerDeliverAsync(
        Guid fulfillmentId, Guid shipmentId,
        ISender sender, IFulfillmentSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken) =>
        SellerMutateAsync(fulfillmentId, sender, authorizer, api, context,
            SellerFulfillmentMutationKind.Deliver, null, null, shipmentId, null, null, null, cancellationToken);

    private static async Task<IResult> SellerMutateAsync(
        Guid fulfillmentId, ISender sender, IFulfillmentSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, SellerFulfillmentMutationKind kind,
        string? carrier, IReadOnlyList<ShipmentLineCommand>? lines, Guid? shipmentId, string? tracking,
        string? shippingMethodCode, string? providerMetadataJson, CancellationToken cancellationToken)
    {
        var (actorUserId, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        var permission = await authorizer.GetHandlePermissionAsync(actorUserId, sellerPartyId, cancellationToken);
        return api.From(await sender.Send(new SellerMutateFulfillmentCommand(
            fulfillmentId, actorUserId, sellerPartyId, permission, kind,
            carrier, lines, shipmentId, tracking, shippingMethodCode, providerMetadataJson), cancellationToken));
    }
}
