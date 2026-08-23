using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;

namespace Tooba.Host;

/// <summary>
/// schema خنثی foundation برای اثبات user+tenant و تصویر عضویت Party. مدل Catalog/Order نیست.
/// </summary>
internal sealed class FoundationAuthorizationSchemaProvider : IAuthorizationSchemaProvider
{
    /// <inheritdoc />
    public int SchemaVersion => 2;

    /// <inheritdoc />
    public string SchemaText =>
        """
        definition user {}

        definition tenant {
          relation member: user
          permission view = member
        }

        definition party {
          relation member: user
          permission view = member
        }
        """;
}

/// <summary>
/// bootstrap فقط وقتی ApplySchemaOnStartup روشن باشد. تولید هر استارت را بازنویسی نمی‌کند.
/// </summary>
internal sealed class ConfiguredAuthorizationSchemaBootstrapper : IAuthorizationSchemaBootstrapper
{
    private readonly IOptions<AuthorizationHostOptions> _options;
    private readonly IAuthorizationSchemaProvider _schema;
    private readonly ILogger<ConfiguredAuthorizationSchemaBootstrapper> _logger;
    private int? _appliedVersion;

    private readonly IServiceProvider? _services;

    /// <summary>
    /// bootstrap را با پیکربندی صریح می‌سازد. بدون SpiceDB زنده schema شبکه نمی‌نویسد.
    /// </summary>
    public ConfiguredAuthorizationSchemaBootstrapper(
        IOptions<AuthorizationHostOptions> options,
        IAuthorizationSchemaProvider schema,
        ILogger<ConfiguredAuthorizationSchemaBootstrapper> logger)
        : this(options, schema, logger, services: null)
    {
    }

    /// <summary>
    /// در Host، adapter واقعی فقط وقتی Mode=SpiceDb و ApplySchemaOnStartup روشن باشد resolve می‌شود تا کانال بی‌دلیل ساخته نشود.
    /// </summary>
    public ConfiguredAuthorizationSchemaBootstrapper(
        IOptions<AuthorizationHostOptions> options,
        IAuthorizationSchemaProvider schema,
        ILogger<ConfiguredAuthorizationSchemaBootstrapper> logger,
        IServiceProvider? services)
    {
        _options = options;
        _schema = schema;
        _logger = logger;
        _services = services;
    }

    /// <inheritdoc />
    public async Task BootstrapIfConfiguredAsync(CancellationToken cancellationToken)
    {
        if (!_options.Value.ApplySchemaOnStartup)
        {
            return;
        }

        _appliedVersion = _schema.SchemaVersion;
        _logger.LogInformation(
            "Authorization schema bootstrap requested. Version {SchemaVersion}. Token is not logged.",
            _schema.SchemaVersion);

        if (!string.Equals(_options.Value.Mode, "SpiceDb", StringComparison.Ordinal) || _services is null)
        {
            return;
        }

        var adapter = _services.GetRequiredService<SpiceDbAuthorizationAdapter>();
        await adapter.WriteSchemaAsync(_schema.SchemaText, cancellationToken);
    }

    /// <summary>
    /// نسخهٔ اعمال‌شده برای تست؛ null یعنی bootstrap اجرا نشده.
    /// </summary>
    public int? AppliedVersion => _appliedVersion;
}

/// <summary>
/// موتور درون‌حافظه‌ای معادل معنایی schema خنثی. تصمیم ALLOW/DENY را cache سراسری نمی‌کند.
/// </summary>
internal sealed class InMemoryAuthorizationAdapter : IAuthorizationService, IAuthorizationTupleWriter
{
    private readonly ConcurrentDictionary<string, byte> _tuples = new(StringComparer.Ordinal);
    private readonly AuthorizationInstrumentation _telemetry;
    private readonly IAuthorizationSecurityEventSink _audit;

    /// <summary>
    /// adapter تست/توسعه را می‌سازد.
    /// </summary>
    public InMemoryAuthorizationAdapter(AuthorizationInstrumentation telemetry, IAuthorizationSecurityEventSink audit)
    {
        _telemetry = telemetry;
        _audit = audit;
    }

    /// <inheritdoc />
    public async Task WriteAsync(AuthorizationRelationshipWrite write, CancellationToken cancellationToken)
    {
        AuthorizationContractValidator.Validate(write);
        _tuples[Key(write.Subject, write.Relation, write.Resource)] = 1;
        await _audit.RecordAsync("relationship_changed", write.Resource.Type, write.Relation, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AuthorizationDecision> CanAsync(AuthorizationCheck check, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        AuthorizationContractValidator.Validate(check);
        var allowed = check.Permission == AuthorizationRelations.View
            && _tuples.ContainsKey(Key(check.Subject, AuthorizationRelations.Member, check.Resource));
        var decision = allowed ? AuthorizationDecision.Allow() : AuthorizationDecision.Deny();
        if (decision.Kind == AuthorizationDecisionKind.Deny)
        {
            await _audit.RecordAsync("permission_denied", check.Resource.Type, check.Permission, cancellationToken);
        }

        _telemetry.Record(
            decision.Kind,
            check.Resource.Type,
            check.Permission,
            check.CallContext.Edition,
            Stopwatch.GetElapsedTime(started).Milliseconds);
        return decision;
    }

    private static string Key(AuthorizationSubject subject, string relation, AuthorizationResource resource) =>
        $"{subject.Type}:{subject.Id}#{relation}@{resource.Type}:{resource.Id}";
}

/// <summary>
/// adapter شکست‌بسته وقتی Mode=Disabled یا SpiceDB پیکربندی/شبکه خراب است. ALLOW برنمی‌گرداند.
/// </summary>
internal sealed class FailClosedAuthorizationAdapter : IAuthorizationService, IAuthorizationTupleWriter
{
    private readonly string _reason;
    private readonly AuthorizationInstrumentation _telemetry;

    /// <summary>
    /// adapter fail-closed را با کد علت داخلی می‌سازد.
    /// </summary>
    public FailClosedAuthorizationAdapter(string reason, AuthorizationInstrumentation telemetry)
    {
        _reason = reason;
        _telemetry = telemetry;
    }

    /// <inheritdoc />
    public Task WriteAsync(AuthorizationRelationshipWrite write, CancellationToken cancellationToken)
    {
        AuthorizationContractValidator.Validate(write);
        return Task.FromException(new InvalidOperationException("authorization.unavailable"));
    }

    /// <inheritdoc />
    public Task<AuthorizationDecision> CanAsync(AuthorizationCheck check, CancellationToken cancellationToken)
    {
        AuthorizationContractValidator.Validate(check);
        _telemetry.Record(
            AuthorizationDecisionKind.Unavailable,
            check.Resource.Type,
            check.Permission,
            check.CallContext.Edition,
            0);
        return Task.FromResult(AuthorizationDecision.Unavailable(_reason));
    }
}

/// <summary>
/// مرز use-case بدون MediatR و بدون SDK در Domain.
/// </summary>
internal sealed class AuthorizationGuard : IAuthorizationGuard
{
    private readonly IAuthorizationService _authorization;

    /// <summary>
    /// نگهبان را روی قرارداد Tooba می‌سازد.
    /// </summary>
    public AuthorizationGuard(IAuthorizationService authorization) => _authorization = authorization;

    /// <inheritdoc />
    public Task<AuthorizationDecision> AuthorizeUseCaseAsync(AuthorizationCheck check, CancellationToken cancellationToken) =>
        _authorization.CanAsync(check, cancellationToken);
}

/// <summary>
/// جمع‌آوری رخداد امنیتی مجوز در حافظه.
/// </summary>
internal sealed class InMemoryAuthorizationSecurityEventSink : IAuthorizationSecurityEventSink
{
    private readonly List<(string Event, string? ResourceType, string? Permission)> _events = [];

    /// <summary>
    /// رخدادهای بدون راز.
    /// </summary>
    public IReadOnlyList<(string Event, string? ResourceType, string? Permission)> Events
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
    public Task RecordAsync(string eventName, string? resourceType, string? permission, CancellationToken cancellationToken)
    {
        lock (_events)
        {
            _events.Add((eventName, resourceType, permission));
        }

        return Task.CompletedTask;
    }
}
