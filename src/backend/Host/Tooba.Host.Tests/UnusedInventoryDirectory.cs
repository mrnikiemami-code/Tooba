using Tooba.Inventory.Application;
using Tooba.Inventory.Domain;

namespace Tooba.Host.Tests;

/// <summary>
/// Stub Inventory for payment-bridge tests that never touch ReservationId commits.
/// </summary>
internal sealed class UnusedInventoryDirectory : IInventoryDirectory
{
    public Task<Guid> CreateLocationAsync(string code, string name, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<Guid> OpenPositionAsync(Guid offerId, Guid locationId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task AdjustAsync(
        Guid stockItemId,
        StockAdjustmentKind kind,
        decimal quantity,
        string reason,
        string? idempotencyKey,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<ReservationReceipt> ReserveAsync(
        Guid stockItemId,
        decimal quantity,
        string? externalReference,
        string? idempotencyKey,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task ReleaseAsync(Guid reservationId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<ReservationReceipt?> FindReservationAsync(Guid reservationId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<int> ReleaseExpiredHoldsAsync(DateTimeOffset utcNow, int batchSize, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task ConsumeAsync(Guid reservationId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<ReservationReceipt> CommitReservationForPaidOrderAsync(
        Guid reservationId,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<ReservationReceipt> PromoteReservationForManualPaymentReviewAsync(
        Guid reservationId,
        DateTimeOffset reviewExpiresAt,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<EnsureOrderSupplyResult> EnsureOrderSupplyAsync(
        EnsureOrderSupplyRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new EnsureOrderSupplyResult(
            OrderSupplyOutcome.NotApplicable,
            OrderSupplyStatusKind.NotApplicable,
            [],
            new Dictionary<Guid, Guid>()));

    public Task<OrderSupplyStatus> GetOrderSupplyStatusAsync(
        Guid checkoutId,
        IReadOnlyList<OrderSupplyLineInput> lines,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
