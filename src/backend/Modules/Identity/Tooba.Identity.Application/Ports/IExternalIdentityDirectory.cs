namespace Tooba.Identity.Application.Ports;

/// <summary>
/// نگاشت issuer+subject به User داخلی. به Keycloak گره نمی‌خورد.
/// </summary>
public interface IExternalIdentityDirectory
{
    /// <summary>
    /// اتصال را ذخیره می‌کند.
    /// </summary>
    Task BindAsync(Guid userId, string issuer, string subject, CancellationToken cancellationToken);

    /// <summary>
    /// User داخلی را از هویت خارجی پیدا می‌کند.
    /// </summary>
    Task<Guid?> FindUserIdAsync(string issuer, string subject, CancellationToken cancellationToken);
}
