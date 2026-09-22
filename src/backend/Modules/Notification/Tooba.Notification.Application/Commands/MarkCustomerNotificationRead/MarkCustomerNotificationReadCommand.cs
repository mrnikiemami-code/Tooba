using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Notification.Application.Errors;
using Tooba.Notification.Application.Ports;
using Tooba.Notification.Contracts.Dtos;

namespace Tooba.Notification.Application.Commands.MarkCustomerNotificationRead;

/// <summary>MediatR mark customer notification read use case.</summary>
public sealed record MarkCustomerNotificationReadCommand(Guid NotificationId, Guid ActorUserId)
    : IRequest<Result>;

/// <summary>Marks one customer notification read; missing → SemanticError.</summary>
public sealed class MarkCustomerNotificationReadHandler(INotificationDirectory directory)
    : IRequestHandler<MarkCustomerNotificationReadCommand, Result>
{
    public async Task<Result> Handle(
        MarkCustomerNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var ok = await directory.MarkReadAsync(
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
