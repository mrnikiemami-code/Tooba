// ریشهٔ ترکیب Host: Observability، resolve Edition/Tenant، ماژول‌های صریح، Outbox dispatcher، MassTransit SQL Transport، کش درون‌فرآیندی.
// Host ورودی routing است نه TenantId. کارگر Outbox و مصرف‌کننده Tenant را از Host نمی‌خوانند.
// مسیرهای /__platform-* فقط Development/Testing هستند و قبل از استقرار عمومی باید محدود شوند.
// لاگ فنی جایگزین Audit نیست. DbContext و Outbox برای /health و /ready باز نمی‌شوند.
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Net;
using System.Text.Json.Serialization;
using Tooba.BuildingBlocks;
using Tooba.Host;
using Tooba.Host.Admin;
using Tooba.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.SpanId
        | ActivityTrackingOptions.TraceId
        | ActivityTrackingOptions.ParentId;
});
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "O";
    options.UseUtcTimestamp = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ToobaExceptionHandler>();

builder.Services.AddOptions<ToobaPlatformOptions>()
    .Bind(builder.Configuration.GetSection(ToobaPlatformOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<ToobaPlatformOptions>, PlatformOptionsValidator>();
builder.Services.AddSingleton(sp =>
    PlatformOptionsValidator.BuildRegistry(sp.GetRequiredService<IOptions<ToobaPlatformOptions>>().Value));
builder.Services.AddSingleton<IDatabaseConnectionResolver, DatabaseConnectionResolver>();
builder.Services.AddScoped<HttpCommerceContextAccessor>();
builder.Services.AddScoped<ICurrentCommerceContext>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
builder.Services.AddScoped<ICurrentEdition>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
builder.Services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
builder.Services.AddScoped<ICommerceContextAssigner>(sp => sp.GetRequiredService<HttpCommerceContextAccessor>());
builder.Services.Configure<OutboxHostOptions>(builder.Configuration.GetSection("Tooba:Outbox"));
builder.Services.AddOptions<MessagingHostOptions>()
    .Bind(builder.Configuration.GetSection("Tooba:Messaging"))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<MessagingHostOptions>, MessagingOptionsValidator>();
builder.Services.AddOptions<CacheHostOptions>()
    .Bind(builder.Configuration.GetSection("Tooba:Cache"))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<CacheHostOptions>, CacheOptionsValidator>();
builder.Services.AddOptions<AuthorizationHostOptions>()
    .Bind(builder.Configuration.GetSection("Tooba:Authorization"))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<AuthorizationHostOptions>, AuthorizationOptionsValidator>();
builder.Services.AddToobaAuthorization();
builder.Services.AddScoped<CurrentAuthenticatedSession>();
builder.Services.AddSingleton<IAuthenticationThrottleSeam, NoOpAuthenticationThrottleSeam>();
builder.Services.AddSingleton<IIntegrationEventSerializer, JsonIntegrationEventSerializer>();
builder.Services.AddSingleton<IOutboxDispatcherStore, NpgsqlOutboxDispatcherStore>();
builder.Services.AddSingleton<IOutboxPollTargetSource, ConfiguredOutboxPollTargetSource>();
builder.Services.AddSingleton<WorkerCommerceContextFactory>();
builder.Services.AddSingleton<OutboxDispatcher>();
var messagingOptions = new MessagingHostOptions();
builder.Configuration.GetSection("Tooba:Messaging").Bind(messagingOptions);
builder.Services.AddToobaIntegrationPublisher(builder.Environment, messagingOptions);
builder.Services.AddScoped<OutboxSaveChangesInterceptor>();
builder.Services.AddHostedService<OutboxDispatcherHostedService>();
builder.Services.AddToobaModules(builder.Configuration, builder.Environment);
builder.Services.AddScoped<Tooba.Host.Admin.ProductWorkspaceComposer>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var trustedProxies = builder.Configuration.GetSection("Tooba:TrustedProxies").Get<string[]>() ?? [];
if (trustedProxies.Length > 0)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
            | ForwardedHeaders.XForwardedProto
            | ForwardedHeaders.XForwardedHost;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
        foreach (var proxy in trustedProxies)
        {
            if (IPAddress.TryParse(proxy, out var address))
            {
                options.KnownProxies.Add(address);
            }
        }
    });
}

var otel = builder.Configuration.GetSection("Tooba:Observability");
var serviceName = otel["ServiceName"] ?? "Tooba.Host";
var otlp = otel["OtlpEndpoint"];
var enableTracing = otel.GetValue("EnableTracing", true);
var enableMetrics = otel.GetValue("EnableMetrics", true);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        serviceName: serviceName,
        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName,
        }))
    .WithTracing(tracing =>
    {
        if (!enableTracing)
        {
            return;
        }

        tracing.AddSource(ToobaTelemetry.ActivitySourceName);
        tracing.AddSource(MessagingRegistration.MassTransitActivitySource);
        tracing.AddAspNetCoreInstrumentation(options =>
        {
            options.Filter = httpContext =>
            {
                var path = httpContext.Request.Path;
                return !path.StartsWithSegments("/health") && !path.StartsWithSegments("/ready");
            };
            options.RecordException = true;
        });
        tracing.AddHttpClientInstrumentation();
        if (!string.IsNullOrWhiteSpace(otlp))
        {
            tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlp));
        }
    })
    .WithMetrics(metrics =>
    {
        if (!enableMetrics)
        {
            return;
        }

        metrics.AddMeter(ToobaTelemetry.MeterName);
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddRuntimeInstrumentation();
        metrics.AddHttpClientInstrumentation();
        if (!string.IsNullOrWhiteSpace(otlp))
        {
            metrics.AddOtlpExporter(o => o.Endpoint = new Uri(otlp));
        }
    });

var app = builder.Build();

app.UseExceptionHandler();
if (trustedProxies.Length > 0)
{
    app.UseForwardedHeaders();
}

app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<SessionAuthenticationMiddleware>();

app.MapAuthenticationBoundary();
app.MapProductWorkspaceEndpoints();

app.MapGet("/health", () => Results.Json(new { status = "ok" }));
app.MapGet("/ready", (IServiceProvider services) =>
{
    var bus = services.GetService<MassTransit.IBusControl>();
    if (bus is null)
    {
        return Results.Json(new { status = "ready" });
    }

    var health = bus.CheckHealth();
    if (health.Status == MassTransit.BusHealthStatus.Unhealthy)
    {
        return Results.Json(new { status = "not-ready", messaging = "unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Json(new
    {
        status = "ready",
        messaging = health.Status.ToString(),
    });
});

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/__platform-error", () =>
    {
        throw new InvalidOperationException("platform-bootstrap-test");
    });
    app.MapGet("/__platform-conflict", () =>
    {
        throw new PlatformHttpException(StatusCodes.Status409Conflict, "Conflict", "platform.conflict");
    });
    app.MapGet("/__platform-commerce", (ICurrentCommerceContext commerce) =>
    {
        var current = commerce.Current
            ?? throw new PlatformHttpException(StatusCodes.Status503ServiceUnavailable, "Service Unavailable", "platform.edition.unconfigured");
        return Results.Json(new
        {
            edition = current.Edition.Edition.ToString(),
            deploymentId = current.Edition.DeploymentId,
            tenantId = current.Tenant?.TenantId.Value,
            connectionReference = current.DatabaseConnectionReference.Value,
            resolvedHost = current.Tenant?.ResolvedHost,
            themeReference = current.Tenant?.ThemeReference,
            defaultMarketReference = current.Tenant?.DefaultMarketReference,
            traceId = current.TraceId,
        });
    });
}

app.Run();

/// <summary>
/// نقطهٔ ورود Host و لنگر WebApplicationFactory. منطق کسب‌وکار در این نوع نیست.
/// </summary>
public partial class Program;
