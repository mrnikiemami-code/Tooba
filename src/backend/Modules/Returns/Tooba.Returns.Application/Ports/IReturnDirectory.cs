using Tooba.Returns.Application.Models;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.Ports;

/// <summary>ردیف سبک وضعیت مرجوعی برای overlay گرید سفارش.</summary>
public sealed record ReturnStatusOverlayRow(Guid SellerOrderId, ReturnRequestStatus Status);

/// <summary>
/// ارکستراسیون مرجوعی.
/// </summary>
public interface IReturnDirectory
{
    /// <summary>درخواست مرجوعی می‌سازد.</summary>
    Task<ReturnSnapshot> CreateAsync(CreateReturnCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// درخواست مرجوعی را از مسیر admin می‌سازد؛ مالکیت را چک نمی‌کند و RequestedByUserId = مشتری سفارش است.
    /// </summary>
    Task<ReturnSnapshot> CreateAdminInitiatedAsync(CreateReturnCommand command, CancellationToken cancellationToken);

    /// <summary>eligibility مرجوعی را از evaluator برمی‌گرداند.</summary>
    Task<ReturnEligibilityResult> EvaluateEligibilityAsync(Guid sellerOrderId, CancellationToken cancellationToken);

    /// <summary>درخواست را می‌خواند.</summary>
    Task<ReturnSnapshot?> GetAsync(Guid returnRequestId, CancellationToken cancellationToken);

    /// <summary>فهرست درخواست‌های یک مشتری.</summary>
    Task<IReadOnlyList<ReturnSnapshot>> ListForCustomerAsync(Guid customerUserId, CancellationToken cancellationToken);

    /// <summary>فهرست درخواست‌های یک فروشنده.</summary>
    Task<IReadOnlyList<ReturnSnapshot>> ListForSellerAsync(Guid sellerPartyId, CancellationToken cancellationToken);

    /// <summary>فهرست همه درخواست‌ها برای admin.</summary>
    Task<IReadOnlyList<ReturnSnapshot>> ListAllAsync(CancellationToken cancellationToken);

    /// <summary>درخواست‌های چند SellerOrder برای پنل Admin (بدون DbContext در Host).</summary>
    Task<IReadOnlyList<ReturnSnapshot>> ListBySellerOrderIdsAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);

    /// <summary>وضعیت‌های مرجوعی برای overlay گرید سفارش Admin.</summary>
    Task<IReadOnlyList<ReturnStatusOverlayRow>> ListStatusOverlayAsync(CancellationToken cancellationToken);

    /// <summary>درخواست را تأیید و refund را آغاز می‌کند.</summary>
    Task<ReturnSnapshot> ApproveAsync(ApproveReturnCommand command, CancellationToken cancellationToken);

    /// <summary>درخواست را رد می‌کند.</summary>
    Task<ReturnSnapshot> RejectAsync(RejectReturnCommand command, CancellationToken cancellationToken);

    /// <summary>refund شکست‌خورده را دوباره تلاش می‌کند (admin).</summary>
    Task<ReturnSnapshot> RetryRefundAsync(RetryRefundCommand command, CancellationToken cancellationToken);
}

/// <summary>Query port صف کار Admin returns (جدا از directory ارکستراسیون).</summary>
public interface IAdminReturnGridQuery
{
    /// <summary>صفحه‌بندی DB-native صف کار مرجوعی.</summary>
    Task<BuildingBlocks.Grid.GridPageResponse<AdminReturnWorkQueueRow>> QueryAsync(
        BuildingBlocks.Grid.GridQueryRequest request,
        CancellationToken cancellationToken);
}
