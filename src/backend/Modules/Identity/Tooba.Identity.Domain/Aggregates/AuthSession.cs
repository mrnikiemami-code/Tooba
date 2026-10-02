using Tooba.BuildingBlocks;

namespace Tooba.Identity.Domain.Aggregates;

/// <summary>
/// نشست احراز هویت. User نیست، بلیت مجوز محصول نیست، و راز Refresh را plaintext نگه نمی‌دارد.
/// </summary>
public sealed class AuthSession
{
    /// <summary>
    /// شناسهٔ پایدار نشست؛ همان SessionHandle بلیت است.
    /// </summary>
    public Guid SessionId { get; init; }

    /// <summary>
    /// اصل ورود مالک نشست. FK به Party نیست.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// زمان ایجاد نشست.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// پایان اعتبار Refresh؛ پس از آن چرخش مجاز نیست.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// آخرین استفادهٔ موفق (صدور یا چرخش).
    /// </summary>
    public DateTimeOffset LastUsedAt { get; set; }

    /// <summary>
    /// زمان لغو؛ تهی یعنی هنوز قابل Refresh است اگر مهر و انقضا درست باشند.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// علت لغو برای audit؛ راز نیست.
    /// </summary>
    public string? RevocationReason { get; set; }

    /// <summary>
    /// نسخهٔ مهر امنیتی در زمان صدور. اگر از User.SecurityStamp عقب بماند Refresh شکست می‌خورد.
    /// </summary>
    public int CredentialVersion { get; init; }

    /// <summary>
    /// Edition فرآیند در زمان صدور؛ از Host پارس نمی‌شود.
    /// </summary>
    public ToobaEdition Edition { get; init; }

    /// <summary>
    /// Tenant پایدار فقط در Single-Store؛ در Marketplace تهی است.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// برچسب اختیاری دستگاه/کلاینت؛ User-Agent خام نیست.
    /// </summary>
    public string? ClientLabel { get; init; }

    /// <summary>
    /// هش SHA-256 راز Refresh جاری. plaintext هرگز persist نمی‌شود.
    /// </summary>
    public string RefreshSecretHash { get; set; } = "";

    /// <summary>
    /// هش راز قبلی پس از چرخش برای تشخیص reuse. اگر ارائه شود خانواده لغو می‌شود.
    /// </summary>
    public string? PreviousRefreshSecretHash { get; set; }

    /// <summary>
    /// خانوادهٔ چرخش برای تشخیص replay.
    /// </summary>
    public Guid RefreshFamilyId { get; init; }

    /// <summary>
    /// نشست هنوز برای چرخش زنده است.
    /// </summary>
    public bool IsRefreshable(DateTimeOffset now, int userSecurityStamp) =>
        RevokedAt is null
        && now < ExpiresAt
        && CredentialVersion == userSecurityStamp;
}
