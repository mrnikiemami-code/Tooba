using Microsoft.Extensions.Options;
using Tooba.AccessControl.Contracts.Readiness;
using Tooba.AccessControl.Infrastructure.Authorization;

namespace Tooba.AccessControl.Infrastructure.Adapters;

/// <summary>
/// AccessControl-owned readiness adapter exposing only the narrow
/// <see cref="IAuthorizationReadinessProbe"/> contract to Host. Mode pre-checks, the optional
/// lightweight SpiceDB probe and every infrastructure/secret detail stay inside this module so
/// Host never consumes <c>SpiceDbAuthorizationOptions</c> or <c>SpiceDbHealthProbe</c>.
/// </summary>
public sealed class AuthorizationReadinessProbe(IOptions<SpiceDbAuthorizationOptions> options)
    : IAuthorizationReadinessProbe
{
    private readonly SpiceDbAuthorizationOptions _options = options.Value;

    /// <inheritdoc />
    public async Task<AuthorizationReadiness> EvaluateAsync(CancellationToken cancellationToken)
    {
        var mode = _options.Mode.Trim();
        if (!mode.Equals("SpiceDb", StringComparison.OrdinalIgnoreCase))
        {
            return new AuthorizationReadiness(true, mode.ToLowerInvariant());
        }

        if (string.IsNullOrWhiteSpace(_options.SpiceDb.Endpoint))
        {
            return new AuthorizationReadiness(false, "spicedb-endpoint-missing");
        }

        if (string.IsNullOrWhiteSpace(_options.SpiceDb.Token))
        {
            return new AuthorizationReadiness(false, "spicedb-token-missing");
        }

        if (_options.SpiceDb.ReadinessProbeEnabled)
        {
            using var probe = new SpiceDbHealthProbe(options);
            if (!await probe.CheckAsync(cancellationToken))
            {
                return new AuthorizationReadiness(false, "spicedb-unreachable");
            }
        }

        return new AuthorizationReadiness(true, mode.ToLowerInvariant());
    }
}
