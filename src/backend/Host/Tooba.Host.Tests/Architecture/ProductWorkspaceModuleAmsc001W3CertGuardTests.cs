using System.Text.Json;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3 (Certify) — durable ARCH-COMPLETE-002 certification lock:
/// manifest promotion, SoT records, canonical mechanisms, schema preservation, Host closure, evidence.
/// </summary>
public sealed class ProductWorkspaceModuleAmsc001W3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/ProductWorkspace";
    private const string App = ModuleRoot + "/Tooba.ProductWorkspace.Application";
    private const string Domain = ModuleRoot + "/Tooba.ProductWorkspace.Domain";
    private const string Infra = ModuleRoot + "/Tooba.ProductWorkspace.Infrastructure";
    private const string Endpoints = ModuleRoot + "/Tooba.ProductWorkspace.Endpoints";
    private const string Contracts = ModuleRoot + "/Tooba.ProductWorkspace.Contracts";
    private const string Capability = App + "/Composition/ProductManagement";
    private const string CatalogContracts = "src/backend/Modules/Catalog/Tooba.Catalog.Contracts";

    [Fact]
    public void ProductWorkspace_is_arch_complete_002_certified_in_sot_and_manifest()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var entry = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "ProductWorkspace");
        Assert.True(entry.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entry.GetProperty("lockVersion").GetString());
        Assert.Contains(
            "TB-TMAR-PRODUCTWORKSPACE-AMSC-001",
            entry.GetProperty("certificationNote").GetString(),
            StringComparison.Ordinal);

        // Promotion is a move, not a copy: the pre-cert slot must not keep a duplicate entry.
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => m.GetProperty("module").GetString() == "ProductWorkspace");
        Assert.DoesNotContain(
            "ProductWorkspace",
            manifest.RootElement.GetProperty("uncertifiedHttpOwningModules").EnumerateArray()
                .Select(x => x.GetString()!).ToArray());

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        var w3 = sot.RootElement.GetProperty("productWorkspaceAmsc001W3");

        // The W3 block is retained as history but is explicitly superseded: TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R1
        // repaired the 17-request inventory and the raw Results.Json 201 mappings. The structural
        // certification state recorded here stays true and is re-asserted by the W3-R1 repair guard.
        Assert.Equal("SUPERSEDED_PENDING_FRESH_CERTIFY", w3.GetProperty("state").GetString());
        Assert.Equal(
            "TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R1",
            w3.GetProperty("supersededBy").GetString());

        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal("CERTIFIED", w3.GetProperty("structureState").GetString());
        Assert.Equal("ARCH-COMPLETE-002", w3.GetProperty("lockVersion").GetString());
        Assert.Equal("EXACT", w3.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ENFORCED", w3.GetProperty("rootAllowlistState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("blockingResidualDebt").GetString());
        Assert.Equal("HTTP_OWNING", w3.GetProperty("moduleApplicabilityState").GetString());
        Assert.Equal("MODULE_OWNED", w3.GetProperty("endpointOwnershipState").GetString());
        Assert.True(w3.GetProperty("microserviceExtractable").GetBoolean());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()!).ToArray();
        Assert.Equal(1, certified.Count(x => x == "ProductWorkspace"));

        // Wave lineage is recorded with its own commit SHAs.
        Assert.Equal("56909122", sot.RootElement.GetProperty("productWorkspaceAmsc001W0").GetProperty("commit").GetString());
        Assert.Equal("e0fe2885", sot.RootElement.GetProperty("productWorkspaceAmsc001W1").GetProperty("commit").GetString());
        Assert.Equal("592c346e", sot.RootElement.GetProperty("productWorkspaceAmsc001W2").GetProperty("commit").GetString());
    }

    [Fact]
    public void ProductWorkspace_uses_canonical_result_localization_and_typed_fault_mechanisms()
    {
        var root = Repo();

        // Result/ApiResponseFactory on every route; the canonical 201 variant shape is produced by
        // ApiResponseFactory.Created, never by a raw Results.* mapping.
        var module = File.ReadAllText(Path.Combine(root, Endpoints, "ProductWorkspaceEndpointModule.cs"));
        Assert.Contains("api.From(", module, StringComparison.Ordinal);
        Assert.Contains("api.FromFailure(", module, StringComparison.Ordinal);
        Assert.Contains("api.Created(", module, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.BadRequest", module, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Problem", module, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json", module, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", module, StringComparison.Ordinal);
        Assert.DoesNotContain("Console.WriteLine", module, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", module, StringComparison.Ordinal);
        Assert.DoesNotContain("StartActivity", module, StringComparison.Ordinal);
        Assert.DoesNotContain("traceparent", module, StringComparison.Ordinal);

        // Declared codes + bilingual resources are module-owned; descriptor ownership stays unique.
        var codes = File.ReadAllText(Path.Combine(root, Contracts, "Errors", "ProductWorkspaceErrorCodes.cs"));
        Assert.Contains("public static bool IsKnown(string? code)", codes, StringComparison.Ordinal);
        foreach (var culture in new[] { "ProductWorkspaceErrors.resx", "ProductWorkspaceErrors.fa.resx" })
        {
            var resx = File.ReadAllText(Path.Combine(root, Contracts, "Resources", culture));
            Assert.Contains("workspace.product.missing", resx, StringComparison.Ordinal);
            Assert.Contains("workspace.permission.denied", resx, StringComparison.Ordinal);
        }

        Assert.False(File.Exists(Path.Combine(
            root, Contracts, "Errors", "ProductWorkspaceErrorCatalogContributor.cs")));
        var moduleRegistration = File.ReadAllText(Path.Combine(root, Infra, "ProductWorkspaceModule.cs"));
        Assert.Contains("IErrorResourceSet, ProductWorkspaceErrorResourceSet>", moduleRegistration, StringComparison.Ordinal);
        Assert.DoesNotContain("IErrorCatalogContributor", moduleRegistration, StringComparison.Ordinal);

        var catalogContributor = File.ReadAllText(Path.Combine(
            root, CatalogContracts, "Errors", "CatalogErrorCatalogContributor.cs"));
        Assert.Contains("CatalogErrorCodes.WorkspaceProductMissing", catalogContributor, StringComparison.Ordinal);
        Assert.Contains("CatalogErrorCodes.WorkspacePermissionDenied", catalogContributor, StringComparison.Ordinal);

        // Typed-fault seam: code-driven only.
        var seam = File.ReadAllText(Path.Combine(root, App, "Composition", "ProductWorkspaceOperation.cs"));
        Assert.Contains(
            "catch (ContractOperationException ex) when (ProductWorkspaceErrorCodes.IsKnown(ex.Code))",
            seam,
            StringComparison.Ordinal);
        Assert.Contains("catch (SemanticException ex)", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", seam, StringComparison.Ordinal);

        // CQRS: 3 read requests (list / grid / aggregate) + 14 write commands = the 17 module routes.
        // The request inventory is enumerated from the module's own IRequest declarations so the
        // count is proven from disk rather than asserted against a folder filename convention.
        Assert.Equal(3, Directory.EnumerateFiles(Path.Combine(root, Capability, "Queries"), "*Query.cs").Count());
        Assert.Equal(14, Directory.EnumerateFiles(Path.Combine(root, Capability, "Commands"), "*Command.cs").Count());
        Assert.Equal(17, CountDeclaredRequests(root));

        // No module-local validator tree was invented: transport shape is owned by the Catalog
        // write capability behind the Contracts boundary.
        Assert.False(Directory.Exists(Path.Combine(root, App, "Validation")));
        Assert.False(Directory.Exists(Path.Combine(root, App, "Validators")));
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, App), "*.cs", SearchOption.AllDirectories)
                     .Where(f => !IsBuildOutput(f)))
        {
            Assert.DoesNotContain("AbstractValidator", File.ReadAllText(file), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ProductWorkspace_boundaries_stay_contracts_only_and_schema_is_preserved()
    {
        var root = Repo();

        // The only foreign project edges are Contracts.
        foreach (var project in new[]
                 {
                     "Tooba.ProductWorkspace.Contracts", "Tooba.ProductWorkspace.Domain",
                     "Tooba.ProductWorkspace.Application", "Tooba.ProductWorkspace.Infrastructure",
                     "Tooba.ProductWorkspace.Endpoints",
                 })
        {
            var csproj = File.ReadAllText(Path.Combine(root, ModuleRoot, project, project + ".csproj"));
            foreach (var foreign in new[]
                     {
                         "Tooba.Catalog.Application", "Tooba.Catalog.Infrastructure", "Tooba.Catalog.Domain",
                         "Tooba.Offer.Application", "Tooba.Pricing.Application", "Tooba.Inventory.Application",
                         "Tooba.Tax.Application", "Tooba.Party.Application", "Tooba.OperatorProfile.Application",
                         "Tooba.Host",
                     })
            {
                Assert.DoesNotContain(foreign, csproj, StringComparison.Ordinal);
            }
        }

        // Declared empty Domain boundary: no types, no references.
        Assert.Empty(Directory.EnumerateFiles(Path.Combine(root, Domain), "*.cs", SearchOption.AllDirectories)
            .Where(f => !IsBuildOutput(f)).ToArray());
        Assert.DoesNotContain(
            "ProjectReference",
            File.ReadAllText(Path.Combine(root, Domain, "Tooba.ProductWorkspace.Domain.csproj")),
            StringComparison.Ordinal);

        // No foreign persistence anywhere in the module.
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, ModuleRoot), "*.cs", SearchOption.AllDirectories)
                     .Where(f => !IsBuildOutput(f)))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("CatalogDbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Microsoft.EntityFrameworkCore", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.Host", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AddDbContext<", text, StringComparison.Ordinal);
        }

        // Schema preservation: the module owns no schema and registers no migration.
        var registry = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs"));
        Assert.DoesNotContain("ProductWorkspace", registry, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(root, Infra, "Persistence")));

        // The Catalog write seam stays the single Contracts port for mutations.
        var gateway = File.ReadAllText(Path.Combine(
            root, CatalogContracts, "Ports", "CatalogAdminProductWorkspaceMutationContracts.cs"));
        Assert.Contains("interface ICatalogAdminProductWorkspaceMutationGateway", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("IQueryable", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Catalog.Application", gateway, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductWorkspace_host_residue_is_composition_only_and_routes_are_preserved()
    {
        var root = Repo();
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/ProductWorkspace")));

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("MapProductWorkspaceModuleEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("AddProductWorkspaceEndpointPresentation", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.ProductWorkspace", program, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(root, Endpoints, "ProductWorkspaceEndpointModule.cs"));
        Assert.Equal(17, System.Text.RegularExpressions.Regex.Matches(
            module, @"\bMap(Get|Post|Put|Patch|Delete)\s*\(").Count);
        Assert.DoesNotContain("CatalogErrorCodes", module, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Catalog.Application", module, StringComparison.Ordinal);
        Assert.DoesNotContain("ICatalogActorContext", module, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductWorkspace_amsc_evidence_and_recovery_checkpoint_exist()
    {
        var root = Repo();
        foreach (var wave in new[] { "W0", "W1", "W2", "W3" })
        {
            Assert.True(Directory.Exists(Path.Combine(
                root, $"docs/architecture/evidence/TB-TMAR-PRODUCTWORKSPACE-AMSC-001-{wave}")));
        }

        var recovery = File.ReadAllText(Path.Combine(root, "docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("ProductWorkspace AMSC W0 Analyze", recovery, StringComparison.Ordinal);
        Assert.Contains("ProductWorkspace AMSC W1 Migrate", recovery, StringComparison.Ordinal);
        Assert.Contains("ProductWorkspace AMSC W2 Structure", recovery, StringComparison.Ordinal);
        Assert.Contains("ProductWorkspace AMSC W3 Certify", recovery, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductWorkspace_amsc_lineage_commits_resolve_and_global_host_closure_is_preserved()
    {
        var root = Repo();
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));

        foreach (var wave in new[] { "productWorkspaceAmsc001W0", "productWorkspaceAmsc001W1", "productWorkspaceAmsc001W2" })
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

    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    /// <summary>
    /// Counts the module's declared endpoint-reachable CQRS request types straight from the
    /// Application assembly surface: every <c>sealed record</c> that closes a MediatR
    /// <c>IRequest&lt;...&gt;</c>. Used so the 17-request inventory is proven from disk instead of
    /// trusting a folder filename convention.
    /// </summary>
    private static int CountDeclaredRequests(string root)
    {
        var pattern = new System.Text.RegularExpressions.Regex(
            @":\s*IRequest<",
            System.Text.RegularExpressions.RegexOptions.Multiline);
        return Directory
            .EnumerateFiles(Path.Combine(root, App), "*.cs", SearchOption.AllDirectories)
            .Where(f => !IsBuildOutput(f))
            .Sum(f => pattern.Matches(File.ReadAllText(f)).Count);
    }

    private static string Repo()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
