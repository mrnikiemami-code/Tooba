using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Commands.CloseCustomerTicket;
using Tooba.Support.Application.Commands.CreateCustomerTicket;
using Tooba.Support.Application.Commands.ReopenCustomerTicket;
using Tooba.Support.Application.Commands.ReplyCustomerTicket;
using Tooba.Support.Application.Errors;
using Tooba.Support.Application.Queries.GetCustomerTicket;
using Tooba.Support.Application.Queries.ListCustomerTickets;

namespace Tooba.Support.Endpoints.Customer;

/// <summary>Wire-only create ticket body.</summary>
public sealed record CreateTicketBody(
    string Subject,
    string Category,
    string? Priority,
    string Body,
    string? RelatedEntityType,
    Guid? RelatedEntityId);

/// <summary>Wire-only reply ticket body.</summary>
public sealed record ReplyTicketBody(string Body, bool IsInternalNote = false);

/// <summary>Thin customer Support HTTP routes.</summary>
public static class SupportCustomerEndpoints
{
    /// <summary>Maps customer Support ticket routes under the group.</summary>
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
        ISender sender, ISupportCustomerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken,
        string? status = null, int page = 1, int pageSize = 20)
    {
        var actor = authorizer.TryResolveActor(context);
        if (actor is null)
            return api.FromFailure(new SemanticError(SupportErrorCodes.CustomerSessionRequired));
        return api.From(await sender.Send(
            new ListCustomerTicketsQuery(actor.Value, status, page, pageSize), cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        CreateTicketBody body, ISender sender, ISupportCustomerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var actor = authorizer.TryResolveActor(context);
        if (actor is null)
            return api.FromFailure(new SemanticError(SupportErrorCodes.CustomerSessionRequired));
        var result = await sender.Send(
            new CreateCustomerTicketCommand(
                actor.Value,
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
        Guid ticketId, ISender sender, ISupportCustomerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var actor = authorizer.TryResolveActor(context);
        if (actor is null)
            return api.FromFailure(new SemanticError(SupportErrorCodes.CustomerSessionRequired));
        return api.From(await sender.Send(
            new GetCustomerTicketQuery(actor.Value, ticketId), cancellationToken));
    }

    private static async Task<IResult> ReplyAsync(
        Guid ticketId, ReplyTicketBody body, ISender sender, ISupportCustomerAuthorizer authorizer,
        ApiResponseFactory api, HttpContext context, CancellationToken cancellationToken)
    {
        var actor = authorizer.TryResolveActor(context);
        if (actor is null)
            return api.FromFailure(new SemanticError(SupportErrorCodes.CustomerSessionRequired));
        return api.From(await sender.Send(
            new ReplyCustomerTicketCommand(
                actor.Value, ticketId, body.Body, ReadIdempotencyKey(context.Request)),
            cancellationToken));
    }

    private static async Task<IResult> CloseAsync(
        Guid ticketId, ISender sender, ISupportCustomerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var actor = authorizer.TryResolveActor(context);
        if (actor is null)
            return api.FromFailure(new SemanticError(SupportErrorCodes.CustomerSessionRequired));
        return api.From(await sender.Send(
            new CloseCustomerTicketCommand(actor.Value, ticketId), cancellationToken));
    }

    private static async Task<IResult> ReopenAsync(
        Guid ticketId, ISender sender, ISupportCustomerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var actor = authorizer.TryResolveActor(context);
        if (actor is null)
            return api.FromFailure(new SemanticError(SupportErrorCodes.CustomerSessionRequired));
        return api.From(await sender.Send(
            new ReopenCustomerTicketCommand(actor.Value, ticketId), cancellationToken));
    }

    private static string? ReadIdempotencyKey(HttpRequest request) =>
        request.Headers.TryGetValue("Idempotency-Key", out var raw) && !string.IsNullOrWhiteSpace(raw)
            ? raw.ToString().Trim()
            : null;
}
