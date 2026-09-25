using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks.Security;
using Tooba.Order.Contracts.Fulfillment;

namespace Tooba.AddressBook.Endpoints.Customer;

/// <summary>
/// درز خنثی تخصیص هویت Actor برای مرز HTTP دفترچهٔ آدرس. مرزهای ماژول از این واسط استفاده می‌کنند
/// تا نه به نشست Host و نه به لایه‌های Application ماژول‌های دیگر وابسته شوند.
/// </summary>
public interface IAddressBookCustomerActorResolver
{
    /// <summary>Actor را حل می‌کند؛ null یعنی هویت قابل اعتماد نیست (بعداً 401).</summary>
    Guid? ResolveActor(HttpContext httpContext);
}

/// <summary>
/// پیاده‌سازی درز Actor فقط بر پایهٔ درز امنیتی عمومی پلتفرم
/// (<see cref="ICurrentAuthenticatedUser"/>) و محیط اجرا. مقدار مهمان از قرارداد پایدار
/// <see cref="StorefrontGuestActor.ActorId"/> می‌آید و نه از Order.Application؛ هیچ ارجاعی به
/// انواع Host وجود ندارد.
/// </summary>
public sealed class AddressBookCustomerActorResolver(
    ICurrentAuthenticatedUser currentUser,
    IHostEnvironment environment) : IAddressBookCustomerActorResolver
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
