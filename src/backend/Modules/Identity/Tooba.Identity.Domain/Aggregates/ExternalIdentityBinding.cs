namespace Tooba.Identity.Domain.Aggregates;

/// <summary>
/// اتصال آینده به IdP. User به یک Keycloak خاص وابسته نیست.
/// </summary>
public sealed class ExternalIdentityBinding
{
    /// <summary>
    /// کلید پایدار اتصال.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// User داخلی Tooba.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// صادرکنندهٔ پایدار (مثلاً issuer OIDC)؛ ایمیل کلید نیست.
    /// </summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// subject پایدار در همان issuer.
    /// </summary>
    public string Subject { get; init; } = string.Empty;

    /// <summary>
    /// زمان ایجاد اتصال.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}
