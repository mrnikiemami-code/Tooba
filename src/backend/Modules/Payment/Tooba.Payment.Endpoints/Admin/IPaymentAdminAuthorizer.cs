using Microsoft.AspNetCore.Http;

namespace Tooba.Payment.Endpoints.Admin;

/// <summary>Admin authorization adapter contract for Payment Endpoints.</summary>
public interface IPaymentAdminAuthorizer
{
    Task RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken);
}
