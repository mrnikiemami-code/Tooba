using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-STORY-AMSC-001-W3 — durable ARCH-COMPLETE-002 certification lock for Story: the SoT/manifest
/// certification records with the AMSC wave lineage, the HTTP_OWNING endpoint/CQRS/validator truth, the
/// W2 structure and cohesion verdict on disk, the Contracts-only microservice-extraction boundary, the
/// canonical result/localization/logging/telemetry mechanisms, the preserved Host closure and the
/// four-wave evidence tree.
/// </summary>
public sealed class StoryModuleAmsc001W3CertGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Story";

    private const string W0Commit = "0c73390a3211e0ee9057e9234d62d3e4f14b5e4e";
    private const string W1Commit = "2a09e7bb7f1ab687435007951160b4bcfefb4c18";
    private const string W2Commit = "4cd9a6cc543ccd307d775dfe703459de7b12c95d";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Story.Contracts",
        "Tooba.Story.Domain",
        "Tooba.Story.Application",
        "Tooba.Story.Infrastructure",
        "Tooba.Story.Endpoints",
    ];

    /// <summary>SoT and manifest carry the honest ARCH-COMPLETE-002 certification for Story.</summary>
    [Fact]
    public void Story_is_arch_complete_002_certified_in_sot_and_manifest()
    {
        var root = Repo();

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var entries = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => string.Equals(m.GetProperty("module").GetString(), "Story", StringComparison.Ordinal))
            .ToArray();
        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());
        var note = entries[0].GetProperty("certificationNote").GetString()!;
        Assert.Contains("TB-TMAR-STORY-AMSC-001-W3", note, StringComparison.Ordinal);
        Assert.Contains("COMPLETE_REFERENCE_PATTERN", note, StringComparison.Ordinal);
        Assert.Contains("HTTP_OWNING", note, StringComparison.Ordinal);
        Assert.Equal(5, entries[0].GetProperty("projects").GetArrayLength());
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "Story", StringComparison.Ordinal));

        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        var w3 = sot.RootElement.GetProperty("storyAmsc001W3");
        Assert.Equal("TB-TMAR-STORY-AMSC-001-W3", w3.GetProperty("task").GetString());
        Assert.Equal("TB-TMAR-STORY-AMSC-001-W2", w3.GetProperty("parentTask").GetString());
        Assert.Equal(W2Commit, w3.GetProperty("startingHead").GetString());
        Assert.Equal("STORY_AMSC_001_CERTIFIED", w3.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", w3.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", w3.GetProperty("lockVersion").GetString());
        Assert.Equal("CERTIFIED", w3.GetProperty("structureState").GetString());
        Assert.True(w3.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("HTTP_OWNING", w3.GetProperty("httpApplicability").GetString());
        Assert.Equal("MODULE_OWNED", w3.GetProperty("endpointOwnershipState").GetString());
        Assert.Equal(0, w3.GetProperty("hostOwnedRouteCount").GetInt32());
        Assert.Equal(25, w3.GetProperty("endpointReachableRequests").GetInt32());
        Assert.Equal(25, w3.GetProperty("totalMediatRRequests").GetInt32());
        Assert.Equal("EXHAUSTIVE_16_VALIDATOR_REQUIRED_9_NO_VALIDATOR_REQUIRED", w3.GetProperty("validatorCoverageState").GetString());
        Assert.Equal(0, w3.GetProperty("unmappedEndpointReachableRequests").GetInt32());
        Assert.Equal("EXACT", w3.GetProperty("pathNamespaceState").GetString());
        Assert.Equal("PROFESSIONAL_SHALLOW", w3.GetProperty("folderGranularityState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("foreignModuleLayerCoupling").GetString());
        Assert.Equal("NONE", w3.GetProperty("crossModuleJoinState").GetString());
        Assert.Equal("NONE", w3.GetProperty("aliasWorkaroundState").GetString());
        Assert.Equal("CANONICAL_ALL_TEN_VALIDATION_AND_FIVE_ERROR_CODES_BILINGUALLY_RESOURCED", w3.GetProperty("localizationState").GetString());
        Assert.Equal("CANONICAL_APIRESPONSEFACTORY_FROM_AND_CREATED_ONLY", w3.GetProperty("apiResultPatternState").GetString());
        Assert.Equal("CANONICAL_ZERO_ILOGGER_ZERO_CONSOLE_ZERO_DEBUG", w3.GetProperty("loggingState").GetString());
        Assert.Equal("NONE", w3.GetProperty("sensitiveLoggingState").GetString());
        Assert.Equal("CANONICAL_NO_PARALLEL_CORRELATION", w3.GetProperty("correlationTraceState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("hostIllegalAuthorityState").GetString());
        Assert.Equal("UNCHANGED_TWO_MIGRATIONS_INITIALSTORY_ADD_STORY_REVIEW_OWNERSHIP", w3.GetProperty("schemaMigrationState").GetString());
        Assert.Equal("ZERO", w3.GetProperty("blockingResidualDebt").GetString());
        Assert.True(w3.GetProperty("microserviceExtractable").GetBoolean());
        Assert.Equal("PRESERVED", w3.GetProperty("globalHostCheckpointState").GetString());
        Assert.Equal("NONE", w3.GetProperty("automaticNextImplementationTask").GetString());

        var certified = sot.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, certified.Count(x => x == "Story"));
    }

    /// <summary>Each wave's recorded starting head is the parent wave's commit, so the chain is verifiable.</summary>
    [Fact]
    public void Story_amsc_wave_lineage_is_recorded_and_chained()
    {
        var root = Repo();
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));

        var w0 = sot.RootElement.GetProperty("storyAmsc001W0");
        var w1 = sot.RootElement.GetProperty("storyAmsc001W1");
        var w2 = sot.RootElement.GetProperty("storyAmsc001W2");
        var w3 = sot.RootElement.GetProperty("storyAmsc001W3");

        Assert.Equal(W0Commit, w0.GetProperty("commit").GetString());
        Assert.Equal("ANALYZE_COMPLETE", w0.GetProperty("state").GetString());
        Assert.Equal(W0Commit, w1.GetProperty("startingHead").GetString());
        Assert.Equal(W1Commit, w1.GetProperty("commit").GetString());
        Assert.Equal("MIGRATE_COMPLETE", w1.GetProperty("state").GetString());
        Assert.Equal(W1Commit, w2.GetProperty("startingHead").GetString());
        Assert.Equal(W2Commit, w2.GetProperty("commit").GetString());
        Assert.Equal("STRUCTURE_COMPLETE", w2.GetProperty("state").GetString());
        Assert.Equal("READY_FOR_CERTIFY", w2.GetProperty("verdict").GetString());
        Assert.Equal(W2Commit, w3.GetProperty("startingHead").GetString());

        // The recorded commits must be real ancestors of the current repository history.
        foreach (var commit in new[] { W0Commit, W1Commit, W2Commit })
        {
            Assert.True(IsAncestor(root, commit), $"{commit} is not in the current history");
        }
    }

    /// <summary>
    /// HTTP_OWNING truth on disk: every route is module-owned, the CQRS surface is real MediatR and the
    /// validator matrix is exactly the classified 16 + 9 over the 25 endpoint-reachable requests.
    /// </summary>
    [Fact]
    public void Story_http_cqrs_and_validator_matrix_match_the_certified_truth()
    {
        var root = Repo();
        var endpoints = Path.Combine(root, ModuleRoot, "Tooba.Story.Endpoints");
        var application = Path.Combine(root, ModuleRoot, "Tooba.Story.Application");

        var endpointSources = Sources(endpoints);
        var applicationSources = Sources(application);

        // 25 routes: 15 admin + 9 seller + 1 storefront.
        Assert.Equal(25, endpointSources.Sum(s => Regex.Matches(s, @"\.Map(Get|Post|Put|Patch|Delete)\(").Count));
        Assert.Equal(15, Regex.Matches(
            File.ReadAllText(Path.Combine(endpoints, "Admin", "StoryAdminEndpoints.cs")),
            @"\.Map(Get|Post|Put|Patch|Delete)\(").Count);
        Assert.Equal(9, Regex.Matches(
            File.ReadAllText(Path.Combine(endpoints, "Seller", "StorySellerEndpoints.cs")),
            @"\.Map(Get|Post|Put|Patch|Delete)\(").Count);
        Assert.Equal(1, Regex.Matches(
            File.ReadAllText(Path.Combine(endpoints, "Storefront", "StoryStorefrontEndpoints.cs")),
            @"\.Map(Get|Post|Put|Patch|Delete)\(").Count);

        var mapGroup = string.Join("\n", endpointSources);
        Assert.Contains("/v1/admin/stories", mapGroup, StringComparison.Ordinal);
        Assert.Contains("/v1/seller/stories", mapGroup, StringComparison.Ordinal);
        Assert.Contains("/v1/storefront/stories", mapGroup, StringComparison.Ordinal);

        // The module owns the HTTP surface through its own composition entry; Host only maps it.
        Assert.Contains("MapStoryModuleEndpoints", endpointSources.Single(s => s.Contains("StoryEndpointModule", StringComparison.Ordinal)), StringComparison.Ordinal);
        var program = File.ReadAllText(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.Contains("app.MapStoryModuleEndpoints();", program, StringComparison.Ordinal);

        // CQRS: 25 real requests, 25 real handlers, dispatched through ISender.
        var joinedApplication = string.Join("\n", applicationSources);
        Assert.Equal(25, Regex.Matches(joinedApplication, @": IRequest(<|\s|$)").Count);
        Assert.Equal(25, Regex.Matches(joinedApplication, @"IRequestHandler<").Count);
        Assert.Equal(16, Regex.Matches(joinedApplication, @"class \w+Validator : AbstractValidator<").Count);
        Assert.Equal(25, endpointSources.Sum(s => Regex.Matches(s, @"sender\.Send\(").Count));

        // No endpoint reaches persistence directly.
        Assert.DoesNotContain("DbContext", string.Join("\n", endpointSources), StringComparison.Ordinal);
        Assert.DoesNotContain("DbSet", string.Join("\n", endpointSources), StringComparison.Ordinal);
    }

    /// <summary>The W2 structure/cohesion verdict is preserved on disk.</summary>
    [Fact]
    public void Story_w2_structure_verdict_is_preserved_on_disk()
    {
        var root = Repo();

        var contracts = Path.Combine(root, ModuleRoot, "Tooba.Story.Contracts");
        var domain = Path.Combine(root, ModuleRoot, "Tooba.Story.Domain");
        var application = Path.Combine(root, ModuleRoot, "Tooba.Story.Application");
        var infrastructure = Path.Combine(root, ModuleRoot, "Tooba.Story.Infrastructure");
        var endpoints = Path.Combine(root, ModuleRoot, "Tooba.Story.Endpoints");

        Assert.Empty(Directory.GetFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.GetFiles(domain, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.GetFiles(application, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.GetFiles(infrastructure, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(
            ["StoryEndpointModule.cs"],
            Directory.GetFiles(endpoints, "*.cs", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).ToArray());

        Assert.True(File.Exists(Path.Combine(infrastructure, "DependencyInjection", "StoryModule.cs")));
        Assert.True(File.Exists(Path.Combine(infrastructure, "Messaging", "StoryOutboxRegistration.cs")));
        Assert.True(File.Exists(Path.Combine(infrastructure, "Directories", "StoryDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(application, "Composition", "StoryOperation.cs")));
        Assert.False(Directory.Exists(Path.Combine(application, "Stories", "Composition")));
        Assert.False(Directory.Exists(Path.Combine(infrastructure, "Directory")));

        // The only god-file candidate stays below the cohesion ceiling.
        var directoryLoc = File.ReadAllLines(Path.Combine(infrastructure, "Directories", "StoryDirectory.cs")).Length;
        Assert.True(directoryLoc <= 800, $"StoryDirectory.cs is {directoryLoc} LOC");

        // Path equals namespace for every production file (exact, no alias workaround).
        foreach (var project in ProductionProjects)
        {
            var projectPath = Path.Combine(root, ModuleRoot, project);
            foreach (var file in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(projectPath, file);
                if (relative.StartsWith("bin", StringComparison.OrdinalIgnoreCase)
                    || relative.StartsWith("obj", StringComparison.OrdinalIgnoreCase)
                    || relative.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                    || relative.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
                    || relative.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                Assert.DoesNotMatch(@"^\s*using\s+[A-Za-z0-9_.]+\s*=", text);

                var directory = Path.GetDirectoryName(relative);
                var expected = string.IsNullOrEmpty(directory)
                    ? project
                    : project + "." + directory.Replace('\\', '.').Replace('/', '.');
                var match = Regex.Match(text, @"^namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Multiline);
                Assert.True(match.Success, $"{relative} has no namespace declaration");
                Assert.Equal(expected, match.Groups[1].Value);
            }
        }

        // Solution Explorer grouping is preserved.
        var doc = XDocument.Load(Path.Combine(root, "src", "backend", "Tooba.slnx"));
        var folder = doc.Root!.Elements("Folder").Single(f =>
            string.Equals((string?)f.Attribute("Name"), "/Modules/Story/", StringComparison.Ordinal));
        Assert.Equal(
            ProductionProjects.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            folder.Elements("Project")
                .Select(p => Path.GetFileNameWithoutExtension((string)p.Attribute("Path")!))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
    }

    /// <summary>Contracts-only boundary with zero foreign coupling: the microservice extraction claim.</summary>
    [Fact]
    public void Story_contracts_only_boundary_makes_microservice_extraction_possible()
    {
        var root = Repo();

        // The direct ProjectReference set is asserted exactly. Tooba.BuildingBlocks is a platform reference
        // that reaches Infrastructure transitively through Tooba.Persistence (which references it) and through
        // the module's own Domain/Application, so it is deliberately absent from the direct list — the same
        // canonical shape used by the certified sibling modules.
        var expected = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Tooba.Story.Contracts"] = [],
            ["Tooba.Story.Domain"] = ["Tooba.BuildingBlocks", "Tooba.Story.Contracts"],
            ["Tooba.Story.Application"] = ["Tooba.BuildingBlocks", "Tooba.Story.Contracts", "Tooba.Story.Domain"],
            ["Tooba.Story.Endpoints"] = ["Tooba.BuildingBlocks", "Tooba.Story.Application", "Tooba.Story.Contracts"],
            ["Tooba.Story.Infrastructure"] = ["Tooba.ModuleContracts", "Tooba.Persistence", "Tooba.Story.Application", "Tooba.Story.Contracts", "Tooba.Story.Domain"],
        };

        foreach (var (project, references) in expected)
        {
            var csproj = XDocument.Load(Path.Combine(root, ModuleRoot, project, $"{project}.csproj"));
            var refs = csproj.Descendants("ProjectReference")
                .Select(x => ((string?)x.Attribute("Include") ?? string.Empty).Replace('\\', '/'))
                .Select(r => r[(r.LastIndexOf('/') + 1)..].Replace(".csproj", string.Empty))
                .OrderBy(r => r, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(references.OrderBy(r => r, StringComparer.Ordinal).ToArray(), refs);

            foreach (var reference in refs)
            {
                Assert.DoesNotContain("Tooba.Host", reference, StringComparison.Ordinal);
                Assert.False(
                    !reference.StartsWith("Tooba.Story.", StringComparison.Ordinal)
                    && (reference.EndsWith(".Application", StringComparison.Ordinal)
                        || reference.EndsWith(".Domain", StringComparison.Ordinal)
                        || reference.EndsWith(".Infrastructure", StringComparison.Ordinal)
                        || reference.EndsWith(".Endpoints", StringComparison.Ordinal)),
                    $"{project} references foreign module layer {reference}");
            }
        }

        var joined = string.Join("\n", ProductionProjects.SelectMany(p => Sources(Path.Combine(root, ModuleRoot, p))));
        var foreignLayer = new Regex(
            @"Tooba\.(?!Story\b|BuildingBlocks\b|ModuleContracts\b|Persistence\b)[A-Za-z]+\.(Application|Domain|Infrastructure|Endpoints)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        var match = foreignLayer.Match(joined);
        Assert.False(match.Success, $"foreign module layer reference {match.Value}");
        Assert.DoesNotContain("Tooba.Host", joined, StringComparison.Ordinal);
        Assert.DoesNotContain("TypeForwardedTo", joined, StringComparison.Ordinal);

        // Exactly one module DbContext, and no persistence type leaks into Application or Endpoints.
        Assert.Equal(1, Regex.Matches(joined, @": DbContext\b").Count);
        foreach (var layer in new[] { "Tooba.Story.Application", "Tooba.Story.Endpoints" })
        {
            Assert.DoesNotContain("DbContext", string.Join("\n", Sources(Path.Combine(root, ModuleRoot, layer))), StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Canonical mechanisms only: no ad-hoc result mapping, one descriptor owner, bilingual resources for
    /// every declared code, and no parallel localization/logging/telemetry mechanism.
    /// </summary>
    [Fact]
    public void Story_uses_canonical_mechanisms_with_no_parallel_invention()
    {
        var root = Repo();
        var application = Path.Combine(root, ModuleRoot, "Tooba.Story.Application");
        var endpoints = Path.Combine(root, ModuleRoot, "Tooba.Story.Endpoints");
        var infrastructure = Path.Combine(root, ModuleRoot, "Tooba.Story.Infrastructure");

        var endpointSources = Sources(endpoints);
        var joinedEndpoints = string.Join("\n", endpointSources);
        Assert.Contains("ApiResponseFactory", joinedEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json", joinedEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.BadRequest", joinedEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Problem", joinedEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ProblemDetails", joinedEndpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("Accept-Language", joinedEndpoints, StringComparison.Ordinal);

        // Every declared code has a resource entry in both cultures.
        var validationCodes = Regex.Matches(
                File.ReadAllText(Path.Combine(application, "Stories", "Validators", "StoryValidators.cs")),
                @"public const string \w+ = ""(story\.[A-Za-z.]+)"";")
            .Select(m => m.Groups[1].Value).ToArray();
        Assert.Equal(10, validationCodes.Length);

        var errorCodes = Regex.Matches(
                File.ReadAllText(Path.Combine(root, ModuleRoot, "Tooba.Story.Contracts", "Errors", "StoryErrorCodes.cs")),
                @"public const string \w+ = ""(story\.[A-Za-z.]+)"";")
            .Select(m => m.Groups[1].Value).ToArray();
        Assert.Equal(5, errorCodes.Length);

        var en = File.ReadAllText(Path.Combine(endpoints, "Resources", "StoryErrors.resx"));
        var fa = File.ReadAllText(Path.Combine(endpoints, "Resources", "StoryErrors.fa.resx"));
        foreach (var code in validationCodes.Concat(errorCodes))
        {
            Assert.Contains($"<data name=\"{code}\"", en, StringComparison.Ordinal);
            Assert.Contains($"<data name=\"{code}\"", fa, StringComparison.Ordinal);
        }

        // Exactly one descriptor owner for the stable codes, and the validation codes stay out of the catalog.
        var contributor = File.ReadAllText(Path.Combine(endpoints, "Errors", "StoryErrorCatalogContributor.cs"));
        foreach (var code in errorCodes)
        {
            Assert.Equal(1, Regex.Matches(contributor, Regex.Escape($"StoryErrorCodes.{CodeMember(code)}")).Count);
        }
        foreach (var code in validationCodes)
        {
            Assert.DoesNotContain(code, contributor, StringComparison.Ordinal);
        }

        Assert.Contains("IErrorResourceSet", File.ReadAllText(Path.Combine(endpoints, "Resources", "StoryErrorResources.cs")), StringComparison.Ordinal);
        Assert.Contains("IErrorCatalogContributor", File.ReadAllText(Path.Combine(endpoints, "StoryEndpointModule.cs")), StringComparison.Ordinal);

        // No parallel logging/telemetry/correlation and no message-text classification.
        var allSources = string.Join("\n", ProductionProjects.SelectMany(p => Sources(Path.Combine(root, ModuleRoot, p))));
        Assert.DoesNotContain("ILogger", allSources, StringComparison.Ordinal);
        Assert.DoesNotContain("Console.WriteLine", allSources, StringComparison.Ordinal);
        Assert.DoesNotContain("Debug.WriteLine", allSources, StringComparison.Ordinal);
        Assert.DoesNotContain("ActivitySource", allSources, StringComparison.Ordinal);
        Assert.DoesNotContain("AsyncLocal", allSources, StringComparison.Ordinal);

        // The only permitted `ex.Message` use is the single canonical typed forwarder that lifts a
        // GridQueryValidationException (which carries a stable ErrorCode) into the platform
        // PlatformHttpException; no failure is ever classified by parsing message text.
        Assert.Equal(1, Regex.Matches(allSources, Regex.Escape("ex.Message")).Count);
        Assert.Equal(
            1,
            Regex.Matches(allSources, Regex.Escape("PlatformHttpException(ex.StatusCode, ex.Message, ex.ErrorCode)")).Count);
        foreach (var classification in new[]
                 {
                     ".Message.Contains(", ".Message.StartsWith(", ".Message ==", "Message.Contains",
                     "message.Contains", "exception.Message", "catch (InvalidOperationException",
                     "new SemanticError(ex.Message", "new SemanticError(exception.Message",
                 })
        {
            Assert.DoesNotContain(classification, allSources, StringComparison.Ordinal);
        }

        // The module owns exactly one DbContext on its own schema.
        var dbContext = File.ReadAllText(Path.Combine(infrastructure, "Persistence", "StoryDbContext.cs"));
        Assert.Contains("public const string Schema = \"story\";", dbContext, StringComparison.Ordinal);
        Assert.Single(Directory.GetFiles(Path.Combine(infrastructure, "Persistence"), "*DbContext.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(2, Directory.GetFiles(Path.Combine(infrastructure, "Persistence", "Migrations"), "*.cs")
            .Count(f => !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
                && !f.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>Host closure is preserved and no illegal Host authority exists.</summary>
    [Fact]
    public void Story_host_closure_is_preserved()
    {
        var root = Repo();

        Assert.False(Directory.Exists(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Story")));
        Assert.Empty(Directory.GetFiles(Path.Combine(root, "src", "backend", "Host", "Tooba.Host"), "Story*.cs",
            SearchOption.AllDirectories));

        var composition = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs"));
        Assert.Contains("using Tooba.Story.Infrastructure.DependencyInjection;", composition, StringComparison.Ordinal);
        Assert.Contains("new StoryModule()", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("StoryDbContext", composition, StringComparison.Ordinal);

        // The single legitimate Host Story seam is the seller authorization adapter.
        var authorizer = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Security/Seller/HostStorySellerAuthorizer.cs"));
        Assert.Contains("IStorySellerAuthorizer", authorizer, StringComparison.Ordinal);

        Assert.Contains("app.MapStoryModuleEndpoints();", File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Program.cs")), StringComparison.Ordinal);
    }

    /// <summary>The four AMSC evidence waves exist and the W3 certification evidence is complete.</summary>
    [Fact]
    public void Story_amsc_evidence_tree_is_present()
    {
        var root = Repo();

        foreach (var wave in new[] { "W0", "W1", "W2", "W3" })
        {
            Assert.True(Directory.Exists(Path.Combine(
                root, "docs", "architecture", "evidence", $"TB-TMAR-STORY-AMSC-001-{wave}")), wave);
        }

        Assert.True(File.Exists(Path.Combine(root, "docs/architecture/evidence/TB-TMAR-STORY-AMSC-001-W0/analyze.md")));
        Assert.True(File.Exists(Path.Combine(root, "docs/architecture/evidence/TB-TMAR-STORY-AMSC-001-W1/migrate.md")));
        Assert.True(File.Exists(Path.Combine(root, "docs/architecture/evidence/TB-TMAR-STORY-AMSC-001-W2/structure.md")));

        var certification = File.ReadAllText(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-STORY-AMSC-001-W3/certification.md"));
        Assert.Contains("ARCH-COMPLETE-002", certification, StringComparison.Ordinal);
        Assert.Contains("COMPLETE_REFERENCE_PATTERN", certification, StringComparison.Ordinal);
        Assert.Contains("HTTP_OWNING", certification, StringComparison.Ordinal);
        Assert.Contains("microservice", certification, StringComparison.OrdinalIgnoreCase);

        // Master Recovery records the Story certification checkpoint.
        var recovery = File.ReadAllText(Path.Combine(root, "docs", "architecture", "TOOBA-TMAR-MASTER-RECOVERY.md"));
        Assert.Contains("Story AMSC module recovery checkpoint", recovery, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-STORY-AMSC-001-W3", recovery, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_STORY_AMSC_001_W3", recovery, StringComparison.Ordinal);

        // Repository-global Host root checkpoint must remain untouched by this module-local task.
        using var sot = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-current-state.json")));
        Assert.Equal("TB-TMAR-HOST-ROOT-FINAL-CERT-001", sot.RootElement.GetProperty("lastAcceptedTask").GetString());
        Assert.Equal("HOST_ROOT_FINAL_CERTIFIED", sot.RootElement.GetProperty("currentHostCheckpoint").GetString());
        Assert.Equal("USER_REVIEW_HOST_ROOT_FINAL_CERT_001", sot.RootElement.GetProperty("workflowStop").GetString());
        Assert.Equal("NONE", sot.RootElement.GetProperty("automaticNextImplementationTask").GetString());
    }

    private static string CodeMember(string code) => code switch
    {
        "story.missing" => "Missing",
        "story.cta.rejected" => "CtaRejected",
        "story.mutation.rejected" => "MutationRejected",
        "story.tenant.missing" => "TenantMissing",
        "story.reviewStatus.invalid" => "ReviewStatusInvalid",
        _ => throw new InvalidOperationException($"unmapped story error code {code}"),
    };

    private static string[] Sources(string projectRoot) =>
        Directory.GetFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText)
            .ToArray();

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
