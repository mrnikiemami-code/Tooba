using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Net;
using System.Text.Json.Serialization;
using Tooba.BuildingBlocks;
using Tooba.Host;

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

app.MapGet("/health", () => Results.Json(new { status = "ok" }));
app.MapGet("/ready", () => Results.Json(new { status = "ready" }));

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

public partial class Program;
