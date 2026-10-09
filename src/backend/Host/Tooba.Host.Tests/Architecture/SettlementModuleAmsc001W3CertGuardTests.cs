using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tooba.Settlement.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-SETTLEMENT-AMSC-001-W3 — durable ARCH-COMPLETE-002 certification lock for Settlement.
/// Locks the SoT/manifest certification records with the AMSC wave lineage, the module-owned HTTP
/// surface with Host route count zero, the exhaustive input-provenance validator matrix and its
/// discovery path, the canonical API-result/typed-fault/localization/catalog mechanisms, the
/// Contracts-only microservice-extractable boundary, the unchanged schema, and the preserved Host
/// final-closure checkpoints.
/// </summary>
public sealed class SettlementModuleAmsc001W3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Settlement";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Settlement.Contracts",
        "Tooba.Settlement.Domain",
        "Tooba.Settlement.Application",
        "Tooba.Settlement.Infrastructure",
        "Tooba.Settlement.Endpoints",
    ];

    private static readonly string[] ValidatorRequired =
    [
        "RequestSellerPayoutCommand", "ProcessAdminPayoutCommand", "RetryAdminPayoutCommand",
        "QueryAdminPayoutGridQuery",
    ];

    private static readonly string[] NoValidatorRequired =
    [
        "GetSellerSettlementBalanceQuery", "ListSellerSettlementEntriesQuery",
        "ListSellerSettlementStatementsQuery", "ListSellerPayoutRequestsQuery",
        "ListAdminSettlementBalancesQuery", "ListAdminPayoutQueueQuery",
    ];

    [Fact]
    public void Settlement_is_arch_complete_002_certified_in_sot_and_manifest()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var entries = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => string.Equals(m.GetProperty("module").GetString(), "Settlement", StringComparison.Ordinal))
            .ToArray();
        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());
        Assert.Contains("TB-TMAR-SETTLEMENT-AMSC-001-W3", entries[0].GetProperty("certificationNote").GetString()!, StringComparison.Ordinal);
        Assert.Equal(3, entries[0].GetProperty("projects").GetArrayLength());
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "Settlement", StringComparison.Ordinal));
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("uncertifiedHttpOwningModules").EnumerateArray(),
            m => string.Equals(m.GetString(), "Settlement", StringComparison.Ordinal));

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        var w3 = sot.RootElement.GetProperty("settlementAmsc001W3");
        Assert.Equal("SETTLEMENT_AMSC_001_CERTIFIED", w3.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", w3.GetProperty("lockVersion").GetString());
        Assert.True(w3.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("READY_FOR_CERTIFY_CONSUMED_BY_W3", w3.GetProperty("currentStructureState").GetString());
        Assert.Equal("TB-TMAR-SETTLEMENT-AMSC-001-W2", w3.GetProperty("currentStructureAuthority").GetString());
        Assert.Equal("HTTP_OWNING", w3.GetProperty("httpApplicability").GetString());
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
        Assert.Equal("USER_REVIEW_SETTLEMENT_AMSC_001_W3", w3.GetProperty("stopGate").GetString());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => x == "Settlement"));

        // Wave lineage: each wave's starting head is the parent wave's commit.
        Assert.Equal("bac4dbe3", sot.RootElement.GetProperty("settlementAmsc001W0").GetProperty("commit").GetString());
        Assert.Equal("4ca4aafc", sot.RootElement.GetProperty("settlementAmsc001W1").GetProperty("commit").GetString());
        Assert.Equal("86ebb4dd", sot.RootElement.GetProperty("settlementAmsc001W2").GetProperty("commit").GetString());

        // W3-R1 provenance correction: the historical W3 wave started from the W2 structure commit, not
        // from W1. The historical W3 evidence stays immutable; the corrected current SoT value is locked
        // here so the wrong lineage cannot silently return.
        Assert.Equal("86ebb4ddb0ba85da480f44a7e797cf1791233f2a", w3.GetProperty("startingHead").GetString());
        Assert.Equal("TB-TMAR-SETTLEMENT-AMSC-001-W2", w3.GetProperty("parentTask").GetString());
    }

    [Fact]
    public void Settlement_owns_its_http_surface_with_host_route_count_zero()
    {
        var root = Repo();

        var seller = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Settlement.Endpoints/Seller/SettlementSellerEndpoints.cs"));
        var admin = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Settlement.Endpoints/Admin/SettlementAdminEndpoints.cs"));

        var routeCount = Regex.Matches(seller, @"group\.Map(Get|Post|Put|Patch|Delete)\(").Count
                         + Regex.Matches(admin, @"group\.Map(Get|Post|Put|Patch|Delete)\(").Count;
        Assert.Equal(10, routeCount);
        Assert.Equal(5, Regex.Matches(seller, @"group\.Map(Get|Post|Put|Patch|Delete)\(").Count);
        Assert.Equal(5, Regex.Matches(admin, @"group\.Map(Get|Post|Put|Patch|Delete)\(").Count);

        var module = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Settlement.Endpoints/SettlementEndpointModule.cs"));
        Assert.Contains("MapSettlementEndpoints", module, StringComparison.Ordinal);
        Assert.Contains("MapGroup(\"/v1/seller\")", module, StringComparison.Ordinal);
        Assert.Contains("MapGroup(\"/v1/admin\")", module, StringComparison.Ordinal);

        // Host owns zero Settlement routes: only the composition map call plus the two security adapters.
        var hostRoot = Path.Combine(root, "src/backend/Host/Tooba.Host");
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Settlement")));
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Grid")));
        var hostSettlementRoutes = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Sum(p => Regex.Matches(File.ReadAllText(p), @"/settlement").Count);
        Assert.Equal(0, hostSettlementRoutes);
    }

    [Fact]
    public void Settlement_cqrs_and_validator_matrix_are_exhaustive_and_discoverable()
    {
        var root = Repo();
        var applicationRoot = Path.Combine(root, ModuleRoot, "Tooba.Settlement.Application");

        // Every endpoint-reachable request is a real MediatR request with a real handler, declared once.
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
        var endpoints = new[]
        {
            Path.Combine(root, ModuleRoot, "Tooba.Settlement.Endpoints/Seller/SettlementSellerEndpoints.cs"),
            Path.Combine(root, ModuleRoot, "Tooba.Settlement.Endpoints/Admin/SettlementAdminEndpoints.cs"),
        };
        Assert.Equal(10, endpoints.Sum(p => Regex.Matches(File.ReadAllText(p), @"sender\.Send\(").Count));
        foreach (var file in endpoints)
        {
            var text = File.ReadAllText(file);
            Assert.Contains("ISender sender", text, StringComparison.Ordinal);
            Assert.Contains("ApiResponseFactory api", text, StringComparison.Ordinal);
            Assert.Contains("api.From(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ISettlementDirectory", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        }

        // Exactly four transport validators, one per VALIDATOR_REQUIRED request.
        var validators = File.ReadAllText(Path.Combine(applicationRoot, "Validation/SettlementRequestValidators.cs"));
        Assert.Equal(4, Regex.Matches(validators, @"class \w+Validator : AbstractValidator<").Count);
        foreach (var request in ValidatorRequired)
        {
            Assert.Contains($"AbstractValidator<{request}>", validators, StringComparison.Ordinal);
        }

        // Validators emit stable machine codes only, never user-facing prose.
        Assert.DoesNotContain("WithMessage(", validators, StringComparison.Ordinal);
        var codes = File.ReadAllText(Path.Combine(applicationRoot, "Validation/SettlementValidationCodes.cs"));
        Assert.All(
            Regex.Matches(codes, @"""([^""]+)""").Select(m => m.Groups[1].Value),
            code => Assert.StartsWith("settlement.validation.", code, StringComparison.Ordinal));

        // Discovery: the Settlement Application assembly is registered with the canonical CQRS
        // foundation, which runs AddValidatorsFromAssembly and installs ValidationBehavior<,>.
        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("typeof(Tooba.Settlement.Application.Payouts.Queries.GetSellerSettlementBalanceQuery).Assembly", program, StringComparison.Ordinal);

        var foundation = File.ReadAllText(Path.Combine(
            root, "src/backend/BuildingBlocks/Tooba.BuildingBlocks/TmarFoundation.cs"));
        Assert.Contains("services.AddValidatorsFromAssembly(assembly)", foundation, StringComparison.Ordinal);
        Assert.Contains("typeof(ValidationBehavior<,>)", foundation, StringComparison.Ordinal);
    }

    [Fact]
    public void Settlement_api_results_localization_and_catalog_are_canonical()
    {
        var root = Repo();
        var joined = string.Join("\n", ProductionSources(root).Select(File.ReadAllText));

        // Canonical API result mapping only.
        Assert.DoesNotContain("Results.Json", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.BadRequest", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Problem", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("ProblemDetails", joined, StringComparison.Ordinal);

        // No message-text fault classification anywhere in production; the typed seam filters on IsKnown.
        Assert.DoesNotContain(".Message.Contains(", joined, StringComparison.Ordinal);
        Assert.DoesNotContain(".Message.StartsWith(", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("when (ex.Message", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("SettlementExceptionMapper", joined, StringComparison.Ordinal);
        var seam = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Settlement.Application/Composition/SettlementOperation.cs"));
        Assert.Contains("SettlementErrorCodes.IsKnown", seam, StringComparison.Ordinal);
        Assert.Contains("ContractOperationException", seam, StringComparison.Ordinal);

        // Single canonical stable-code home with the reachability split preserved.
        Assert.Equal(17, SettlementErrorCodes.HttpReachable.Count);
        Assert.Equal(1, SettlementErrorCodes.PlatformFaults.Count);
        Assert.Equal(18, SettlementErrorCodes.HttpReachable.Count + SettlementErrorCodes.PlatformFaults.Count);

        var contributor = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Settlement.Infrastructure/Errors/SettlementErrorCatalogContributor.cs"));
        Assert.Equal(17, Regex.Matches(contributor, @"D\(SettlementErrorCodes\.").Count);
        Assert.DoesNotContain("OutboxUnmappedEvent", contributor, StringComparison.Ordinal);

        // Bilingual resources: one entry per declared code in both cultures.
        var declared = typeof(SettlementErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToArray();
        Assert.Equal(18, declared.Length);
        foreach (var culture in new[] { "SettlementErrors.resx", "SettlementErrors.fa.resx" })
        {
            var resx = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Settlement.Contracts/Resources", culture));
            foreach (var code in declared)
            {
                Assert.Contains($"name=\"{code}\"", resx, StringComparison.Ordinal);
            }
        }

        // Registered exactly once by the module endpoint presentation entry.
        var endpointModule = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Settlement.Endpoints/SettlementEndpointModule.cs"));
        Assert.Equal(1, Regex.Matches(endpointModule, @"AddSingleton<IErrorResourceSet").Count);
        Assert.Contains("SettlementErrorResourceSet", endpointModule, StringComparison.Ordinal);

        // No ad-hoc logging / telemetry / correlation mechanism.
        Assert.DoesNotContain("Console.WriteLine", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("Debug.WriteLine", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("new ActivitySource(", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("traceparent", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("new Meter(", joined, StringComparison.Ordinal);
    }

    [Fact]
    public void Settlement_is_contracts_only_and_microservice_extractable()
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
                if (reference.Contains("Tooba.Settlement.", StringComparison.Ordinal)
                    || reference.Contains("Tooba.BuildingBlocks", StringComparison.Ordinal)
                    || reference.Contains("Tooba.Persistence", StringComparison.Ordinal)
                    || reference.Contains("Tooba.ModuleContracts", StringComparison.Ordinal))
                {
                    continue;
                }

                // Every remaining edge must be a foreign module *.Contracts project.
                var normalized = reference.Replace('\\', '/');
                Assert.Contains("Tooba.", normalized, StringComparison.Ordinal);
                Assert.Contains(".Contracts/", normalized, StringComparison.Ordinal);
                Assert.DoesNotContain(".Application/", normalized, StringComparison.Ordinal);
                Assert.DoesNotContain(".Infrastructure/", normalized, StringComparison.Ordinal);
                Assert.DoesNotContain(".Domain/", normalized, StringComparison.Ordinal);
                Assert.DoesNotContain(".Endpoints/", normalized, StringComparison.Ordinal);
            }
        }

        // Endpoints must never reach Infrastructure or Host.
        var endpointsRefs = XDocument.Load(Path.Combine(root, ModuleRoot, "Tooba.Settlement.Endpoints/Tooba.Settlement.Endpoints.csproj"))
            .Descendants("ProjectReference").Select(x => (string?)x.Attribute("Include") ?? string.Empty).ToArray();
        Assert.Contains(endpointsRefs, r => r.Contains("Settlement.Application", StringComparison.Ordinal));
        Assert.Contains(endpointsRefs, r => r.Contains("Settlement.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(endpointsRefs, r => r.Contains("Settlement.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointsRefs, r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));

        // Application must never reach Infrastructure or Host.
        var applicationRefs = XDocument.Load(Path.Combine(root, ModuleRoot, "Tooba.Settlement.Application/Tooba.Settlement.Application.csproj"))
            .Descendants("ProjectReference").Select(x => (string?)x.Attribute("Include") ?? string.Empty).ToArray();
        Assert.DoesNotContain(applicationRefs, r => r.Contains("Settlement.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(applicationRefs, r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));

        // Zero foreign persistence reach-through and zero cross-module join.
        var joined = string.Join("\n", ProductionSources(root).Select(File.ReadAllText));
        foreach (var foreignContext in new[]
                 {
                     "OrderDbContext", "PaymentDbContext", "ReturnsDbContext", "PartyDbContext",
                     "CartDbContext", "OfferDbContext", "InventoryDbContext",
                 })
        {
            Assert.DoesNotContain(foreignContext, joined, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("TypeForwardedTo", joined, StringComparison.Ordinal);
        var globalUsings = Directory.EnumerateFiles(Path.Combine(root, ModuleRoot), "GlobalUsings*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(globalUsings);

        // Own schema, own outbox, and the migration set byte-identical to the W0 baseline.
        var context = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Settlement.Infrastructure/Persistence/SettlementDbContext.cs"));
        Assert.Contains("Schema = \"settlement\"", context, StringComparison.Ordinal);
        Assert.Contains("HasDefaultSchema(Schema)", context, StringComparison.Ordinal);

        var migrations = Directory.EnumerateFiles(
                Path.Combine(root, ModuleRoot, "Tooba.Settlement.Infrastructure/Persistence/Migrations"), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(
            [
                "20260827030000_InitialSettlement.Designer.cs",
                "20260827030000_InitialSettlement.cs",
                "SettlementDbContextModelSnapshot.cs",
            ],
            migrations);

        var outbox = File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Settlement.Infrastructure/Messaging/SettlementOutboxRegistration.cs"));
        Assert.Contains("SettlementDbContext.Schema", outbox, StringComparison.Ordinal);
    }

    [Fact]
    public void Settlement_wave_evidence_and_host_closure_are_preserved()
    {
        var root = Repo();

        foreach (var wave in new[] { "W0", "W1", "W2", "W3" })
        {
            Assert.True(Directory.Exists(Path.Combine(
                root, "docs/architecture/evidence", $"TB-TMAR-SETTLEMENT-AMSC-001-{wave}")),
                $"evidence directory for {wave} is required");
        }

        var w3Evidence = File.ReadAllText(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-SETTLEMENT-AMSC-001-W3/certification.md"));
        Assert.Contains("ARCH-COMPLETE-002", w3Evidence, StringComparison.Ordinal);
        Assert.Contains("COMPLETE_REFERENCE_PATTERN", w3Evidence, StringComparison.Ordinal);
        Assert.Contains("STRUCTURE_CERTIFIED", w3Evidence, StringComparison.Ordinal);

        // Repository-global Host root checkpoint must remain untouched by this module-local task.
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());

        // Master Recovery records the Settlement certification checkpoint.
        var recovery = File.ReadAllText(Path.Combine(root, "docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("Settlement AMSC W3 fresh ARCH-COMPLETE-002 certification (module-local)", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-SETTLEMENT-AMSC-001-W3", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_SETTLEMENT_AMSC_001_W3", recovery, StringComparison.Ordinal);
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
