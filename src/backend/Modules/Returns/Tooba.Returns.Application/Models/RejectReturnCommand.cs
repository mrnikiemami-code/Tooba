using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.Models;


/// <summary>
/// فرمان رد مرجوعی.
/// </summary>
public sealed record RejectReturnCommand(Guid ReturnRequestId, Guid ActorUserId, string? Reason);
