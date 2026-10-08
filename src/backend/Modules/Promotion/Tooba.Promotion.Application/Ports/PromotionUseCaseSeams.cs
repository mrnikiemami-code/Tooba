namespace Tooba.Promotion.Application.Ports;

/// <summary>
/// درز نگهبان نوشتن پروموشن. ماتریس نهایی ادمین اینجا نیست.
/// </summary>
public interface IPromotionUseCaseGuard
{
    /// <summary>
    /// اجازهٔ تعریف/فعال‌سازی را بررسی می‌کند.
    /// </summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}

/// <summary>
/// درز آیندهٔ سقف مصرف. الان ارزیابی‌only است و سهمیه را قفل نمی‌کند.
/// </summary>
public interface IPromotionRedemptionLedger
{
    /// <summary>
    /// برای foundation فقط موفق بودن مسیر را اعلام می‌کند؛ شمارش همزمان به Task بعدی موکول است.
    /// </summary>
    Task<bool> CanRedeemAsync(Guid promotionId, Guid? customerPartyId, CancellationToken cancellationToken);
}
