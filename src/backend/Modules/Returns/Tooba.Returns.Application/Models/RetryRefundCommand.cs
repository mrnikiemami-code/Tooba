using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.Models;


/// <summary>
/// فرمان retry refund (admin).
/// </summary>
public sealed record RetryRefundCommand(Guid ReturnRequestId, Guid ActorUserId);
