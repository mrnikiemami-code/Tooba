using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Security;

namespace Tooba.Shipping.Endpoints.Admin;

/// <summary>
/// استخراج مکانیکی و بدون تغییر رفتارِ اعتبارسنجی زمینهٔ نصب که پیش‌تر خصوصی داخل
/// <see cref="ShippingAdminAuthorizer"/> بود: فقط Edition و Tenant باواسطهٔ پلتفرم، با همان
/// قواعد fail-closed و همان کد خطای canonical.
/// </summary>
/// <remarks>
/// این helper یک مرجع حالت <b>نمی‌سازد</b>: مقدار از <see cref="ICurrentEdition"/> و
/// <see cref="ICurrentTenant"/> همین درخواست خوانده می‌شود و هرگز از هدر/کوکی/query/پیکربندی
/// بازسازی نمی‌شود. SingleStore فقط با Tenant فعال و شناسهٔ پایدار معتبر است؛ Marketplace فقط
/// بدون Tenant. هر حالت دیگر <c>admin.authorization.unavailable</c> است.
/// </remarks>
internal static class ShippingInstallationContext
{
    /// <summary>زمینهٔ فراخوانی مجوز را از زمینهٔ trusted همین درخواست می‌سازد.</summary>
    /// <param name="edition">حالت نصب resolve‌شده.</param>
    /// <param name="tenant">Tenant resolve‌شده (در Marketplace باید تهی باشد).</param>
    /// <exception cref="SemanticException">زمینهٔ ناقص/ناسازگار؛ همیشه fail-closed.</exception>
    public static AuthorizationCallContext Resolve(ICurrentEdition edition, ICurrentTenant tenant)
    {
        ArgumentNullException.ThrowIfNull(edition);
        ArgumentNullException.ThrowIfNull(tenant);

        return edition.Current?.Edition switch
        {
            ToobaEdition.SingleStore => SingleStore(tenant),
            ToobaEdition.Marketplace => Marketplace(tenant),
            _ => throw Unavailable(),
        };
    }

    private static AuthorizationCallContext SingleStore(ICurrentTenant tenant)
    {
        var current = tenant.Current;
        var tenantId = current?.TenantId.Value;

        if (current is null
            || current.Status != TenantStatus.Active
            || string.IsNullOrWhiteSpace(tenantId))
        {
            throw Unavailable();
        }

        return new AuthorizationCallContext
        {
            Edition = ToobaEdition.SingleStore,
            TenantId = tenantId.Trim(),
        };
    }

    private static AuthorizationCallContext Marketplace(ICurrentTenant tenant)
    {
        // در Marketplace هیچ Tenant مجاز نیست؛ Tenant باقی‌مانده یا ناسازگار، زمینهٔ نامعتبر است.
        if (tenant.Current is not null)
        {
            throw Unavailable();
        }

        return new AuthorizationCallContext
        {
            Edition = ToobaEdition.Marketplace,
            TenantId = null,
        };
    }

    private static SemanticException Unavailable() =>
        new(new SemanticError(FoundationErrorCodes.AdminAuthorizationUnavailable));
}
