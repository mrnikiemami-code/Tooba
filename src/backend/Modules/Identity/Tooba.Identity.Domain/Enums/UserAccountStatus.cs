using Tooba.BuildingBlocks;
using Tooba.Identity.Contracts;

namespace Tooba.Identity.Domain.Enums;

/// <summary>
/// وضعیت چرخهٔ عمر حساب احراز هویت. مشتری، فروشنده یا Tenant نیست؛ فقط اجازهٔ ورود را محدود می‌کند.
/// </summary>
public enum UserAccountStatus
{
    /// <summary>
    /// حساب فعال است و در صورت اعتبار درست می‌تواند احراز شود.
    /// </summary>
    Active = 0,

    /// <summary>
    /// حساب به‌صورت اداری تعلیق شده و نباید احراز شود.
    /// </summary>
    Disabled = 1,

    /// <summary>
    /// حساب قفل شده (مثلاً پس از شکست‌های امنیتی) و نباید احراز شود.
    /// </summary>
    Locked = 2,
}
