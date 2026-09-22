using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Notification.Application.Models;
using Tooba.Notification.Application.Ports;
using Tooba.Notification.Contracts.Dtos;

namespace Tooba.Notification.Application.Commands.MarkAllSellerNotificationsRead;

/// <summary>MediatR mark-all seller notifications read use case.</summary>
public sealed record MarkAllSellerNotificationsReadCommand(Guid SellerPartyId)
    : IRequest<Result<NotificationMarkedCountResponse>>;

/// <summary>Marks all unread seller notifications for the party.</summary>
public sealed class MarkAllSellerNotificationsReadHandler(INotificationDirectory directory)
    : IRequestHandler<MarkAllSellerNotificationsReadCommand, Result<NotificationMarkedCountResponse>>
{
    public async Task<Result<NotificationMarkedCountResponse>> Handle(
        MarkAllSellerNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var changed = await directory.MarkAllReadAsync(
            NotificationRecipientKind.Seller,
            request.SellerPartyId,
            null,
            cancellationToken);
        return Result.Success(new NotificationMarkedCountResponse(changed));
    }
}
