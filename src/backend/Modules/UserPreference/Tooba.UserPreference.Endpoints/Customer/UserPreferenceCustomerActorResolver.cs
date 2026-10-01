using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks.Security;
using Tooba.Order.Contracts.Fulfillment;

namespace Tooba.UserPreference.Endpoints.Customer;

/// <summary>درز تخصیص Actor مشتری برای مرز HTTP UserPreference.</summary>
public interface IUserPreferenceCustomerActorResolver
{
    /// <summary>Actor را حل می‌کند؛ null یعنی هویت قابل اعتماد نیست.</summary>
    Guid? ResolveActor(HttpContext httpContext);
}

/// <summary>پیاده‌سازی Actor بر پایهٔ <see cref="ICurrentAuthenticatedUser"/> و Guest Contracts.</summary>
public sealed class UserPreferenceCustomerActorResolver(
    ICurrentAuthenticatedUser currentUser,
    IHostEnvironment environment) : IUserPreferenceCustomerActorResolver
{
    /// <summary>هدر Actor توسعه؛ فقط Dev/Testing.</summary>
    public const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <inheritdoc />
    public Guid? ResolveActor(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (currentUser.IsAuthenticated && currentUser.UserId is { } authenticated)
        {
            return authenticated;
        }

        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            return null;
        }

        if (httpContext.Request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var devActor)
            && devActor != Guid.Empty)
        {
            return devActor;
        }

        return StorefrontGuestActor.ActorId;
    }
}
