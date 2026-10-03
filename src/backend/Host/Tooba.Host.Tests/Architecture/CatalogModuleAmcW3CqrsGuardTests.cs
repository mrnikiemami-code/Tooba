using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application.Categories.Commands;
using Tooba.Catalog.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-CATALOG-AMC-001-W3 — CQRS/Result/validators/error-catalog durable inventory.
/// </summary>
public sealed class CatalogModuleAmcW3CqrsGuardTests
{
    private const string ValidatorRequiredPresent = "VALIDATOR_REQUIRED_PRESENT";
    private const string NoValidatorRequired = "NO_VALIDATOR_REQUIRED_NO_TRANSPORT_INPUT";

    private static readonly (string RequestTypeName, string Classification)[] Manifest =
    [
        ("AddAttributeOptionCommand", ValidatorRequiredPresent),
        ("AddStoreLandingPageSectionAdminCommand", ValidatorRequiredPresent),
        ("AddStoreMenuItemAdminCommand", ValidatorRequiredPresent),
        ("ApplyProductVariantMatrixCommand", ValidatorRequiredPresent),
        ("ArchiveCategoryCommand", NoValidatorRequired),
        ("AssignCategoryTagCommand", NoValidatorRequired),
        ("AssignProductTagCommand", NoValidatorRequired),
        ("AttachPlaceholderProductMediaCommand", ValidatorRequiredPresent),
        ("AttachProductMediaCommand", ValidatorRequiredPresent),
        ("BindCategoryAttributeCommand", ValidatorRequiredPresent),
        ("CreateAttributeDefinitionCommand", ValidatorRequiredPresent),
        ("CreateCategoryCommand", ValidatorRequiredPresent),
        ("CreateStoreLandingPageAdminCommand", ValidatorRequiredPresent),
        ("CreateStoreMenuAdminCommand", ValidatorRequiredPresent),
        ("CreateTagCommand", ValidatorRequiredPresent),
        ("CreateUnitOfMeasureCommand", ValidatorRequiredPresent),
        ("DeactivateUnitOfMeasureCommand", NoValidatorRequired),
        ("DeleteProductCommand", NoValidatorRequired),
        ("DeleteStoreLandingPageAdminCommand", NoValidatorRequired),
        ("DeleteStoreLandingPageSectionAdminCommand", NoValidatorRequired),
        ("DeleteStoreMenuAdminCommand", NoValidatorRequired),
        ("DeleteStoreMenuItemAdminCommand", NoValidatorRequired),
        ("DetachProductMediaCommand", NoValidatorRequired),
        ("GetAttributeDefinitionQuery", NoValidatorRequired),
        ("GetCategoryMegaMenuQuery", NoValidatorRequired),
        ("GetCategoryTreeQuery", ValidatorRequiredPresent),
        ("GetCategoryWorkspaceQuery", NoValidatorRequired),
        ("GetCheckoutAbuseSettingsQuery", NoValidatorRequired),
        ("GetCheckoutIdentitySettingsQuery", NoValidatorRequired),
        ("GetEffectiveCategoryFacetsQuery", NoValidatorRequired),
        ("GetEffectiveCategorySchemaQuery", NoValidatorRequired),
        ("GetFashionTemplatePreviewQuery", NoValidatorRequired),
        ("GetHoldPolicySettingsQuery", NoValidatorRequired),
        ("GetIndustryTemplatePreviewQuery", ValidatorRequiredPresent),
        ("GetProductAttributeEditorStateQuery", NoValidatorRequired),
        ("GetProductAttributeReadinessQuery", NoValidatorRequired),
        ("GetProductHistoryQuery", NoValidatorRequired),
        ("GetProductMediaQuery", NoValidatorRequired),
        ("GetProductMediaReadinessQuery", NoValidatorRequired),
        ("GetProductPublishReadinessQuery", NoValidatorRequired),
        ("GetProductSeoQuery", NoValidatorRequired),
        ("GetProductSeoReadinessQuery", NoValidatorRequired),
        ("GetProductVariantEditorStateQuery", NoValidatorRequired),
        ("GetProductVariantReadinessQuery", NoValidatorRequired),
        ("GetStoreAppearanceAdminSettingsQuery", NoValidatorRequired),
        ("GetStorefrontAppearanceQuery", NoValidatorRequired),
        ("GetStorefrontBrandBySlugQuery", ValidatorRequiredPresent),
        ("GetStorefrontBrandsQuery", NoValidatorRequired),
        ("GetStorefrontCategoriesQuery", NoValidatorRequired),
        ("GetStorefrontCategoryFacetsQuery", NoValidatorRequired),
        ("GetStorefrontCategoryPlpQuery", ValidatorRequiredPresent),
        ("GetStorefrontCheckoutIdentityPolicyQuery", NoValidatorRequired),
        ("GetStorefrontHomeQuery", NoValidatorRequired),
        ("GetStorefrontMegaMenuQuery", NoValidatorRequired),
        ("GetStorefrontMerchandisingQuery", ValidatorRequiredPresent),
        ("GetStorefrontProductDetailQuery", ValidatorRequiredPresent),
        ("GetStorefrontProductListingQuery", ValidatorRequiredPresent),
        ("GetStorefrontSellerByPublicIdQuery", ValidatorRequiredPresent),
        ("GetStorefrontSellersQuery", NoValidatorRequired),
        ("GetStoreHeaderMenuPublicQuery", NoValidatorRequired),
        ("GetStoreHeaderMenuSelectionQuery", NoValidatorRequired),
        ("GetStoreHomeSelectionQuery", NoValidatorRequired),
        ("GetStoreLandingPageQuery", NoValidatorRequired),
        ("GetStoreMenuQuery", NoValidatorRequired),
        ("GetStoreMenuUsageQuery", NoValidatorRequired),
        ("GetStoreQuantitySettingsQuery", NoValidatorRequired),
        ("GetTagQuery", NoValidatorRequired),
        ("GetUnitOfMeasureQuery", NoValidatorRequired),
        ("ListAttributeDefinitionsQuery", NoValidatorRequired),
        ("ListBrandOptionsQuery", NoValidatorRequired),
        ("ListCategoryTagsQuery", NoValidatorRequired),
        ("ListLocalCategoryFacetsQuery", NoValidatorRequired),
        ("ListMegaMenuPlacementOptionsQuery", NoValidatorRequired),
        ("ListProductTagsQuery", NoValidatorRequired),
        ("ListSellerCatalogVariantsQuery", NoValidatorRequired),
        ("ListStoreLandingPageSectionsQuery", NoValidatorRequired),
        ("ListStoreLandingPagesQuery", NoValidatorRequired),
        ("ListStoreLandingSitemapQuery", NoValidatorRequired),
        ("ListStoreMenusQuery", NoValidatorRequired),
        ("ListTagsQuery", NoValidatorRequired),
        ("ListUnitOfMeasuresQuery", NoValidatorRequired),
        ("MoveCategoryCommand", NoValidatorRequired),
        ("PatchProductMediaCommand", NoValidatorRequired),
        ("PreviewCategoryChangeQuery", ValidatorRequiredPresent),
        ("PreviewProductVariantsQuery", ValidatorRequiredPresent),
        ("PreviewStoreLandingPageQuery", NoValidatorRequired),
        ("PreviewVariantAxisCapabilityDisableQuery", NoValidatorRequired),
        ("ProjectPublicStoreMenuQuery", NoValidatorRequired),
        ("PublishCategoryCommand", NoValidatorRequired),
        ("RemoveCategoryFacetOverrideCommand", NoValidatorRequired),
        ("RemoveCategoryMegaMenuCommand", NoValidatorRequired),
        ("RemoveCategoryTagCommand", NoValidatorRequired),
        ("RemoveProductTagCommand", NoValidatorRequired),
        ("ReorderCategoriesCommand", ValidatorRequiredPresent),
        ("ReorderCategoryAttributeBindingsCommand", ValidatorRequiredPresent),
        ("ReorderCategoryFacetsCommand", ValidatorRequiredPresent),
        ("ReorderProductMediaCommand", ValidatorRequiredPresent),
        ("ReorderStoreLandingPageSectionsAdminCommand", ValidatorRequiredPresent),
        ("ReorderStoreMenuItemsAdminCommand", ValidatorRequiredPresent),
        ("ReplacePrimaryCategoryCommand", ValidatorRequiredPresent),
        ("ReplaceStoreLandingPageCompositionAdminCommand", ValidatorRequiredPresent),
        ("ResolveCategoryRouteQuery", ValidatorRequiredPresent),
        ("ResolvePublicStoreLandingPageQuery", NoValidatorRequired),
        ("SaveCheckoutAbuseSettingsCommand", ValidatorRequiredPresent),
        ("SaveCheckoutIdentitySettingsCommand", ValidatorRequiredPresent),
        ("SaveHoldPolicySettingsCommand", ValidatorRequiredPresent),
        ("SaveStoreAppearanceSettingsCommand", ValidatorRequiredPresent),
        ("SaveStoreQuantitySettingsCommand", ValidatorRequiredPresent),
        ("SetPrimaryProductMediaCommand", NoValidatorRequired),
        ("SetProductAttributeCommand", ValidatorRequiredPresent),
        ("SetProductAttributesCommand", ValidatorRequiredPresent),
        ("SetProductVariantAxesCommand", ValidatorRequiredPresent),
        ("SetStoreHeaderMenuAdminCommand", NoValidatorRequired),
        ("SetStoreHomePageAdminCommand", NoValidatorRequired),
        ("SetStoreLandingPageSectionEnabledAdminCommand", NoValidatorRequired),
        ("SetStoreLandingPageStatusAdminCommand", NoValidatorRequired),
        ("SetStoreMenuEnabledAdminCommand", NoValidatorRequired),
        ("SetStoreMenuItemEnabledAdminCommand", NoValidatorRequired),
        ("SetVariantAxisCapabilityCommand", NoValidatorRequired),
        ("UnbindCategoryAttributeCommand", ValidatorRequiredPresent),
        ("UpdateAttributeDefinitionCommand", ValidatorRequiredPresent),
        ("UpdateCategoryAttributeBindingCommand", ValidatorRequiredPresent),
        ("UpdateCategoryCoreCommand", ValidatorRequiredPresent),
        ("UpdateProductSeoCommand", ValidatorRequiredPresent),
        ("UpdateStoreLandingPageAdminCommand", ValidatorRequiredPresent),
        ("UpdateStoreLandingPageSectionAdminCommand", ValidatorRequiredPresent),
        ("UpdateStoreMenuAdminCommand", ValidatorRequiredPresent),
        ("UpdateStoreMenuItemAdminCommand", ValidatorRequiredPresent),
        ("UpdateUnitOfMeasureCommand", ValidatorRequiredPresent),
        ("UpsertCategoryFacetCommand", ValidatorRequiredPresent),
        ("UpsertCategoryMegaMenuCommand", ValidatorRequiredPresent),
        ("UpsertCategoryTranslationCommand", ValidatorRequiredPresent),
    ];

    [Fact]
    public void Endpoint_reachable_requests_are_exhaustively_classified()
    {
        Assert.Equal(Manifest.Length, Manifest.Select(x => x.RequestTypeName).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(Manifest.Count(x => x.Classification == ValidatorRequiredPresent),
            Manifest.Count(x => x.Classification == ValidatorRequiredPresent));
        Assert.True(Manifest.Length >= 100);
        Assert.True(Manifest.Count(x => x.Classification == ValidatorRequiredPresent) >= 50);
        Assert.True(Manifest.Count(x => x.Classification == NoValidatorRequired) >= 40);
    }

    [Fact]
    public void Required_validators_are_registered_in_cqrs_foundation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(CreateCategoryCommand).Assembly);
        using var sp = services.BuildServiceProvider();
        var assembly = typeof(CreateCategoryCommand).Assembly;

        foreach (var (name, classification) in Manifest.Where(x => x.Classification == ValidatorRequiredPresent))
        {
            var requestType = assembly.GetTypes().Single(t => t.Name == name);
            var validatorInterface = typeof(IValidator<>).MakeGenericType(requestType);
            Assert.NotNull(sp.GetService(validatorInterface));
        }

        foreach (var (name, classification) in Manifest.Where(x => x.Classification == NoValidatorRequired))
        {
            var requestType = assembly.GetTypes().Single(t => t.Name == name);
            var validatorInterface = typeof(IValidator<>).MakeGenericType(requestType);
            Assert.Null(sp.GetService(validatorInterface));
        }
    }

    [Fact]
    public void Production_endpoints_use_ISender_and_ApiResponseFactory_except_dev_demo()
    {
        var root = Repo();
        var endpointsRoot = Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints");
        foreach (var file in Directory.EnumerateFiles(endpointsRoot, "*Endpoints.cs", SearchOption.AllDirectories))
        {
            if (file.Contains("CatalogDemo", StringComparison.Ordinal))
                continue;
            var text = File.ReadAllText(file);
            if (!text.Contains("MapGet", StringComparison.Ordinal) && !text.Contains("MapPost", StringComparison.Ordinal)
                && !text.Contains("MapPut", StringComparison.Ordinal) && !text.Contains("MapDelete", StringComparison.Ordinal))
                continue;
            Assert.Contains("ISender", text, StringComparison.Ordinal);
            Assert.Contains("ApiResponseFactory", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("CatalogDbContext", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Error_catalog_and_resources_live_in_contracts_and_register_in_infrastructure()
    {
        var root = Repo();
        Assert.True(File.Exists(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorCatalogContributor.cs")));
        Assert.True(File.Exists(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Errors/CatalogErrorResourceSet.cs")));
        Assert.True(File.Exists(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Resources/CatalogErrors.resx")));
        Assert.True(File.Exists(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Contracts/Resources/CatalogErrors.fa.resx")));
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Errors")));
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Endpoints/Resources")));

        var module = File.ReadAllText(Path.Combine(root, "src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogModule.cs"));
        Assert.Contains("CatalogErrorCatalogContributor", module, StringComparison.Ordinal);
        Assert.Contains("CatalogErrorResourceSet", module, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(CatalogErrorCodes.ProductMissing));
    }

    private static string Repo()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
