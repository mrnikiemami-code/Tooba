using Microsoft.AspNetCore.Http;

namespace Tooba.Localization.Endpoints.Admin;

/// <summary>Admin authorization adapter contract for Localization Endpoints.</summary>
public interface ILocalizationAdminAuthorizer
{
    /// <summary>Actor ادمین مجاز را برمی‌گرداند یا استثنای پلتفرم پرتاب می‌کند.</summary>
    Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken);
}
