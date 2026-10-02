using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;

namespace Tooba.Identity.Application.Ports;

/// <summary>
/// مرز access credential. JWT سفارشی اختراع نمی‌شود؛ cookie/BFF/IdP بعداً روی همین نشست سوار می‌شوند.
/// </summary>
public interface IAccessCredentialBoundary
{
    /// <summary>
    /// بلیت کوتاه‌عمر آزمایشی از نشست صادر می‌کند بدون JWT اختصاصی.
    /// </summary>
    AuthenticationTicket ToAccessTicket(AuthenticationTicket sessionTicket);
}
