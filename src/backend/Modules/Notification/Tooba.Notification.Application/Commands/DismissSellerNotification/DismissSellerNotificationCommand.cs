using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Notification.Application.Errors;
using Tooba.Notification.Application.Ports;
using Tooba.Notification.Contracts.Dtos;

namespace Tooba.Notification.Application.Commands.DismissSellerNotification;

/// <summary>MediatR dismiss seller notification use case.</summary>
public sealed record DismissSellerNotificationCommand(Guid NotificationId, Guid SellerPartyId)
    : IRequest<Result>;

/// <summary>Soft-deletes one seller notification; missing → SemanticError.</summary>
public sealed class DismissSellerNotificationHandler(INotificationDirectory directory)
    : IRequestHandler<DismissSellerNotificationCommand, Result>
{
    public async Task<Result> Handle(
        DismissSellerNotificationCommand request, CancellationToken cancellationToken)
    {
        var ok = await directory.SoftDeleteAsync(
            request.NotificationId,
            NotificationRecipientKind.Seller,
            request.SellerPartyId,
            null,
            cancellationToken);
        return ok
            ? Result.Success()
            : Result.Failure(new SemanticError(NotificationErrorCodes.Missing));
    }
}
