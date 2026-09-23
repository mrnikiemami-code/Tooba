using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Order.Application.Admin.Operations.Commands.ApproveReturn;
using Tooba.Order.Application.Admin.Operations.Commands.AssignConsolidatedPackageTracking;
using Tooba.Order.Application.Admin.Operations.Commands.AssignTracking;
using Tooba.Order.Application.Admin.Operations.Commands.CancelConsolidatedPackage;
using Tooba.Order.Application.Admin.Operations.Commands.CancelOrder;
using Tooba.Order.Application.Admin.Operations.Commands.CancelShipment;
using Tooba.Order.Application.Admin.Operations.Commands.ConfirmDeposit;
using Tooba.Order.Application.Admin.Operations.Commands.CorrectTracking;
using Tooba.Order.Application.Admin.Operations.Commands.CreateConsolidatedPackage;
using Tooba.Order.Application.Admin.Operations.Commands.CreateShipment;
using Tooba.Order.Application.Admin.Operations.Commands.DeliverConsolidatedPackage;
using Tooba.Order.Application.Admin.Operations.Commands.DeliverShipment;
using Tooba.Order.Application.Admin.Operations.Commands.DispatchConsolidatedPackage;
using Tooba.Order.Application.Admin.Operations.Commands.DispatchShipment;
using Tooba.Order.Application.Admin.Operations.Commands.MarkFulfillmentPacked;
using Tooba.Order.Application.Admin.Operations.Commands.MarkFulfillmentProcessing;
using Tooba.Order.Application.Admin.Operations.Commands.PackFulfillmentSelected;
using Tooba.Order.Application.Admin.Operations.Commands.RecoverInventoryReservation;
using Tooba.Order.Application.Admin.Operations.Commands.RejectDeposit;
using Tooba.Order.Application.Admin.Operations.Commands.RejectReturn;
using Tooba.Order.Application.Admin.Operations.Commands.RequestReturn;
using Tooba.Order.Application.Admin.Operations.Commands.RestoreCancelledOrder;
using Tooba.Order.Application.Admin.Operations.Commands.RestoreDeposit;
using Tooba.Order.Application.Admin.Operations.Commands.RetryRefund;
using Tooba.Order.Application.Admin.Operations.Commands.UnconfirmDeposit;
using Tooba.Order.Application.Admin.Operations.Commands.UnpackFulfillment;
using Tooba.Order.Application.Admin.Operations.Commands.UnprocessFulfillment;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Queries.GetAdminOrderOperations;
using Tooba.Order.Application.Admin.Operations.Queries.ListAdminOrderReturnEligibility;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

using Tooba.Order.Endpoints;

namespace Tooba.Order.Endpoints.Admin.Operations;

internal static class AdminOrderOperationsEndpoints
{
    internal static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin/orders");
        group.MapGet("/{checkoutId:guid}/operations", ListAsync);
        group.MapPost("/{checkoutId:guid}/operations", ExecuteAsync);
        group.MapGet("/{checkoutId:guid}/return-eligibility", ListReturnEligibilityAsync);
    }

    private static async Task<IResult> ListAsync(
        Guid checkoutId,
        ISender sender,
        IOrderAdminAuthorizer auth,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken ct)
    {
        var actor = await auth.RequireAdminAsync(context, ct);
        return api.From(await sender.Send(new GetAdminOrderOperationsQuery(checkoutId, actor), ct));
    }

    private static async Task<IResult> ListReturnEligibilityAsync(
        Guid checkoutId,
        ISender sender,
        IOrderAdminAuthorizer auth,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken ct)
    {
        await auth.RequireAdminAsync(context, ct);
        return api.From(await sender.Send(new ListAdminOrderReturnEligibilityQuery(checkoutId), ct));
    }

    private static async Task<IResult> ExecuteAsync(
        Guid checkoutId,
        AdminOrderOperationRequest? body,
        ISender sender,
        IOrderAdminAuthorizer auth,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken ct)
    {
        var actor = await auth.RequireAdminAsync(context, ct);
        var request = body ?? new AdminOrderOperationRequest(
            string.Empty, null, null, null, null, null, null, null, null, null);
        var code = (request.Code ?? string.Empty).Trim().ToLowerInvariant();
        var result = code switch
        {
            "cancel" => await sender.Send(new CancelOrderCommand(checkoutId, actor, request), ct),
            "restore_cancelled_order" => await sender.Send(new RestoreCancelledOrderCommand(checkoutId, actor, request), ct),
            "confirm_deposit" => await sender.Send(new ConfirmDepositCommand(checkoutId, actor, request), ct),
            "reject_deposit" => await sender.Send(new RejectDepositCommand(checkoutId, actor, request), ct),
            "restore_deposit" => await sender.Send(new RestoreDepositCommand(checkoutId, actor, request), ct),
            "unconfirm_deposit" => await sender.Send(new UnconfirmDepositCommand(checkoutId, actor, request), ct),
            "recover_inventory_reservation" => await sender.Send(new RecoverInventoryReservationCommand(checkoutId, actor, request), ct),
            "mark_processing" => await sender.Send(new MarkFulfillmentProcessingCommand(checkoutId, actor, request), ct),
            "mark_packed" => await sender.Send(new MarkFulfillmentPackedCommand(checkoutId, actor, request), ct),
            "pack_selected" => await sender.Send(new PackFulfillmentSelectedCommand(checkoutId, actor, request), ct),
            "unprocess" => await sender.Send(new UnprocessFulfillmentCommand(checkoutId, actor, request), ct),
            "unpack" => await sender.Send(new UnpackFulfillmentCommand(checkoutId, actor, request), ct),
            "create_shipment" => await sender.Send(new CreateShipmentCommand(checkoutId, actor, request), ct),
            "cancel_shipment" => await sender.Send(new CancelShipmentCommand(checkoutId, actor, request), ct),
            "assign_tracking" => await sender.Send(new AssignTrackingCommand(checkoutId, actor, request), ct),
            "correct_tracking" => await sender.Send(new CorrectTrackingCommand(checkoutId, actor, request), ct),
            "dispatch_shipment" => await sender.Send(new DispatchShipmentCommand(checkoutId, actor, request), ct),
            "deliver_shipment" => await sender.Send(new DeliverShipmentCommand(checkoutId, actor, request), ct),
            "request_return" => await sender.Send(new RequestReturnCommand(checkoutId, actor, request), ct),
            "approve_return" => await sender.Send(new ApproveReturnCommand(checkoutId, actor, request), ct),
            "reject_return" => await sender.Send(new RejectReturnCommand(checkoutId, actor, request), ct),
            "retry_refund" => await sender.Send(new RetryRefundCommand(checkoutId, actor, request), ct),
            "create_consolidated_package" => await sender.Send(new CreateConsolidatedPackageCommand(checkoutId, actor, request), ct),
            "cancel_consolidated_package" => await sender.Send(new CancelConsolidatedPackageCommand(checkoutId, actor, request), ct),
            "assign_consolidated_package_tracking" => await sender.Send(new AssignConsolidatedPackageTrackingCommand(checkoutId, actor, request), ct),
            "dispatch_consolidated_package" => await sender.Send(new DispatchConsolidatedPackageCommand(checkoutId, actor, request), ct),
            "deliver_consolidated_package" => await sender.Send(new DeliverConsolidatedPackageCommand(checkoutId, actor, request), ct),
            _ => Result.Failure<object>(new SemanticError("order.operation.invalid")),
        };
        return api.From(result);
    }
}
