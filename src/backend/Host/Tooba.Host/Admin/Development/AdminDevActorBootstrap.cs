using Tooba.BuildingBlocks;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Problems;

namespace Tooba.Host.Admin.Development;

/// <summary>
/// Actor مستقل مدیر را فقط برای Development ایجاد می‌کند و تنها برای Tenant جاری tuple عضویت می‌نویسد.
/// Actorهای فروشنده در این bootstrap هیچ مجوز مدیریتی دریافت نمی‌کنند.
/// </summary>
internal static class AdminDevActorBootstrap
{
    /// <summary>
    /// شناسهٔ ورود نمونهٔ مدیر که با کاربران فروشنده مشترک نیست.
    /// </summary>
    internal const string AdminEmail = "admin-actor@tooba.local";

    private static readonly object Gate = new();
    private static AdminDevActorSnapshot? _snapshot;

    /// <summary>
    /// نگاشت آمادهٔ Development برای اتصال UI، بدون افشای رمز عبور.
    /// </summary>
    public static AdminDevActorSnapshot? Snapshot
    {
        get
        {
            lock (Gate)
            {
                return _snapshot;
            }
        }
    }

    /// <summary>
    /// کاربر مدیر و رابطهٔ user→tenant#member را از قراردادهای Identity/Authorization آماده می‌کند.
    /// </summary>
    public static async Task EnsureAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var tenant = provider.GetRequiredService<ICurrentTenant>().Current;
        if (tenant is null)
        {
            return;
        }

        var authentication = provider.GetRequiredService<IIdentityAuthenticationService>();
        var actor = await authentication.FindUserIdByIdentifierAsync(
            LoginIdentifierKind.Email, AdminEmail, cancellationToken);
        if (actor is null)
        {
            try
            {
                actor = (await authentication.RegisterAsync(
                    new RegisterUserCommand
                    {
                        IdentifierKind = LoginIdentifierKind.Email,
                        Identifier = AdminEmail,
                        Password = "admin-dev-horse-1",
                    },
                    cancellationToken)).UserId;
            }
            catch (IdentityDuplicateIdentifierFault)
            {
                actor = await authentication.FindUserIdByIdentifierAsync(
                    LoginIdentifierKind.Email, AdminEmail, cancellationToken);
            }
        }

        if (actor is null)
        {
            return;
        }

        // قرارداد IAuthorizationTupleWriter رابطه را upsert (Touch) می‌کند و idempotent است؛
        // بنابراین تکرار عضویت خطا نیست و هیچ catch عمومی برای «جریان موردانتظار» لازم نیست.
        await provider.GetRequiredService<IAuthorizationTupleWriter>().WriteAsync(
            new AuthorizationRelationshipWrite
            {
                Subject = AuthorizationSubject.ForUser(actor.Value),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Tenant,
                    Id = tenant.TenantId.Value,
                },
                Relation = AuthorizationRelations.Member,
            },
            cancellationToken);

        lock (Gate)
        {
            _snapshot = new AdminDevActorSnapshot(actor.Value, AdminEmail, tenant.TenantId.Value);
        }
    }
}

/// <summary>
/// زمینهٔ Actor مدیر Development برای مصرف امن رابط محلی.
/// </summary>
internal sealed record AdminDevActorSnapshot(Guid ActorUserId, string ActorLabel, string TenantId);
