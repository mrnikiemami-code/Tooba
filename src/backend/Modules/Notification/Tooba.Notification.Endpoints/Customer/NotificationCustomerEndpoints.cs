using MediatR;
using Tooba.BuildingBlocks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Notification.Application.Commands.DismissCustomerNotification;
using Tooba.Notification.Application.Commands.MarkAllCustomerNotificationsRead;
using Tooba.Notification.Application.Commands.MarkCustomerNotificationRead;
using Tooba.Notification.Application.Errors;
using Tooba.Notification.Application.Queries.GetCustomerUnreadNotificationCount;
using Tooba.Notification.Application.Queries.ListCustomerNotifications;

namespace Tooba.Notification.Endpoints.Customer;

/// <summary>Thin customer Notification HTTP routes.</summary>
public static class NotificationCustomerEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/", ListAsync);
        group.MapGet("/unread-count", UnreadCountAsync);
        group.MapPost("/{id:guid}/read", MarkReadAsync);
        group.MapPost("/read-all", MarkAllReadAsync);
        group.MapDelete("/{id:guid}", DismissAsync);
    }

    private static async Task<IResult> ListAsync(
        ISender sender, INotificationCustomerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken,
        int skip = 0, int take = 20, string? locale = null)
    {
        var actor = authorizer.TryResolveActor(context);
        if (actor is null)
            return api.FromFailure(new SemanticError(NotificationErrorCodes.CustomerSessionRequired));
        return api.From(await sender.Send(
            new ListCustomerNotificationsQuery(actor.Value, skip, take, locale ?? "fa"),
            cancellationToken));
    }

    private static async Task<IResult> UnreadCountAsync(
        ISender sender, INotificationCustomerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var actor = authorizer.TryResolveActor(context);
        if (actor is null)
            return api.FromFailure(new SemanticError(NotificationErrorCodes.CustomerSessionRequired));
        return api.From(await sender.Send(
            new GetCustomerUnreadNotificationCountQuery(actor.Value), cancellationToken));
    }

    private static async Task<IResult> MarkReadAsync(
        Guid id, ISender sender, INotificationCustomerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var actor = authorizer.TryResolveActor(context);
        if (actor is null)
            return api.FromFailure(new SemanticError(NotificationErrorCodes.CustomerSessionRequired));
        return api.From(await sender.Send(
            new MarkCustomerNotificationReadCommand(id, actor.Value), cancellationToken));
    }

    private static async Task<IResult> MarkAllReadAsync(
        ISender sender, INotificationCustomerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var actor = authorizer.TryResolveActor(context);
        if (actor is null)
            return api.FromFailure(new SemanticError(NotificationErrorCodes.CustomerSessionRequired));
        return api.From(await sender.Send(
            new MarkAllCustomerNotificationsReadCommand(actor.Value), cancellationToken));
    }

    private static async Task<IResult> DismissAsync(
        Guid id, ISender sender, INotificationCustomerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var actor = authorizer.TryResolveActor(context);
        if (actor is null)
            return api.FromFailure(new SemanticError(NotificationErrorCodes.CustomerSessionRequired));
        return api.From(await sender.Send(
            new DismissCustomerNotificationCommand(id, actor.Value), cancellationToken));
    }
}
