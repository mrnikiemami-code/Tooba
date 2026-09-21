using MediatR;
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Cart.Contracts;
using Tooba.Fulfillment.Application.Commands;
using Tooba.Fulfillment.Application.Models;
using Tooba.Fulfillment.Application.Ports;
using Tooba.Fulfillment.Application.Queries;
using Tooba.Fulfillment.Contracts;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Host.Admin;
using Tooba.Host.Grid;
using Tooba.Host.Seller;
using Tooba.Host.Storefront;
using Tooba.Order.Contracts.Fulfillment;

namespace Tooba.Host.Fulfillment;

/// <summary>
/// Endpoint-State: HOST_THIN_TRANSPORT — auth + ISender/ApiResponseFactory only.
/// </summary>
public static class FulfillmentEndpoints
{
    /// <summary>مسیرهای fulfillment را ثبت می‌کند.</summary>
    public static void MapFulfillmentEndpoints(this WebApplication app)
    {
        var seller = app.MapGroup("/v1/seller");
        seller.MapGet("/fulfillments", SellerListAsync);
        seller.MapGet("/fulfillments/{fulfillmentId:guid}", SellerGetAsync);
        seller.MapPost("/fulfillments/{fulfillmentId:guid}/processing", SellerProcessingAsync);
        seller.MapPost("/fulfillments/{fulfillmentId:guid}/packed", SellerPackedAsync);
        seller.MapPost("/fulfillments/{fulfillmentId:guid}/shipments", SellerCreateShipmentAsync);
        seller.MapPost("/fulfillments/{fulfillmentId:guid}/shipments/{shipmentId:guid}/tracking", SellerTrackingAsync);
        seller.MapPost("/fulfillments/{fulfillmentId:guid}/shipments/{shipmentId:guid}/dispatch", SellerDispatchAsync);
        seller.MapPost("/fulfillments/{fulfillmentId:guid}/shipments/{shipmentId:guid}/deliver", SellerDeliverAsync);

        var admin = app.MapGroup("/v1/admin");
        admin.MapGet("/fulfillments", AdminListAsync);
        admin.MapPost("/fulfillments/query", AdminQueryGridAsync);
        admin.MapPost("/fulfillments/work-queue/query", AdminWorkQueueQueryAsync);
        admin.MapPost("/fulfillments/work-queue/bulk", AdminWorkQueueBulkAsync);
        admin.MapGet("/fulfillments/{fulfillmentId:guid}", AdminGetAsync);

        var customer = app.MapGroup("/v1/customer");
        customer.MapGet("/orders/{checkoutId:guid}/fulfillments", CustomerListAsync);
    }

    private static async Task<IResult> SellerListAsync(
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            return api.From(await sender.Send(new ListSellerFulfillmentsQuery(sellerPartyId), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> SellerGetAsync(
        Guid fulfillmentId,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var (_, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            return api.From(await sender.Send(new GetSellerFulfillmentQuery(sellerPartyId, fulfillmentId), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static Task<IResult> SellerProcessingAsync(
        Guid fulfillmentId,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        IAccessControlDirectory access,
        CancellationToken cancellationToken) =>
        SellerMutateAsync(
            fulfillmentId, sender, api, request, session, guard, environment, access,
            SellerFulfillmentMutationKind.MarkProcessing, null, null, null, null, null, null, cancellationToken);

    private static Task<IResult> SellerPackedAsync(
        Guid fulfillmentId,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        IAccessControlDirectory access,
        CancellationToken cancellationToken) =>
        SellerMutateAsync(
            fulfillmentId, sender, api, request, session, guard, environment, access,
            SellerFulfillmentMutationKind.MarkPacked, null, null, null, null, null, null, cancellationToken);

    private static Task<IResult> SellerCreateShipmentAsync(
        Guid fulfillmentId,
        FulfillmentCreateShipmentRequest body,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        IAccessControlDirectory access,
        CancellationToken cancellationToken) =>
        SellerMutateAsync(
            fulfillmentId, sender, api, request, session, guard, environment, access,
            SellerFulfillmentMutationKind.CreateShipment,
            body.CarrierDisplayName,
            body.Items.Select(x => new ShipmentLineCommand(x.OrderLineId, x.Quantity)).ToArray(),
            null,
            null,
            body.ShippingMethodCode,
            body.ProviderMetadataJson,
            cancellationToken);

    private static Task<IResult> SellerTrackingAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        FulfillmentAssignTrackingRequest body,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        IAccessControlDirectory access,
        CancellationToken cancellationToken) =>
        SellerMutateAsync(
            fulfillmentId, sender, api, request, session, guard, environment, access,
            SellerFulfillmentMutationKind.AssignTracking, null, null, shipmentId, body.TrackingReference, null, null, cancellationToken);

    private static Task<IResult> SellerDispatchAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        IAccessControlDirectory access,
        CancellationToken cancellationToken) =>
        SellerMutateAsync(
            fulfillmentId, sender, api, request, session, guard, environment, access,
            SellerFulfillmentMutationKind.Dispatch, null, null, shipmentId, null, null, null, cancellationToken);

    private static Task<IResult> SellerDeliverAsync(
        Guid fulfillmentId,
        Guid shipmentId,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        IAccessControlDirectory access,
        CancellationToken cancellationToken) =>
        SellerMutateAsync(
            fulfillmentId, sender, api, request, session, guard, environment, access,
            SellerFulfillmentMutationKind.Deliver, null, null, shipmentId, null, null, null, cancellationToken);

    private static async Task<IResult> SellerMutateAsync(
        Guid fulfillmentId,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        IAccessControlDirectory access,
        SellerFulfillmentMutationKind kind,
        string? carrier,
        IReadOnlyList<ShipmentLineCommand>? lines,
        Guid? shipmentId,
        string? tracking,
        string? shippingMethodCode,
        string? providerMetadataJson,
        CancellationToken cancellationToken)
    {
        try
        {
            var (actorUserId, sellerPartyId) = await SellerPanelAccess.RequireAuthorizedAsync(
                request, session, guard, environment, cancellationToken);
            var permission = await BuildPermissionSnapshotAsync(actorUserId, sellerPartyId, access, cancellationToken);
            return api.From(await sender.Send(new SellerMutateFulfillmentCommand(
                fulfillmentId,
                actorUserId,
                sellerPartyId,
                permission,
                kind,
                carrier,
                lines,
                shipmentId,
                tracking,
                shippingMethodCode,
                providerMetadataJson), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<SellerHandlePermissionInput> BuildPermissionSnapshotAsync(
        Guid actorUserId,
        Guid sellerPartyId,
        IAccessControlDirectory access,
        CancellationToken cancellationToken)
    {
        var effective = await access.GetEffectiveAccessAsync(
            actorUserId,
            new AccessOwnerScope(AccessOwnerScopeKind.Seller, sellerPartyId),
            cancellationToken);
        var handles = effective.Permissions
            .Where(p => p.PermissionId == "order.handle" && !p.DeniedByCeiling)
            .ToList();
        var hasGlobal = handles.Any(p => p.ScopeKind == AccessScopeKind.GlobalWithinOwner);
        var allowed = handles
            .Where(p => p.ScopeKind == AccessScopeKind.Category && p.ScopeResourceId is not null)
            .Select(p => p.ScopeResourceId!.Value)
            .Distinct()
            .ToArray();
        return new SellerHandlePermissionInput(hasGlobal, allowed);
    }

    private static async Task<IResult> AdminListAsync(
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            return api.From(await sender.Send(new ListAdminFulfillmentsQuery(), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static Task<IResult> AdminQueryGridAsync(
        GridQueryRequest body,
        IAdminFulfillmentWorkQueueQuery query,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        AdminGridQueryEndpoint.ExecuteAsync(
            body,
            request,
            session,
            tenant,
            guard,
            environment,
            async (q, ct) =>
            {
                var normalized = AdminListGridPolicies.Fulfillments.Normalize(q);
                return await query.QueryAsync(normalized, ct);
            },
            cancellationToken);

    private static Task<IResult> AdminWorkQueueQueryAsync(
        GridQueryRequest body,
        IAdminFulfillmentWorkQueueQuery query,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken) =>
        AdminGridQueryEndpoint.ExecuteAsync(
            body,
            request,
            session,
            tenant,
            guard,
            environment,
            async (q, ct) =>
            {
                var normalized = AdminListGridPolicies.Fulfillments.Normalize(q);
                return await query.QueryAsync(normalized, ct);
            },
            cancellationToken);

    private static async Task<IResult> AdminWorkQueueBulkAsync(
        AdminFulfillmentWorkQueueBulkRequest body,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var outcome = await sender.Send(
                new ExecuteAdminFulfillmentBulkCommand(actor, body),
                cancellationToken);
            if (outcome.IsFailure)
            {
                return api.From(outcome);
            }

            var result = outcome.Value;
            if (result.ErrorCode is not null)
            {
                return api.FromFailure(new SemanticError(result.ErrorCode));
            }

            return api.From(Result.Success(result));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
        catch (InvalidOperationException)
        {
            return api.FromFailure(new SemanticError(FulfillmentErrorCodes.WorkQueueBulkFailed));
        }
    }

    private static async Task<IResult> AdminGetAsync(
        Guid fulfillmentId,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            return api.From(await sender.Send(new GetAdminFulfillmentQuery(fulfillmentId), cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static async Task<IResult> CustomerListAsync(
        Guid checkoutId,
        FulfillmentPanelComposer composer,
        IFulfillmentDirectory fulfillment,
        ICartQueryGateway carts,
        ICustomerCheckoutOwnershipReader ownership,
        ApiResponseFactory api,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = ResolveCustomerActor(request, session, environment);
            var guestSecret = ReadGuestSecret(request);
            if (actor is null && string.IsNullOrWhiteSpace(guestSecret))
            {
                return api.FromFailure(new SemanticError(FulfillmentErrorCodes.CustomerActorMissing));
            }

            var checkout = await ownership.GetAsync(checkoutId, cancellationToken);
            if (checkout is null)
            {
                return api.FromFailure(new SemanticError(FulfillmentErrorCodes.CustomerOrderMissing));
            }

            var ownedByActor = actor is not null && checkout.PlacedByUserId == actor.Value;
            if (checkout.PlacedByUserId == StorefrontCheckoutComposer.StorefrontGuestActorId)
            {
                ownedByActor = false;
            }

            var ownedByGuest = false;
            if (!ownedByActor && !string.IsNullOrWhiteSpace(guestSecret))
            {
                try
                {
                    var cart = await carts.GetCartAsync(
                        checkout.CartId,
                        new CartAccess(null, guestSecret),
                        cancellationToken);
                    ownedByGuest = cart is not null;
                }
                catch (InvalidOperationException)
                {
                    ownedByGuest = false;
                }
            }

            if (!ownedByActor && !ownedByGuest)
            {
                return api.FromFailure(new SemanticError(FulfillmentErrorCodes.CustomerOrderMissing));
            }

            var list = await composer.ListForCheckoutAsync(checkoutId, cancellationToken);
            var packages = await fulfillment.GetPackagesForCheckoutAsync(checkoutId, cancellationToken);
            var preferred = FulfillmentPanelComposer.SelectPreferredCustomerPackage(packages);
            return api.From(Result.Success(new
            {
                fulfillments = list,
                preferredCustomerTrackingReference = preferred?.TrackingReference,
                preferredCustomerTrackingPackageNumber = preferred?.PackageNumber,
                preferredCustomerPackageStatus = preferred?.Status.ToString(),
            }));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromFailure(new SemanticError(ex.ErrorCode ?? "platform.http"));
        }
    }

    private static Guid? ResolveCustomerActor(HttpRequest request, CurrentAuthenticatedSession session, IHostEnvironment environment)
    {
        if (session.IsAuthenticated && session.UserId is { } authenticated)
        {
            return authenticated;
        }

        var isDevSeam = environment.IsDevelopment() || environment.IsEnvironment("Testing");
        if (!isDevSeam)
        {
            return null;
        }

        if (request.Headers.TryGetValue("X-Tooba-Dev-Actor-User-Id", out var raw)
            && Guid.TryParse(raw.ToString(), out var devActor)
            && devActor != Guid.Empty)
        {
            return devActor;
        }

        return StorefrontCheckoutComposer.StorefrontGuestActorId;
    }

    private static string? ReadGuestSecret(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-Tooba-Guest-Secret", out var header) && !string.IsNullOrWhiteSpace(header))
        {
            return header.ToString();
        }

        return null;
    }
}
