using Tooba.BuildingBlocks.Grid;

namespace Tooba.Payment.Endpoints.Admin;

/// <summary>
/// Payment-owned normalization and field whitelist for the admin payments list grid.
/// Payment owns the exact field policy; no Host grid engine or Host row model participates.
/// </summary>
public interface IPaymentAdminGridQueryNormalizer
{
    /// <summary>درخواست گرید ادمین پرداخت را validate و normalize می‌کند.</summary>
    /// <param name="request">درخواست خام گرید.</param>
    /// <returns>درخواست نرمال‌شده با whitelist فیلد Payment.</returns>
    /// <exception cref="GridQueryValidationException">فیلد/عملگر خارج از whitelist Payment.</exception>
    GridQueryRequest Normalize(GridQueryRequest request);
}
