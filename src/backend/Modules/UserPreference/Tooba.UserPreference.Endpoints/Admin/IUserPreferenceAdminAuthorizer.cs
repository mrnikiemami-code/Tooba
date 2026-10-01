using Microsoft.AspNetCore.Http;

namespace Tooba.UserPreference.Endpoints.Admin;

/// <summary>Admin authorization adapter contract for UserPreference Endpoints.</summary>
public interface IUserPreferenceAdminAuthorizer
{
    /// <summary>Actor ادمین مجاز را برمی‌گرداند یا استثنای پلتفرم پرتاب می‌کند.</summary>
    Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken);
}
