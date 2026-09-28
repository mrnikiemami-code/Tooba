namespace Tooba.AccessControl.Contracts.Readiness;

/// <summary>
/// Result of the AccessControl-owned authorization readiness evaluation, expressed only as the
/// minimum safe information Host needs: whether authorization is ready and the existing
/// non-secret check label. No endpoint, token, SDK type, or raw infrastructure option is exposed.
/// </summary>
/// <param name="Ready">Whether the authorization dependency is ready for the current mode.</param>
/// <param name="CheckLabel">
/// Non-secret readiness label preserved from the pre-cleanup readiness behavior, for example
/// <c>disabled</c>, <c>inmemory</c>, <c>spicedb</c>, <c>spicedb-endpoint-missing</c>,
/// <c>spicedb-token-missing</c>, or <c>spicedb-unreachable</c>.
/// </param>
public sealed record AuthorizationReadiness(bool Ready, string CheckLabel);

/// <summary>
/// Narrow readiness port owned by AccessControl and consumed by Host. The mode decision, the
/// configuration pre-checks, and the optional lightweight SpiceDB probe stay inside
/// AccessControl.Infrastructure; Host only reads the evaluated readiness.
/// </summary>
public interface IAuthorizationReadinessProbe
{
    /// <summary>
    /// Evaluates authorization readiness for the configured mode without emitting secrets.
    /// </summary>
    Task<AuthorizationReadiness> EvaluateAsync(CancellationToken cancellationToken);
}
