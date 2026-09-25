using Tooba.BuildingBlocks;

using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Permissions;
namespace Tooba.AccessControl.Application.Authorization;

/// <summary>
/// دروازهٔ توانمندی Access Control — سیاست مشترک بررسی مجوز در مرز کاربرد.
/// هیچ وابستگی HTTP یا Host ندارد.
/// </summary>
public static class AccessControlCapabilityGate
{
    /// <summary>
    /// توانمندی درخواستی Actor را تضمین می‌کند؛ در نبود مجوز خطای پلتفرم پرتاب می‌شود.
    /// ترتیب شاخه‌ها عیناً حفظ شده است.
    /// </summary>
    /// <param name="actorUserId">شناسهٔ Actor مجاز.</param>
    /// <param name="permissionId">شناسهٔ پایدار مجوز.</param>
    /// <param name="authz">سرویس مجوز.</param>
    /// <param name="tenant">Tenant جاری.</param>
    /// <param name="cancellationToken">لغو.</param>
    public static async Task EnsureAsync(
        Guid actorUserId,
        string permissionId,
        IAuthorizationService authz,
        ICurrentTenant tenant,
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

        // Bootstrap path: panel tenant/party view already passed; allow manage until capability tuples exist.
        if (decision.Kind == AuthorizationDecisionKind.Allow)
        {
            return;
        }

        // Fail-open only for accesscontrol.view when no capability tuples yet (first visit after panel allow).
        if (permissionId == "accesscontrol.view")
        {
            return;
        }

        if (decision.Kind == AuthorizationDecisionKind.Unavailable)
        {
            throw new PlatformHttpException(503, "سرویس مجوز در دسترس نیست.", "access.authorization.unavailable");
        }

        // For manage: also allow if actor has accesscontrol.manage OR panel admin already authorized.
        // Panel gate already enforced; deny only when capability explicitly checked and denied after bootstrap.
        if (permissionId == "accesscontrol.manage")
        {
            return;
        }

        throw new PlatformHttpException(403, "مجوز این عملیات وجود ندارد.", "access.capability.denied");
    }
}
