using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks.Security;
using Tooba.Order.Contracts.Fulfillment;

namespace Tooba.AddressBook.Endpoints.Customer;

/// <summary>
/// درز خنثی هویت Actor برای مرز HTTP دفترچهٔ آدرس. فقط از درز امنیتی عمومی پلتفرم
/// (<see cref="ICurrentAuthenticatedUser"/>) و محیط استفاده می‌کند؛ به Host وابسته نیست.
/// مقدار مهمان از قرارداد پایدار <see cref="StorefrontGuestActor.ActorId"/> می‌آید و نه از
/// Order.Application.
/// </summary>
public static class AddressBookCustomerActorResolver
{
    private const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <summary>
    /// Actor را با همان معنای فعلی Host حل می‌کند: نشست معتبر، سپس هدر توسعه، سپس مهمان فروشگاه
    /// فقط در Development/Testing؛ Production بدون نشست نتیجه‌اش null (یعنی 401) است.
    /// بدنهٔ درخواست هرگز هویت نمی‌سازد.
    /// </summary>
    public static Guid? ResolveActor(
        HttpContext httpContext,
        ICurrentAuthenticatedUser currentUser,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(currentUser);
        ArgumentNullException.ThrowIfNull(environment);

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
