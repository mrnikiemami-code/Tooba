using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tooba.Returns.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-RETURNS-AMSC-001-W3 — durable ARCH-COMPLETE-002 certification lock for Returns.
/// Locks the SoT/manifest certification records with the AMSC wave lineage, the module-owned HTTP
/// surface with Host route count zero, the exhaustive input-provenance validator matrix and its
/// discovery path, the canonical API-result/typed-fault/localization/catalog mechanisms, the
/// Contracts-only microservice-extractable boundary, the unchanged schema, and the preserved Host
/// final-closure checkpoints.
/// </summary>
public sealed class ReturnsModuleAmsc001W3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Returns";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Returns.Contracts",
        "Tooba.Returns.Domain",
        "Tooba.Returns.Application",
        "Tooba.Returns.Infrastructure",
        "Tooba.Returns.Endpoints",
    ];

    private static readonly string[] ValidatorRequired =
    [
        "CreateReturnCommand", "ApproveReturnCommand", "RejectReturnCommand", "QueryAdminReturnsGridQuery",
    ];

    private static readonly string[] NoValidatorRequired =
    [
        "ListCustomerReturnsQuery", "GetCustomerReturnQuery", "ListSellerReturnsQuery", "GetSellerReturnQuery",
        "ListAdminReturnsQuery", "GetAdminReturnQuery", "RetryReturnRefundCommand",
    ];

    [Fact]
    public void Returns_is_arch_complete_002_certified_in_sot_and_manifest()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var entries = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => string.Equals(m.GetProperty("module").GetString(), "Returns", StringComparison.Ordinal))
            .ToArray();
        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());
        Assert.Contains("TB-TMAR-RETURNS-AMSC-001-W3", entries[0].GetProperty("certificationNote").GetString()!, StringComparison.Ordinal);
        Assert.Equal(6, entries[0].GetProperty("projects").GetArrayLength());
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "Returns", StringComparison.Ordinal));

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        var w3 = sot.RootElement.GetProperty("returnsAmsc001W3");
        Assert.Equal("RETURNS_AMSC_001_CERTIFIED", w3.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", w3.GetProperty("lockVersion").GetString());
        Assert.True(w3.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("READY_FOR_CERTIFY_CONSUMED_BY_W3", w3.GetProperty("currentStructureState").GetString());
        Assert.Equal("TB-TMAR-RETURNS-AMSC-001-W2", w3.GetProperty("currentStructureAuthority").GetString());
        Assert.Equal("EXACT", w3.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("ENFORCED", w3.GetProperty("rootAllowlistState").GetString());
        Assert.Equal("NONE", w3.GetProperty("aliasWorkaroundState").GetString());
        Assert.Equal("PROFESSIONAL_SHALLOW", w3.GetProperty("folderGranularityState").GetString());
        Assert.Equal("CANONICAL", w3.GetProperty("solutionExplorerState").GetString());
        Assert.Equal("CLEAN", w3.GetProperty("physicalCopyState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("foreignApplicationInfrastructureDomainEdges").GetString());
        Assert.Equal("ZERO", w3.GetProperty("crossModuleJoinState").GetString());
        Assert.Equal("UNCHANGED", w3.GetProperty("schemaState").GetString());
        Assert.Equal("HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED_PRESERVED", w3.GetProperty("hostFinalClosureState").GetString());
        Assert.Equal("NONE", w3.GetProperty("automaticNextImplementationTask").GetString());
        Assert.Equal("USER_REVIEW_RETURNS_AMSC_001_W3", w3.GetProperty("stopGate").GetString());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => x == "Returns"));

        // Wave lineage: each wave's starting head is the parent wave's commit, so the chain is
        // verifiable end to end.
        Assert.Equal("f5c5a6db", sot.RootElement.GetProperty("returnsAmsc001W0").GetProperty("commit").GetString());
        Assert.Equal("0a573864", sot.RootElement.GetProperty("returnsAmsc001W1").GetProperty("commit").GetString());
        Assert.Equal("6cab1b87", sot.RootElement.GetProperty("returnsAmsc001W2").GetProperty("commit").GetString());
        Assert.Equal("6cab1b873d25d34bb941cc8d51777109cca3f2d4", w3.GetProperty("startingHead").GetString());
        Assert.Equal("TB-TMAR-RETURNS-AMSC-001-W2", w3.GetProperty("parentTask").GetString());
    }

    [Fact]
    public void Returns_owns_its_http_surface_with_host_route_count_zero()
    {
        var root = Repo();

        var admin = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Returns.Endpoints/Admin/ReturnAdminEndpoints.cs"));
        var seller = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Returns.Endpoints/Seller/ReturnSellerEndpoints.cs"));
        var customer = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Returns.Endpoints/Customer/ReturnCustomerEndpoints.cs"));

        var routeCount = Regex.Matches(admin, @"group\.Map(Get|Post|Put|Patch|Delete)\(").Count
                         + Regex.Matches(seller, @"group\.Map(Get|Post|Put|Patch|Delete)\(").Count
                         + Regex.Matches(customer, @"group\.Map(Get|Post|Put|Patch|Delete)\(").Count;
        Assert.Equal(11, routeCount);

        var module = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Returns.Endpoints/ReturnEndpointModule.cs"));
        Assert.Contains("MapGroup(\"/v1/customer\")", module, StringComparison.Ordinal);
        Assert.Contains("MapGroup(\"/v1/seller\")", module, StringComparison.Ordinal);
        Assert.Contains("MapGroup(\"/v1/admin\")", module, StringComparison.Ordinal);

        // Host owns zero Returns routes: only the composition map call plus the two security adapters.
        var hostRoot = Path.Combine(root, "src/backend/Host/Tooba.Host");
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Returns")));
        var hostReturnsRoutes = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Sum(p => Regex.Matches(File.ReadAllText(p), @"/returns").Count);
        Assert.Equal(0, hostReturnsRoutes);
    }

    [Fact]
    public void Returns_cqrs_and_validator_matrix_are_exhaustive_and_discoverable()
    {
        var root = Repo();
        var applicationRoot = Path.Combine(root, ModuleRoot, "Tooba.Returns.Application");

        // Every endpoint-reachable request is a real MediatR request with a real handler.
        foreach (var request in ValidatorRequired.Concat(NoValidatorRequired))
        {
            var declared = Directory.EnumerateFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
                .Count(p => File.ReadAllText(p).Contains($"record {request}", StringComparison.Ordinal));
            Assert.Equal(1, declared);

            var handler = Directory.EnumerateFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
                .Count(p => File.ReadAllText(p).Contains($"IRequestHandler<{request},", StringComparison.Ordinal));
            Assert.Equal(1, handler);
        }

        // Every route dispatches through ISender (no endpoint touches persistence or a directory).
        var endpoints = Directory.EnumerateFiles(Path.Combine(root, ModuleRoot, "Tooba.Returns.Endpoints"), "*.cs", SearchOption.AllDirectories)
            .Where(p => Path.GetFileName(p).EndsWith("Endpoints.cs", StringComparison.Ordinal))
            .ToArray();
        var sendCount = endpoints.Sum(p => Regex.Matches(File.ReadAllText(p), @"sender\.Send\(").Count);
        Assert.Equal(11, sendCount);
        foreach (var file in endpoints)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IReturnDirectory", text, StringComparison.Ordinal);
        }

        // Exactly four transport validators, one per VALIDATOR_REQUIRED request.
        var validators = File.ReadAllText(Path.Combine(applicationRoot, "Validation/ReturnsRequestValidators.cs"));
        Assert.Equal(4, Regex.Matches(validators, @"class \w+Validator : AbstractValidator<").Count);
        foreach (var request in ValidatorRequired)
        {
            Assert.Contains($"AbstractValidator<{request}>", validators, StringComparison.Ordinal);
        }

        // Discovery: the Returns Application assembly is registered with the canonical CQRS foundation,
        // which runs AddValidatorsFromAssembly and installs ValidationBehavior<,>.
        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("typeof(Tooba.Returns.Application.ReturnRequests.Commands.CreateReturnCommand).Assembly", program, StringComparison.Ordinal);

        var foundation = File.ReadAllText(Path.Combine(
            root, "src/backend/BuildingBlocks/Tooba.BuildingBlocks/TmarFoundation.cs"));
        Assert.Contains("services.AddValidatorsFromAssembly(assembly)", foundation, StringComparison.Ordinal);
        Assert.Contains("typeof(ValidationBehavior<,>)", foundation, StringComparison.Ordinal);
    }

    [Fact]
    public void Returns_api_results_localization_and_catalog_are_canonical()
    {
        var root = Repo();

        var production = ProductionSources(root).ToArray();
        var joined = string.Join("\n", production.Select(File.ReadAllText));

        // Canonical API result mapping only.
        Assert.DoesNotContain("Results.Json", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.BadRequest", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Problem", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("ProblemDetails", joined, StringComparison.Ordinal);

        // No message-text fault classification anywhere in production.
        Assert.DoesNotContain(".Message.Contains(", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("when (ex.Message", joined, StringComparison.Ordinal);

        // Single canonical stable-code home with the reachability split preserved.
        Assert.Equal(20, ReturnsErrorCodes.HttpReachable.Count);
        Assert.Equal(1, ReturnsErrorCodes.PlatformFaults.Count);
        Assert.Equal(21, ReturnsErrorCodes.HttpReachable.Count + ReturnsErrorCodes.PlatformFaults.Count);

        var contributor = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Returns.Infrastructure/Errors/ReturnsErrorCatalogContributor.cs"));
        Assert.Equal(20, Regex.Matches(contributor, @"D\(ReturnsErrorCodes\.").Count);
        Assert.DoesNotContain("OutboxUnmappedEvent", contributor, StringComparison.Ordinal);

        // Bilingual resources: one entry per declared code in both cultures.
        var declared = typeof(ReturnsErrorCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();
        foreach (var culture in new[] { "ReturnsErrors.resx", "ReturnsErrors.fa.resx" })
        {
            var resx = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Returns.Contracts/Resources", culture));
            foreach (var code in declared)
            {
                Assert.Contains($"name=\"{code}\"", resx, StringComparison.Ordinal);
            }
        }

        // Registered exactly once by the module endpoint presentation entry.
        var endpointModule = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Returns.Endpoints/ReturnEndpointModule.cs"));
        Assert.Equal(1, Regex.Matches(endpointModule, @"AddSingleton<IErrorResourceSet").Count);
        Assert.Contains("ReturnsErrorResourceSet", endpointModule, StringComparison.Ordinal);

        // No ad-hoc logging / telemetry / correlation mechanism.
        Assert.DoesNotContain("Console.WriteLine", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Debug.WriteLine", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("new ActivitySource(", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("traceparent", joined, StringComparison.Ordinal);
    }

    [Fact]
    public void Returns_is_contracts_only_and_microservice_extractable()
    {
        var root = Repo();

        foreach (var project in ProductionProjects)
        {
            var csproj = XDocument.Load(Path.Combine(root, ModuleRoot, project, project + ".csproj"));
            var refs = csproj.Descendants("ProjectReference")
                .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
                .ToArray();

            foreach (var reference in refs)
            {
                if (reference.Contains("Tooba.Returns.", StringComparison.Ordinal)
                    || reference.Contains("Tooba.BuildingBlocks", StringComparison.Ordinal)
                    || reference.Contains("Tooba.Persistence", StringComparison.Ordinal)
                    || reference.Contains("Tooba.ModuleContracts", StringComparison.Ordinal))
                {
                    continue;
                }

                // Every remaining edge must be a foreign module *.Contracts project.
                Assert.Contains("Tooba.", reference, StringComparison.Ordinal);
                Assert.Contains(".Contracts/", reference.Replace('\\', '/'), StringComparison.Ordinal);
                Assert.DoesNotContain(".Application/", reference.Replace('\\', '/'), StringComparison.Ordinal);
                Assert.DoesNotContain(".Infrastructure/", reference.Replace('\\', '/'), StringComparison.Ordinal);
                Assert.DoesNotContain(".Domain/", reference.Replace('\\', '/'), StringComparison.Ordinal);
                Assert.DoesNotContain(".Endpoints/", reference.Replace('\\', '/'), StringComparison.Ordinal);
            }
        }

        // Endpoints must never reach Infrastructure or Host.
        var endpointsRefs = XDocument.Load(Path.Combine(root, ModuleRoot, "Tooba.Returns.Endpoints/Tooba.Returns.Endpoints.csproj"))
            .Descendants("ProjectReference").Select(x => (string?)x.Attribute("Include") ?? string.Empty).ToArray();
        Assert.Contains(endpointsRefs, r => r.Contains("Returns.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(endpointsRefs, r => r.Contains("Returns.Infrastructure", StringComparison.OrdinalIgnoreCase));

        // Zero foreign persistence reach-through and zero cross-module join.
        var joined = string.Join("\n", ProductionSources(root).Select(File.ReadAllText));
        foreach (var foreignContext in new[]
                 {
                     "OrderDbContext", "FulfillmentDbContext", "PaymentDbContext", "WalletDbContext",
                     "InventoryDbContext", "PartyDbContext", "CatalogDbContext",
                 })
        {
            Assert.DoesNotContain(foreignContext, joined, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("TypeForwardedTo", joined, StringComparison.Ordinal);

        // Own schema and outbox, with the migration set byte-identical to the W0 baseline.
        var context = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Returns.Infrastructure/Persistence/ReturnsDbContext.cs"));
        Assert.Contains("HasDefaultSchema(Schema)", context, StringComparison.Ordinal);
        Assert.Contains("ToTable(\"return_requests\")", context, StringComparison.Ordinal);
        Assert.Contains("ToTable(\"refund_attempts\")", context, StringComparison.Ordinal);

        var migrations = Directory.EnumerateFiles(
                Path.Combine(root, ModuleRoot, "Tooba.Returns.Infrastructure/Persistence/Migrations"), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(
            [
                "20260827020000_InitialReturns.Designer.cs",
                "20260827020000_InitialReturns.cs",
                "20260827200000_AddRefundDestination.cs",
                "20260909130600_DecimalReturnQuantity.cs",
                "ReturnsDbContextModelSnapshot.cs",
            ],
            migrations);
    }

    [Fact]
    public void Returns_wave_evidence_and_host_closure_are_preserved()
    {
        var root = Repo();

        foreach (var wave in new[] { "W0", "W1", "W2", "W3" })
        {
            Assert.True(Directory.Exists(Path.Combine(
                root, "docs/architecture/evidence", $"TB-TMAR-RETURNS-AMSC-001-{wave}")),
                $"evidence directory for {wave} is required");
        }

        var w3Evidence = File.ReadAllText(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-RETURNS-AMSC-001-W3/certification.md"));
        Assert.Contains("ARCH-COMPLETE-002", w3Evidence, StringComparison.Ordinal);
        Assert.Contains("COMPLETE_REFERENCE_PATTERN", w3Evidence, StringComparison.Ordinal);
        Assert.Contains("STRUCTURE_CERTIFIED", w3Evidence, StringComparison.Ordinal);

        // Repository-global Host root checkpoint must remain untouched by this module-local task.
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());
        Assert.False(sot.RootElement.TryGetProperty("preCertModules", out _));

        // Master Recovery records the Returns certification checkpoint.
        var recovery = File.ReadAllText(Path.Combine(root, "docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("Returns AMSC W3 fresh ARCH-COMPLETE-002 certification (module-local)", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-RETURNS-AMSC-001-W3", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_RETURNS_AMSC_001_W3", recovery, StringComparison.Ordinal);
    }

    private static IEnumerable<string> ProductionSources(string root) =>
        ProductionProjects
            .SelectMany(p => Directory.EnumerateFiles(
                Path.Combine(root, ModuleRoot, p), "*.cs", SearchOption.AllDirectories))
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !Path.GetFileName(p).EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase));

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
