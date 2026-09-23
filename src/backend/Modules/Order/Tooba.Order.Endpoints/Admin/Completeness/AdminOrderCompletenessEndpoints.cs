using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Order.Application.Admin.Completeness.Commands.AddAdminOrderNote;
using Tooba.Order.Application.Admin.Completeness.Commands.DeleteAdminOrderNote;
using Tooba.Order.Application.Admin.Completeness.Models;
using Tooba.Order.Application.Admin.Completeness.Queries.GetAdminOrderInvoice;
using Tooba.Order.Application.Admin.Completeness.Queries.GetAdminOrderOperationalHistory;
using Tooba.Order.Application.Admin.Completeness.Queries.GetAdminOrderReceipt;
using Tooba.Order.Application.Admin.Completeness.Queries.ListAdminOrderNotes;

using Tooba.Order.Endpoints;

namespace Tooba.Order.Endpoints.Admin.Completeness;

internal static class AdminOrderCompletenessEndpoints
{
    private const string ViewPermission = "order.view";
    private const string HandlePermission = "order.handle";

    internal static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin/orders");
        group.MapGet("/{checkoutId:guid}/notes", ListNotesAsync);
        group.MapPost("/{checkoutId:guid}/notes", AddNoteAsync);
        group.MapDelete("/{checkoutId:guid}/notes/{noteId:guid}", DeleteNoteAsync);
        group.MapGet("/{checkoutId:guid}/operational-history", GetHistoryAsync);
        group.MapGet("/{checkoutId:guid}/invoice.html", GetInvoiceAsync);
        group.MapGet("/{checkoutId:guid}/receipt.html", GetReceiptAsync);
    }

    private static async Task<IResult> ListNotesAsync(Guid checkoutId, ISender sender, IOrderAdminAuthorizer auth, ApiResponseFactory api, HttpContext context, CancellationToken ct) =>
        api.From(await sender.Send(new ListAdminOrderNotesQuery(checkoutId, new(await auth.RequirePermissionAsync(context, ViewPermission, ct))), ct));

    private static async Task<IResult> AddNoteAsync(Guid checkoutId, AdminOrderNoteRequest? body, ISender sender, IOrderAdminAuthorizer auth, ApiResponseFactory api, HttpContext context, CancellationToken ct) =>
        api.From(await sender.Send(new AddAdminOrderNoteCommand(checkoutId, new(await auth.RequirePermissionAsync(context, HandlePermission, ct)), body?.Body ?? string.Empty), ct));

    private static async Task<IResult> DeleteNoteAsync(Guid checkoutId, Guid noteId, ISender sender, IOrderAdminAuthorizer auth, ApiResponseFactory api, HttpContext context, CancellationToken ct) =>
        api.From(await sender.Send(new DeleteAdminOrderNoteCommand(checkoutId, noteId, new(await auth.RequirePermissionAsync(context, HandlePermission, ct))), ct));

    private static async Task<IResult> GetHistoryAsync(Guid checkoutId, int? page, int? pageSize, ISender sender, IOrderAdminAuthorizer auth, ApiResponseFactory api, HttpContext context, CancellationToken ct) =>
        api.From(await sender.Send(new GetAdminOrderOperationalHistoryQuery(checkoutId, new(await auth.RequirePermissionAsync(context, ViewPermission, ct)), page ?? 1, pageSize ?? 20), ct));

    private static async Task<IResult> GetInvoiceAsync(Guid checkoutId, ISender sender, IOrderAdminAuthorizer auth, ApiResponseFactory api, HttpContext context, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminOrderInvoiceQuery(checkoutId, new(await auth.RequirePermissionAsync(context, ViewPermission, ct))), ct);
        return result.IsFailure ? api.From(result) : Results.Content(result.Value!.Html, "text/html; charset=utf-8");
    }

    private static async Task<IResult> GetReceiptAsync(Guid checkoutId, ISender sender, IOrderAdminAuthorizer auth, ApiResponseFactory api, HttpContext context, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminOrderReceiptQuery(checkoutId, new(await auth.RequirePermissionAsync(context, ViewPermission, ct))), ct);
        return result.IsFailure ? api.From(result) : Results.Content(result.Value!.Html, "text/html; charset=utf-8");
    }

    private sealed record AdminOrderNoteRequest(string? Body);
}
