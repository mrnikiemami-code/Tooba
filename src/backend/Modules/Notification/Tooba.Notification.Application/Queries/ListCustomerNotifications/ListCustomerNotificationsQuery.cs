using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Notification.Application.Models;
using Tooba.Notification.Application.Ports;
using Tooba.Notification.Contracts.Dtos;

namespace Tooba.Notification.Application.Queries.ListCustomerNotifications;

/// <summary>MediatR list customer notifications use case.</summary>
public sealed record ListCustomerNotificationsQuery(
    Guid ActorUserId,
    int Skip,
    int Take,
    string Locale) : IRequest<Result<NotificationListHttpResponse>>;

/// <summary>Lists customer notifications for the resolved actor.</summary>
public sealed class ListCustomerNotificationsHandler(INotificationDirectory directory)
    : IRequestHandler<ListCustomerNotificationsQuery, Result<NotificationListHttpResponse>>
{
    public async Task<Result<NotificationListHttpResponse>> Handle(
        ListCustomerNotificationsQuery request, CancellationToken cancellationToken)
    {
        var page = await directory.ListAsync(
            new NotificationRecipientQuery(
                NotificationRecipientKind.Customer,
                request.ActorUserId,
                request.ActorUserId,
                request.Skip,
                request.Take,
                string.IsNullOrWhiteSpace(request.Locale) ? "fa" : request.Locale),
            cancellationToken);
        return Result.Success(NotificationHttpMapper.ToListResponse(page));
    }
}
