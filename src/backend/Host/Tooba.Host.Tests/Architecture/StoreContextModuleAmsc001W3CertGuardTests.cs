using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-STORECONTEXT-AMSC-001-W3 — durable ARCH-COMPLETE-002 certification lock for StoreContext:
/// the SoT/manifest certification records with the AMSC wave lineage, the INTERNAL_ONLY applicability
/// verdict with a vacuous validator matrix, the two-wave structure/cohesion truth, the Contracts-only
/// microservice-extraction boundary, the preserved repository-global Host checkpoint and the evidence
/// tree.
/// </summary>
public sealed class StoreContextModuleAmsc001W3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/StoreContext";

    private const string W0Commit = "c73545f547d970745cbcb0e5c9fc634aff3c96ae";
    private const string W1Commit = "d8abe38af8920bb1ac0417270e2de5b8756a104d";
    private const string W2Commit = "452855fa2379080aa8ca9f8a93509f947901fa79";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.StoreContext.Contracts",
        "Tooba.StoreContext.Infrastructure",
    ];

    private static readonly string[] ProductionFiles =
    [
        "Tooba.StoreContext.Contracts/Current/StoreCommerceContext.cs",
        "Tooba.StoreContext.Infrastructure/Current/StoreCommerceContextAccessor.cs",
        "Tooba.StoreContext.Infrastructure/DependencyInjection/StoreContextModule.cs",
    ];

    /// <summary>SoT and manifest carry the honest ARCH-COMPLETE-002 certification for StoreContext.</summary>
    [Fact]
    public void StoreContext_is_arch_complete_002_certified_in_sot_and_manifest()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var entries = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => string.Equals(m.GetProperty("module").GetString(), "StoreContext", StringComparison.Ordinal))
            .ToArray();
        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());
        Assert.Contains("TB-TMAR-STORECONTEXT-AMSC-001-W3",
            entries[0].GetProperty("certificationNote").GetString()!, StringComparison.Ordinal);
        Assert.Contains("INTERNAL_ONLY",
            entries[0].GetProperty("certificationNote").GetString()!, StringComparison.Ordinal);
        Assert.Equal(2, entries[0].GetProperty("projects").GetArrayLength());
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "StoreContext", StringComparison.Ordinal));

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        var w3 = sot.RootElement.GetProperty("storeContextAmsc001W3");
        Assert.Equal("TB-TMAR-STORECONTEXT-AMSC-001-W3", w3.GetProperty("task").GetString());
        Assert.Equal("TB-TMAR-STORECONTEXT-AMSC-001-W2", w3.GetProperty("parentTask").GetString());
        Assert.Equal(W2Commit, w3.GetProperty("startingHead").GetString());
        Assert.Equal("STORECONTEXT_AMSC_001_CERTIFIED", w3.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", w3.GetProperty("lockVersion").GetString());
        Assert.Equal("CERTIFIED", w3.GetProperty("structureState").GetString());
        Assert.True(w3.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("INTERNAL_ONLY", w3.GetProperty("httpApplicability").GetString());
        Assert.Equal("NOT_APPLICABLE", w3.GetProperty("endpointOwnershipState").GetString());
        Assert.Equal("NOT_APPLICABLE_NO_APPLICATION_USE_CASE", w3.GetProperty("cqrsState").GetString());
        Assert.Equal(0, w3.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal("NOT_APPLICABLE_INTERNAL_ONLY_VACUOUS_SET_EQUALITY", w3.GetProperty("validatorCoverageState").GetString());
        Assert.Equal("EXACT", w3.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ENFORCED_EMPTY_ROOT_ON_BOTH_PROJECTS", w3.GetProperty("rootAllowlistState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("foreignModuleLayerCoupling").GetString());
        Assert.Equal("NONE", w3.GetProperty("crossModuleJoinState").GetString());
        Assert.Equal("NONE", w3.GetProperty("namespaceAliasWorkaroundState").GetString());
        Assert.Equal("CANONICAL_NO_OWNED_ERROR_CODES_NO_RESOURCE_SET_BY_DESIGN", w3.GetProperty("localizationState").GetString());
        Assert.Equal("CANONICAL_NO_HTTP_SURFACE", w3.GetProperty("apiResultPatternState").GetString());
        Assert.Equal("CANONICAL", w3.GetProperty("loggingState").GetString());
        Assert.Equal("NONE", w3.GetProperty("sensitiveLoggingState").GetString());
        Assert.Equal("CANONICAL", w3.GetProperty("correlationState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("hostIllegalAuthorityState").GetString());
        Assert.Equal("NONE", w3.GetProperty("schemaMigrationState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("blockingResidualDebt").GetString());
        Assert.True(w3.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("PRESERVED", w3.GetProperty("globalHostCheckpointState").GetString());
        Assert.Equal("NONE", w3.GetProperty("automaticNextImplementationTask").GetString());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => x == "StoreContext"));
    }

    /// <summary>Each wave's recorded starting head is the parent wave's commit, so the chain is verifiable.</summary>
    [Fact]
    public void StoreContext_amsc_wave_lineage_is_recorded_and_chained()
    {
        var root = Repo();
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));

        var w0 = sot.RootElement.GetProperty("storeContextAmsc001W0");
        var w1 = sot.RootElement.GetProperty("storeContextAmsc001W1");
        var w2 = sot.RootElement.GetProperty("storeContextAmsc001W2");
        var w3 = sot.RootElement.GetProperty("storeContextAmsc001W3");

        Assert.Equal(W0Commit, w0.GetProperty("commit").GetString());
        Assert.Equal("ANALYZE_COMPLETE", w0.GetProperty("state").GetString());
        Assert.Equal(W0Commit, w1.GetProperty("startingHead").GetString());
        Assert.Equal("MIGRATE_COMPLETE", w1.GetProperty("state").GetString());
        Assert.Equal(W1Commit, w2.GetProperty("startingHead").GetString());
        Assert.Equal("STRUCTURE_COMPLETE", w2.GetProperty("state").GetString());
        Assert.Equal(W2Commit, w3.GetProperty("startingHead").GetString());

        // The recorded commits must be real ancestors of the current repository history.
        foreach (var commit in new[] { W0Commit, W1Commit, W2Commit })
        {
            Assert.True(IsAncestor(root, commit), $"{commit} is not in the current history");
        }
    }

    /// <summary>
    /// INTERNAL_ONLY by construction: the module owns no route, no application use case, no domain
    /// model, no persistence and no Host folder, so its validator matrix is vacuously complete.
    /// </summary>
    [Fact]
    public void StoreContext_validator_matrix_is_vacuously_complete_for_an_internal_only_module()
    {
        var root = Repo();

        foreach (var absent in new[]
                 {
                     "Tooba.StoreContext.Application",
                     "Tooba.StoreContext.Domain",
                     "Tooba.StoreContext.Endpoints",
                     "Tooba.StoreContext.Tests",
                 })
        {
            Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, absent)), absent);
        }

        Assert.False(Directory.Exists(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "StoreContext")));

        var production = ProductionFiles
            .Select(f => File.ReadAllText(Path.Combine(root, ModuleRoot, f)))
            .ToArray();

        // Zero routes, zero MediatR requests, zero validators, zero persistence: nothing to classify.
        Assert.DoesNotContain(production, t => t.Contains("MapGroup", StringComparison.Ordinal));
        Assert.DoesNotContain(production, t => t.Contains("IRequest", StringComparison.Ordinal));
        Assert.DoesNotContain(production, t => t.Contains("AbstractValidator", StringComparison.Ordinal));
        Assert.DoesNotContain(production, t => t.Contains("DbContext", StringComparison.Ordinal));

        // No module composition entry is registered by the Host as an endpoint surface.
        var composition = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs"));
        Assert.Contains("new StoreContextModule()", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("MapStoreContext", composition, StringComparison.Ordinal);

        // The module still registers exactly one scoped accessor behind its read/assign seams. The worker
        // factory seam is declared in Contracts but implemented by the Host composition root (it depends on
        // the control-plane registry), so it is a legitimate Host composition seam, not a module registration.
        var module = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.StoreContext.Infrastructure", "DependencyInjection", "StoreContextModule.cs"));
        Assert.Contains("AddScoped<StoreCommerceContextAccessor>()", module, StringComparison.Ordinal);
        Assert.Contains("ICurrentStoreCommerceContext", module, StringComparison.Ordinal);
        Assert.Contains("IStoreCommerceContextAssigner", module, StringComparison.Ordinal);

        var contracts = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.StoreContext.Contracts", "Current", "StoreCommerceContext.cs"));
        Assert.Contains("public interface ICurrentStoreCommerceContext", contracts, StringComparison.Ordinal);
        Assert.Contains("public interface IStoreCommerceContextAssigner", contracts, StringComparison.Ordinal);
        Assert.Contains("public interface IWorkerStoreCommerceContextFactory", contracts, StringComparison.Ordinal);

        var program = File.ReadAllText(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.Contains("AddSingleton<IWorkerStoreCommerceContextFactory>", program, StringComparison.Ordinal);
    }

    /// <summary>The W2 structure/cohesion verdict is preserved as certification input and by disk.</summary>
    [Fact]
    public void StoreContext_w2_structure_verdict_is_preserved_on_disk()
    {
        var root = Repo();

        var contracts = Path.Combine(root, ModuleRoot, "Tooba.StoreContext.Contracts");
        var infra = Path.Combine(root, ModuleRoot, "Tooba.StoreContext.Infrastructure");

        Assert.Empty(Directory.GetFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.GetFiles(infra, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.True(File.Exists(Path.Combine(infra, "DependencyInjection", "StoreContextModule.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Current", "StoreCommerceContextAccessor.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Current", "StoreCommerceContext.cs")));

        Assert.Equal(3, ProductionFiles.Count(f => File.Exists(Path.Combine(root, ModuleRoot, f))));
        foreach (var relative in ProductionFiles)
        {
            var lines = File.ReadAllLines(Path.Combine(root, ModuleRoot, relative));
            Assert.True(lines.Length <= 500, $"{relative} exceeds the cohesion ceiling ({lines.Length} lines)");
        }

        // Path equals namespace for every production file (exact, no alias workaround).
        foreach (var project in ProductionProjects)
        {
            var projectPath = Path.Combine(root, ModuleRoot, project);
            foreach (var file in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(projectPath, file);
                if (relative.StartsWith("bin", StringComparison.OrdinalIgnoreCase)
                    || relative.StartsWith("obj", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var directory = Path.GetDirectoryName(relative);
                var expected = string.IsNullOrEmpty(directory)
                    ? project
                    : project + "." + directory.Replace('\\', '.').Replace('/', '.');
                var match = Regex.Match(File.ReadAllText(file), @"^namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Multiline);
                Assert.True(match.Success, $"{relative} has no namespace declaration");
                Assert.Equal(expected, match.Groups[1].Value);
                Assert.DoesNotMatch(@"^\s*using\s+[A-Za-z0-9_.]+\s*=", File.ReadAllText(file));
            }
        }

        // Solution Explorer grouping is preserved.
        var doc = XDocument.Load(Path.Combine(root, "src", "backend", "Tooba.slnx"));
        var folder = doc.Root!.Elements("Folder").Single(f =>
            string.Equals((string?)f.Attribute("Name"), "/Modules/StoreContext/", StringComparison.Ordinal));
        Assert.Equal(
            ProductionProjects.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            folder.Elements("Project")
                .Select(p => Path.GetFileNameWithoutExtension((string)p.Attribute("Path")!))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
    }

    /// <summary>Contracts-only boundary with zero foreign coupling: the microservice extraction claim.</summary>
    [Fact]
    public void StoreContext_contracts_only_boundary_makes_microservice_extraction_possible()
    {
        var root = Repo();

        var contractsCsproj = XDocument.Load(Path.Combine(
            root, ModuleRoot, "Tooba.StoreContext.Contracts", "Tooba.StoreContext.Contracts.csproj"));
        var contractsRefs = contractsCsproj.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty).ToArray();
        Assert.Contains(contractsRefs, r => r.Contains("Tooba.BuildingBlocks", StringComparison.Ordinal));
        foreach (var reference in contractsRefs)
        {
            Assert.DoesNotContain("Tooba.StoreContext.", reference, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Host", reference, StringComparison.Ordinal);
        }

        var infraCsproj = XDocument.Load(Path.Combine(
            root, ModuleRoot, "Tooba.StoreContext.Infrastructure", "Tooba.StoreContext.Infrastructure.csproj"));
        var infraRefs = infraCsproj.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty).ToArray();
        Assert.Contains(infraRefs, r => r.Contains("Tooba.StoreContext.Contracts", StringComparison.Ordinal));
        Assert.Contains(infraRefs, r => r.Contains("Tooba.ModuleContracts", StringComparison.Ordinal));
        foreach (var reference in infraRefs)
        {
            Assert.DoesNotContain("Tooba.Host", reference, StringComparison.Ordinal);
            Assert.DoesNotContain(".Application/", reference, StringComparison.Ordinal);
            Assert.DoesNotContain(".Domain/", reference, StringComparison.Ordinal);
            Assert.DoesNotContain(".Endpoints/", reference, StringComparison.Ordinal);
        }

        var joined = string.Join("\n", ProductionFiles.Select(f => File.ReadAllText(Path.Combine(root, ModuleRoot, f))));
        var foreignLayer = new Regex(
            @"Tooba\.(?!StoreContext\b|BuildingBlocks\b|ModuleContracts\b)[A-Za-z]+\.(Application|Domain|Infrastructure|Endpoints)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        var match = foreignLayer.Match(joined);
        Assert.False(match.Success, $"foreign module layer reference {match.Value}");
        Assert.DoesNotContain("TypeForwardedTo", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("global using", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("DbSet", joined, StringComparison.Ordinal);
    }

    /// <summary>No ad-hoc result, localization, logging or telemetry mechanism exists in the module.</summary>
    [Fact]
    public void StoreContext_uses_no_parallel_mechanism_for_results_localization_logging_or_telemetry()
    {
        var root = Repo();
        foreach (var relative in ProductionFiles)
        {
            // Documentation prose legitimately names the mechanisms it claims to avoid (for example the
            // Persian summary explaining that the accessor is not AsyncLocal based), so only code lines are
            // inspected here.
            var text = string.Join("\n", File.ReadAllLines(Path.Combine(root, ModuleRoot, relative))
                .Where(line => !line.TrimStart().StartsWith("///", StringComparison.Ordinal)));

            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.BadRequest", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Problem", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ProblemDetails", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ApiResponseFactory", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IErrorResourceSet", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IErrorCatalogContributor", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ILogger", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Console.WriteLine", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Debug.WriteLine", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ActivitySource", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AsyncLocal", text, StringComparison.Ordinal);
            Assert.DoesNotContain("HttpContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Exception.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Accept-Language", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IErrorMessageLocalizer", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IModuleCallTracer", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ToobaTelemetry", text, StringComparison.Ordinal);
        }
    }

    /// <summary>Host closure is preserved and no illegal Host authority exists.</summary>
    [Fact]
    public void StoreContext_host_closure_is_preserved()
    {
        var root = Repo();

        Assert.False(Directory.Exists(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "StoreContext")));
        Assert.Empty(Directory.GetFiles(Path.Combine(root, "src", "backend", "Host", "Tooba.Host"), "StoreContext*.cs",
            SearchOption.AllDirectories));

        var composition = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs"));
        Assert.Contains("using Tooba.StoreContext.Infrastructure.DependencyInjection;", composition, StringComparison.Ordinal);

        // The worker-side factory adapter remains the single legitimate Host seam and lives outside the module.
        Assert.DoesNotContain("IWorkerStoreCommerceContextFactory", composition, StringComparison.Ordinal);
    }

    /// <summary>The four AMSC evidence waves exist and the W3 certification evidence is complete.</summary>
    [Fact]
    public void StoreContext_amsc_evidence_tree_is_present()
    {
        var root = Repo();

        foreach (var wave in new[] { "W0", "W1", "W2", "W3" })
        {
            Assert.True(Directory.Exists(Path.Combine(
                root, "docs", "architecture", "evidence", $"TB-TMAR-STORECONTEXT-AMSC-001-{wave}")), wave);
        }

        Assert.True(File.Exists(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-STORECONTEXT-AMSC-001-W0/analyze.md")));
        Assert.True(File.Exists(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-STORECONTEXT-AMSC-001-W1/migrate.md")));
        Assert.True(File.Exists(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-STORECONTEXT-AMSC-001-W2/structure.md")));

        var certification = File.ReadAllText(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-STORECONTEXT-AMSC-001-W3/certification.md"));
        Assert.Contains("ARCH-COMPLETE-002", certification, StringComparison.Ordinal);
        Assert.Contains("COMPLETE_REFERENCE_PATTERN", certification, StringComparison.Ordinal);
        Assert.Contains("INTERNAL_ONLY", certification, StringComparison.Ordinal);

        // Master Recovery records the StoreContext certification checkpoint.
        var recovery = File.ReadAllText(Path.Combine(root, "docs", "architecture", "TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("StoreContext AMSC module recovery checkpoint", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-STORECONTEXT-AMSC-001-W3", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_STORECONTEXT_AMSC_001_W3", recovery, StringComparison.Ordinal);

        // Repository-global Host root checkpoint must remain untouched by this module-local task.
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", sot.RootElement.GetProperty("currentHostCheckpoint").GetString());
        Assert.Equal("USER_REVIEW_HOST_ROOT_FINAL_CERT_001", sot.RootElement.GetProperty("workflowStop").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());
    }

    private static bool IsAncestor(string root, string commit)
    {
        using var process = new System.Diagnostics.Process();
        process.StartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"merge-base --is-ancestor {commit} HEAD",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        process.Start();
        process.WaitForExit();
        return process.ExitCode == 0;
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
