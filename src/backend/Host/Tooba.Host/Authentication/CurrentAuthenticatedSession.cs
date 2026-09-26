using Tooba.Identity.Application;

namespace Tooba.Host;

/// <summary>
/// اصل احراز همین درخواست HTTP. مجوز کسب‌وکار اینجا حل نمی‌شود و Tenant از هدر/کوئری/بدنه جعل نمی‌شود.
/// </summary>
internal sealed class CurrentAuthenticatedSession
{
    /// <summary>
    /// User پایدار پس از اعتبارسنجی Bearer نشست.
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// شناسهٔ نشست جاری؛ راز Refresh نیست.
    /// </summary>
    public Guid? SessionId { get; private set; }

    /// <summary>
    /// Edition ذخیره‌شده روی نشست، نه از Host درخواست.
    /// </summary>
    public string? Edition { get; private set; }

    /// <summary>
    /// Tenant پایدار نشست در Single-Store؛ از X-Tenant-Id خوانده نمی‌شود.
    /// </summary>
    public string? TenantId { get; private set; }

    /// <summary>
    /// آیا Bearer به نشست زنده و حساب فعال Resolve شده است.
    /// </summary>
    public bool IsAuthenticated => UserId is not null && SessionId is not null;

    /// <summary>
    /// اصل را پس از Resolve نشست می‌نشاند تا لایه‌های بعدی Host از EF Identity نخوانند.
    /// </summary>
    public void Assign(AuthenticatedIdentity identity)
    {
        UserId = identity.UserId;
        SessionId = identity.SessionId;
        Edition = identity.Edition;
        TenantId = identity.TenantId;
    }
}
