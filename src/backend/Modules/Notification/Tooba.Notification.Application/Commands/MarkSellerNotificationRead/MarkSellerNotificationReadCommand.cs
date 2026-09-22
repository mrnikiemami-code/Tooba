using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Notification.Application.Errors;
using Tooba.Notification.Application.Ports;
using Tooba.Notification.Contracts.Dtos;

namespace Tooba.Notification.Application.Commands.MarkSellerNotificationRead;

/// <summary>MediatR mark seller notification read use case.</summary>
public sealed record MarkSellerNotificationReadCommand(Guid NotificationId, Guid SellerPartyId)
    : IRequest<Result>;

/// <summary>Marks one seller notification read; missing → SemanticError.</summary>
public sealed class MarkSellerNotificationReadHandler(INotificationDirectory directory)
    : IRequestHandler<MarkSellerNotificationReadCommand, Result>
{
    public async Task<Result> Handle(
        MarkSellerNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var ok = await directory.MarkReadAsync(
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
