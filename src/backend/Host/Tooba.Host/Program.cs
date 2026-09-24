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
using Tooba.Host.Admin;
using Tooba.Host.Localization;
using Tooba.Localization.Application;
using Tooba.Host.Admin.CatalogDemo;
using Tooba.Host.Customer;
using Tooba.Host.Seller;
using Tooba.Returns.Endpoints;
using Tooba.Notification.Endpoints;
using Tooba.Host.AccessControl;
using Tooba.Payment.Endpoints;
using Tooba.Promotion.Endpoints;
using Tooba.Host.Storefront;
using Tooba.Host.Reviews;
using Tooba.Host.ProductQnA;
using Tooba.Host.Wishlist;
using Tooba.Host.AddressBook;
using Tooba.Host.Content;
using Tooba.Content.Infrastructure;
using Tooba.Host.Media;
using Tooba.Host.PageComposition;
using Tooba.Host.Story;
using Tooba.Host.Preferences;
using Tooba.Host.OperatorProfile;
using Tooba.Host.Support;
using Tooba.Support.Endpoints;
using Tooba.Host.Wallet;
using Tooba.Wallet.Endpoints;
using Tooba.Offer.Endpoints;
using Tooba.Offer.Endpoints.Seller;
using Tooba.Cart.Endpoints;
using Tooba.Settlement.Endpoints;
using Tooba.Fulfillment.Endpoints;
using Tooba.Offer.Infrastructure.Adapters;
using Tooba.Tax.Endpoints;
using Tooba.Pricing.Endpoints;
using Tooba.Order.Endpoints;
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
builder.Services.Configure<OutboxHostOptions>(builder.Configuration.GetSection("Tooba:Outbox"));
builder.Services.Configure<PaymentReconciliationHostOptions>(builder.Configuration.GetSection("Tooba:PaymentReconciliation"));
builder.Services.Configure<UnpaidOrderExpiryHostOptions>(builder.Configuration.GetSection("Tooba:UnpaidOrderExpiry"));
builder.Services.AddSingleton<BackgroundWorkerRegistry>();
builder.Services.AddSingleton<IBackgroundWorkerRegistry>(sp => sp.GetRequiredService<BackgroundWorkerRegistry>());
builder.Services.AddScoped<ILanguageReferenceGuard, ContentLanguageReferenceGuard>();
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
builder.Services.AddSingleton<IWorkerCommerceContextFactory>(sp => sp.GetRequiredService<WorkerCommerceContextFactory>());
builder.Services.AddSingleton<WorkerStoreCommerceContextFactory>();
builder.Services.AddSingleton<IWorkerStoreCommerceContextFactory>(sp => sp.GetRequiredService<WorkerStoreCommerceContextFactory>());
builder.Services.AddSingleton<OutboxDispatcher>();
var messagingOptions = new MessagingHostOptions();
builder.Configuration.GetSection("Tooba:Messaging").Bind(messagingOptions);
builder.Services.AddToobaIntegrationPublisher(builder.Environment, messagingOptions);
builder.Services.AddScoped<OutboxSaveChangesInterceptor>();
builder.Services.AddHostedService<OutboxDispatcherHostedService>();
builder.Services.AddHostedService<PaymentReconciliationHostedService>();
builder.Services.AddHostedService<UnpaidOrderExpiryHostedService>();
builder.Services.AddToobaCqrsFoundation(
    typeof(Tooba.Catalog.Application.CreateStoreLandingPageCommand).Assembly,
    typeof(Tooba.Fulfillment.Application.Commands.CreateShippingService.CreateShippingServiceCommand).Assembly,
    typeof(Tooba.Offer.Application.Commands.CreateOffer.CreateOfferCommand).Assembly,
    typeof(Tooba.Settlement.Application.Queries.GetSellerSettlementBalance.GetSellerSettlementBalanceQuery).Assembly,
    typeof(Tooba.Cart.Application.Commands.CreateGuestCart.CreateGuestCartCommand).Assembly,
    typeof(Tooba.Returns.Application.Commands.CreateReturn.CreateReturnCommand).Assembly,
    typeof(Tooba.Notification.Application.Commands.MarkCustomerNotificationRead.MarkCustomerNotificationReadCommand).Assembly,
    typeof(Tooba.Support.Application.Commands.CreateCustomerTicket.CreateCustomerTicketCommand).Assembly,
    typeof(Tooba.Wallet.Application.Commands.RedeemCustomerGiftCard.RedeemCustomerGiftCardCommand).Assembly,
    typeof(Tooba.Payment.Application.Commands.InitiateStorefrontPayment.InitiateStorefrontPaymentCommand).Assembly,
    typeof(Tooba.Promotion.Application.Commands.CreateSellerPromotion.CreateSellerPromotionCommand).Assembly,
    typeof(Tooba.Order.Application.Admin.Completeness.Queries.ListAdminOrderNotes.ListAdminOrderNotesQuery).Assembly);
builder.Services.AddScoped<IOrderAdminAuthorizer, HostOrderAdminAuthorizer>();
builder.Services.AddScoped<
    Tooba.Order.Application.Admin.Operations.Ports.IOrderAdminEffectiveAccessReader,
    Tooba.Host.Admin.HostOrderAdminEffectiveAccessReader>();
builder.Services.AddScoped<Tooba.Catalog.Application.IStoreLandingExternalReferenceGate, Tooba.Host.Admin.MerchandisingStoreLandingReferenceGate>();
builder.Services.AddScoped<Tooba.Catalog.Application.IUnitOfMeasureLanguageGate, Tooba.Host.Admin.HostUnitOfMeasureLanguageGate>();
builder.Services.AddToobaModules(builder.Configuration, builder.Environment);
builder.Services.AddOfferModuleCallTracing();
builder.Services.Configure<Tooba.Order.Application.ReservationCycle.Contracts.ReservationCycleOptions>(
    builder.Configuration.GetSection(Tooba.Order.Application.ReservationCycle.Contracts.ReservationCycleOptions.SectionName));
builder.Services.AddScoped<CommerceHoldPolicy>();
builder.Services.AddScoped<Tooba.Payment.Contracts.Hold.ICommerceHoldPolicySource>(sp => sp.GetRequiredService<CommerceHoldPolicy>());
builder.Services.AddScoped<Tooba.Order.Application.Checkout.Contracts.ICheckoutReservationHoldPolicy>(sp => sp.GetRequiredService<CommerceHoldPolicy>());
builder.Services.AddScoped<Tooba.Host.Admin.ProductWorkspaceComposer>();
builder.Services.AddScoped<Tooba.Host.Grid.AdminContentGridQueryEngine>();
builder.Services.AddScoped<Tooba.Host.Grid.AdminStoryGridQueryEngine>();
builder.Services.AddScoped<Tooba.Host.Grid.AdminReviewGridQueryEngine>();
builder.Services.AddScoped<Tooba.Host.Grid.AdminSellersGridQueryEngine>();
builder.Services.Configure<CatalogDemoSeedOptions>(
    builder.Configuration.GetSection(CatalogDemoSeedOptions.SectionName));
builder.Services.AddScoped<CatalogDemoMediaFactory>();
builder.Services.AddScoped<CatalogDemoResetService>();
builder.Services.AddScoped<CatalogDemoAssignmentIntegrityService>();
builder.Services.AddScoped<CatalogDemoProductSeedService>();
builder.Services.AddScoped<CatalogDemoSeedService>();
builder.Services.AddScoped<CatalogDemoResetAndSeedHost>();
builder.Services.AddScoped<Tooba.Host.Storefront.StorefrontComposer>();
builder.Services.AddScoped<Tooba.Host.Storefront.FashionTemplatePreviewQuery>();
builder.Services.AddScoped<Tooba.Host.Storefront.IndustryTemplatePreviewQuery>();
builder.Services.AddScoped<Tooba.Order.Application.Storefront.Ports.IOrderStorefrontActor, Tooba.Host.Order.HostOrderStorefrontActor>();
builder.Services.AddScoped<Tooba.Order.Application.Storefront.Ports.IOrderStorefrontCheckoutIdentityGate, Tooba.Host.Order.HostOrderStorefrontCheckoutIdentityGate>();
builder.Services.AddScoped<Tooba.AddressBook.Contracts.IAddressBookCheckoutLookup>(sp => sp.GetRequiredService<Tooba.AddressBook.Application.IAddressBookDirectory>());

builder.Services.AddMemoryCache();
builder.Services.AddScoped<Tooba.Host.Storefront.StoreAppearanceProjector>();
builder.Services.AddScoped<Tooba.Host.Admin.StoreAppearanceSettingsComposer>();
builder.Services.AddScoped<Tooba.Host.Admin.MerchandisingCampaignAdminComposer>();
builder.Services.AddScoped<Tooba.Host.Admin.StoreLandingPageComposer>();
builder.Services.AddScoped<Tooba.Host.Admin.StoreMenuComposer>();
builder.Services.AddScoped(sp =>
    new CheckoutIdentityGate(
        sp.GetRequiredService<Tooba.Catalog.Infrastructure.Persistence.CatalogDbContext>(),
        sp.GetRequiredService<CurrentAuthenticatedSession>()));
builder.Services.AddScoped<Tooba.BuildingBlocks.Security.ICurrentAuthenticatedUser, HostCurrentAuthenticatedUser>();
builder.Services.AddScoped<Tooba.Payment.Application.Orchestration.StorefrontPaymentOrchestrator>();
builder.Services.AddScoped<Tooba.Payment.Application.Ports.ICheckoutActorPolicyPort, Tooba.Host.Storefront.HostCheckoutActorPolicyAdapter>();
builder.Services.AddScoped<Tooba.Payment.Endpoints.Storefront.IPaymentStorefrontAuthorizer, Tooba.Host.Storefront.HostPaymentStorefrontAuthorizer>();
builder.Services.AddScoped<Tooba.Payment.Endpoints.Admin.IPaymentAdminAuthorizer, Tooba.Host.Admin.HostPaymentAdminAuthorizer>();
builder.Services.AddScoped<Tooba.Payment.Endpoints.Admin.IPaymentAdminGridQueryNormalizer, Tooba.Host.Admin.HostPaymentAdminGridQueryNormalizer>();
builder.Services.AddScoped<Tooba.Promotion.Endpoints.Seller.IPromotionSellerAuthorizer, Tooba.Host.Seller.HostPromotionSellerAuthorizer>();
builder.Services.AddScoped<Tooba.Promotion.Endpoints.Admin.IPromotionAdminAuthorizer, Tooba.Host.Admin.HostPromotionAdminAuthorizer>();
builder.Services.AddScoped<Tooba.Host.Seller.SellerPanelComposer>();
builder.Services.AddScoped<Tooba.Offer.Endpoints.Seller.IOfferSellerAuthorizer, Tooba.Host.Seller.HostOfferSellerAuthorizer>();
builder.Services.AddScoped<Tooba.Settlement.Endpoints.Seller.ISettlementSellerAuthorizer, Tooba.Host.Seller.HostSettlementSellerAuthorizer>();
builder.Services.AddScoped<Tooba.Settlement.Endpoints.Admin.ISettlementAdminAuthorizer, Tooba.Host.Admin.HostSettlementAdminAuthorizer>();
builder.Services.AddScoped<Tooba.Fulfillment.Endpoints.Seller.IFulfillmentSellerAuthorizer, Tooba.Host.Seller.HostFulfillmentSellerAuthorizer>();
builder.Services.AddScoped<Tooba.Fulfillment.Endpoints.Admin.IFulfillmentAdminAuthorizer, Tooba.Host.Admin.HostFulfillmentAdminAuthorizer>();
builder.Services.AddScoped<Tooba.Fulfillment.Endpoints.Customer.IFulfillmentCustomerAuthorizer, Tooba.Host.Customer.HostFulfillmentCustomerAuthorizer>();
builder.Services.AddScoped<Tooba.Order.Endpoints.Customer.IOrderCustomerAuthorizer, Tooba.Host.Customer.HostOrderCustomerAuthorizer>();
builder.Services.AddScoped<Tooba.Order.Endpoints.Seller.IOrderSellerAuthorizer, Tooba.Host.Seller.HostOrderSellerAuthorizer>();
builder.Services.AddScoped<Tooba.Order.Application.Seller.Ports.ISellerOrderViewAccessReader, Tooba.Host.Seller.HostSellerOrderViewAccessReader>();
builder.Services.AddScoped<Tooba.Returns.Endpoints.Customer.IReturnCustomerAuthorizer, Tooba.Host.Customer.HostReturnCustomerAuthorizer>();
builder.Services.AddScoped<Tooba.Returns.Endpoints.Seller.IReturnSellerAuthorizer, Tooba.Host.Seller.HostReturnSellerAuthorizer>();
builder.Services.AddScoped<Tooba.Returns.Endpoints.Admin.IReturnAdminAuthorizer, Tooba.Host.Admin.HostReturnAdminAuthorizer>();
builder.Services.AddScoped<Tooba.Notification.Endpoints.Customer.INotificationCustomerAuthorizer, Tooba.Host.Customer.HostNotificationCustomerAuthorizer>();
builder.Services.AddScoped<Tooba.Notification.Endpoints.Seller.INotificationSellerAuthorizer, Tooba.Host.Seller.HostNotificationSellerAuthorizer>();
builder.Services.AddScoped<Tooba.Support.Endpoints.Customer.ISupportCustomerAuthorizer, Tooba.Host.Customer.HostSupportCustomerAuthorizer>();
builder.Services.AddScoped<Tooba.Support.Endpoints.Seller.ISupportSellerAuthorizer, Tooba.Host.Seller.HostSupportSellerAuthorizer>();
builder.Services.AddScoped<Tooba.Support.Endpoints.Admin.ISupportAdminAuthorizer, Tooba.Host.Admin.HostSupportAdminAuthorizer>();
builder.Services.AddScoped<Tooba.Wallet.Endpoints.Customer.IWalletCustomerAuthorizer, Tooba.Host.Customer.HostWalletCustomerAuthorizer>();
builder.Services.AddScoped<Tooba.Wallet.Endpoints.Admin.IWalletAdminAuthorizer, Tooba.Host.Admin.HostWalletAdminAuthorizer>();
builder.Services.AddScoped<Tooba.Host.Customer.CustomerPanelComposer>();
builder.Services.AddScoped<Tooba.Host.Admin.AdminPanelComposer>();
builder.Services.AddScoped<Tooba.Host.Wishlist.WishlistComposer>();
builder.Services.AddScoped<Tooba.Host.Content.ContentPanelComposer>();
builder.Services.AddScoped<Tooba.Host.Content.ContentAuthorPanelComposer>();
builder.Services.AddScoped<Tooba.Host.Content.ContentArticleMediaPanelComposer>();
builder.Services.AddScoped<Tooba.Content.Application.IContentMediaAssetValidator, Tooba.Host.Content.ContentMediaAssetValidator>();
builder.Services.AddScoped<Tooba.Host.Reviews.ReviewPanelComposer>();
builder.Services.AddScoped<Tooba.Host.PageComposition.PageCompositionPanelComposer>();
builder.Services.AddScoped<Tooba.Host.Story.StoryPanelComposer>();
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
            await ProductWorkspaceDevelopmentBootstrap.MigrateSchemaOnlyAsync(app.Services);
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

        try
        {
            await LandingPageDevelopmentSeedHost.ApplyAsync(app.Services);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "LandingPageDevelopmentSeed failed; Host continues without landing demo pages.");
        }

        try
        {
            await StoreMenuDevelopmentSeedHost.ApplyAsync(app.Services);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "StoreMenuDevelopmentSeed failed; Host continues without demo menu.");
        }

        try
        {
            await FashionTemplateCatalogSeedHost.ApplyAsync(app.Services);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "FashionTemplateCatalogSeed failed; Host continues without Fashion Template Catalog seed.");
        }

        try
        {
            await IndustryBatchATemplateCatalogSeedHost.ApplyAsync(app.Services);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "IndustryBatchATemplateCatalogSeed failed; Host continues without Batch A Template Catalog seed.");
        }

        try
        {
            await IndustryBatchBTemplateCatalogSeedHost.ApplyAsync(app.Services);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "IndustryBatchBTemplateCatalogSeed failed; Host continues without Batch B Template Catalog seed.");
        }

        try
        {
            await IndustryBatchCTemplateCatalogSeedHost.ApplyAsync(app.Services);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "IndustryBatchCTemplateCatalogSeed failed; Host continues without Batch C Template Catalog seed.");
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

app.MapAuthenticationBoundary(enableCors: true);
app.MapProductWorkspaceEndpoints();
app.MapQuantitySettingsEndpoints();
app.MapHoldPolicySettingsEndpoints();
app.MapCheckoutIdentitySettingsEndpoints();
app.MapCheckoutAbuseSettingsEndpoints();
app.MapStoreAppearanceSettingsEndpoints();
app.MapStoreLandingPageEndpoints();
app.MapMerchandisingCampaignAdminEndpoints();
app.MapStoreMenuEndpoints();
app.MapReservationPolicyAdminEndpoints();
app.MapUnitOfMeasureEndpoints();
app.MapCatalogAttributeEndpoints();
app.MapCatalogFacetEndpoints();
app.MapCatalogMegaMenuEndpoints();
app.MapCatalogTagEndpoints();
app.MapCatalogCategoryEndpoints();
app.MapCatalogDemoDevEndpoints();
app.MapAdminPanelEndpoints();
app.MapOrderEndpoints();
app.MapStorefrontEndpoints();
app.MapCartEndpoints();
app.MapPaymentEndpoints();
app.MapSellerPanelEndpoints();
app.MapOfferModule();
app.MapTaxModule();
app.MapPricingModule();
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
app.MapContentCategoryEndpoints();
app.MapContentAuthorEndpoints();
app.MapContentTagEndpoints();
app.MapContentArticleMediaEndpoints();
app.MapContentArticleCommentEndpoints();
app.MapLocaleAdminEndpoints();
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

