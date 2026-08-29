using Tooba.Catalog.Application;
using Tooba.OperatorProfile.Application;

namespace Tooba.Host.Admin;

/// <summary>
/// اتصال بازیگر HTTP به زمینهٔ تاریخچهٔ Catalog در همان درخواست.
/// </summary>
internal static class CatalogActorHttpBinding
{
    /// <summary>
    /// فیلتر گروهی: ActorUserId و نام نمایشی را از نشست/پروفایل می‌نشاند.
    /// </summary>
    public static async ValueTask<object?> BindAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var session = context.HttpContext.RequestServices.GetRequiredService<CurrentAuthenticatedSession>();
        var actor = context.HttpContext.RequestServices.GetRequiredService<ICatalogActorContext>();
        var profiles = context.HttpContext.RequestServices.GetRequiredService<IOperatorProfileDirectory>();
        if (session.UserId is Guid userId)
        {
            actor.ActorUserId = userId;
            var profile = await profiles.GetAsync(userId, context.HttpContext.RequestAborted);
            actor.ActorDisplayName = string.IsNullOrWhiteSpace(profile?.DisplayName)
                ? "اپراتور"
                : profile.DisplayName.Trim();
        }

        return await next(context);
    }
}
