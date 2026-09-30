using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FluentValidation;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Attributes.Definitions.Ports;
using Tooba.Catalog.Application.Attributes.ProductValues.Ports;
using Tooba.Catalog.Application.Attributes.Schema.Ports;
using Tooba.Catalog.Application.Categories.Ports;
using Tooba.Catalog.Application.Facets.Ports;
using Tooba.Catalog.Application.MegaMenu.Ports;
using Tooba.Catalog.Application.Settings.Quantity.Ports;
using Tooba.Catalog.Application.Settings.CheckoutAbuse.Ports;
using Tooba.Catalog.Application.Settings.CheckoutIdentity.Ports;
using Tooba.Catalog.Application.Settings.StoreAppearance.Ports;
using Tooba.Catalog.Application.Development;
using Tooba.Catalog.Application.Development.CatalogDemo;
using Tooba.Catalog.Application.Tags.Ports;
using Tooba.Catalog.Application.Units.Ports;
using Tooba.Catalog.Application.Variants.Ports;
using Tooba.Catalog.Application.CategoryChanges.Ports;
using Tooba.Catalog.Application.ProductMedia.Ports;
using Tooba.Catalog.Application.Brands.Ports;
using Tooba.Catalog.Application.StoreLandingPages.Ports;
using Tooba.Catalog.Application.StoreMenus.Ports;
using Tooba.Catalog.Application.ProductHistory.Ports;
using Tooba.Catalog.Application.ProductPublishing.Ports;
using Tooba.Catalog.Application.ProductDeletion.Ports;
using Tooba.Catalog.Application.ProductIdentity.Ports;
using Tooba.Catalog.Application.ProductTaxonomy.Ports;
using Tooba.Catalog.Application.ProductSeo.Ports;
using Tooba.Catalog.Application.Seller.Ports;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Checkout;
using Tooba.Catalog.Contracts.Reservation;
using Tooba.Catalog.Infrastructure.Adapters;
using Tooba.Catalog.Infrastructure.Checkout;
using Tooba.Catalog.Application.TemplateCatalog.Ports;
using Tooba.Catalog.Application.Storefront.Ports;
using Tooba.Catalog.Infrastructure.Development;
using Tooba.Catalog.Infrastructure.Development.TemplateCatalog;
using Tooba.Catalog.Infrastructure.Development.CatalogDemo;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Catalog.Infrastructure.Reservation;
using Tooba.Catalog.Infrastructure.Storefront;
using Tooba.ModuleContracts;
using Tooba.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Tooba.Catalog.Infrastructure;

/// <summary>
/// ماژول Catalog: حقیقت توصیفی محصول. قیمت، موجودی، Offer و UI تجاری اینجا نیست.
/// </summary>
public sealed class CatalogModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Catalog";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddSingleton<IQuantityNormalizer, QuantityNormalizer>();
        services.AddSingleton<IOutboxModuleRegistration, CatalogOutboxRegistration>();
        services.AddScoped<ICatalogUseCaseGuard, OpenCatalogUseCaseGuard>();
        services.AddScoped<ICatalogActorContext, CatalogActorContext>();
        services.AddScoped<ICatalogDirectory, CatalogDirectory>();
        services.AddScoped<ICatalogLookupGateway>(sp => (CatalogDirectory)sp.GetRequiredService<ICatalogDirectory>());
        services.AddScoped<IAccessControlScopeResourceLookup, CatalogAccessControlScopeResourceLookup>();
        services.AddScoped<ICatalogVariantLookup>(sp => (CatalogDirectory)sp.GetRequiredService<ICatalogDirectory>());
        services.AddScoped<ICatalogCartQuantityPolicyGateway>(sp => (CatalogDirectory)sp.GetRequiredService<ICatalogDirectory>());
        services.AddScoped<ICatalogCartPresentationLookup>(sp => (CatalogDirectory)sp.GetRequiredService<ICatalogDirectory>());
        services.AddScoped<Tooba.Catalog.Contracts.Checkout.ICatalogCheckoutLookup>(sp => (CatalogDirectory)sp.GetRequiredService<ICatalogDirectory>());
        services.AddScoped<ICatalogOfferReadGateway, CatalogOfferReadGateway>();
        services.AddScoped<ICatalogAdminProductCountGateway, CatalogAdminProductCountGateway>();
        services.AddScoped<ICatalogAdminProductWorkspaceReadGateway, CatalogAdminProductWorkspaceReadGateway>();
        services.AddScoped<ICatalogAdminProductWorkspaceListGateway, CatalogAdminProductWorkspaceListGateway>();
        services.AddScoped<ICatalogAdminProductTitleIdLookup, Admin.CatalogAdminProductTitleIdLookup>();
        services.AddScoped<IStoreCheckoutAbuseSettingsReader, StoreCheckoutAbuseSettingsReader>();
        services.AddScoped<ICatalogCheckoutIdentityPolicyLookup, Checkout.CatalogCheckoutIdentityPolicyLookup>();
        services.AddScoped<IReservationCycleHoldPolicyReader, ReservationCycleHoldPolicyReader>();
        services.AddScoped<IStoreReservationPolicySettingsPort, StoreReservationPolicySettingsPort>();
        services.AddScoped<IStoreHoldPolicySettingsPort, StoreHoldPolicySettingsPort>();
        services.AddScoped<IStoreHoldPolicyHoursReader>(sp => sp.GetRequiredService<IStoreHoldPolicySettingsPort>());
        services.AddScoped<IStoreCartPersistenceHoursReader, StoreCartPersistenceHoursReader>();
        services.AddScoped<IStoreLandingPageDirectory, StoreLandingPageDirectory>();
        services.AddScoped<IStoreLandingPageWorkspace, StoreLandingPageWorkspace>();
        services.AddScoped<IStoreMenuDirectory, StoreMenuDirectory>();
        services.AddScoped<IStoreMenuWorkspace, StoreMenuWorkspace>();
        services.AddScoped<IStoreAppearanceSettingsDirectory, StoreAppearanceSettingsDirectory>();
        services.AddScoped<IStoreAppearanceProjector, StoreAppearance.StoreAppearanceProjector>();
        services.AddScoped<StoreAppearance.StoreAppearanceProjector>(sp =>
            (StoreAppearance.StoreAppearanceProjector)sp.GetRequiredService<IStoreAppearanceProjector>());
        services.AddScoped<IStoreQuantitySettingsDirectory, StoreQuantitySettingsDirectory>();
        services.AddScoped<IStoreCheckoutAbuseSettingsDirectory, StoreCheckoutAbuseSettingsDirectory>();
        services.AddScoped<IStoreCheckoutIdentitySettingsDirectory, StoreCheckoutIdentitySettingsDirectory>();
        services.AddScoped<IUnitOfMeasureDirectory, UnitOfMeasureDirectory>();
        services.AddScoped<ITagDirectory, TagDirectory>();
        services.AddScoped<IMegaMenuDirectory, MegaMenuDirectory>();
        services.AddScoped<IFacetDirectory, FacetDirectory>();
        services.AddScoped<ICategoryDirectory, CategoryDirectory>();
        services.AddScoped<IAttributeDefinitionDirectory, AttributeDefinitionDirectory>();
        services.AddScoped<ICategoryAttributeSchemaDirectory, CategoryAttributeSchemaDirectory>();
        services.AddScoped<IProductAttributeDirectory, ProductAttributeDirectory>();
        services.AddScoped<IProductVariantDirectory, ProductVariantDirectory>();
        services.AddScoped<ICategoryChangeDirectory, CategoryChangeDirectory>();
        services.AddScoped<IProductMediaDirectory, ProductMediaDirectory>();
        services.AddScoped<IProductSeoDirectory, ProductSeoDirectory>();
        services.AddScoped<IProductHistoryReader, ProductHistoryReader>();
        services.AddScoped<IProductPublishReadinessReader, ProductPublishReadinessReader>();
        services.AddScoped<IProductLifecycleDirectory, ProductLifecycleDirectory>();
        services.AddScoped<IProductDeletionDirectory, ProductDeletionDirectory>();
        services.AddScoped<IProductIdentityDirectory, ProductIdentityDirectory>();
        services.AddScoped<IProductTaxonomyDirectory, ProductTaxonomyDirectory>();
        services.AddScoped<IBrandOptionReader, BrandOptionReader>();
        services.AddScoped<IVariantOfferLookup, VariantOfferLookupAdapter>();
        services.AddScoped<ISellerCatalogVariantDirectory, Seller.SellerCatalogVariantDirectory>();
        services.AddScoped<IFashionTemplatePreviewReader, FashionTemplatePreviewQuery>();
        services.AddScoped<IIndustryTemplatePreviewReader, IndustryTemplatePreviewQuery>();
        services.AddScoped<IStorefrontComposer, StorefrontComposer>();
        services.Configure<CatalogDemoSeedOptions>(
            configuration.GetSection(CatalogDemoSeedOptions.SectionName));
        services.AddScoped<CatalogDemoMediaFactory>();
        services.AddScoped<CatalogDemoResetService>();
        services.AddScoped<CatalogDemoAssignmentIntegrityService>();
        services.AddScoped<CatalogDemoProductSeedService>();
        services.AddScoped<CatalogDemoSeedService>();
        services.AddScoped<ICatalogDemoResetAndSeedGateway, CatalogDemoResetAndSeedHost>();
        services.AddScoped<ICatalogAttributeSchemaSellableEnricher, CatalogAttributeSchemaSellableEnricher>();
        services.AddScoped<WorkspaceDemoProductSeed>();
        services.AddScoped<WorkspaceDemoMarketplaceSeed>();
        services.AddScoped<IWorkspaceDemoSeed, WorkspaceDemoSeed>();
        services.AddSingleton<IQuantityNormalizer, QuantityNormalizer>();
        services.AddValidatorsFromAssembly(typeof(CreateStoreLandingPageCommand).Assembly);
        services.AddModuleSchemaMigrator<CatalogDbContext>("Catalog", ModuleSchemaMigrationOrder.Catalog);
        services.AddModuleSchemaMigrationStep(
            "Catalog",
            ModuleSchemaMigrationOrder.Catalog,
            static (sp, ct) => CatalogDevelopmentSeed.PostMigration.EnsureAsync(sp, ct));
        services.AddDbContext<CatalogDbContext>((sp, options) =>
        {
            var connectionString = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connectionString,
                CatalogDbContext.Schema,
                typeof(CatalogDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
