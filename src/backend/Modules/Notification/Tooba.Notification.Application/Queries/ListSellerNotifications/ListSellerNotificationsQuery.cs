using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Notification.Application.Models;
using Tooba.Notification.Application.Ports;
using Tooba.Notification.Contracts.Dtos;

namespace Tooba.Notification.Application.Queries.ListSellerNotifications;

/// <summary>MediatR list seller notifications use case.</summary>
public sealed record ListSellerNotificationsQuery(
    Guid SellerPartyId,
    int Skip,
    int Take,
    string Locale) : IRequest<Result<NotificationListHttpResponse>>;

/// <summary>Lists seller notifications for the authorized party.</summary>
public sealed class ListSellerNotificationsHandler(INotificationDirectory directory)
    : IRequestHandler<ListSellerNotificationsQuery, Result<NotificationListHttpResponse>>
{
    public async Task<Result<NotificationListHttpResponse>> Handle(
        ListSellerNotificationsQuery request, CancellationToken cancellationToken)
    {
        var page = await directory.ListAsync(
            new NotificationRecipientQuery(
                NotificationRecipientKind.Seller,
                request.SellerPartyId,
                null,
                request.Skip,
                request.Take,
                string.IsNullOrWhiteSpace(request.Locale) ? "fa" : request.Locale),
            cancellationToken);
        return Result.Success(NotificationHttpMapper.ToListResponse(page));
    }
}
