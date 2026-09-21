using Tooba.Promotion.Application.Ports;
using Tooba.Promotion.Infrastructure.Queries;
using Tooba.Promotion.Infrastructure.Messaging;
using Tooba.Promotion.Infrastructure.Adapters;
using Tooba.Promotion.Infrastructure.Directories;
using Tooba.Inventory.Infrastructure.Messaging;
using Tooba.Inventory.Infrastructure.Adapters;
using Tooba.Inventory.Infrastructure.Directories;
using Tooba.Inventory.Application.Ports;
using Tooba.Inventory.Application.Checkout;
using Tooba.Inventory.Application.Orders;
using Tooba.Inventory.Application.Returns;
using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Inventory.Domain.Events;

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
