using Tooba.Returns.Application.Models;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.Ports;


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

    /// <summary>درخواست را تأیید و refund را آغاز می‌کند.</summary>
    Task<ReturnSnapshot> ApproveAsync(ApproveReturnCommand command, CancellationToken cancellationToken);

    /// <summary>درخواست را رد می‌کند.</summary>
    Task<ReturnSnapshot> RejectAsync(RejectReturnCommand command, CancellationToken cancellationToken);

    /// <summary>refund شکست‌خورده را دوباره تلاش می‌کند (admin).</summary>
    Task<ReturnSnapshot> RetryRefundAsync(RetryRefundCommand command, CancellationToken cancellationToken);
}
