using Tooba.Returns.Application.Models;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.Ports;


/// <summary>
/// ارزیابی‌کنندهٔ authoritative eligibility مرجوعی.
/// </summary>
public interface IReturnEligibilityEvaluator
{
    /// <summary>eligibility را برای یک سفارش فروشنده ارزیابی می‌کند.</summary>
    Task<ReturnEligibilityResult> EvaluateAsync(Guid sellerOrderId, CancellationToken cancellationToken);
}
