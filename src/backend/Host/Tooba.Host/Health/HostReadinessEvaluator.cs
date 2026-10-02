using MassTransit;
using Tooba.AccessControl.Contracts.Readiness;
using Tooba.BuildingBlocks;
using Tooba.Host.Configuration;
using Tooba.Host.Messaging;

namespace Tooba.Host.Health;

/// <summary>
/// ارزیابی readiness بدون باز کردن DbContext یا نشت connection string.
/// فقط وابستگی‌های بحرانی پیکربندی و messaging (در صورت فعال بودن) را بررسی می‌کند.
/// آمادگی مجوز فقط از درز باریک <see cref="IAuthorizationReadinessProbe"/> خوانده می‌شود.
/// </summary>
internal static class HostReadinessEvaluator
{
    /// <summary>
    /// نتیجهٔ readiness و برچسب‌های امن برای پاسخ JSON.
    /// </summary>
    internal sealed record Evaluation(bool Ready, IReadOnlyDictionary<string, string> Checks);

    /// <summary>
    /// readiness را از registry، options و درز آمادگی مجوز می‌سازد.
    /// </summary>
    internal static async Task<Evaluation> EvaluateAsync(
        ControlPlaneRegistry registry,
        ToobaPlatformOptions platformOptions,
        MessagingHostOptions messagingOptions,
        IAuthorizationReadinessProbe authorizationReadiness,
        IEnumerable<IBusControl> busControls,
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
                checks["postgresql"] = "missing-reference";
                return new Evaluation(false, checks);
            }
        }

        checks["postgresql"] = "configured";

        var readiness = await authorizationReadiness.EvaluateAsync(cancellationToken);
        checks["authorization"] = readiness.CheckLabel;
        if (!readiness.Ready)
        {
            return new Evaluation(false, checks);
        }

        if (messagingOptions.Enabled)
        {
            if (!TryGetSingleBus(busControls, out var bus))
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
    /// دقیقاً یک IBusControl می‌پذیرد؛ صفر یا چند ثبت = unavailable.
    /// </summary>
    private static bool TryGetSingleBus(IEnumerable<IBusControl> busControls, out IBusControl bus)
    {
        using var enumerator = busControls.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            bus = null!;
            return false;
        }

        bus = enumerator.Current;
        if (enumerator.MoveNext())
        {
            bus = null!;
            return false;
        }

        return true;
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
