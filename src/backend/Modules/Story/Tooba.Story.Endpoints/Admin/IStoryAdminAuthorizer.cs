using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Security;
using Tooba.Story.Endpoints.Permissions;

namespace Tooba.Story.Endpoints.Admin;

/// <summary>درز مجوز Admin برای Story: گذر پنل + capability همان اقدام.</summary>
public interface IStoryAdminAuthorizer
{
    /// <summary>
    /// Actor مدیر مجاز را الزامی می‌کند و capability درخواستی را بررسی می‌کند.
    /// </summary>
    /// <param name="httpContext">زمینهٔ HTTP درخواست جاری.</param>
    /// <param name="permissionId">شناسهٔ capability مالک از کاتالوگ Access Control.</param>
    /// <param name="cancellationToken">توکن لغو درخواست.</param>
    /// <returns>شناسهٔ کاربر actor.</returns>
    Task<Guid> RequireAuthorizedAsync(
        HttpContext httpContext,
        string permissionId,
        CancellationToken cancellationToken);
}

/// <summary>
/// پیاده‌سازی روی IAdminPanelAccess و IAuthorizationService؛ بدون وابستگی به Host.
/// DENY و Unavailable هر دو fail-closed هستند تا مخفی‌کردن منو جای بررسی سرور را نگیرد.
/// </summary>
public sealed class StoryAdminAuthorizer(
    IAdminPanelAccess adminAccess,
    IAuthorizationService authorization,
    ICurrentTenant tenant) : IStoryAdminAuthorizer
{
    /// <inheritdoc />
    public async Task<Guid> RequireAuthorizedAsync(
        HttpContext httpContext,
        string permissionId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionId);

        var actorUserId = await adminAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);

        var decision = await authorization.CanAsync(
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
            return actorUserId;
        }

        if (decision.Kind == AuthorizationDecisionKind.Unavailable)
        {
            throw new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationUnavailable));
        }

        throw new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationDenied));
    }
}
