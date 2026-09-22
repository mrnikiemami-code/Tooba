using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Order.Application.Admin.InventoryRecovery.Queries.AssessOrderInventoryRecovery;
using Tooba.Order.Application.Admin.InventoryRecovery.Queries.AuditOrderInventoryRecovery;
using Tooba.Order.Application.Admin.Supply.Queries.GetOrderSupplyStatus;

namespace Tooba.Order.Endpoints;

internal static class AdminOrderInventoryRecoverySupplyEndpoints
{
    internal static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin/orders");
        group.MapGet("/inventory-recovery/audit", AuditAsync);
        group.MapGet("/{checkoutId:guid}/inventory-recovery", AssessAsync);
        group.MapGet("/{checkoutId:guid}/supply-status", GetSupplyStatusAsync);
    }

    private static async Task<IResult> AuditAsync(
        ISender sender,
        IOrderAdminAuthorizer auth,
        ApiResponseFactory api,
        HttpContext context,
        int? take,
        CancellationToken ct)
    {
        await auth.RequireAdminAsync(context, ct);
        return api.From(await sender.Send(new AuditOrderInventoryRecoveryQuery(take ?? 50), ct));
    }

    private static async Task<IResult> AssessAsync(
        Guid checkoutId,
        ISender sender,
        IOrderAdminAuthorizer auth,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken ct)
    {
        await auth.RequireAdminAsync(context, ct);
        return api.From(await sender.Send(new AssessOrderInventoryRecoveryQuery(checkoutId), ct));
    }

    private static async Task<IResult> GetSupplyStatusAsync(
        Guid checkoutId,
        ISender sender,
        IOrderAdminAuthorizer auth,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken ct)
    {
        await auth.RequireAdminAsync(context, ct);
        return api.From(await sender.Send(new GetOrderSupplyStatusQuery(checkoutId), ct));
    }
}
