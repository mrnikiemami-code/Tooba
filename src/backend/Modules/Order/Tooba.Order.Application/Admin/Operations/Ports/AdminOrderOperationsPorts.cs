using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Domain;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;

namespace Tooba.Order.Application.Admin.Operations.Ports;

/// <summary>Checkout snapshot needed by admin order operations (Order.Infrastructure).</summary>
public sealed record AdminOrderOpsLineSnapshot(Guid LineId, decimal Quantity);

/// <summary>Seller order rows for admin operations load.</summary>
public sealed record AdminOrderOpsSellerOrderSnapshot(
    Guid SellerOrderId,
    Guid SellerPartyId,
    SellerOrderStatus Status,
    SellerOrderStatus? CancelledFromStatus,
    IReadOnlyList<AdminOrderOpsLineSnapshot> Lines);

/// <summary>Checkout group projection for admin operations (no Host DbContext).</summary>
public sealed record AdminOrderOpsCheckoutSnapshot(
    Guid CheckoutId,
    Guid PlacedByUserId,
    IReadOnlyList<AdminOrderOpsSellerOrderSnapshot> SellerOrders);

/// <summary>Loads checkout + seller orders for admin operations.</summary>
public interface IAdminOrderOperationsCheckoutReader
{
    /// <summary>Returns null when the checkout is missing.</summary>
    Task<AdminOrderOpsCheckoutSnapshot?> GetAsync(Guid checkoutId, CancellationToken cancellationToken);
}

/// <summary>One effective permission grant for admin ops authorization.</summary>
public sealed record OrderAdminPermissionGrant(string PermissionId, bool DeniedByCeiling);

/// <summary>Effective access for the actor performing admin order operations.</summary>
public sealed record OrderAdminEffectiveAccess(IReadOnlyList<OrderAdminPermissionGrant> Permissions);

/// <summary>Host/thin adapter over AccessControl — no AccessControl Application in Order handlers.</summary>
public interface IOrderAdminEffectiveAccessReader
{
    /// <summary>Loads platform-scoped effective permissions for the actor.</summary>
    Task<OrderAdminEffectiveAccess> GetAsync(Guid actorUserId, CancellationToken cancellationToken);
}

/// <summary>Inventory recovery assessment visible to admin operations.</summary>
public sealed record AdminOrderInventoryRecoveryAssessment(
    Guid CheckoutId,
    string ClassCode,
    bool NeedsRecovery,
    string ReasonFa);

/// <summary>Inventory recovery mutation outcome.</summary>
public sealed record AdminOrderInventoryRecoveryResult(
    string Outcome,
    string ClassCode,
    bool NeedsRecovery,
    string MessageFa,
    string OrderNumbers);

/// <summary>
/// Narrow port over Order inventory-recovery service — assess/recover only.
/// </summary>
public interface IAdminOrderOperationsInventoryRecoveryPort
{
    /// <summary>Assesses whether the checkout needs reservation recovery.</summary>
    Task<AdminOrderInventoryRecoveryAssessment> AssessAsync(Guid checkoutId, CancellationToken cancellationToken);

    /// <summary>Runs reservation recovery for the checkout.</summary>
    Task<AdminOrderInventoryRecoveryResult> RecoverAsync(
        Guid checkoutId,
        Guid actorUserId,
        string? reason,
        CancellationToken cancellationToken);
}

/// <summary>Supply line shortage for admin operations.</summary>
public sealed record AdminOrderOpsSupplyLine(
    string? ItemTitle,
    string? UnitCode,
    decimal Required,
    decimal Available,
    decimal Shortage,
    string LineStatus);

/// <summary>Supply status snapshot for admin operations.</summary>
public sealed record AdminOrderOpsSupplyStatus(
    Guid CheckoutId,
    string Status,
    string MessageFa,
    IReadOnlyList<AdminOrderOpsSupplyLine> Lines);

/// <summary>Ensure-supply outcome for confirm-deposit.</summary>
public sealed record AdminOrderOpsSupplyEnsureResult(
    string Outcome,
    string Status,
    bool Unavailable);

/// <summary>
/// Narrow port over Order supply service — status/ensure only.
/// </summary>
public interface IAdminOrderOperationsSupplyPort
{
    /// <summary>Current supply status of a checkout.</summary>
    Task<AdminOrderOpsSupplyStatus> GetStatusAsync(Guid checkoutId, CancellationToken cancellationToken);

    /// <summary>Ensures paid-durable supply before deposit confirmation.</summary>
    Task<AdminOrderOpsSupplyEnsureResult> EnsurePaidDurableAsync(
        Guid checkoutId,
        string reason,
        CancellationToken cancellationToken);
}
