using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks.Security;

namespace Tooba.ProductQnA.Endpoints.Customer;

/// <summary>درز تخصیص Actor مشتری برای مرز HTTP ProductQnA.</summary>
public interface IProductQnACustomerActorResolver
{
    /// <summary>Actor را حل می‌کند؛ null یعنی هویت قابل اعتماد نیست.</summary>
    Guid? ResolveActor(HttpContext httpContext);
}

/// <summary>Actor فقط از <see cref="ICurrentAuthenticatedUser"/> یا هدر Dev/Testing؛ بدون guest.</summary>
public sealed class ProductQnACustomerActorResolver(
    ICurrentAuthenticatedUser currentUser,
    IHostEnvironment environment) : IProductQnACustomerActorResolver
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
