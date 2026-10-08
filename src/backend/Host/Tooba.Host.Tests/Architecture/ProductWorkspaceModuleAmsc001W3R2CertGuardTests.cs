using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R2 — FRESH_CERTIFY_AFTER_BLOCKER_REPAIR lock.
/// <para>
/// The Architect refused final acceptance of the W3 certification (two ARCH-COMPLETE-002
/// blockers), W3-R1 repaired only those two blockers, and this wave re-certifies the module
/// independently from disk. This guard pins the fresh certification truth so the accepted
/// verdict cannot silently drift away from the code the wave actually verified:
/// </para>
/// <list type="bullet">
/// <item><b>Route/request inventory.</b> Exactly 17 module-owned routes over exactly 17
/// endpoint-reachable CQRS request types (3 queries + 14 commands), each dispatched by the
/// endpoint module and each bound to exactly one handler.</item>
/// <item><b>Validator matrix.</b> An exhaustive 17-row classification, re-derived per request
/// rather than asserted "by construction": 0 VALIDATOR_REQUIRED + 17 NO_VALIDATOR_REQUIRED with
/// zero module-local FluentValidation tree.</item>
/// <item><b>Canonical API results.</b> Zero raw <c>Results.Json</c>/<c>BadRequest</c>/<c>Problem</c>
/// and exactly two canonical <c>ApiResponseFactory.Created</c> 201 paths.</item>
/// <item><b>Authorities and preservation.</b> W2 structure and W3-R1 repair remain the authorities;
/// Contracts-only boundaries, schema/migration immutability and the repository-global Host
/// checkpoint are untouched.</item>
/// </list>
/// </summary>
public sealed class ProductWorkspaceModuleAmsc001W3R2CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/ProductWorkspace";
    private const string App = ModuleRoot + "/Tooba.ProductWorkspace.Application";
    private const string Endpoints = ModuleRoot + "/Tooba.ProductWorkspace.Endpoints";
    private const string EndpointModule = Endpoints + "/ProductWorkspaceEndpointModule.cs";
    private const string Capability = App + "/Composition/ProductManagement";
    private const string RoutePrefix = "/v1/admin/products";
    private const string EvidenceRoot = "docs/architecture/evidence/TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R2";

    private const int ExpectedQueries = 3;
    private const int ExpectedCommands = 14;
    private const int ExpectedRequests = ExpectedQueries + ExpectedCommands;

    [Fact]
    public void Fresh_certification_truth_is_recorded_in_sot_and_manifest()
    {
        var root = Repo();

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")).Replace("\uFEFF", string.Empty));
        var r2 = sot.RootElement.GetProperty("productWorkspaceAmsc001W3R2");

        Assert.Equal("TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R2", r2.GetProperty("task").GetString());
        Assert.Equal("TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R1", r2.GetProperty("parentTask").GetString());
        Assert.Equal("FRESH_CERTIFY_AFTER_BLOCKER_REPAIR", r2.GetProperty("mode").GetString());
        Assert.Equal("e5575d68e72171f8bd09458e2fc91a4432de5cb5", r2.GetProperty("startingHead").GetString());
        Assert.Equal("PRODUCTWORKSPACE_AMSC_001_RECERTIFIED", r2.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", r2.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", r2.GetProperty("lockVersion").GetString());
        Assert.True(r2.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("HTTP_OWNING", r2.GetProperty("httpApplicability").GetString());
        Assert.True(r2.GetProperty("microserviceExtractable").GetBoolean());
        Assert.False(r2.GetProperty("productionCodeChanged").GetBoolean());
        Assert.Equal("NONE", r2.GetProperty("guardsWeakened").GetString());
        Assert.Equal("NONE", r2.GetProperty("baselinesWidened").GetString());
        Assert.Equal("NONE", r2.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("USER_REVIEW_PRODUCTWORKSPACE_AMSC_001_W3_R2", r2.GetProperty("workflowStop").GetString());

        // Authorities: W2 structure and W3-R1 repair stay authoritative, the old W3 record superseded.
        Assert.Equal("TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W2", r2.GetProperty("structureAuthority").GetString());
        Assert.Equal("592c346e91d22cb5c31fb50f560a049a4883f19e", r2.GetProperty("structureAuthorityCommit").GetString());
        Assert.Equal("TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R1", r2.GetProperty("repairAuthority").GetString());
        Assert.Equal("f0500c29d9d97e8761f70ce0036452d50bdeefe8", r2.GetProperty("repairAuthorityCommit").GetString());
        Assert.Equal("TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3", r2.GetProperty("supersededCertificationTask").GetString());
        Assert.Equal("c0b86feacd897d26172fef7dff47b93db75d8e9d", r2.GetProperty("supersededCertificationCommit").GetString());

        // The W3-R1 handoff is consumed: the repair wave stopped at READY_FOR_FRESH_CERTIFY and this
        // wave is the fresh certify that closes it, without rewriting the historical repair record.
        var r1 = sot.RootElement.GetProperty("productWorkspaceAmsc001W3R1");
        Assert.Equal("READY_FOR_FRESH_CERTIFY", r1.GetProperty("state").GetString());
        Assert.Equal("CLOSED", r1.GetProperty("blockerB1").GetProperty("status").GetString());
        Assert.Equal("CLOSED", r1.GetProperty("blockerB2").GetProperty("status").GetString());

        // No fabricated self-referential SHA: the certification commit is reported in the Bridge result.
        Assert.Equal(JsonValueKind.Null, r2.GetProperty("commit").ValueKind);
        Assert.Equal(
            "REPORTED_IN_BRIDGE_RESULT_ONLY_NO_SELF_REFERENTIAL_SOT_SHA",
            r2.GetProperty("commitState").GetString());

        // Manifest stays promoted exactly once with no pre-cert duplicate and no structural change.
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")).Replace("\uFEFF", string.Empty));
        var entry = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => string.Equals(m.GetProperty("module").GetString(), "ProductWorkspace", StringComparison.Ordinal));
        Assert.True(entry.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entry.GetProperty("lockVersion").GetString());
        Assert.Equal(5, entry.GetProperty("projects").GetArrayLength());
        var note = entry.GetProperty("certificationNote").GetString()!;
        Assert.Contains("W3-R2", note, StringComparison.Ordinal);
        Assert.Contains("W3-R1 f0500c29", note, StringComparison.Ordinal);
        Assert.Contains("EXHAUSTIVE 17", note, StringComparison.Ordinal);
        Assert.DoesNotContain("EXHAUSTIVE 0-REQUIRED 0-NO_VALIDATOR_REQUIRED", note, StringComparison.Ordinal);
        Assert.DoesNotContain("4 queries", note, StringComparison.Ordinal);
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "ProductWorkspace", StringComparison.Ordinal));

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()!).ToArray();
        Assert.Equal(1, certified.Count(x => string.Equals(x, "ProductWorkspace", StringComparison.Ordinal)));
    }

    [Fact]
    public void Route_request_and_validator_matrix_truth_is_exact_and_proven_from_disk()
    {
        var root = Repo();
        var module = Read(root, EndpointModule);

        // Exactly 17 module-owned routes.
        Assert.Equal(ExpectedRequests, Regex.Matches(module, @"\bMap(Get|Post|Put|Patch|Delete)\s*\(").Count);
        Assert.Contains("app.MapGroup(\"/v1/admin/products\")", module, StringComparison.Ordinal);

        // Exactly 17 endpoint-reachable requests declared from disk, each dispatched by the endpoints
        // module and each bound to exactly one handler - no orphan request, no phantom handler.
        var declared = DeclaredRequestTypes(root);
        Assert.Equal(ExpectedRequests, declared.Count);
        Assert.Equal(ExpectedQueries, declared.Count(n => n.EndsWith("Query", StringComparison.Ordinal)));
        Assert.Equal(ExpectedCommands, declared.Count(n => n.EndsWith("Command", StringComparison.Ordinal)));
        Assert.Equal(ExpectedQueries, Directory.EnumerateFiles(
            Path.Combine(root, Capability, "Queries"), "*Query.cs").Count());
        Assert.Equal(ExpectedCommands, Directory.EnumerateFiles(
            Path.Combine(root, Capability, "Commands"), "*Command.cs").Count());
        foreach (var request in declared)
        {
            Assert.Contains($"new {request}(", module, StringComparison.Ordinal);
        }

        var handlers = HandlerRequestTypes(root);
        Assert.Equal(ExpectedRequests, handlers.Count);
        Assert.Equal(declared, new SortedSet<string>(handlers, StringComparer.Ordinal));

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")).Replace("\uFEFF", string.Empty));
        var r2 = sot.RootElement.GetProperty("productWorkspaceAmsc001W3R2");
        Assert.Equal(ExpectedRequests, r2.GetProperty("moduleOwnedRoutes").GetInt32());
        Assert.Equal(ExpectedRequests, r2.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal(ExpectedQueries, r2.GetProperty("endpointReachableQueries").GetInt32());
        Assert.Equal(ExpectedCommands, r2.GetProperty("endpointReachableCommands").GetInt32());
        Assert.Equal("EXACT_17", r2.GetProperty("routeState").GetString());
        Assert.Equal("EXACT_17_3Q_14C", r2.GetProperty("requestState").GetString());
        Assert.Equal("EXHAUSTIVE_17", r2.GetProperty("validatorMatrixState").GetString());
        Assert.Equal(
            "EXHAUSTIVE_0_REQUIRED_17_NO_VALIDATOR_REQUIRED_CATALOG_OWNS_MUTATION_BOUNDARY",
            r2.GetProperty("validatorCoverageState").GetString());

        // The exhaustive matrix the R2 certification consumes is the W3-R1 matrix, and it must still
        // cover exactly the requests that exist on disk.
        var matrix = sot.RootElement.GetProperty("productWorkspaceAmsc001W3R1").GetProperty("validatorMatrix");
        var entries = matrix.GetProperty("queries").EnumerateArray()
            .Concat(matrix.GetProperty("commands").EnumerateArray())
            .ToArray();
        Assert.Equal(ExpectedRequests, entries.Length);
        var classified = entries.Select(e => e.GetProperty("request").GetString()!).ToArray();
        Assert.Equal(ExpectedRequests, classified.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(declared, new SortedSet<string>(classified, StringComparer.Ordinal));
        foreach (var entry in entries)
        {
            Assert.Equal("NO_VALIDATOR_REQUIRED", entry.GetProperty("classification").GetString());
            Assert.False(string.IsNullOrWhiteSpace(entry.GetProperty("reason").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(entry.GetProperty("detail").GetString()));

            // Every matrix route is '<VERB> <path>' and every path sits under the module prefix, so the
            // certified matrix cannot drift away from the physical route table.
            var route = entry.GetProperty("route").GetString()!;
            var separator = route.IndexOf(' ');
            Assert.True(separator > 0, "route must be '<VERB> <path>': " + route);
            var path = route[(separator + 1)..];
            Assert.StartsWith(RoutePrefix, path, StringComparison.Ordinal);
            Assert.Contains($"\"{path[RoutePrefix.Length..]}\"", module, StringComparison.Ordinal);
        }

        // Zero module-local validator tree anywhere under the module.
        Assert.False(Directory.Exists(Path.Combine(root, App, "Validation")));
        Assert.False(Directory.Exists(Path.Combine(root, App, "Validators")));
        Assert.False(Directory.Exists(Path.Combine(root, Capability, "Validators")));
        foreach (var file in ProductionSources(root))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("AbstractValidator", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IValidator<", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Canonical_api_results_are_zero_raw_with_two_api_created_paths()
    {
        var root = Repo();
        var module = Read(root, EndpointModule);

        Assert.DoesNotContain("Results.Json", module, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.BadRequest", module, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Problem", module, StringComparison.Ordinal);
        Assert.DoesNotContain("Status201Created", module, StringComparison.Ordinal);
        Assert.DoesNotContain("statusCode:", module, StringComparison.Ordinal);
        Assert.Contains("api.From(", module, StringComparison.Ordinal);
        Assert.Contains("api.FromFailure(", module, StringComparison.Ordinal);

        Assert.Equal(2, Regex.Matches(module, @"api\.Created\(").Count);
        Assert.Equal(2, Regex.Matches(
            module,
            Regex.Escape("api.Created($\"/v1/admin/products/{workspace.Value.ProductId}\", workspace)")).Count);

        foreach (var file in ProductionSources(root))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Status201Created", text, StringComparison.Ordinal);
        }

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")).Replace("\uFEFF", string.Empty));
        var r2 = sot.RootElement.GetProperty("productWorkspaceAmsc001W3R2");
        Assert.Equal("CANONICAL", r2.GetProperty("apiResultPatternState").GetString());
        Assert.Equal("ZERO", r2.GetProperty("rawResultsState").GetString());
        Assert.Equal(2, r2.GetProperty("canonicalCreatedPathCount").GetInt32());
        Assert.Equal(2, r2.GetProperty("canonicalCreatedPaths").GetArrayLength());
    }

    [Fact]
    public void W2_structure_authority_and_contracts_only_boundaries_hold_on_disk()
    {
        var root = Repo();

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")).Replace("\uFEFF", string.Empty));
        var r2 = sot.RootElement.GetProperty("productWorkspaceAmsc001W3R2");
        Assert.Equal("CERTIFIED", r2.GetProperty("structureState").GetString());
        Assert.Equal("PROFESSIONAL_SHALLOW", r2.GetProperty("folderGranularityState").GetString());
        Assert.Equal(5, r2.GetProperty("solutionProjectCount").GetInt32());
        Assert.Equal("EXACT", r2.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("CLEAN", r2.GetProperty("physicalCopyState").GetString());
        Assert.Equal("ENFORCED", r2.GetProperty("rootAllowlistState").GetString());
        Assert.Equal("CANONICAL", r2.GetProperty("solutionExplorerState").GetString());
        Assert.Equal("CONTRACTS_ONLY", r2.GetProperty("crossModuleBoundaryState").GetString());
        Assert.Equal("ZERO", r2.GetProperty("foreignAppInfraDomainCoupling").GetString());

        // W2 authority commit must still resolve in the repository.
        Assert.Single(RunGit(root, "rev-list", "--max-count=1", r2.GetProperty("structureAuthorityCommit").GetString()!));
        Assert.Single(RunGit(root, "rev-list", "--max-count=1", r2.GetProperty("repairAuthorityCommit").GetString()!));
        Assert.Single(RunGit(root, "rev-list", "--max-count=1", r2.GetProperty("supersededCertificationCommit").GetString()!));

        // Solution group holds exactly the 5 module projects.
        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        var start = slnx.IndexOf("<Folder Name=\"/Modules/ProductWorkspace/\">", StringComparison.Ordinal);
        Assert.True(start >= 0, "ProductWorkspace solution folder missing");
        var end = slnx.IndexOf("</Folder>", start, StringComparison.Ordinal);
        var group = slnx[start..end];
        Assert.Equal(5, Regex.Matches(group, "<Project Path=").Count);

        // Zero .gitkeep ceremony and no technical-axis Application roots.
        Assert.Empty(Directory.EnumerateFiles(
            Path.Combine(root, ModuleRoot), ".gitkeep", SearchOption.AllDirectories).ToArray());
        foreach (var folder in new[] { "Commands", "Queries", "Models", "Grid", "Ports", "Validators" })
        {
            Assert.False(Directory.Exists(Path.Combine(root, App, folder)));
        }

        // Contracts-only foreign edges: no foreign Application/Infrastructure/Domain project reference.
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

        // Declared empty Domain boundary assembly and zero foreign persistence anywhere.
        Assert.Empty(Directory.EnumerateFiles(Path.Combine(root, ModuleRoot, "Tooba.ProductWorkspace.Domain"),
            "*.cs", SearchOption.AllDirectories).Where(f => !IsBuildOutput(f)).ToArray());
        foreach (var file in ProductionSources(root))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("CatalogDbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Microsoft.EntityFrameworkCore", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.Host", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AddDbContext<", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Schema_migration_and_global_host_checkpoint_are_preserved()
    {
        var root = Repo();

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")).Replace("\uFEFF", string.Empty));
        var r2 = sot.RootElement.GetProperty("productWorkspaceAmsc001W3R2");
        Assert.Equal("UNCHANGED", r2.GetProperty("schemaMigrationState").GetString());
        Assert.Equal(0, r2.GetProperty("migrationFilesChanged").GetInt32());
        Assert.Equal("NONE_MODULE_OWNS_NO_SCHEMA", r2.GetProperty("persistenceOwnershipState").GetString());
        Assert.Equal("PRESERVED", r2.GetProperty("hostFinalClosure").GetString());
        Assert.Equal("PRESERVED", r2.GetProperty("globalHostCheckpointState").GetString());

        // The module owns no schema and registers no migration; Host keeps no business residue.
        var registry = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs"));
        Assert.DoesNotContain("ProductWorkspace", registry, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/ProductWorkspace")));
        Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.ProductWorkspace.Infrastructure/Persistence")));

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("MapProductWorkspaceModuleEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("AddProductWorkspaceEndpointPresentation", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.ProductWorkspace", program, StringComparison.Ordinal);

        // Repository-global Host root checkpoint is not displaced by a module-local certify wave.
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", sot.RootElement.GetProperty("currentHostCheckpoint").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("USER_REVIEW_HOST_ROOT_FINAL_CERT_001", sot.RootElement.GetProperty("workflowStop").GetString());
    }

    [Fact]
    public void R2_certification_evidence_and_recovery_checkpoint_exist()
    {
        var root = Repo();

        Assert.True(Directory.Exists(Path.Combine(root, EvidenceRoot)));
        Assert.True(File.Exists(Path.Combine(root, EvidenceRoot, "certification.md")));
        Assert.True(File.Exists(Path.Combine(root, EvidenceRoot, "validation.md")));

        var recovery = File.ReadAllText(Path.Combine(root, "docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("ProductWorkspace AMSC W3-R2 fresh Certify", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_PRODUCTWORKSPACE_AMSC_001_W3_R2", recovery, StringComparison.Ordinal);
    }

    /// <summary>
    /// Every endpoint-reachable CQRS request the module declares, discovered from the module's own
    /// Application sources: a <c>public sealed record</c> whose declaration closes a MediatR
    /// <c>IRequest&lt;...&gt;</c>.
    /// </summary>
    private static SortedSet<string> DeclaredRequestTypes(string root)
    {
        var names = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var file in ProductionSources(root))
        {
            var text = File.ReadAllText(file);
            var declaration = Regex.Match(text, @"public\s+sealed\s+record\s+(\w+)");
            if (declaration.Success && Regex.IsMatch(text, @":\s*IRequest<"))
            {
                names.Add(declaration.Groups[1].Value);
            }
        }

        return names;
    }

    /// <summary>Every request type the module binds to a handler, discovered from disk.</summary>
    private static SortedSet<string> HandlerRequestTypes(string root)
    {
        var names = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var file in ProductionSources(root))
        {
            var handler = Regex.Match(File.ReadAllText(file), @"IRequestHandler<\s*(\w+)");
            if (handler.Success)
            {
                names.Add(handler.Groups[1].Value);
            }
        }

        return names;
    }

    private static IEnumerable<string> ProductionSources(string root) =>
        Directory.EnumerateFiles(Path.Combine(root, ModuleRoot), "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsBuildOutput(file));

    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static string Read(string root, string relativePath) =>
        File.ReadAllText(Path.Combine(root, relativePath));

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
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
