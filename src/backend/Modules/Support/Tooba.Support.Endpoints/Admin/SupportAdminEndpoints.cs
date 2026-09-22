using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Support.Application.Commands.PatchAdminTicket;
using Tooba.Support.Application.Commands.ReplyAdminTicket;
using Tooba.Support.Application.Queries.GetAdminTicket;
using Tooba.Support.Application.Queries.GetSupportDemoPreview;
using Tooba.Support.Application.Queries.ListAdminTickets;
using Tooba.Support.Endpoints.Customer;

namespace Tooba.Support.Endpoints.Admin;

/// <summary>Wire-only admin patch body.</summary>
public sealed record AdminPatchBody(
    string? Status,
    string? Priority,
    Guid? AssignedOperatorActorUserId);

/// <summary>Thin admin Support HTTP routes.</summary>
public static class SupportAdminEndpoints
{
    /// <summary>Maps admin Support ticket routes under the group.</summary>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/tickets", ListAsync);
        group.MapGet("/tickets/{ticketId:guid}", GetAsync);
        group.MapPost("/tickets/{ticketId:guid}/replies", ReplyAsync);
        group.MapPatch("/tickets/{ticketId:guid}", PatchAsync);
        group.MapGet("/demo-preview", DemoPreviewAsync);
    }

    private static async Task<IResult> ListAsync(
        ISender sender, ISupportAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken,
        string? status = null,
        string? requesterKind = null,
        string? category = null,
        string? priority = null,
        string? q = null,
        int page = 1,
        int pageSize = 20)
    {
        await authorizer.RequireAuthorizedAsync(context, "support.view", cancellationToken);
        return api.From(await sender.Send(
            new ListAdminTicketsQuery(status, requesterKind, category, priority, q, page, pageSize),
            cancellationToken));
    }

    private static async Task<IResult> GetAsync(
        Guid ticketId, ISender sender, ISupportAdminAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, "support.view", cancellationToken);
        return api.From(await sender.Send(new GetAdminTicketQuery(ticketId), cancellationToken));
    }

    private static async Task<IResult> ReplyAsync(
        Guid ticketId, ReplyTicketBody body, ISender sender, ISupportAdminAuthorizer authorizer,
        ApiResponseFactory api, HttpContext context, CancellationToken cancellationToken)
    {
        var actor = await authorizer.RequireAuthorizedAsync(context, "support.manage", cancellationToken);
        return api.From(await sender.Send(
            new ReplyAdminTicketCommand(
                actor,
                ticketId,
                body.Body,
                body.IsInternalNote,
                ReadIdempotencyKey(context.Request)),
            cancellationToken));
    }

    private static async Task<IResult> PatchAsync(
        Guid ticketId, AdminPatchBody body, ISender sender, ISupportAdminAuthorizer authorizer,
        ApiResponseFactory api, HttpContext context, CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, "support.manage", cancellationToken);
        return api.From(await sender.Send(
            new PatchAdminTicketCommand(ticketId, body.Status, body.Priority, body.AssignedOperatorActorUserId),
            cancellationToken));
    }

    private static async Task<IResult> DemoPreviewAsync(
        ISender sender, ApiResponseFactory api,
        IHostEnvironment environment, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            return Results.NotFound();

        return api.From(await sender.Send(new GetSupportDemoPreviewQuery(), cancellationToken));
    }

    private static string? ReadIdempotencyKey(HttpRequest request) =>
        request.Headers.TryGetValue("Idempotency-Key", out var raw) && !string.IsNullOrWhiteSpace(raw)
            ? raw.ToString().Trim()
            : null;
}
