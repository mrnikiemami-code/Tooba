using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-STOREFRONT-AMC-001-R1 — template-catalog preview evacuated from Host/Storefront
/// into Catalog CQRS + Endpoints. Durable no-regression guard for the R1 slice only.
/// </summary>
public sealed class HostStorefrontAmcR1GuardTests
{
    [Fact]
    public void Host_no_longer_owns_template_preview_files_or_registrations()
    {
        var hostRoot = HostRoot();
        Assert.False(File.Exists(Path.Combine(hostRoot, "Storefront", "FashionTemplatePreviewQuery.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Storefront", "IndustryTemplatePreviewQuery.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Storefront", "IndustryPersistedTemplateCatalog.cs")));
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Storefront")));

        var program = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));
        Assert.DoesNotContain("FashionTemplatePreviewQuery", program, StringComparison.Ordinal);
        Assert.DoesNotContain("IndustryTemplatePreviewQuery", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapStorefrontEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Host.Storefront;", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_owns_template_preview_cqrs_infra_and_routes()
    {
        var root = FindRepoRoot();
        var catalogRoot = Path.Combine(root, "src", "backend", "Modules", "Catalog");

        Assert.True(File.Exists(Path.Combine(
            catalogRoot, "Tooba.Catalog.Application", "TemplateCatalog", "Queries",
            "GetTemplateCatalogPreviewQueries.cs")));
        Assert.True(File.Exists(Path.Combine(
            catalogRoot, "Tooba.Catalog.Application", "TemplateCatalog", "Queries",
            "GetTemplateCatalogPreviewHandlers.cs")));
        Assert.True(File.Exists(Path.Combine(
            catalogRoot, "Tooba.Catalog.Application", "TemplateCatalog", "Ports",
            "ITemplateCatalogPreviewReaders.cs")));
        Assert.True(File.Exists(Path.Combine(
            catalogRoot, "Tooba.Catalog.Application", "TemplateCatalog", "Models",
            "TemplateCatalogPreviewModels.cs")));
        Assert.True(File.Exists(Path.Combine(
            catalogRoot, "Tooba.Catalog.Application", "TemplateCatalog", "Validators",
            "GetIndustryTemplatePreviewQueryValidator.cs")));

        Assert.True(File.Exists(Path.Combine(
            catalogRoot, "Tooba.Catalog.Infrastructure", "Development", "TemplateCatalog",
            "FashionTemplatePreviewQuery.cs")));
        Assert.True(File.Exists(Path.Combine(
            catalogRoot, "Tooba.Catalog.Infrastructure", "Development", "TemplateCatalog",
            "IndustryTemplatePreviewQuery.cs")));
        Assert.True(File.Exists(Path.Combine(
            catalogRoot, "Tooba.Catalog.Infrastructure", "Development", "TemplateCatalog",
            "IndustryPersistedTemplateCatalog.cs")));

        var endpoints = File.ReadAllText(Path.Combine(
            catalogRoot, "Tooba.Catalog.Endpoints", "Storefront", "TemplateCatalog",
            "CatalogTemplateCatalogStorefrontEndpoints.cs"));
        Assert.Contains("MapGet(\"/template-catalog/fashion/preview\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/template-catalog/{templateKey}/preview\"", endpoints, StringComparison.Ordinal);
        Assert.Contains("ISender", endpoints, StringComparison.Ordinal);
        Assert.Contains("GetFashionTemplatePreviewQuery", endpoints, StringComparison.Ordinal);
        Assert.Contains("GetIndustryTemplatePreviewQuery", endpoints, StringComparison.Ordinal);
        Assert.Contains("errorCode", endpoints, StringComparison.Ordinal);
        Assert.Contains("Results.Json", endpoints, StringComparison.Ordinal);

        var handlers = File.ReadAllText(Path.Combine(
            catalogRoot, "Tooba.Catalog.Application", "TemplateCatalog", "Queries",
            "GetTemplateCatalogPreviewHandlers.cs"));
        Assert.Contains("template_catalog.fashion.missing", handlers, StringComparison.Ordinal);
        Assert.Contains("template_catalog.use_fashion_route", handlers, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            catalogRoot, "Tooba.Catalog.Endpoints", "CatalogEndpointModule.cs"));
        Assert.Contains("MapCatalogTemplateCatalogStorefrontEndpoints()", module, StringComparison.Ordinal);

        var infraModule = File.ReadAllText(Path.Combine(
            catalogRoot, "Tooba.Catalog.Infrastructure", "CatalogModule.cs"));
        Assert.Contains("IFashionTemplatePreviewReader, FashionTemplatePreviewQuery", infraModule, StringComparison.Ordinal);
        Assert.Contains("IIndustryTemplatePreviewReader, IndustryTemplatePreviewQuery", infraModule, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_development_sink_not_used_for_template_preview()
    {
        var development = Path.Combine(HostRoot(), "Development");
        if (!Directory.Exists(development))
        {
            return;
        }

        foreach (var path in Directory.EnumerateFiles(development, "*.cs", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(path);
            Assert.DoesNotContain("FashionTemplate", name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("IndustryTemplate", name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("TemplateCatalog", name, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void SoT_and_evidence_hostStorefrontAmcR1_present()
    {
        var root = FindRepoRoot();
        var sot = File.ReadAllText(Path.Combine(root, "docs", "architecture", "tmar-current-state.json"));
        Assert.Contains("\"hostStorefrontAmcR1\"", sot, StringComparison.Ordinal);
        Assert.True(Directory.Exists(Path.Combine(
            root, "docs", "evidence", "TB-TMAR-HOST-STOREFRONT-AMC-001-R1")));
    }

    private static string HostRoot()
        => Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Tooba.sln"))
                || File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
