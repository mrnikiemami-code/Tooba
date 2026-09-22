using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Notification.Application.Models;
using Tooba.Notification.Application.Ports;
using Tooba.Notification.Contracts.Dtos;

namespace Tooba.Notification.Application.Queries.GetCustomerUnreadNotificationCount;

/// <summary>MediatR customer unread-count use case.</summary>
public sealed record GetCustomerUnreadNotificationCountQuery(Guid ActorUserId)
    : IRequest<Result<NotificationUnreadCountResponse>>;

/// <summary>Returns unread count for the customer actor.</summary>
public sealed class GetCustomerUnreadNotificationCountHandler(INotificationDirectory directory)
    : IRequestHandler<GetCustomerUnreadNotificationCountQuery, Result<NotificationUnreadCountResponse>>
{
    public async Task<Result<NotificationUnreadCountResponse>> Handle(
        GetCustomerUnreadNotificationCountQuery request, CancellationToken cancellationToken)
    {
        var count = await directory.UnreadCountAsync(
            NotificationRecipientKind.Customer,
            request.ActorUserId,
            request.ActorUserId,
            cancellationToken);
        return Result.Success(new NotificationUnreadCountResponse(count));
    }
}
