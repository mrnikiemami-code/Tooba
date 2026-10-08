using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R1 — CERT_BLOCKER_REPAIR_ONLY lock.
/// <para>
/// The Architect refused final acceptance of the W3 certification on two hard ARCH-COMPLETE-002
/// rules and this guard pins the repaired truth so neither blocker can silently return:
/// </para>
/// <list type="number">
/// <item>
/// <b>B1 — request inventory / validator classification.</b> The module owns exactly <b>17</b>
/// endpoint-reachable CQRS request types (3 queries + 14 commands) over 17 module-owned routes, and
/// every one of them must be classified exactly once as VALIDATOR_REQUIRED or
/// NO_VALIDATOR_REQUIRED. The superseded W3 record said 18 requests / 4 queries and classified
/// 0 REQUIRED / 0 NO_VALIDATOR_REQUIRED, which is not a classification at all.
/// </item>
/// <item>
/// <b>B2 — canonical API result.</b> The two successful 201 paths (create product and create
/// variant) must be produced by the canonical <c>ApiResponseFactory.Created</c>; raw
/// <c>Results.Json(..., statusCode: 201)</c> is forbidden anywhere in the module.
/// </item>
/// </list>
/// <para>
/// The inventory is proven from disk (the module's own <c>IRequest</c> declarations and its mapped
/// routes), not from a folder filename convention, and the SoT matrix is bound back to those routes,
/// so a future wave cannot satisfy the count by editing prose.
/// </para>
/// </summary>
public sealed class ProductWorkspaceModuleAmsc001W3R1CertRepairGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/ProductWorkspace";
    private const string App = ModuleRoot + "/Tooba.ProductWorkspace.Application";
    private const string Endpoints = ModuleRoot + "/Tooba.ProductWorkspace.Endpoints";
    private const string EndpointModule = Endpoints + "/ProductWorkspaceEndpointModule.cs";
    private const string Capability = App + "/Composition/ProductManagement";
    private const string RoutePrefix = "/v1/admin/products";

    /// <summary>Expected endpoint-reachable inventory: 3 queries + 14 commands.</summary>
    private const int ExpectedQueries = 3;

    /// <summary>Expected endpoint-reachable inventory: 14 write commands.</summary>
    private const int ExpectedCommands = 14;

    /// <summary>Expected endpoint-reachable inventory total (also the module-owned route count).</summary>
    private const int ExpectedRequests = ExpectedQueries + ExpectedCommands;

    [Fact]
    public void Endpoint_reachable_inventory_is_exactly_17_proven_from_disk()
    {
        var root = Repo();

        var declared = DeclaredRequestTypes(root);
        Assert.Equal(ExpectedRequests, declared.Count);

        // The three reads and fourteen writes are visible in the capability-first folders too.
        Assert.Equal(ExpectedQueries, Directory.EnumerateFiles(
            Path.Combine(root, Capability, "Queries"), "*Query.cs").Count());
        Assert.Equal(ExpectedCommands, Directory.EnumerateFiles(
            Path.Combine(root, Capability, "Commands"), "*Command.cs").Count());

        // Every request is dispatchable and every request type is declared exactly once.
        Assert.Equal(ExpectedQueries, declared.Count(n => n.EndsWith("Query", StringComparison.Ordinal)));
        Assert.Equal(ExpectedCommands, declared.Count(n => n.EndsWith("Command", StringComparison.Ordinal)));

        // 17 module-owned routes over those 17 requests: the route surface is unchanged by the repair.
        var module = Read(EndpointModule);
        Assert.Equal(ExpectedRequests, Regex.Matches(module, @"\bMap(Get|Post|Put|Patch|Delete)\s*\(").Count);
    }

    [Fact]
    public void Validator_matrix_classifies_every_request_exactly_once_and_sums_to_17()
    {
        var root = Repo();
        var declared = DeclaredRequestTypes(root);

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")).Replace("\uFEFF", string.Empty));
        var r1 = sot.RootElement.GetProperty("productWorkspaceAmsc001W3R1");

        Assert.Equal(ExpectedRequests, r1.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal(ExpectedCommands, r1.GetProperty("endpointReachableCommands").GetInt32());
        Assert.Equal(ExpectedQueries, r1.GetProperty("endpointReachableQueries").GetInt32());
        Assert.Equal(ExpectedRequests, r1.GetProperty("moduleOwnedRoutes").GetInt32());
        Assert.Equal("EXACT_17", r1.GetProperty("requestInventoryState").GetString());
        Assert.Equal("EXHAUSTIVE_17", r1.GetProperty("validatorMatrixState").GetString());

        var matrix = r1.GetProperty("validatorMatrix");
        var queries = matrix.GetProperty("queries").EnumerateArray().ToArray();
        var commands = matrix.GetProperty("commands").EnumerateArray().ToArray();

        // Exact split, exact total, and no duplicated classification entry.
        Assert.Equal(ExpectedQueries, queries.Length);
        Assert.Equal(ExpectedCommands, commands.Length);
        var classified = queries.Concat(commands)
            .Select(e => e.GetProperty("request").GetString()!)
            .ToArray();
        Assert.Equal(ExpectedRequests, classified.Length);
        Assert.Equal(ExpectedRequests, classified.Distinct(StringComparer.Ordinal).Count());

        // The matrix must name exactly the requests that actually exist on disk — no phantom request,
        // no unclassified request.
        Assert.Equal(declared, new SortedSet<string>(classified, StringComparer.Ordinal));

        // Every entry is explicitly classified and carries a concrete durable reason.
        foreach (var entry in queries.Concat(commands))
        {
            Assert.Equal("NO_VALIDATOR_REQUIRED", entry.GetProperty("classification").GetString());
            Assert.False(string.IsNullOrWhiteSpace(entry.GetProperty("reason").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(entry.GetProperty("detail").GetString()));
        }

        Assert.Equal(0, r1.GetProperty("validatorRequiredCount").GetInt32());
        Assert.Equal(ExpectedRequests, r1.GetProperty("noValidatorRequiredCount").GetInt32());
        Assert.Equal(
            "EXHAUSTIVE_0_REQUIRED_17_NO_VALIDATOR_REQUIRED_CATALOG_OWNS_MUTATION_BOUNDARY",
            r1.GetProperty("validatorCoverageState").GetString());
        Assert.Equal(
            "0 VALIDATOR_REQUIRED + 17 NO_VALIDATOR_REQUIRED = 17 = endpointReachableRequests = distinct IRequest types = mapped routes",
            r1.GetProperty("classificationSumCheck").GetString());

        // No module-local validator tree was invented to satisfy the count.
        Assert.Equal("ABSENT_BY_DESIGN", r1.GetProperty("moduleLocalValidatorTreeState").GetString());
        Assert.Equal("NONE_INTRODUCED", r1.GetProperty("moduleLocalValidationCodesState").GetString());
        Assert.False(Directory.Exists(Path.Combine(root, App, "Validation")));
        Assert.False(Directory.Exists(Path.Combine(root, App, "Validators")));
        Assert.False(Directory.Exists(Path.Combine(root, Capability, "Validators")));
        foreach (var file in ProductionSources(root))
        {
            Assert.DoesNotContain("AbstractValidator", File.ReadAllText(file), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Matrix_routes_bind_back_to_the_routes_the_module_actually_maps()
    {
        var root = Repo();
        var module = Read(EndpointModule);

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")).Replace("\uFEFF", string.Empty));
        var matrix = sot.RootElement.GetProperty("productWorkspaceAmsc001W3R1").GetProperty("validatorMatrix");

        var entries = matrix.GetProperty("queries").EnumerateArray()
            .Concat(matrix.GetProperty("commands").EnumerateArray())
            .ToArray();
        Assert.Equal(ExpectedRequests, entries.Length);

        var routes = new List<string>();
        foreach (var entry in entries)
        {
            var route = entry.GetProperty("route").GetString()!;
            var separator = route.IndexOf(' ');
            Assert.True(separator > 0, "route must be '<VERB> <path>': " + route);
            var path = route[(separator + 1)..];
            Assert.StartsWith(RoutePrefix, path, StringComparison.Ordinal);
            routes.Add(route);

            // Each matrix path is the route prefix plus the literal the module maps, so the SoT
            // matrix cannot drift away from the physical route table.
            var literal = path[RoutePrefix.Length..];
            Assert.Contains($"\"{literal}\"", module, StringComparison.Ordinal);
        }

        // 17 distinct verb+path routes; the collection GET and the create POST legitimately share
        // the "/v1/admin/products/" path literal, so distinctness is asserted on the verb too.
        Assert.Equal(ExpectedRequests, routes.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Raw_results_json_is_zero_and_the_two_201_paths_use_canonical_api_created()
    {
        var root = Repo();
        var module = Read(EndpointModule);

        // B2: no raw Results.* payload mapping survives in the module's HTTP surface.
        Assert.DoesNotContain("Results.Json", module, StringComparison.Ordinal);
        Assert.DoesNotContain("Status201Created", module, StringComparison.Ordinal);
        Assert.DoesNotContain("statusCode:", module, StringComparison.Ordinal);

        // Both 201 paths (create product + create variant) go through the canonical factory.
        Assert.Equal(2, Regex.Matches(module, @"api\.Created\(").Count);
        Assert.Equal(2, Regex.Matches(
            module, Regex.Escape("api.Created($\"/v1/admin/products/{workspace.Value.ProductId}\", workspace)")).Count);

        // Nothing anywhere in the module may bypass the canonical result factory.
        foreach (var file in ProductionSources(root))
        {
            Assert.DoesNotContain("Results.Json", File.ReadAllText(file), StringComparison.Ordinal);
        }

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")).Replace("\uFEFF", string.Empty));
        var r1 = sot.RootElement.GetProperty("productWorkspaceAmsc001W3R1");
        Assert.Equal("ZERO", r1.GetProperty("rawResultsJsonState").GetString());
        Assert.Equal(2, r1.GetProperty("rawResultsJsonOccurrencesBefore").GetInt32());
        Assert.Equal(0, r1.GetProperty("rawResultsJsonOccurrencesAfter").GetInt32());
        Assert.Equal("CANONICAL", r1.GetProperty("apiResultPatternState").GetString());
        Assert.Equal("UNCHANGED_PROBLEM_DETAILS_WITHOUT_LOCATION", r1.GetProperty("apiResultFailurePathState").GetString());
    }

    [Fact]
    public void Both_blockers_are_recorded_closed_and_the_w3_record_is_superseded()
    {
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs/architecture/tmar-current-state.json")).Replace("\uFEFF", string.Empty));

        var w3 = sot.RootElement.GetProperty("productWorkspaceAmsc001W3");
        Assert.Equal("SUPERSEDED_PENDING_FRESH_CERTIFY", w3.GetProperty("state").GetString());
        Assert.Equal("TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R1", w3.GetProperty("supersededBy").GetString());

        // The superseded record enumerates exactly which fields it got wrong, so the historical
        // 18-request / 0-of-0 claim cannot be mistaken for current truth.
        var correction = w3.GetProperty("historicalRecordCorrection");
        Assert.Contains("18 -> 17", correction.GetProperty("endpointReachableRequests").GetString()!, StringComparison.Ordinal);
        Assert.Contains("14_COMMANDS_3_QUERIES", correction.GetProperty("cqrsState").GetString()!, StringComparison.Ordinal);
        Assert.Contains("EXHAUSTIVE_0_REQUIRED_17_NO_VALIDATOR_REQUIRED", correction.GetProperty("validatorCoverageState").GetString()!, StringComparison.Ordinal);
        Assert.Contains("NON_CANONICAL", correction.GetProperty("apiResultPatternState").GetString()!, StringComparison.Ordinal);

        var r1 = sot.RootElement.GetProperty("productWorkspaceAmsc001W3R1");
        Assert.Equal("TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R1", r1.GetProperty("task").GetString());
        Assert.Equal("TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3", r1.GetProperty("parentTask").GetString());
        Assert.Equal("CERT_BLOCKER_REPAIR_ONLY", r1.GetProperty("mode").GetString());
        Assert.Equal("c0b86fea", r1.GetProperty("startingHead").GetString());
        Assert.Equal("c0b86feacd897d26172fef7dff47b93db75d8e9d", r1.GetProperty("startingHeadFull").GetString());
        Assert.Equal("READY_FOR_FRESH_CERTIFY", r1.GetProperty("state").GetString());
        Assert.Equal("CLOSED", r1.GetProperty("blockerB1").GetProperty("status").GetString());
        Assert.Equal("CLOSED", r1.GetProperty("blockerB2").GetProperty("status").GetString());
    }

    [Fact]
    public void Structure_manifest_and_global_host_checkpoint_are_untouched_by_this_repair()
    {
        var root = Repo();

        // Structure-State must remain the W2-accepted surface: this wave is forbidden from moving files.
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")).Replace("\uFEFF", string.Empty));
        var r1 = sot.RootElement.GetProperty("productWorkspaceAmsc001W3R1");
        Assert.Equal("UNCHANGED_W2_ACCEPTED", r1.GetProperty("structureState").GetString());
        Assert.Equal("READY_FOR_FRESH_CERTIFY", r1.GetProperty("structureHandoffState").GetString());
        Assert.Equal("NOT_TOUCHED_THIS_WAVE", r1.GetProperty("manifestStructuralState").GetString());
        Assert.Equal("UNCHANGED", r1.GetProperty("schemaMigrationState").GetString());
        Assert.Equal("PRESERVED", r1.GetProperty("globalHostCheckpointState").GetString());
        Assert.Equal("READY", r1.GetProperty("freshCertifyState").GetString());
        Assert.Equal("NONE", r1.GetProperty("guardsWeakened").GetString());
        Assert.Equal("NONE", r1.GetProperty("baselinesWidened").GetString());
        Assert.Equal("NONE", r1.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("USER_REVIEW_PRODUCTWORKSPACE_AMSC_001_W3_R1", r1.GetProperty("workflowStop").GetString());

        // Manifest promotion/demotion is a fresh-Certify obligation, not a repair obligation: the
        // ProductWorkspace entry stays promoted and present exactly once with no pre-cert duplicate.
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")).Replace("\uFEFF", string.Empty));
        var entry = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => string.Equals(m.GetProperty("module").GetString(), "ProductWorkspace", StringComparison.Ordinal));
        Assert.True(entry.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entry.GetProperty("lockVersion").GetString());
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "ProductWorkspace", StringComparison.Ordinal));

        // The module is certified exactly once and the module owns no schema descriptor.
        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => string.Equals(x, "ProductWorkspace", StringComparison.Ordinal)));

        // Repository-global Host root checkpoint is not displaced by a module-local repair wave.
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", sot.RootElement.GetProperty("currentHostCheckpoint").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("USER_REVIEW_HOST_ROOT_FINAL_CERT_001", sot.RootElement.GetProperty("workflowStop").GetString());
    }

    [Fact]
    public void Repair_evidence_and_recovery_checkpoint_exist()
    {
        var root = Repo();

        Assert.True(Directory.Exists(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R1")));
        Assert.True(File.Exists(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R1/cert-repair.md")));

        var recovery = File.ReadAllText(Path.Combine(root, "docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("ProductWorkspace AMSC W3-R1 cert-blocker repair", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_PRODUCTWORKSPACE_AMSC_001_W3_R1", recovery, StringComparison.Ordinal);
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

    /// <summary>Module production <c>.cs</c> files, excluding <c>bin</c>/<c>obj</c> build output.</summary>
    private static IEnumerable<string> ProductionSources(string root) =>
        Directory.EnumerateFiles(Path.Combine(root, ModuleRoot), "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsBuildOutput(file));

    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(Repo(), relativePath));

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
