using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Notification.Application.Errors;
using Tooba.Notification.Application.Ports;
using Tooba.Notification.Contracts.Dtos;

namespace Tooba.Notification.Application.Commands.DismissCustomerNotification;

/// <summary>MediatR dismiss customer notification use case.</summary>
public sealed record DismissCustomerNotificationCommand(Guid NotificationId, Guid ActorUserId)
    : IRequest<Result>;

/// <summary>Soft-deletes one customer notification; missing → SemanticError.</summary>
public sealed class DismissCustomerNotificationHandler(INotificationDirectory directory)
    : IRequestHandler<DismissCustomerNotificationCommand, Result>
{
    public async Task<Result> Handle(
        DismissCustomerNotificationCommand request, CancellationToken cancellationToken)
    {
        var ok = await directory.SoftDeleteAsync(
            request.NotificationId,
            NotificationRecipientKind.Customer,
            request.ActorUserId,
            request.ActorUserId,
            cancellationToken);
        return ok
            ? Result.Success()
            : Result.Failure(new SemanticError(NotificationErrorCodes.Missing));
    }
}
