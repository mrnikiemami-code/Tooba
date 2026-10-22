using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Security;

namespace Tooba.Shipping.Endpoints.Admin;

/// <summary>
/// پیاده‌سازی پیش‌فرض مجوزدهی ادمین نرخ/پوشش روی همان درزهای مجوز پلتفرم که آداپتورهای Host
/// استفاده می‌کنند: <see cref="IAdminPanelAccess"/> برای گیت پنل و
/// <see cref="IAuthorizationService"/> برای بررسی قابلیت. دسترسی به سرویس مجوز هرگز ALLOW
/// نمی‌شود (fail-closed با کد <c>admin.authorization.unavailable</c>).
/// </summary>
/// <remarks>
/// چرا اینجا و نه یک فایل تازه در <c>Host/Admin/Access/Authorizers</c>: مجموعهٔ فایل‌های
/// <c>Host/Admin</c> و <c>Host/Security</c> تحت گاردهای AMSC قفل‌اند (۱۷ و ۱۹ فایل) و افزودن
/// آداپتور تازه آن گاردها را می‌شکند؛ تضعیف گارد معماری مجاز نیست. با نگه‌داشتن همان درز
/// انتزاعی <see cref="IShippingAdminAuthorizer"/> و پیاده‌سازی روی seamهای عمومی BuildingBlocks،
/// هیچ اجازهٔ تازه‌ای ساخته نمی‌شود و هیچ مسیری بی‌مجوز نمی‌ماند.
/// <para>
/// زمینهٔ بررسی مجوز (<c>Edition</c>/<c>TenantId</c>) از زمینهٔ تجارت resolve‌شدهٔ همان درخواست
/// گرفته می‌شود، نه از یک مقدار ثابت و نه از هدر/کوکی/query. مقدار نامعتبر یا ناسازگار پیش از
/// <see cref="IAuthorizationService.CanAsync"/> fail-closed می‌شود.
/// </para>
/// <para>
/// این تعمیر فقط «زمینهٔ فراخوانی» Shipping را درست می‌کند: هویت فروشنده/مالک، تفکیک دادهٔ
/// seller-scoped در Marketplace و پشتیبانی کامل حالت نصب را ایجاد نمی‌کند.
/// </para>
/// </remarks>
public sealed class ShippingAdminAuthorizer(
    IAdminPanelAccess adminAccess,
    IAuthorizationService authz,
    ICurrentEdition edition,
    ICurrentTenant tenant) : IShippingAdminAuthorizer
{
    /// <inheritdoc />
    public async Task<Guid> RequireAuthorizedAsync(
        HttpContext httpContext, string permissionId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionId);

        var actor = await adminAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
        var callContext = ShippingInstallationContext.Resolve(edition, tenant);
        await EnsureAdminCapabilityAsync(actor, permissionId, callContext, cancellationToken);
        return actor;
    }

    private async Task EnsureAdminCapabilityAsync(
        Guid actorUserId,
        string permissionId,
        AuthorizationCallContext callContext,
        CancellationToken cancellationToken)
    {
        var decision = await authz.CanAsync(
            new AuthorizationCheck
            {
                Subject = AuthorizationSubject.ForUser(actorUserId),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Permission,
                    Id = permissionId,
                },
                Permission = AuthorizationRelations.Check,
                CallContext = callContext,
            },
            cancellationToken);

        if (decision.Kind == AuthorizationDecisionKind.Allow)
        {
            return;
        }

        // شکست زیرساخت مجوز هرگز ALLOW نیست؛ قابلیت باید fail-closed بماند.
        if (decision.Kind == AuthorizationDecisionKind.Unavailable)
        {
            throw Unavailable();
        }

        throw new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationDenied));
    }

    private static SemanticException Unavailable() =>
        new(new SemanticError(FoundationErrorCodes.AdminAuthorizationUnavailable));
}
