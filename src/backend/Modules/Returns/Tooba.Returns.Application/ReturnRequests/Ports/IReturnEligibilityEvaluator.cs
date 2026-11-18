using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.ReturnRequests.Ports;


/// <summary>
/// ارزیابی‌کنندهٔ authoritative eligibility مرجوعی.
/// </summary>
public interface IReturnEligibilityEvaluator
{
    /// <summary>
    /// eligibility را برای یک سفارش فروشنده ارزیابی می‌کند.
    /// <para>
    /// TB-279: <paramref name="excludeReturnRequestId"/> answers the availability WITHOUT the occupancy of that one
    /// case. The correction path uses it so a case that re-acquires its own released/legacy capacity is never blocked by
    /// its own recorded quantity while every OTHER case still competes for the same slices.
    /// </para>
    /// </summary>
    Task<ReturnEligibilityResult> EvaluateAsync(
        Guid sellerOrderId,
        CancellationToken cancellationToken,
        Guid? excludeReturnRequestId = null);
}
