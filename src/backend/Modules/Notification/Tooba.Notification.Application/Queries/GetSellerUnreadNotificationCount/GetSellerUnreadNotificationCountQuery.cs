using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Notification.Application.Models;
using Tooba.Notification.Application.Ports;
using Tooba.Notification.Contracts.Dtos;

namespace Tooba.Notification.Application.Queries.GetSellerUnreadNotificationCount;

/// <summary>MediatR seller unread-count use case.</summary>
public sealed record GetSellerUnreadNotificationCountQuery(Guid SellerPartyId)
    : IRequest<Result<NotificationUnreadCountResponse>>;

/// <summary>Returns unread count for the seller party.</summary>
public sealed class GetSellerUnreadNotificationCountHandler(INotificationDirectory directory)
    : IRequestHandler<GetSellerUnreadNotificationCountQuery, Result<NotificationUnreadCountResponse>>
{
    public async Task<Result<NotificationUnreadCountResponse>> Handle(
        GetSellerUnreadNotificationCountQuery request, CancellationToken cancellationToken)
    {
        var count = await directory.UnreadCountAsync(
            NotificationRecipientKind.Seller,
            request.SellerPartyId,
            null,
            cancellationToken);
        return Result.Success(new NotificationUnreadCountResponse(count));
    }
}
