using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks.Security;
using Tooba.Order.Contracts.Fulfillment;

namespace Tooba.Wishlist.Endpoints.Customer;

/// <summary>درز خنثی تخصیص هویت Actor برای مرز HTTP Wishlist.</summary>
public interface IWishlistCustomerActorResolver
{
    /// <summary>Actor را حل می‌کند؛ null یعنی هویت قابل اعتماد نیست (بعداً 401).</summary>
    Guid? ResolveActor(HttpContext httpContext);
}

/// <summary>
/// پیاده‌سازی درز Actor فقط بر پایهٔ <see cref="ICurrentAuthenticatedUser"/> و محیط اجرا.
/// مقدار مهمان از <see cref="StorefrontGuestActor.ActorId"/> می‌آید.
/// </summary>
public sealed class WishlistCustomerActorResolver(
    ICurrentAuthenticatedUser currentUser,
    IHostEnvironment environment) : IWishlistCustomerActorResolver
{
    /// <summary>نام دقیق هدر Actor توسعه؛ تنها هدر مجاز Dev/Testing.</summary>
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
