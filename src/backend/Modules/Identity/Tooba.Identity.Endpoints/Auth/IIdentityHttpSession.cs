namespace Tooba.Identity.Endpoints.Auth;

/// <summary>Request-scoped authenticated session view for Identity HTTP endpoints.</summary>
public interface IIdentityHttpSession
{
    Guid? UserId { get; }
    Guid? SessionId { get; }
    string? Edition { get; }
    string? TenantId { get; }
    bool IsAuthenticated { get; }
}
