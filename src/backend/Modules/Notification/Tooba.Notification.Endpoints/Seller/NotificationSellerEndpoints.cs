using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Notification.Application.Commands.DismissSellerNotification;
using Tooba.Notification.Application.Commands.MarkAllSellerNotificationsRead;
using Tooba.Notification.Application.Commands.MarkSellerNotificationRead;
using Tooba.Notification.Application.Queries.GetSellerUnreadNotificationCount;
using Tooba.Notification.Application.Queries.ListSellerNotifications;

namespace Tooba.Notification.Endpoints.Seller;

/// <summary>Thin seller Notification HTTP routes.</summary>
public static class NotificationSellerEndpoints
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
        ISender sender, INotificationSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken,
        int skip = 0, int take = 20, string? locale = null)
    {
        var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(
            new ListSellerNotificationsQuery(sellerPartyId, skip, take, locale ?? "fa"),
            cancellationToken));
    }

    private static async Task<IResult> UnreadCountAsync(
        ISender sender, INotificationSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(
            new GetSellerUnreadNotificationCountQuery(sellerPartyId), cancellationToken));
    }

    private static async Task<IResult> MarkReadAsync(
        Guid id, ISender sender, INotificationSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(
            new MarkSellerNotificationReadCommand(id, sellerPartyId), cancellationToken));
    }

    private static async Task<IResult> MarkAllReadAsync(
        ISender sender, INotificationSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(
            new MarkAllSellerNotificationsReadCommand(sellerPartyId), cancellationToken));
    }

    private static async Task<IResult> DismissAsync(
        Guid id, ISender sender, INotificationSellerAuthorizer authorizer, ApiResponseFactory api,
        HttpContext context, CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await authorizer.RequireAuthorizedAsync(context, cancellationToken);
        return api.From(await sender.Send(
            new DismissSellerNotificationCommand(id, sellerPartyId), cancellationToken));
    }
}
