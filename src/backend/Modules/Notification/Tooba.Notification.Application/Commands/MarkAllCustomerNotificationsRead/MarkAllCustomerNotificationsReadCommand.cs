using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Notification.Application.Models;
using Tooba.Notification.Application.Ports;
using Tooba.Notification.Contracts.Dtos;

namespace Tooba.Notification.Application.Commands.MarkAllCustomerNotificationsRead;

/// <summary>MediatR mark-all customer notifications read use case.</summary>
public sealed record MarkAllCustomerNotificationsReadCommand(Guid ActorUserId)
    : IRequest<Result<NotificationMarkedCountResponse>>;

/// <summary>Marks all unread customer notifications for the actor.</summary>
public sealed class MarkAllCustomerNotificationsReadHandler(INotificationDirectory directory)
    : IRequestHandler<MarkAllCustomerNotificationsReadCommand, Result<NotificationMarkedCountResponse>>
{
    public async Task<Result<NotificationMarkedCountResponse>> Handle(
        MarkAllCustomerNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var changed = await directory.MarkAllReadAsync(
            NotificationRecipientKind.Customer,
            request.ActorUserId,
            request.ActorUserId,
            cancellationToken);
        return Result.Success(new NotificationMarkedCountResponse(changed));
    }
}
