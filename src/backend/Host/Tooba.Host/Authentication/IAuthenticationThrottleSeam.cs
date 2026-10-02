using Tooba.Identity.Endpoints.Auth;

namespace Tooba.Host;

/// <summary>
/// Host binding of <see cref="IIdentityAuthThrottle"/> (platform rate-limit seam).
/// </summary>
internal interface IAuthenticationThrottleSeam : IIdentityAuthThrottle
{
}
