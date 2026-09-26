using Tooba.Identity.Application;

namespace Tooba.Identity.Infrastructure.SecurityEvents;

/// <summary>
/// جمع‌آوری رخداد امنیتی در حافظه برای تست و درز Audit بعدی.
/// </summary>
public sealed class InMemoryIdentitySecurityEventSink : IIdentitySecurityEventSink
{
    private readonly List<IdentitySecurityEvent> _events = [];

    /// <summary>
    /// رخدادهای ثبت‌شده بدون راز.
    /// </summary>
    public IReadOnlyList<IdentitySecurityEvent> Events
    {
        get
        {
            lock (_events)
            {
                return _events.ToArray();
            }
        }
    }

    /// <inheritdoc />
    public Task RecordAsync(IdentitySecurityEvent securityEvent, CancellationToken cancellationToken)
    {
        lock (_events)
        {
            _events.Add(securityEvent);
        }

        return Task.CompletedTask;
    }
}
