using MassTransit;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;

namespace Tooba.Host;

/// <summary>
/// ارزیابی readiness بدون باز کردن DbContext یا نشت connection string.
/// فقط وابستگی‌های بحرانی پیکربندی و messaging (در صورت فعال بودن) را بررسی می‌کند.
/// </summary>
internal static class HostReadinessEvaluator
{
    /// <summary>
    /// نتیجهٔ readiness و برچسب‌های امن برای پاسخ JSON.
    /// </summary>
    internal sealed record Evaluation(bool Ready, IReadOnlyDictionary<string, string> Checks);

    /// <summary>
    /// readiness را از registry و options می‌سازد؛ SpiceDB probe فقط وقتی Mode=SpiceDb فعال است.
    /// </summary>
    internal static async Task<Evaluation> EvaluateAsync(
        ControlPlaneRegistry registry,
        ToobaPlatformOptions platformOptions,
        MessagingHostOptions messagingOptions,
        AuthorizationHostOptions authorizationOptions,
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var checks = new Dictionary<string, string>(StringComparer.Ordinal);

        if (registry.Edition == ToobaEdition.Unset)
        {
            checks["edition"] = "unconfigured";
            return new Evaluation(false, checks);
        }

        checks["edition"] = registry.Edition.ToString();

        foreach (var reference in CollectConnectionReferences(registry, messagingOptions))
        {
            if (!platformOptions.PostgreSQL.ConnectionReferences.TryGetValue(reference, out var connection)
                || string.IsNullOrWhiteSpace(connection))
            {
                checks["postgresql"] = $"missing-reference:{reference}";
                return new Evaluation(false, checks);
            }
        }

        checks["postgresql"] = "configured";

        var authMode = authorizationOptions.Mode.Trim();
        if (authMode.Equals("SpiceDb", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(authorizationOptions.SpiceDb.Endpoint))
            {
                checks["authorization"] = "spicedb-endpoint-missing";
                return new Evaluation(false, checks);
            }

            if (string.IsNullOrWhiteSpace(authorizationOptions.SpiceDb.Token))
            {
                checks["authorization"] = "spicedb-token-missing";
                return new Evaluation(false, checks);
            }

            var probe = services.GetService<SpiceDbHealthProbe>();
            if (probe is not null && authorizationOptions.SpiceDb.ReadinessProbeEnabled)
            {
                var reachable = await probe.CheckAsync(cancellationToken);
                if (!reachable)
                {
                    checks["authorization"] = "spicedb-unreachable";
                    return new Evaluation(false, checks);
                }
            }
        }

        checks["authorization"] = authMode.ToLowerInvariant();

        if (messagingOptions.Enabled)
        {
            var bus = services.GetService<IBusControl>();
            if (bus is null)
            {
                checks["messaging"] = "bus-unavailable";
                return new Evaluation(false, checks);
            }

            var health = bus.CheckHealth();
            if (health.Status == BusHealthStatus.Unhealthy)
            {
                checks["messaging"] = "unhealthy";
                return new Evaluation(false, checks);
            }

            checks["messaging-transport"] = "postgresql-sql";
            checks["messaging-schema"] = messagingOptions.Schema;
            checks["messaging"] = health.Status.ToString();
        }
        else
        {
            checks["messaging"] = "disabled";
            checks["messaging-transport"] = "n/a";
        }

        return new Evaluation(true, checks);
    }

    /// <summary>
    /// مراجع اتصال مورد نیاز edition و messaging را جمع می‌کند.
    /// </summary>
    private static IEnumerable<string> CollectConnectionReferences(
        ControlPlaneRegistry registry,
        MessagingHostOptions messagingOptions)
    {
        if (registry.Edition == ToobaEdition.Marketplace
            && registry.MarketplaceConnectionReference is { } marketplaceReference)
        {
            yield return marketplaceReference.Value;
        }

        if (registry.Edition == ToobaEdition.SingleStore)
        {
            foreach (var tenant in registry.Tenants.Values)
            {
                yield return tenant.ConnectionReference.Value;
            }
        }

        if (messagingOptions.Enabled && !string.IsNullOrWhiteSpace(messagingOptions.ConnectionReference))
        {
            yield return messagingOptions.ConnectionReference.Trim();
        }
    }
}
