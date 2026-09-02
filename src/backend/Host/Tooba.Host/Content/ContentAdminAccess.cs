using Tooba.BuildingBlocks;
using Tooba.Host.Admin;

namespace Tooba.Host.Content;

/// <summary>
/// مجوز ریزدانهٔ Admin برای Content.
/// کاتالوگ فقط چهار کد دارد (بدون content.article.* و مشابه)؛
/// عملیات article / category / author / media روی همین کدها نگاشت می‌شوند — هم‌تراز FE T009.
/// </summary>
internal static class ContentAdminAccess
{
    /// <summary>لیست/جزئیات/query/picker/tree/workspace/media GET.</summary>
    public const string View = "content.view";

    /// <summary>ایجاد article / category / author.</summary>
    public const string Create = "content.create";

    /// <summary>
    /// به‌روزرسانی/آرشیو/حذف article؛ جهش‌های category (update/seo/media/move/reorder/archive)؛
    /// به‌روزرسانی/غیرفعال‌سازی author؛ جهش‌های media مقاله.
    /// </summary>
    public const string Edit = "content.edit";

    /// <summary>publish / unpublish مقاله.</summary>
    public const string Publish = "content.publish";

    /// <summary>
    /// ابتدا <see cref="AdminPanelAccess.RequireAuthorizedAsync"/> سپس capability روی permissionId.
    /// Unavailable / indeterminate = fail-closed (۵۰۳)؛ Deny = ۴۰۳. هرگز پس از tenant#view اجازهٔ ضمنی نمی‌دهد.
    /// </summary>
    public static async Task<Guid> RequireAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        IAuthorizationService authz,
        string permissionId,
        CancellationToken cancellationToken)
    {
        var actorUserId = await AdminPanelAccess.RequireAuthorizedAsync(
            request, session, tenant, guard, environment, cancellationToken);

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
            return actorUserId;

        if (decision.Kind == AuthorizationDecisionKind.Unavailable)
        {
            throw new PlatformHttpException(
                503,
                "سرویس مجوز در دسترس نیست.",
                "admin.authorization.unavailable");
        }

        throw new PlatformHttpException(403, "دسترسی محتوا مجاز نیست.", "admin.authorization.denied");
    }
}
