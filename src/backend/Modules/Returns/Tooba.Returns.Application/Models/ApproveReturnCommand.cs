using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.Models;


/// <summary>
/// فرمان تأیید مرجوعی.
/// </summary>
public sealed record ApproveReturnCommand(
    Guid ReturnRequestId,
    Guid ActorUserId,
    RefundDestination? RefundDestination = null);
