using System.Text.Json;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PRODUCTQNA-AMSC-001-W3 — durable ARCH-COMPLETE-002 certification lock:
/// SoT records, manifest promotion, canonical mechanisms, Host ownership and evidence.
/// </summary>
public sealed class ProductQnAModuleAmsc001W3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/ProductQnA";

    [Fact]
    public void ProductQnA_is_arch_complete_002_certified_in_sot_and_manifest()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var entry = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "ProductQnA");
        Assert.True(entry.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entry.GetProperty("lockVersion").GetString());
        Assert.Contains(
            "TB-TMAR-PRODUCTQNA-AMSC-001",
            entry.GetProperty("certificationNote").GetString(),
            StringComparison.Ordinal);
        Assert.Equal(
            new[]
            {
                "Tooba.ProductQnA.Application", "Tooba.ProductQnA.Contracts", "Tooba.ProductQnA.Domain",
                "Tooba.ProductQnA.Endpoints", "Tooba.ProductQnA.Infrastructure",
            },
            entry.GetProperty("projects").EnumerateArray()
                .Select(p => p.GetProperty("projectName").GetString()!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        var w3 = sot.RootElement.GetProperty("productQnAAmsc001W3");
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal("CERTIFIED", w3.GetProperty("structureState").GetString());
        Assert.Equal("ARCH-COMPLETE-002", w3.GetProperty("lockVersion").GetString());
        Assert.Equal("EXACT", w3.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ENFORCED", w3.GetProperty("rootAllowlistState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("MODULE_OWNED", w3.GetProperty("endpointOwnershipState").GetString());
        Assert.Equal(2, w3.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal("ZERO", w3.GetProperty("blockingResidualDebt").GetString());
        Assert.True(w3.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("NONE", w3.GetProperty("automaticNextImplementationTask").GetString());

        var amc = sot.RootElement.GetProperty("productQnAAmc001");
        Assert.Equal("TB-TMAR-PRODUCTQNA-AMSC-001-W3", amc.GetProperty("supersededBy").GetString());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => x == "ProductQnA"));

        // Wave lineage is recorded with its own commit SHAs.
        Assert.Equal("e7e28c49", sot.RootElement.GetProperty("productQnAAmsc001W0").GetProperty("commit").GetString());
        Assert.Equal("b29340de", sot.RootElement.GetProperty("productQnAAmsc001W1").GetProperty("commit").GetString());
        Assert.Equal("70c7b46b", sot.RootElement.GetProperty("productQnAAmsc001W2").GetProperty("commit").GetString());
    }

    [Fact]
    public void ProductQnA_uses_canonical_result_localization_and_typed_fault_mechanisms()
    {
        var root = Repo();
        var contracts = Path.Combine(root, ModuleRoot, "Tooba.ProductQnA.Contracts");
        var app = Path.Combine(root, ModuleRoot, "Tooba.ProductQnA.Application");
        var domain = Path.Combine(root, ModuleRoot, "Tooba.ProductQnA.Domain");
        var infra = Path.Combine(root, ModuleRoot, "Tooba.ProductQnA.Infrastructure");
        var endpoints = Path.Combine(root, ModuleRoot, "Tooba.ProductQnA.Endpoints");

        // Catalog owns exactly two semantic descriptors; validation codes are Application-owned and uncatalogued.
        var catalog = File.ReadAllText(Path.Combine(contracts, "Errors", "ProductQnAErrorCatalogContributor.cs"));
        Assert.Contains("ProductQnAErrorCodes.Rejected", catalog, StringComparison.Ordinal);
        Assert.Contains("ProductQnAErrorCodes.NotFound", catalog, StringComparison.Ordinal);
        var registrations = catalog[catalog.IndexOf("Contribute()", StringComparison.Ordinal)..];
        Assert.DoesNotContain("ProductQnAValidationCodes", registrations, StringComparison.Ordinal);
        Assert.DoesNotContain("product_qna.validation.", registrations, StringComparison.Ordinal);

        var codes = File.ReadAllText(Path.Combine(contracts, "Errors", "ProductQnAErrorCodes.cs"));
        Assert.Contains("product_qna.rejected", codes, StringComparison.Ordinal);
        Assert.Contains("product_qna.not_found", codes, StringComparison.Ordinal);
        Assert.DoesNotContain("product_qna.validation.", codes, StringComparison.Ordinal);
        Assert.Contains("public static bool IsKnown(string? code)", codes, StringComparison.Ordinal);

        var validationCodes = File.ReadAllText(Path.Combine(app, "Validation", "ProductQnAValidationCodes.cs"));
        Assert.Contains("product_qna.validation.actor_required", validationCodes, StringComparison.Ordinal);
        Assert.Contains("product_qna.validation.page_size_invalid", validationCodes, StringComparison.Ordinal);

        // Both cultures still carry every localization key.
        foreach (var culture in new[] { "ProductQnAErrors.resx", "ProductQnAErrors.fa.resx" })
        {
            var resx = File.ReadAllText(Path.Combine(contracts, "Resources", culture));
            Assert.Contains("product_qna.rejected", resx, StringComparison.Ordinal);
            Assert.Contains("product_qna.not_found", resx, StringComparison.Ordinal);
            Assert.Contains("product_qna.validation.page_size_invalid", resx, StringComparison.Ordinal);
        }

        // Typed faults: Domain/Directory use code-carrying ContractOperationException; the seam maps by code.
        foreach (var relative in new[]
                 {
                     "Tooba.ProductQnA.Domain/Aggregates/ProductQuestion.cs",
                     "Tooba.ProductQnA.Domain/Aggregates/ProductAnswer.cs",
                     "Tooba.ProductQnA.Infrastructure/Directories/ProductQaDirectory.cs",
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, ModuleRoot, relative));
            Assert.Contains("ContractOperationException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticException", text, StringComparison.Ordinal);
        }

        var seam = File.ReadAllText(Path.Combine(app, "Composition", "ProductQnAOperation.cs"));
        Assert.Contains(
            "catch (ContractOperationException ex) when (ProductQnAErrorCodes.IsKnown(ex.Code))",
            seam,
            StringComparison.Ordinal);
        Assert.Contains("catch (SemanticException ex)", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", seam, StringComparison.Ordinal);

        // Endpoints stay thin and canonical.
        foreach (var file in Directory.EnumerateFiles(endpoints, "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                                 && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.BadRequest", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Problem", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Console.WriteLine", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("StartActivity", text, StringComparison.Ordinal);
            Assert.DoesNotContain("traceparent", text, StringComparison.Ordinal);
        }

        var customer = File.ReadAllText(Path.Combine(endpoints, "Customer", "ProductQnACustomerEndpoints.cs"));
        Assert.Contains("ISender", customer, StringComparison.Ordinal);
        Assert.Contains("api.Created", customer, StringComparison.Ordinal);
        Assert.Contains("ProductQnAErrorCodes.SessionRequired", customer, StringComparison.Ordinal);

        var storefront = File.ReadAllText(Path.Combine(endpoints, "Storefront", "ProductQnAStorefrontEndpoints.cs"));
        Assert.Contains("ISender", storefront, StringComparison.Ordinal);
        Assert.Contains("api.From", storefront, StringComparison.Ordinal);

        // Catalog + resource-set registration lives in the module composition root (Infrastructure), not Host.
        var module = File.ReadAllText(Path.Combine(infra, "ProductQnAModule.cs"));
        Assert.Contains("ProductQnAErrorCatalogContributor", module, StringComparison.Ordinal);
        Assert.Contains("ProductQnAErrorResourceSet", module, StringComparison.Ordinal);
        Assert.Contains("IProductQaDirectory", module, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductQnA_host_residue_is_composition_only_and_closed_folder_is_preserved()
    {
        var root = Repo();
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/ProductQnA")));

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("MapProductQnAModuleEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("AddProductQnAEndpointPresentation", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.ProductQnA", program, StringComparison.Ordinal);

        // The module's Infrastructure csproj keeps the Contracts-only Catalog seam and nothing foreign.
        var infraCsproj = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.ProductQnA.Infrastructure", "Tooba.ProductQnA.Infrastructure.csproj"));
        Assert.Contains("Tooba.Catalog.Contracts", infraCsproj, StringComparison.Ordinal);
        foreach (var forbidden in new[]
                 {
                     "Tooba.Catalog.Application", "Tooba.Catalog.Domain", "Tooba.Catalog.Infrastructure",
                     "Tooba.Order.", "Tooba.Cart.", "Tooba.BulkInquiry.", "Tooba.Payment.",
                 })
        {
            Assert.DoesNotContain(forbidden, infraCsproj, StringComparison.Ordinal);
        }

        // Domain keeps only BuildingBlocks + its own Contracts reference.
        var domainCsproj = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.ProductQnA.Domain", "Tooba.ProductQnA.Domain.csproj"));
        Assert.Contains("Tooba.BuildingBlocks.csproj", domainCsproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.ProductQnA.Contracts.csproj", domainCsproj, StringComparison.Ordinal);
        foreach (var foreign in new[] { "Tooba.Catalog.", "Tooba.Order.", "Tooba.Cart.", "Tooba.BulkInquiry." })
        {
            Assert.DoesNotContain(foreign, domainCsproj, StringComparison.Ordinal);
        }

        // Schema / migration safety: no new migration, unchanged schema name.
        var migrations = Path.Combine(root, ModuleRoot, "Tooba.ProductQnA.Infrastructure", "Persistence", "Migrations");
        Assert.Equal(
            [
                "20260826120000_InitialProductQnA.Designer.cs",
                "20260826120000_InitialProductQnA.cs",
                "ProductQnADbContextModelSnapshot.cs",
            ],
            Directory.EnumerateFiles(migrations, "*.cs").Select(Path.GetFileName).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        Assert.Contains(
            "\"product_qna\"",
            File.ReadAllText(Path.Combine(
                root, ModuleRoot, "Tooba.ProductQnA.Infrastructure", "Persistence", "ProductQnADbContext.cs")),
            StringComparison.Ordinal);
    }

    [Fact]
    public void ProductQnA_amsc_evidence_and_recovery_checkpoint_exist()
    {
        var root = Repo();
        foreach (var wave in new[] { "W0", "W1", "W2", "W3" })
        {
            Assert.True(Directory.Exists(Path.Combine(
                root, $"docs/architecture/evidence/TB-TMAR-PRODUCTQNA-AMSC-001-{wave}")));
        }

        var recovery = File.ReadAllText(Path.Combine(root, "docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("ProductQnA AMSC module recovery checkpoint", recovery, StringComparison.Ordinal);
        Assert.Contains("ProductQnA AMSC W1 Migrate checkpoint", recovery, StringComparison.Ordinal);
        Assert.Contains("ProductQnA AMSC W2 Structure checkpoint", recovery, StringComparison.Ordinal);
        Assert.Contains("ProductQnA AMSC W3 Certify checkpoint", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-PRODUCTQNA-AMSC-001-W0", recovery, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductQnA_amsc_lineage_commits_resolve_and_global_host_closure_is_preserved()
    {
        var root = Repo();
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));

        foreach (var wave in new[] { "productQnAAmsc001W0", "productQnAAmsc001W1", "productQnAAmsc001W2" })
        {
            var sha = sot.RootElement.GetProperty(wave).GetProperty("commit").GetString()!;
            Assert.False(sha.StartsWith("PENDING", StringComparison.Ordinal), $"{wave} commit is unresolved: {sha}");
            Assert.Single(RunGit(root, "rev-list", "--max-count=1", sha));
        }

        // Repository-global Host root checkpoint must remain untouched by this module-local task.
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", sot.RootElement.GetProperty("currentHostCheckpoint").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("USER_REVIEW_HOST_ROOT_FINAL_CERT_001", sot.RootElement.GetProperty("workflowStop").GetString());
    }

    private static string[] RunGit(string root, params string[] args)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("git")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = System.Diagnostics.Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
