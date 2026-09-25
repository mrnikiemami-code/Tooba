using Microsoft.AspNetCore.Http;

namespace Tooba.BuildingBlocks.Security;

/// <summary>
/// درز خنثی/عمومی دسترسی پنل مدیر. پیاده‌سازی Host است و هیچ سیاست ماژولی ندارد.
/// </summary>
public interface IAdminPanelAccess
{
    /// <summary>
    /// Actor مجاز پنل مدیر را برمی‌گرداند؛ در نبود Tenant/Edition نامعتبر fail-closed است.
    /// </summary>
    Task<Guid> RequireAuthorizedAsync(HttpRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// درز خنثی/عمومی دسترسی پنل فروشنده. مرجع مجوز فقط موتور مجوز است، نه هدر Party.
/// </summary>
public interface ISellerPanelAccess
{
    /// <summary>
    /// Actor و SellerPartyId مجاز را برمی‌گرداند؛ DENY/Unavailable/Ceiling fail-closed هستند.
    /// </summary>
    Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// گونهٔ مالک scope دسترسی مؤثر، مستقل از ماژول.
/// </summary>
public enum PlatformAccessOwnerKind
{
    /// <summary>محدودهٔ پلتفرم/مدیر.</summary>
    Platform = 1,

    /// <summary>محدودهٔ یک فروشنده.</summary>
    Seller = 2,
}

/// <summary>
/// گونهٔ scope یک مجوز مؤثر، مستقل از ماژول.
/// </summary>
public enum PlatformAccessScopeKind
{
    /// <summary>کل محدودهٔ مالک.</summary>
    GlobalWithinOwner = 1,

    /// <summary>محدود به دسته.</summary>
    Category = 2,

    /// <summary>محدود به محصول.</summary>
    Product = 3,

    /// <summary>محدود به برند.</summary>
    Brand = 4,

    /// <summary>محدود به انبار.</summary>
    Warehouse = 5,
}

/// <summary>
/// یک مجوز مؤثر بدون نشت نوع ماژول AccessControl.
/// </summary>
/// <param name="PermissionId">شناسهٔ پایدار مجوز.</param>
/// <param name="ScopeKind">گونهٔ scope.</param>
/// <param name="ScopeResourceId">شناسهٔ منبع scope در صورت محدود بودن.</param>
/// <param name="DeniedByCeiling">ردشده توسط سقف؛ مصرف‌کننده باید حذف کند.</param>
public sealed record PlatformPermissionGrant(
    string PermissionId,
    PlatformAccessScopeKind ScopeKind,
    Guid? ScopeResourceId,
    bool DeniedByCeiling);

/// <summary>
/// درز فقط‌خواندنی/خنثی مجوز مؤثر. سیاست کسب‌وکار هر ماژول نزد خود ماژول می‌ماند.
/// </summary>
public interface IPlatformEffectiveAccessReader
{
    /// <summary>
    /// مجوزهای مؤثر یک Actor در محدودهٔ مالک را برمی‌گرداند.
    /// </summary>
    Task<IReadOnlyList<PlatformPermissionGrant>> GetEffectivePermissionsAsync(
        Guid userId,
        PlatformAccessOwnerKind ownerKind,
        Guid? ownerScopeId,
        CancellationToken cancellationToken);
}
