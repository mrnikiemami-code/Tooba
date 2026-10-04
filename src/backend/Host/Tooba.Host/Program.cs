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
using Tooba.BuildingBlocks.DependencyInjection;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Presentation;
using Tooba.StoreContext.Contracts.Current;
using Tooba.Host;
using Tooba.Host.Configuration;
using Tooba.Identity.Endpoints;
using Tooba.Host.MultiTenancy;
using Tooba.Host.Health;
using Tooba.Host.Messaging;
using Tooba.Host.Observability;
using Tooba.Host.Outbox;
using Tooba.Host.Errors;
using Tooba.Host.Caching;
using Tooba.Host.Persistence;
using Tooba.Host.Security.Seller;
using Tooba.Host.Security;
using Tooba.Host.Admin.Access;
using Tooba.Host.Admin.Access.Authorizers;
using Tooba.Host.Admin.Development;
using Tooba.Host.Admin.Panel;
using Tooba.Host.Development;
using Tooba.Catalog.Application.Development.CatalogDemo;
using Tooba.Catalog.Infrastructure.Development;
using Tooba.CustomerProfile.Endpoints;
using Tooba.Returns.Endpoints;
using Tooba.Notification.Endpoints;
using Tooba.AccessControl.Endpoints;
using Tooba.Payment.Endpoints;
using Tooba.Promotion.Endpoints;
using Tooba.Reviews.Endpoints;
using Tooba.BulkInquiry.Endpoints;
using Tooba.ProductQnA.Endpoints;
using Tooba.Wishlist.Endpoints;
using Tooba.AddressBook.Endpoints;
using Tooba.Catalog.Endpoints;
using Tooba.Content.Endpoints;
using Tooba.ProductWorkspace.Endpoints;
using Tooba.Content.Infrastructure;
using Tooba.Content.Infrastructure.Development;
using Tooba.Host.Composition;
using Tooba.PageComposition.Endpoints;
using Tooba.Story.Endpoints;
using Tooba.UserPreference.Endpoints;
using Tooba.OperatorProfile.Endpoints;
using Tooba.Localization.Endpoints;
using Tooba.Support.Endpoints;
using Tooba.Wallet.Endpoints;
using Tooba.Offer.Endpoints;
using Tooba.Offer.Endpoints.Seller;
using Tooba.Party.Endpoints;
using Tooba.Cart.Endpoints;
using Tooba.Settlement.Endpoints;
using Tooba.Fulfillment.Endpoints;
using Tooba.Media.Endpoints;
using Tooba.Offer.Infrastructure.Adapters;
using Tooba.Offer.Infrastructure.Adapters.Tracing;
using Tooba.Tax.Endpoints;
using Tooba.Pricing.Endpoints;
using Tooba.Order.Endpoints;
using Tooba.Order.Infrastructure.ReservationCycle;
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
builder.Services.AddToobaObservabilityFoundation();
builder.Services.AddOfferEndpointPresentation();
builder.Services.AddCartEndpointPresentation();
builder.Services.AddNotificationEndpointPresentation();
builder.Services.AddSupportEndpointPresentation();
builder.Services.AddWalletEndpointPresentation();
builder.Services.AddPaymentEndpointPresentation();
builder.Services.AddPromotionEndpointPresentation();
builder.Services.AddOrderEndpointPresentation();
builder.Services.AddPricingEndpointPresentation();
builder.Services.AddFulfillmentEndpointPresentation();
builder.Services.AddReturnEndpointPresentation();
builder.Services.AddAddressBookEndpointPresentation();
builder.Services.AddWishlistEndpointPresentation();
builder.Services.AddStoryEndpointPresentation();
builder.Services.AddAccessControlEndpointPresentation();
builder.Services.AddPageCompositionEndpointPresentation();
builder.Services.AddReviewsEndpointPresentation();
builder.Services.AddUserPreferenceEndpointPresentation();
builder.Services.AddOperatorProfileEndpointPresentation();
builder.Services.AddLocalizationEndpointPresentation();
builder.Services.AddProductQnAEndpointPresentation();
builder.Services.AddBulkInquiryEndpointPresentation();
builder.Services.AddCustomerProfileEndpointPresentation();
builder.Services.AddPartyEndpointPresentation();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IExceptionPresentationService, ExceptionPresentationService>();
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
builder.Services.AddOptions<OutboxHostOptions>()
    .Bind(builder.Configuration.GetSection("Tooba:Outbox"))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<OutboxHostOptions>, OutboxHostOptionsValidator>();
builder.Services.AddOptions<MessagingHostOptions>()
    .Bind(builder.Configuration.GetSection("Tooba:Messaging"))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<MessagingHostOptions>>(sp =>
    new MessagingOptionsValidator(sp.GetRequiredService<IHostEnvironment>()));
builder.Services.AddOptions<CacheHostOptions>()
    .Bind(builder.Configuration.GetSection("Tooba:Cache"))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<CacheHostOptions>, CacheOptionsValidator>();
builder.Services.AddToobaCache();
builder.Services.AddOptions<AuthSecurityHostOptions>()
    .Bind(builder.Configuration.GetSection(AuthSecurityHostOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<AuthSecurityHostOptions>, AuthSecurityOptionsValidator>();
builder.Services.AddSingleton<AuthenticationInstrumentation>();
builder.Services.AddScoped<CurrentAuthenticatedSession>();
builder.Services.AddScoped<Tooba.Identity.Endpoints.Auth.IIdentityHttpSession>(sp => sp.GetRequiredService<CurrentAuthenticatedSession>());
builder.Services.AddSingleton<IAuthenticationThrottleSeam, AuthenticationRateLimitThrottleSeam>();
builder.Services.AddSingleton<Tooba.Identity.Endpoints.Auth.IIdentityAuthThrottle>(sp => sp.GetRequiredService<IAuthenticationThrottleSeam>());
builder.Services.AddSingleton<IIntegrationEventSerializer, JsonIntegrationEventSerializer>();
builder.Services.AddSingleton<IOutboxDispatcherStore, NpgsqlOutboxDispatcherStore>();
builder.Services.AddSingleton<IOutboxPollTargetSource, ConfiguredOutboxPollTargetSource>();
builder.Services.AddSingleton<WorkerCommerceContextFactory>();
builder.Services.AddSingleton<IWorkerCommerceContextFactory>(sp => sp.GetRequiredService<WorkerCommerceContextFactory>());
builder.Services.AddSingleton<WorkerStoreCommerceContextFactory>();
builder.Services.AddSingleton<IWorkerStoreCommerceContextFactory>(sp => sp.GetRequiredService<WorkerStoreCommerceContextFactory>());
builder.Services.AddSingleton<OutboxDispatcher>();
var messagingOptions = new MessagingHostOptions();
builder.Configuration.GetSection("Tooba:Messaging").Bind(messagingOptions);
builder.Services.AddToobaIntegrationPublisher(builder.Environment, messagingOptions);
builder.Services.AddScoped<OutboxSaveChangesInterceptor>();
builder.Services.AddHostedService<OutboxDispatcherHostedService>();
builder.Services.AddToobaCqrsFoundation(
    typeof(Tooba.Catalog.Application.StoreLandingPages.Commands.CreateStoreLandingPageCommand).Assembly,
    typeof(Tooba.Fulfillment.Application.Commands.CreateShippingService.CreateShippingServiceCommand).Assembly,
    typeof(Tooba.Offer.Application.Offers.Commands.CreateOffer.CreateOfferCommand).Assembly,
    typeof(Tooba.Settlement.Application.Queries.GetSellerSettlementBalance.GetSellerSettlementBalanceQuery).Assembly,
    typeof(Tooba.Cart.Application.Commands.CreateGuestCart.CreateGuestCartCommand).Assembly,
    typeof(Tooba.Returns.Application.Commands.CreateReturn.CreateReturnCommand).Assembly,
    typeof(Tooba.Notification.Application.Commands.MarkCustomerNotificationRead.MarkCustomerNotificationReadCommand).Assembly,
    typeof(Tooba.Support.Application.Commands.CreateCustomerTicket.CreateCustomerTicketCommand).Assembly,
    typeof(Tooba.Wallet.Application.Commands.RedeemCustomerGiftCard.RedeemCustomerGiftCardCommand).Assembly,
    typeof(Tooba.Payment.Application.Commands.InitiateStorefrontPayment.InitiateStorefrontPaymentCommand).Assembly,
    typeof(Tooba.Promotion.Application.Commands.CreateSellerPromotion.CreateSellerPromotionCommand).Assembly,
    typeof(Tooba.Order.Application.Admin.Completeness.Queries.ListAdminOrderNotes.ListAdminOrderNotesQuery).Assembly,
    typeof(Tooba.AccessControl.Application.Bootstrap.Commands.EnsureAccessControlBootstrapCommand).Assembly,
    typeof(Tooba.Identity.Application.Auth.Commands.RegisterAuthUserCommand).Assembly,
    typeof(Tooba.Media.Application.Assets.Commands.UploadMediaAssetCommand).Assembly,
    typeof(Tooba.Localization.Application.Languages.Commands.CreateLanguageCommand).Assembly,
    typeof(Tooba.AddressBook.Application.Ports.IAddressBookDirectory).Assembly,
    typeof(Tooba.Wishlist.Application.Customer.Commands.AddWishlistItemCommand).Assembly,
    typeof(Tooba.Story.Application.Stories.Queries.Storefront.GetPublicStoriesQuery).Assembly,
    typeof(Tooba.PageComposition.Application.Storefront.Queries.GetHomeCompositionQuery).Assembly,
    typeof(Tooba.Reviews.Application.Queries.GetPublishedReviewsQuery).Assembly,
    typeof(Tooba.UserPreference.Application.LocalePreferences.Commands.UpsertUserPreferenceCommand).Assembly,
    typeof(Tooba.OperatorProfile.Application.Admin.Commands.UpsertOperatorProfileCommand).Assembly,
    typeof(Tooba.ProductQnA.Application.Customer.Commands.SubmitProductQuestionCommand).Assembly,
    typeof(Tooba.BulkInquiry.Application.Storefront.Commands.SubmitBulkInquiryCommand).Assembly,
    typeof(Tooba.CustomerProfile.Application.Queries.GetCustomerProfilePage.GetCustomerProfilePageQuery).Assembly,
    typeof(Tooba.Content.Application.Articles.Commands.CreateArticleCommand).Assembly,
    typeof(Tooba.ProductWorkspace.Application.Composition.Queries.GetProductWorkspaceQuery).Assembly,
    typeof(Tooba.Party.Application.Seller.Queries.GetSellerSettingsQuery).Assembly);
builder.Services.AddScoped<IOrderAdminAuthorizer, HostOrderAdminAuthorizer>();
builder.Services.AddScoped<
    Tooba.Order.Contracts.Admin.Operations.IOrderAdminEffectiveAccessReader,
    Tooba.Host.Admin.Access.Authorizers.HostOrderAdminEffectiveAccessReader>();
builder.Services.AddToobaModules(builder.Configuration, builder.Environment);
builder.Services.AddOrderReservationCycleBoundaries(builder.Configuration);
builder.Services.AddOfferModuleCallTracing();
builder.Services.Configure<Tooba.Order.Application.ReservationCycle.Contracts.ReservationCycleOptions>(
    builder.Configuration.GetSection(Tooba.Order.Application.ReservationCycle.Contracts.ReservationCycleOptions.SectionName));
builder.Services.AddScoped<Tooba.Order.Contracts.Storefront.IOrderStorefrontActor, Tooba.Host.Order.HostOrderStorefrontActor>();
builder.Services.AddScoped<Tooba.Order.Contracts.Storefront.IOrderStorefrontCheckoutIdentityGate, Tooba.Host.Order.HostOrderStorefrontCheckoutIdentityGate>();
builder.Services.AddScoped<Tooba.AddressBook.Contracts.Ports.IAddressBookCheckoutLookup>(sp => sp.GetRequiredService<Tooba.AddressBook.Application.Ports.IAddressBookDirectory>());

builder.Services.AddMemoryCache();
builder.Services.AddScoped(sp =>
    new Tooba.Host.Security.Checkout.CheckoutIdentityGate(
        sp.GetRequiredService<Tooba.Catalog.Contracts.Checkout.ICatalogCheckoutIdentityPolicyLookup>(),
        sp.GetRequiredService<CurrentAuthenticatedSession>()));
builder.Services.AddScoped<Tooba.BuildingBlocks.Security.ICurrentAuthenticatedUser, HostCurrentAuthenticatedUser>();
builder.Services.AddScoped<Tooba.BuildingBlocks.Security.IAdminPanelAccess, Tooba.Host.Admin.Access.HostAdminPanelAccess>();
builder.Services.AddScoped<Tooba.BuildingBlocks.Security.ISellerPanelAccess, Tooba.Host.Security.Seller.HostSellerPanelAccess>();
builder.Services.AddScoped<Tooba.BuildingBlocks.Security.IPlatformEffectiveAccessReader, Tooba.AccessControl.Infrastructure.Adapters.Security.PlatformEffectiveAccessReader>();
builder.Services.AddScoped<Tooba.Payment.Application.Orchestration.StorefrontPaymentOrchestrator>();
builder.Services.AddScoped<Tooba.Payment.Contracts.Ports.ICheckoutActorPolicyPort, Tooba.Host.Security.Checkout.HostCheckoutActorPolicyAdapter>();
builder.Services.AddScoped<Tooba.Payment.Endpoints.Storefront.IPaymentStorefrontAuthorizer, Tooba.Host.Security.Payment.HostPaymentStorefrontAuthorizer>();
builder.Services.AddScoped<Tooba.Payment.Endpoints.Admin.IPaymentAdminAuthorizer, Tooba.Host.Admin.Access.Authorizers.HostPaymentAdminAuthorizer>();
builder.Services.AddScoped<Tooba.UserPreference.Endpoints.Admin.IUserPreferenceAdminAuthorizer, HostUserPreferenceAdminAuthorizer>();
builder.Services.AddScoped<Tooba.OperatorProfile.Endpoints.Admin.IOperatorProfileAdminAuthorizer, HostOperatorProfileAdminAuthorizer>();
builder.Services.AddScoped<Tooba.Localization.Endpoints.Admin.ILocalizationAdminAuthorizer, HostLocalizationAdminAuthorizer>();
builder.Services.AddScoped<Tooba.Promotion.Endpoints.Seller.IPromotionSellerAuthorizer, Tooba.Host.Security.Seller.HostPromotionSellerAuthorizer>();
builder.Services.AddScoped<Tooba.Offer.Endpoints.Seller.IOfferSellerAuthorizer, Tooba.Host.Security.Seller.HostOfferSellerAuthorizer>();
builder.Services.AddScoped<Tooba.Catalog.Endpoints.Seller.ICatalogSellerAuthorizer, Tooba.Host.Security.Seller.HostCatalogSellerAuthorizer>();
builder.Services.AddScoped<Tooba.Story.Endpoints.Seller.IStorySellerAuthorizer, Tooba.Host.Security.Seller.HostStorySellerAuthorizer>();
builder.Services.AddScoped<Tooba.Reviews.Endpoints.Seller.IReviewsSellerAuthorizer, Tooba.Host.Security.Seller.HostReviewsSellerAuthorizer>();
builder.Services.AddScoped<Tooba.Settlement.Endpoints.Seller.ISettlementSellerAuthorizer, Tooba.Host.Security.Seller.HostSettlementSellerAuthorizer>();
builder.Services.AddScoped<Tooba.Settlement.Endpoints.Admin.ISettlementAdminAuthorizer, Tooba.Host.Admin.Access.Authorizers.HostSettlementAdminAuthorizer>();
builder.Services.AddScoped<Tooba.Order.Endpoints.Seller.IOrderSellerAuthorizer, Tooba.Host.Security.Seller.HostOrderSellerAuthorizer>();
builder.Services.AddScoped<Tooba.Returns.Endpoints.Seller.IReturnSellerAuthorizer, Tooba.Host.Security.Seller.HostReturnSellerAuthorizer>();
builder.Services.AddScoped<Tooba.Returns.Endpoints.Admin.IReturnAdminAuthorizer, Tooba.Host.Admin.Access.Authorizers.HostReturnAdminAuthorizer>();
builder.Services.AddScoped<Tooba.Notification.Endpoints.Seller.INotificationSellerAuthorizer, Tooba.Host.Security.Seller.HostNotificationSellerAuthorizer>();
builder.Services.AddScoped<Tooba.Support.Endpoints.Seller.ISupportSellerAuthorizer, Tooba.Host.Security.Seller.HostSupportSellerAuthorizer>();
builder.Services.AddScoped<Tooba.Support.Endpoints.Admin.ISupportAdminAuthorizer, Tooba.Host.Admin.Access.Authorizers.HostSupportAdminAuthorizer>();
builder.Services.AddScoped<Tooba.Wallet.Endpoints.Admin.IWalletAdminAuthorizer, Tooba.Host.Admin.Access.Authorizers.HostWalletAdminAuthorizer>();
builder.Services.AddScoped<Tooba.Party.Endpoints.Seller.IPartySellerAuthorizer, Tooba.Host.Security.Seller.HostPartySellerAuthorizer>();
builder.Services.AddScoped<Tooba.Host.Admin.Panel.AdminPanelComposer>();
builder.Services.AddCatalogEndpointPresentation();
builder.Services.AddProductWorkspaceEndpointPresentation();
builder.Services.AddContentEndpointPresentation();
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
            options.KnownProxies.Add(IPAddress.Parse(proxy));
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
        // Host فقط ترکیب است: مهاجرت schema و دانه‌های Development در مالکیت ماژول‌ها اجرا می‌شوند.
        if (catalogDemoOptions.RunLegacyBootstraps)
        {
            await DevelopmentSchemaMigrator.ApplyAsync(app.Services);
            await CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync(app.Services, CancellationToken.None);
        }
        else
        {
            app.Logger.LogInformation(
                "Legacy Catalog Development bootstraps skipped (Tooba:CatalogDemo:RunLegacyBootstraps=false). Use POST /v1/admin/catalog/demo/reset-and-seed.");
            await DevelopmentSchemaMigrator.MigrateSchemaOnlyAsync(app.Services);
            // TB-P08-T009: Content demo بدون Catalog legacy.
            try
            {
                await ContentDevelopmentSeedHost.ApplyAsync(app.Services);
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "ContentDevelopmentSeed failed; Host continues without Content demo snapshot.");
            }
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

// Forwarded headers before IP-dependent context; correlation wraps exception pipeline so ProblemDetails join works.
if (trustedProxies.Length > 0)
{
    app.UseForwardedHeaders();
}

app.UseToobaCorrelationId();
app.UseExceptionHandler();

app.UseCors("ToobaCors");
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<SessionAuthenticationMiddleware>();
app.UseMiddleware<RequestObservabilityEnrichmentMiddleware>();

app.MapIdentityModuleEndpoints(enableCors: true);
app.MapProductWorkspaceModuleEndpoints();
app.MapAdminPanelEndpoints();
app.MapAdminDevContextEndpoints();
app.MapOrderEndpoints();
app.MapCartEndpoints();
app.MapPaymentEndpoints();
app.MapOfferModule();
app.MapTaxModule();
app.MapPricingModule();
app.MapPartyEndpoints();
app.MapCustomerProfileModuleEndpoints();
app.MapUserPreferenceModuleEndpoints();
app.MapOperatorProfileModuleEndpoints();
app.MapLocalizationModuleEndpoints();
app.MapReviewsModuleEndpoints();
app.MapProductQnAModuleEndpoints();
app.MapBulkInquiryModuleEndpoints();
app.MapWishlistModuleEndpoints();
app.MapAddressBookModuleEndpoints();
app.MapFulfillmentEndpoints();
app.MapReturnEndpoints();
app.MapSettlementEndpoints();
app.MapSupportEndpoints();
app.MapWalletEndpoints();
app.MapNotificationEndpoints();
app.MapAccessControlModuleEndpoints();
app.MapCatalogModuleEndpoints();
app.MapContentModuleEndpoints();
app.MapMediaModuleEndpoints();
app.MapPageCompositionModuleEndpoints();
app.MapStoryModuleEndpoints();
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

