using Tooba.Identity.Application.Models;

namespace Tooba.Identity.Application.Ports;

/// <summary>
/// درز Security Audit آینده. لاگ فنی جایگزین این درز نیست.
/// </summary>
public interface IIdentitySecurityEventSink
{
    /// <summary>
    /// رخداد امنیتی را بدون راز به مصرف‌کنندهٔ بعدی می‌دهد.
    /// </summary>
    Task RecordAsync(IdentitySecurityEvent securityEvent, CancellationToken cancellationToken);
}
