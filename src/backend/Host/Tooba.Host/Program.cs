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
using Tooba.Host.Admin.CatalogDemo;
using Tooba.Host.Customer;
using Tooba.Host.Seller;
using Tooba.Host.Fulfillment;
using Tooba.Host.Returns;
using Tooba.Host.Settlement;
using Tooba.Host.Notifications;
using Tooba.Host.AccessControl;
using Tooba.Host.Payments;
using Tooba.Host.Storefront;
using Tooba.Host.Reviews;
using Tooba.Host.ProductQnA;
using Tooba.Host.Wishlist;
using Tooba.Host.AddressBook;
using Tooba.Host.Content;
using Tooba.Host.Media;
using Tooba.Host.PageComposition;
using Tooba.Host.Story;
using Tooba.Host.Preferences;
using Tooba.Host.OperatorProfile;
using Tooba.Host.Promotion;
using Tooba.Host.Support;
using Tooba.Host.Wallet;
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
builder.Services.Configure<CartExpiryHostOptions>(builder.Configuration.GetSection("Tooba:CartExpiry"));
builder.Services.Configure<PaymentReconciliationHostOptions>(builder.Configuration.GetSection("Tooba:PaymentReconciliation"));
builder.Services.AddSingleton<BackgroundWorkerRegistry>();
builder.Services.AddOptions<MessagingHostOptions>()
    .Bind(builder.Configuration.GetSection("Tooba:Messaging"))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<MessagingHostOptions>>(sp =>
    new MessagingOptionsValidator(sp.GetRequiredService<IHostEnvironment>()));
builder.Services.AddOptions<CacheHostOptions>()
    .Bind(builder.Configuration.GetSection("Tooba:Cache"))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<CacheHostOptions>, CacheOptionsValidator>();
builder.Services.AddOptions<AuthorizationHostOptions>()
    .Bind(builder.Configuration.GetSection("Tooba:Authorization"))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<AuthorizationHostOptions>, AuthorizationOptionsValidator>();
builder.Services.AddToobaAuthorization();
builder.Services.AddOptions<AuthSecurityHostOptions>()
    .Bind(builder.Configuration.GetSection(AuthSecurityHostOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<AuthSecurityHostOptions>, AuthSecurityOptionsValidator>();
builder.Services.AddSingleton<AuthenticationInstrumentation>();
builder.Services.AddScoped<CurrentAuthenticatedSession>();
builder.Services.AddSingleton<IAuthenticationThrottleSeam, AuthenticationRateLimitThrottleSeam>();
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
builder.Services.AddHostedService<CartExpiryHostedService>();
builder.Services.AddHostedService<PaymentReconciliationHostedService>();
builder.Services.AddToobaModules(builder.Configuration, builder.Environment);
builder.Services.AddScoped<Tooba.Host.Admin.ProductWorkspaceComposer>();
builder.Services.Configure<CatalogDemoSeedOptions>(
    builder.Configuration.GetSection(CatalogDemoSeedOptions.SectionName));
builder.Services.AddScoped<CatalogDemoMediaFactory>();
builder.Services.AddScoped<CatalogDemoResetService>();
builder.Services.AddScoped<CatalogDemoAssignmentIntegrityService>();
builder.Services.AddScoped<CatalogDemoProductSeedService>();
builder.Services.AddScoped<CatalogDemoSeedService>();
builder.Services.AddScoped<CatalogDemoResetAndSeedHost>();
builder.Services.AddScoped<Tooba.Host.Storefront.StorefrontComposer>();
builder.Services.AddScoped<Tooba.Host.Storefront.StorefrontCartComposer>();
builder.Services.AddScoped<StorefrontCheckoutComposer>(sp =>
    new StorefrontCheckoutComposer(
        sp.GetRequiredService<StorefrontCartComposer>(),
        sp.GetRequiredService<Tooba.Order.Application.ICheckoutDirectory>(),
        sp.GetRequiredService<Tooba.AddressBook.Application.IAddressBookDirectory>(),
        sp.GetRequiredService<CurrentAuthenticatedSession>(),
        sp.GetRequiredService<IHostEnvironment>(),
        sp.GetRequiredService<IHttpContextAccessor>()));
builder.Services.AddScoped<Tooba.Host.Fulfillment.FulfillmentPanelComposer>();
builder.Services.AddScoped<ReturnPanelComposer>();
builder.Services.AddScoped<Tooba.Host.Settlement.SettlementPanelComposer>();
builder.Services.AddScoped<Tooba.Host.Storefront.StorefrontPaymentComposer>();
builder.Services.AddScoped<Tooba.Host.Seller.SellerPanelComposer>();
builder.Services.AddScoped<Tooba.Host.Customer.CustomerPanelComposer>();
builder.Services.AddScoped<Tooba.Host.Admin.AdminPanelComposer>();
builder.Services.AddScoped<Tooba.Host.Wishlist.WishlistComposer>();
builder.Services.AddScoped<Tooba.Host.Content.ContentPanelComposer>();
builder.Services.AddScoped<Tooba.Host.PageComposition.PageCompositionPanelComposer>();
builder.Services.AddScoped<Tooba.Host.Story.StoryPanelComposer>();
builder.Services.AddScoped<Tooba.Host.Promotion.PromotionPanelComposer>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var authSecurityOptions = new AuthSecurityHostOptions();
builder.Configuration.GetSection(AuthSecurityHostOptions.SectionName).Bind(authSecurityOptions);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = authSecurityOptions.MaxRequestBodyBytes;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("ToobaCors", policy =>
    {
        var origins = builder.Configuration.GetSection("Tooba:AuthSecurity:CorsAllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length == 0)
        {
            policy.SetIsOriginAllowed(_ => false);
            return;
        }

        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
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
if (app.Environment.IsDevelopment())
{
    if (app.Services.GetRequiredService<ControlPlaneRegistry>().Edition == ToobaEdition.Marketplace)
    {
        await MarketplaceDevelopmentBootstrap.ApplyAsync(app.Services);
    }
    else
    {
        var catalogDemoOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<CatalogDemoSeedOptions>>().Value;
        // TB-P07-T033: bootstrapهای قدیمی Catalog به‌طور پیش‌فرض خاموش‌اند تا reset+seed تمیز بماند.
        if (catalogDemoOptions.RunLegacyBootstraps)
        {
            await ProductWorkspaceDevelopmentBootstrap.ApplyAsync(app.Services);
            // دانهٔ نمایشی فروشگاه پس از bootstrap اصلی اجرا می‌شود و با slug نگهبان idempotent است؛
            // معنای bootstrap تولیدی عوض نمی‌شود چون فقط در Development صدا زده می‌شود.
            await StorefrontDemoCatalogBootstrap.ApplyAsync(app.Services);
            try
            {
                await CatalogAttributeSchemaDevelopmentBootstrap.ApplyAsync(app.Services);
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "CatalogAttributeSchemaDevelopmentBootstrap failed; Host continues without attribute schema demo.");
            }
            try
            {
                await AccessControlDevelopmentSeed.ApplyAsync(app.Services);
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "AccessControlDevelopmentSeed failed; Host continues without ACC demo snapshot.");
            }
        }
        else
        {
            app.Logger.LogInformation(
                "Legacy Catalog Development bootstraps skipped (Tooba:CatalogDemo:RunLegacyBootstraps=false). Use POST /v1/admin/catalog/demo/reset-and-seed.");
        }

        try
        {
            await SupportDevelopmentSeedHost.ApplyAsync(app.Services);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "SupportDevelopmentSeed failed; Host continues without Support demo snapshot.");
        }

        try
        {
            await WalletDevelopmentSeedHost.ApplyAsync(app.Services);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "WalletDevelopmentSeed failed; Host continues without Wallet demo snapshot.");
        }
    }
}

app.UseExceptionHandler();
if (trustedProxies.Length > 0)
{
    app.UseForwardedHeaders();
}

app.UseCors("ToobaCors");
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<SessionAuthenticationMiddleware>();

app.MapAuthenticationBoundary(enableCors: true);
app.MapProductWorkspaceEndpoints();
app.MapCatalogAttributeEndpoints();
app.MapCatalogFacetEndpoints();
app.MapCatalogMegaMenuEndpoints();
app.MapCatalogTagEndpoints();
app.MapCatalogCategoryEndpoints();
app.MapCatalogDemoDevEndpoints();
app.MapAdminPanelEndpoints();
app.MapStorefrontEndpoints();
app.MapPaymentWebhookEndpoints();
app.MapSellerPanelEndpoints();
app.MapSellerSettingsEndpoints();
app.MapCustomerPanelEndpoints();
app.MapUserPreferenceEndpoints();
app.MapUiPreferenceEndpoints();
app.MapOperatorProfileEndpoints();
app.MapReviewEndpoints();
app.MapProductQnAEndpoints();
app.MapWishlistEndpoints();
app.MapAddressBookEndpoints();
app.MapFulfillmentEndpoints();
app.MapReturnEndpoints();
app.MapSettlementEndpoints();
app.MapSupportEndpoints();
app.MapWalletEndpoints();
app.MapNotificationEndpoints();
app.MapAccessControlEndpoints();
app.MapContentEndpoints();
app.MapMediaEndpoints();
app.MapPageCompositionEndpoints();
app.MapStoryEndpoints();
app.MapPromotionEndpoints();

HostHealthEndpoints.Map(app, enableCors: true);

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
