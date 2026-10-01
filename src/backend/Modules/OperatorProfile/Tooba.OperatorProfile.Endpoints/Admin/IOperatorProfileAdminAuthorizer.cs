using Microsoft.AspNetCore.Http;

namespace Tooba.OperatorProfile.Endpoints.Admin;

/// <summary>Admin authorization adapter contract for OperatorProfile Endpoints.</summary>
public interface IOperatorProfileAdminAuthorizer
{
    /// <summary>Actor ادمین مجاز را برمی‌گرداند یا استثنای پلتفرم پرتاب می‌کند.</summary>
    Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken);
}
