using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.Models;


/// <summary>
/// فرمان ایجاد درخواست مرجوعی.
/// </summary>
public sealed record CreateReturnCommand(
    Guid SellerOrderId,
    Guid ActorUserId,
    string IdempotencyKey,
    string? Reason,
    IReadOnlyList<ReturnLineCommand> Items,
    RefundDestination RefundDestination = RefundDestination.OriginalPayment);
