using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks.Security;

namespace Tooba.Reviews.Endpoints.Customer;

/// <summary>درز خنثی تخصیص هویت Actor برای مرز HTTP مشتری Reviews.</summary>
public interface IReviewsCustomerActorResolver
{
    /// <summary>Actor را حل می‌کند؛ null یعنی هویت قابل اعتماد نیست (401).</summary>
    Guid? ResolveActor(HttpContext httpContext);
}

/// <summary>
/// Actor فقط از <see cref="ICurrentAuthenticatedUser"/> یا هدر Dev/Testing؛ بدون guest fallback.
/// </summary>
public sealed class ReviewsCustomerActorResolver(
    ICurrentAuthenticatedUser currentUser,
    IHostEnvironment environment) : IReviewsCustomerActorResolver
{
    /// <summary>نام دقیق هدر Actor توسعه.</summary>
    public const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <inheritdoc />
    public Guid? ResolveActor(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (currentUser.IsAuthenticated && currentUser.UserId is { } authenticated)
            return authenticated;

        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            return null;

        if (httpContext.Request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var devActor)
            && devActor != Guid.Empty)
        {
            return devActor;
        }

        return null;
    }
}
