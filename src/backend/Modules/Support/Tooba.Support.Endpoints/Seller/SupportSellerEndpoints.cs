using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Support.Application.Commands.CloseSellerTicket;
using Tooba.Support.Application.Commands.CreateSellerTicket;
using Tooba.Support.Application.Commands.ReopenSellerTicket;
using Tooba.Support.Application.Commands.ReplySellerTicket;
using Tooba.Support.Application.Queries.GetSellerTicket;
using Tooba.Support.Application.Queries.ListSellerTickets;
using Tooba.Support.Endpoints.Customer;

namespace Tooba.Support.Endpoints.Seller;

/// <summary>Thin seller Support HTTP routes.</summary>
public static class SupportSellerEndpoints
{
    /// <summary>Maps seller Support ticket routes under the group.</summary>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/tickets", ListAsync);
        group.MapPost("/tickets", CreateAsync);
        group.MapGet("/tickets/{ticketId:guid}", GetAsync);
        group.MapPost("/tickets/{ticketId:guid}/replies", ReplyAsync);
        group.MapPost("/tickets/{ticketId:guid}/close", CloseAsync);
        group.MapPost("/tickets/{ticketId:guid}/reopen", ReopenAsync);
    }

    private static async Task<IResult> ListAsync(
        ISender sender, ISupportSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken,
        string? status = null, int page = 1, int pageSize = 20)
    {
        var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, "support.view", cancellationToken);
        return api.From(await sender.Send(
            new ListSellerTicketsQuery(sellerPartyId, status, page, pageSize), cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        CreateTicketBody body, ISender sender, ISupportSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var (actorUserId, sellerPartyId) = await authorizer.RequireAuthorizedAsync(
            context, "support.create", cancellationToken);
        var result = await sender.Send(
            new CreateSellerTicketCommand(
                actorUserId,
                sellerPartyId,
                body.Subject,
                body.Category,
                body.Priority,
                body.Body,
                body.RelatedEntityType,
                body.RelatedEntityId,
                ReadIdempotencyKey(context.Request)),
            cancellationToken);
        if (result.IsFailure)
            return api.From(result);
        return Results.Json(result.Value, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetAsync(
        Guid ticketId, ISender sender, ISupportSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, "support.view", cancellationToken);
        return api.From(await sender.Send(
            new GetSellerTicketQuery(sellerPartyId, ticketId), cancellationToken));
    }

    private static async Task<IResult> ReplyAsync(
        Guid ticketId, ReplyTicketBody body, ISender sender, ISupportSellerAuthorizer authorizer,
        ApiResponseFactory api, HttpContext context, CancellationToken cancellationToken)
    {
        var (actorUserId, sellerPartyId) = await authorizer.RequireAuthorizedAsync(
            context, "support.reply", cancellationToken);
        return api.From(await sender.Send(
            new ReplySellerTicketCommand(
                actorUserId, sellerPartyId, ticketId, body.Body, ReadIdempotencyKey(context.Request)),
            cancellationToken));
    }

    private static async Task<IResult> CloseAsync(
        Guid ticketId, ISender sender, ISupportSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, "support.reply", cancellationToken);
        return api.From(await sender.Send(
            new CloseSellerTicketCommand(sellerPartyId, ticketId), cancellationToken));
    }

    private static async Task<IResult> ReopenAsync(
        Guid ticketId, ISender sender, ISupportSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, "support.reply", cancellationToken);
        return api.From(await sender.Send(
            new ReopenSellerTicketCommand(sellerPartyId, ticketId), cancellationToken));
    }

    private static string? ReadIdempotencyKey(HttpRequest request) =>
        request.Headers.TryGetValue("Idempotency-Key", out var raw) && !string.IsNullOrWhiteSpace(raw)
            ? raw.ToString().Trim()
            : null;
}
