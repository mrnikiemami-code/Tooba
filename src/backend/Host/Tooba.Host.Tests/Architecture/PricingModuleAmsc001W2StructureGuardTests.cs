using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PRICING-AMSC-001-W2 — scoped structure gate (tooba-architecture-structure).
/// Pins the capability-first shallow physical tree, the exact path↔namespace rule for the four
/// production projects, the manifest root allowlists/forbidden lists, the canonical
/// <c>/Modules/Pricing/</c> solution grouping and the absence of stale/duplicate physical copies of
/// the relocated Contracts localization surface. Behavior is unchanged by W2; this guard only locks
/// the physical shape.
///
/// TB-TMAR-PRICING-AMSC-001-W3-R2 demoted Pricing to the pre-cert structure state and removed the
/// ceremonial Endpoints project, so the Endpoints ceremony assertions are retired and the module is
/// now pinned as INTERNAL_ONLY (four production projects + Tests).
/// </summary>
public sealed class PricingModuleAmsc001W2StructureGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Pricing";
    private const string ManifestPath = "docs/architecture/tmar-module-structure-manifests.json";

    private static readonly string[] Projects =
    [
        "Tooba.Pricing.Contracts",
        "Tooba.Pricing.Domain",
        "Tooba.Pricing.Application",
        "Tooba.Pricing.Infrastructure",
    ];

    /// <summary>
    /// Contracts is a boundary surface: capability folders only (Dtos/Errors/Ports/Seller/Resources) with
    /// no root dump. Pricing owns zero endpoint-reachable requests, so no CQRS/request tree may appear.
    /// </summary>
    [Fact]
    public void Contracts_and_Domain_are_capability_first_with_no_root_dump()
    {
        var contracts = Path.Combine(Repo(), ModuleRoot, "Tooba.Pricing.Contracts");
        Assert.Empty(Directory.GetFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(
            new[] { "Dtos", "Errors", "Ports", "Resources", "Seller" },
            Directory.GetDirectories(contracts)
                .Select(Path.GetFileName!)
                .Where(n => n is not ("bin" or "obj" or "artifacts"))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());

        var domain = Path.Combine(Repo(), ModuleRoot, "Tooba.Pricing.Domain");
        // The only permitted root source file is the GlobalUsings namespace bridge (certified
        // Catalog/Order Domain precedent); it declares no namespace and holds no type.
        Assert.Equal(
            new[] { "GlobalUsings.cs" },
            Directory.GetFiles(domain, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            new[] { "Aggregates", "Enums", "Events", "ValueObjects" },
            Directory.GetDirectories(domain)
                .Select(Path.GetFileName!)
                .Where(n => n is not ("bin" or "obj" or "artifacts"))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());

        // The retired W0/W1 defect: a duplicated stable-code home and a Contracts root dump.
        Assert.False(Directory.Exists(Path.Combine(domain, "Errors")),
            "Domain/Errors must stay retired; the canonical home is Contracts/Errors");
        Assert.False(File.Exists(Path.Combine(contracts, "PricingErrorCodes.cs")),
            "Contracts root must not resurrect the stable-code dump");
        Assert.False(File.Exists(Path.Combine(contracts, "SellerOfferPricingContracts.cs")),
            "Contracts root must not resurrect the seller boundary dump");
    }

    /// <summary>Application stays a shallow two-folder capability surface (zero CQRS ceremony).</summary>
    [Fact]
    public void Application_stays_shallow_with_no_technical_axis_or_use_case_leaves()
    {
        var application = Path.Combine(Repo(), ModuleRoot, "Tooba.Pricing.Application");
        Assert.Empty(Directory.GetFiles(application, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(
            new[] { "Composition", "Ports" },
            Directory.GetDirectories(application)
                .Select(Path.GetFileName!)
                .Where(n => n is not ("bin" or "obj" or "artifacts"))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());

        foreach (var banned in new[] { "Commands", "Queries", "Validators", "Models", "Handlers", "Requests" })
        {
            Assert.False(Directory.Exists(Path.Combine(application, banned)),
                $"Application/{banned} must not appear: Pricing owns zero endpoint-reachable requests");
        }

        foreach (var dir in Directory.EnumerateDirectories(application, "*", SearchOption.AllDirectories))
        {
            if (dir.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || dir.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var files = Directory.EnumerateFiles(dir, "*.cs", SearchOption.TopDirectoryOnly).ToList();
            if (files.Count != 1 || Directory.EnumerateDirectories(dir).Any())
            {
                continue;
            }

            var folderName = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar));
            Assert.False(
                folderName.EndsWith("Command", StringComparison.Ordinal)
                    || folderName.EndsWith("Query", StringComparison.Ordinal)
                    || folderName.EndsWith("UseCase", StringComparison.Ordinal),
                $"per-use-case request leaf folder {dir}");
        }
    }

    /// <summary>Infrastructure uses the canonical integration folders; no loose module files at root.</summary>
    [Fact]
    public void Infrastructure_uses_canonical_integration_folders()
    {
        var infra = Path.Combine(Repo(), ModuleRoot, "Tooba.Pricing.Infrastructure");
        Assert.Empty(Directory.GetFiles(infra, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(
            new[] { "Adapters", "DependencyInjection", "Events", "Outbox", "Persistence" },
            Directory.GetDirectories(infra)
                .Select(Path.GetFileName!)
                .Where(n => n is not ("bin" or "obj" or "artifacts"))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());

        Assert.True(File.Exists(Path.Combine(infra, "DependencyInjection", "PricingModule.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Outbox", "PricingOutboxRegistration.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Adapters", "PricingModuleMigration.cs")));
        Assert.True(File.Exists(Path.Combine(
            infra, "Persistence", "Migrations", "PricingDbContextModelSnapshot.cs")));
        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")),
            "migrations must live under Persistence/Migrations");
    }

    /// <summary>
    /// W3-R2 retired the ceremonial Endpoints project: Pricing is INTERNAL_ONLY, owns zero HTTP
    /// routes and must not resurrect an Endpoints project, an empty route group or its presentation
    /// extension. The Contracts localization surface stays the single authoritative home.
    /// </summary>
    [Fact]
    public void Ceremonial_endpoints_project_is_retired()
    {
        var root = Repo();
        Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.Pricing.Endpoints")),
            "the ceremonial Tooba.Pricing.Endpoints project must stay retired (INTERNAL_ONLY)");
        Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.Pricing.Tests", "Endpoints")),
            "the Endpoints test folder must stay retired");

        foreach (var file in ProductionSources(root))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("namespace Tooba.Pricing.Endpoints", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPricingModule", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AddPricingEndpointPresentation", text, StringComparison.Ordinal);
            Assert.DoesNotContain("PricingEndpointModule", text, StringComparison.Ordinal);
            Assert.DoesNotContain("\"/v1/pricing\"", text, StringComparison.Ordinal);
        }

        // The Host composition path is retired too.
        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("MapPricingModule", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddPricingEndpointPresentation", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Pricing.Endpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/v1/pricing\"", program, StringComparison.Ordinal);

        var hostCsproj = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Tooba.Host.csproj"));
        Assert.DoesNotContain("Tooba.Pricing.Endpoints", hostCsproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.Pricing.Infrastructure.csproj", hostCsproj, StringComparison.Ordinal);

        // The presentation registration moved to the Pricing Infrastructure composition root, exactly
        // once, following the certified Inventory precedent.
        var module = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Pricing.Infrastructure", "DependencyInjection", "PricingModule.cs"));
        Assert.Equal(1, Regex.Matches(module, @"AddSingleton<\s*IErrorCatalogContributor,\s*PricingErrorCatalogContributor>").Count);
        Assert.Equal(1, Regex.Matches(module, @"AddSingleton<\s*IErrorResourceSet,\s*PricingErrorResourceSet>").Count);
        Assert.Contains("using Tooba.Pricing.Contracts.Errors;", module, StringComparison.Ordinal);
    }

    /// <summary>The moved localization surface has exactly one authoritative physical home.</summary>
    [Fact]
    public void Relocated_localization_surface_has_a_single_authoritative_home()
    {
        var contracts = Path.Combine(Repo(), ModuleRoot, "Tooba.Pricing.Contracts");
        foreach (var relative in new[]
                 {
                     "Errors/PricingErrorCodes.cs",
                     "Errors/PricingErrorCatalogContributor.cs",
                     "Errors/PricingErrorResourceSet.cs",
                     "Resources/PricingErrors.resx",
                     "Resources/PricingErrors.fa.resx",
                 })
        {
            Assert.True(File.Exists(Path.Combine(contracts, relative.Replace('/', Path.DirectorySeparatorChar))),
                $"missing Contracts/{relative}");
        }

        var declarations = Directory
            .EnumerateFiles(Path.Combine(Repo(), ModuleRoot), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}Tooba.Pricing.Tests{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Count(p => File.ReadAllText(p).Contains("class PricingErrorCatalogContributor", StringComparison.Ordinal));
        Assert.Equal(1, declarations);

        var resourceSets = Directory
            .EnumerateFiles(Path.Combine(Repo(), ModuleRoot), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}Tooba.Pricing.Tests{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Count(p => File.ReadAllText(p).Contains("class PricingErrorResourceSet", StringComparison.Ordinal));
        Assert.Equal(1, resourceSets);
    }

    /// <summary>Every production <c>.cs</c> namespace equals its path-derived namespace.</summary>
    [Fact]
    public void Path_derived_namespaces_are_exact()
    {
        var root = Repo();
        foreach (var project in Projects)
        {
            var projectPath = Path.Combine(root, ModuleRoot, project);
            foreach (var file in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
            {
                var relative = file[Path.GetFullPath(projectPath).Length..]
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (relative.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || relative.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                // Global using files are pure import aggregation and declare no namespace by design.
                if (Path.GetFileName(relative).StartsWith("GlobalUsings", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var dir = Path.GetDirectoryName(relative);
                var expected = string.IsNullOrEmpty(dir)
                    ? project
                    : project + "." + dir.Replace(Path.DirectorySeparatorChar, '.')
                        .Replace(Path.AltDirectorySeparatorChar, '.');
                var match = Regex.Match(
                    File.ReadAllText(file).TrimStart('\uFEFF'),
                    @"^namespace\s+([A-Za-z0-9_.]+)",
                    RegexOptions.Multiline);
                Assert.True(match.Success, $"no namespace in {project}/{relative}");
                Assert.Equal(expected, match.Groups[1].Value);
            }
        }
    }

    /// <summary>The manifest carries Pricing in the pre-cert structure state (W3-R2 repair).</summary>
    [Fact]
    public void Root_allowlists_and_forbidden_lists_match_the_manifest()
    {
        var root = Repo();
        using var manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, ManifestPath)).Replace("\uFEFF", string.Empty));
        // W3-R2 demoted Pricing out of the certified modules array into the pre-cert state because the
        // former W3 certification retained a ceremonial Endpoints project. The structural allowlists
        // asserted below are unchanged; only the certification claim is honest again.
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("modules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "Pricing", StringComparison.Ordinal));
        var module = manifest.RootElement.GetProperty("preCertModules").EnumerateArray()
            .Single(m => string.Equals(m.GetProperty("module").GetString(), "Pricing", StringComparison.Ordinal));
        Assert.False(module.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("READY_FOR_CERTIFY", module.GetProperty("structureState").GetString());

        foreach (var project in module.GetProperty("projects").EnumerateArray())
        {
            var projectName = project.GetProperty("projectName").GetString()!;
            var projectPath = Path.Combine(root, ModuleRoot, projectName);
            Assert.True(Directory.Exists(projectPath), projectPath);

            var allowlist = project.GetProperty("rootAllowlist").EnumerateArray().Select(x => x.GetString()!)
                .OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var actualRoot = Directory.GetFiles(projectPath, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Assert.Equal(allowlist, actualRoot);

            foreach (var forbidden in project.GetProperty("forbiddenRootFiles").EnumerateArray())
            {
                Assert.False(File.Exists(Path.Combine(projectPath, forbidden.GetString()!)),
                    $"{projectName} resurrected forbidden root file {forbidden.GetString()}");
            }

            foreach (var forbiddenFolder in project.GetProperty("forbiddenTopLevelFolders").EnumerateArray())
            {
                Assert.False(Directory.Exists(Path.Combine(projectPath, forbiddenFolder.GetString()!)),
                    $"{projectName} resurrected forbidden top-level folder {forbiddenFolder.GetString()}");
            }
        }
    }

    /// <summary>The canonical <c>/Modules/Pricing/</c> solution grouping holds exactly five projects.</summary>
    [Fact]
    public void Solution_grouping_is_canonical_modules_pricing()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src/backend/Tooba.slnx"));
        var folderStart = slnx.IndexOf("<Folder Name=\"/Modules/Pricing/\">", StringComparison.Ordinal);
        Assert.True(folderStart >= 0, "missing /Modules/Pricing/ solution folder");
        var folderEnd = slnx.IndexOf("</Folder>", folderStart, StringComparison.Ordinal);
        var group = slnx[folderStart..folderEnd];
        foreach (var project in Projects.Concat(["Tooba.Pricing.Tests"]))
        {
            Assert.Contains($"Modules/Pricing/{project}/{project}.csproj", group, StringComparison.Ordinal);
        }

        Assert.Equal(5, Regex.Matches(group, @"<Project Path=").Count);
        Assert.DoesNotContain("Tooba.Pricing.Endpoints", group, StringComparison.Ordinal);
    }

    /// <summary>
    /// Every Pricing <c>.cs</c> outside the Test project and outside <c>bin</c>/<c>obj</c>. The Test
    /// project is excluded because the durable guards necessarily name the retired identifiers as
    /// negative assertions.
    /// </summary>
    private static IEnumerable<string> ProductionSources(string root)
    {
        var testsRoot = Path.Combine(root, ModuleRoot, "Tooba.Pricing.Tests") + Path.DirectorySeparatorChar;
        return Directory.EnumerateFiles(Path.Combine(root, ModuleRoot), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !file.StartsWith(testsRoot, StringComparison.Ordinal));
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
