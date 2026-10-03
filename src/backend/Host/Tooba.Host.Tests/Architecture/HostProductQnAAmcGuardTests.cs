using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>Durable guards for TB-TMAR-HOST-PRODUCTQNA-AMC-001 — Host ProductQnA HOST_ZERO.</summary>
public sealed class HostProductQnAAmcGuardTests
{
    [Fact]
    public void Host_ProductQnA_folder_is_absent_and_modules_own_http()
    {
        var root = FindRepoRoot();
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/ProductQnA")));

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("MapProductQnAModuleEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("MapBulkInquiryModuleEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("AddProductQnAEndpointPresentation", program, StringComparison.Ordinal);
        Assert.Contains("AddBulkInquiryEndpointPresentation", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapProductQnAEndpoints()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.ProductQnA", program, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Endpoints/ProductQnAEndpointModule.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/BulkInquiry/Tooba.BulkInquiry.Endpoints/BulkInquiryEndpointModule.cs")));
    }

    [Fact]
    public void ProductQnA_and_BulkInquiry_endpoint_boundaries_are_clean()
    {
        var root = FindRepoRoot();
        foreach (var project in new[]
                 {
                     "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Endpoints/Tooba.ProductQnA.Endpoints.csproj",
                     "src/backend/Modules/BulkInquiry/Tooba.BulkInquiry.Endpoints/Tooba.BulkInquiry.Endpoints.csproj",
                 })
        {
            var csproj = File.ReadAllText(Path.Combine(root, project));
            Assert.DoesNotContain("Tooba.ProductQnA.Domain", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.BulkInquiry.Domain", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.ProductQnA.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.BulkInquiry.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Catalog.Application", csproj, StringComparison.Ordinal);
        }

        foreach (var file in EnumerateEndpointSources(root,
                     "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Endpoints")
                     .Concat(EnumerateEndpointSources(root,
                         "src/backend/Modules/BulkInquiry/Tooba.BulkInquiry.Endpoints")))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Tooba.Host.", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.ProductQnA.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.BulkInquiry.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Catalog.Application", text, StringComparison.Ordinal);
            Assert.DoesNotContain("title = ex.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("InvalidOperationException", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Infrastructure_uses_Catalog_Contracts_only()
    {
        var root = FindRepoRoot();
        foreach (var relative in new[]
                 {
                     "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Infrastructure/Tooba.ProductQnA.Infrastructure.csproj",
                     "src/backend/Modules/BulkInquiry/Tooba.BulkInquiry.Infrastructure/Tooba.BulkInquiry.Infrastructure.csproj",
                 })
        {
            var csproj = File.ReadAllText(Path.Combine(root, relative));
            Assert.Contains("Tooba.Catalog.Contracts", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Catalog.Application", csproj, StringComparison.Ordinal);
        }

        var qnaDir = File.ReadAllText(Path.Combine(root,
            "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Infrastructure/Directories/ProductQaDirectory.cs"));
        var bulkDir = File.ReadAllText(Path.Combine(root,
            "src/backend/Modules/BulkInquiry/Tooba.BulkInquiry.Infrastructure/BulkInquiryDirectory.cs"));
        Assert.Contains("ICatalogReviewProductLookup", qnaDir, StringComparison.Ordinal);
        Assert.Contains("ICatalogReviewProductLookup", bulkDir, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogLookupGateway", qnaDir, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogLookupGateway", bulkDir, StringComparison.Ordinal);
    }

    [Fact]
    public void SoT_hostProductQnAAmc_present()
    {
        var sot = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostProductQnAAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-PRODUCTQNA-AMC-001", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_PRODUCTQNA_AMC_001_CLOSED_HOST_ZERO", sot, StringComparison.Ordinal);
    }

    private static IEnumerable<string> EnumerateEndpointSources(string root, string relative) =>
        Directory.EnumerateFiles(Path.Combine(root, relative), "*.cs", SearchOption.AllDirectories)
            .Where(file =>
                !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    private static string FindRepoRoot()
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
