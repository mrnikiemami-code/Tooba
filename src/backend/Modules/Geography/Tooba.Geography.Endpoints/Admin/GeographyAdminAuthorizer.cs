using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Security;

namespace Tooba.Geography.Endpoints.Admin;

/// <summary>
/// پیاده‌سازی پیش‌فرض مجوزدهی ادمین خواندن Geography روی همان درزهای مجوز پلتفرم که آداپتورهای Host
/// استفاده می‌کنند. نبود سرویس مجوز هرگز ALLOW نیست (fail-closed با
/// <c>admin.authorization.unavailable</c>).
/// </summary>
/// <remarks>
/// چرا اینجا و نه یک فایل تازه در <c>Host/Admin/Access/Authorizers</c>: مجموعهٔ فایل‌های
/// <c>Host/Admin</c> و <c>Host/Security</c> تحت گاردهای AMSC قفل‌اند (۱۷ و ۱۹ فایل)؛ افزودن آداپتور
/// تازه آن گاردها را می‌شکند و تضعیف گارد مجاز نیست. با نگه‌داشتن همان درز انتزاعی و پیاده‌سازی روی
/// seamهای عمومی BuildingBlocks، هیچ اجازهٔ تازه‌ای ساخته نمی‌شود.
/// </remarks>
public sealed class GeographyAdminAuthorizer(
    IAdminPanelAccess adminAccess,
    IAuthorizationService authz,
    ICurrentTenant tenant) : IGeographyAdminAuthorizer
{
    /// <inheritdoc />
    public async Task<Guid> RequireAuthorizedAsync(
        HttpContext httpContext, string permissionId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionId);

        var actor = await adminAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
        await EnsureAdminCapabilityAsync(actor, permissionId, cancellationToken);
        return actor;
    }

    private async Task EnsureAdminCapabilityAsync(
        Guid actorUserId,
        string permissionId,
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
                CallContext = new AuthorizationCallContext
                {
                    Edition = ToobaEdition.SingleStore,
                    TenantId = tenant.Current?.TenantId.Value ?? "unknown",
                },
            },
            cancellationToken);

        if (decision.Kind == AuthorizationDecisionKind.Allow)
        {
            return;
        }

        if (decision.Kind == AuthorizationDecisionKind.Unavailable)
        {
            throw new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationUnavailable));
        }

        throw new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationDenied));
    }
}
