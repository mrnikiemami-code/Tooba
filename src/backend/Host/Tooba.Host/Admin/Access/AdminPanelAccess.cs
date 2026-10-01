using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.Host.Admin.Access;

/// <summary>
/// مرز مجوز پنل مدیر که Actor نشست را به Tenant حل‌شدهٔ سرور متصل می‌کند.
/// هیچ هدر مسیر یا شناسهٔ فروشنده‌ای مرجع اختیار مدیر نیست.
/// </summary>
internal static class AdminPanelAccess
{
    /// <summary>
    /// هدر محدود محیط Development برای Actor؛ در محیط‌های دیگر نادیده گرفته می‌شود.
    /// </summary>
    public const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <summary>
    /// Actor احرازشده را برای مجوز <c>tenant#view</c> بررسی می‌کند و در نبود هویت یا Tenant بسته می‌ماند.
    /// </summary>
    public static async Task<Guid> RequireAuthorizedAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant currentTenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var actorUserId = ResolveActorUserId(request, session, environment);
        var tenant = currentTenant.Current
            ?? throw new SemanticException(new SemanticError(FoundationErrorCodes.AdminTenantMissing));

        var decision = await guard.AuthorizeUseCaseAsync(
            new AuthorizationCheck
            {
                Subject = AuthorizationSubject.ForUser(actorUserId),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Tenant,
                    Id = tenant.TenantId.Value,
                },
                Permission = AuthorizationRelations.View,
                CallContext = new AuthorizationCallContext
                {
                    Edition = ToobaEdition.SingleStore,
                    TenantId = tenant.TenantId.Value,
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

    /// <summary>
    /// Actor را با اولویت نشست Bearer و فقط در Development از هدر جداگانه می‌خواند.
    /// </summary>
    internal static Guid ResolveActorUserId(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment)
    {
        if (session.IsAuthenticated && session.UserId is { } authenticated)
        {
            return authenticated;
        }

        if (environment.IsDevelopment()
            && request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var actor)
            && actor != Guid.Empty)
        {
            return actor;
        }

        throw new SemanticException(new SemanticError(FoundationErrorCodes.AdminActorMissing));
    }
}
