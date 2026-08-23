using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;

namespace Tooba.Host;

/// <summary>
/// schema خنثی foundation برای اثبات user+tenant. مدل Catalog/Order نیست.
/// </summary>
internal sealed class FoundationAuthorizationSchemaProvider : IAuthorizationSchemaProvider
{
    /// <inheritdoc />
    public int SchemaVersion => 1;

    /// <inheritdoc />
    public string SchemaText =>
        """
        definition user {}

        definition tenant {
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

    /// <summary>
    /// bootstrap را با پیکربندی صریح می‌سازد.
    /// </summary>
    public ConfiguredAuthorizationSchemaBootstrapper(
        IOptions<AuthorizationHostOptions> options,
        IAuthorizationSchemaProvider schema,
        ILogger<ConfiguredAuthorizationSchemaBootstrapper> logger)
    {
        _options = options;
        _schema = schema;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task BootstrapIfConfiguredAsync(CancellationToken cancellationToken)
    {
        if (!_options.Value.ApplySchemaOnStartup)
        {
            return Task.CompletedTask;
        }

        _appliedVersion = _schema.SchemaVersion;
        _logger.LogInformation(
            "Authorization schema bootstrap requested. Version {SchemaVersion}. Token is not logged.",
            _schema.SchemaVersion);
        return Task.CompletedTask;
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

/// <summary>
/// adapter آمادهٔ یکپارچگی SpiceDB. تا وقتی سرویس واقعی بالا نباشد ALLOW جعلی برنمی‌گرداند.
/// </summary>
internal sealed class SpiceDbAuthorizationAdapter : IAuthorizationService, IAuthorizationTupleWriter
{
    private readonly FailClosedAuthorizationAdapter _unavailable;

    /// <summary>
    /// تا اتصال واقعی SpiceDB در این محیط برقرار نشود، همهٔ عملیات Unavailable هستند.
    /// </summary>
    public SpiceDbAuthorizationAdapter(AuthorizationInstrumentation telemetry)
    {
        _unavailable = new FailClosedAuthorizationAdapter("spicedb.unavailable", telemetry);
    }

    /// <inheritdoc />
    public Task<AuthorizationDecision> CanAsync(AuthorizationCheck check, CancellationToken cancellationToken) =>
        _unavailable.CanAsync(check, cancellationToken);

    /// <inheritdoc />
    public Task WriteAsync(AuthorizationRelationshipWrite write, CancellationToken cancellationToken) =>
        _unavailable.WriteAsync(write, cancellationToken);
}
