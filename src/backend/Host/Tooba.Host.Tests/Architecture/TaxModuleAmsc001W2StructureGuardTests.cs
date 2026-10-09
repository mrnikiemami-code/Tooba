using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-TAX-AMSC-001-W2 — scoped structure gate (tooba-architecture-structure).
/// Pins the INTERNAL_ONLY physical shape (four production projects + Tests, zero Endpoints
/// ceremony), the capability-first shallow folders, the exact path↔namespace rule, the manifest
/// root allowlists/forbidden lists, the canonical <c>/Modules/Tax/</c> solution grouping and the
/// absence of stale/duplicate physical copies of the relocated error/localization surface.
/// Behavior is unchanged by W2; this guard only locks the physical shape.
/// </summary>
public sealed class TaxModuleAmsc001W2StructureGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Tax";
    private const string ManifestPath = "docs/architecture/tmar-module-structure-manifests.json";

    private static readonly string[] Projects =
    [
        "Tooba.Tax.Contracts",
        "Tooba.Tax.Domain",
        "Tooba.Tax.Application",
        "Tooba.Tax.Infrastructure",
    ];

    /// <summary>
    /// Tax owns zero HTTP routes and zero endpoint-reachable requests (W0 applicability gate), so it
    /// is INTERNAL_ONLY: the ceremonial <c>Tooba.Tax.Endpoints</c> project, its empty <c>/v1/tax</c>
    /// route group and its Host mapping were retired by W2 and must not resurrect.
    /// </summary>
    [Fact]
    public void Ceremonial_endpoints_project_stays_retired_internal_only()
    {
        var root = Repo();
        Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.Tax.Endpoints")),
            "the ceremonial Tooba.Tax.Endpoints project must stay retired (INTERNAL_ONLY)");
        Assert.False(Directory.Exists(Path.Combine(root, ModuleRoot, "Tooba.Tax.Tests", "Endpoints")),
            "the Endpoints test folder must stay retired");

        foreach (var file in ProductionSources(root))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("namespace Tooba.Tax.Endpoints", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapTaxModule", text, StringComparison.Ordinal);
            Assert.DoesNotContain("TaxEndpointModule", text, StringComparison.Ordinal);
            Assert.DoesNotContain("\"/v1/tax\"", text, StringComparison.Ordinal);
        }

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.DoesNotContain("MapTaxModule", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Tax.Endpoints", program, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/v1/tax\"", program, StringComparison.Ordinal);

        var hostCsproj = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Tooba.Host.csproj"));
        Assert.DoesNotContain("Tooba.Tax.Endpoints", hostCsproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.Tax.Infrastructure.csproj", hostCsproj, StringComparison.Ordinal);
    }

    /// <summary>Contracts/Domain are boundary + capability surfaces with no root dump.</summary>
    [Fact]
    public void Contracts_and_Domain_are_capability_first_with_no_root_dump()
    {
        var root = Repo();
        var contracts = Path.Combine(root, ModuleRoot, "Tooba.Tax.Contracts");
        Assert.Empty(Directory.GetFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(
            new[] { "Dtos", "Errors", "Ports", "Resources" },
            Directory.GetDirectories(contracts)
                .Select(Path.GetFileName!)
                .Where(n => n is not ("bin" or "obj" or "artifacts"))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());

        var domain = Path.Combine(root, ModuleRoot, "Tooba.Tax.Domain");
        // The only permitted root source file is the GlobalUsings namespace bridge (certified
        // Catalog/Order/Pricing Domain precedent); it declares no namespace and holds no type.
        Assert.Equal(
            new[] { "GlobalUsings.cs" },
            Directory.GetFiles(domain, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
        Assert.Equal(
            new[] { "Aggregates", "Enums", "Events", "Policies" },
            Directory.GetDirectories(domain)
                .Select(Path.GetFileName!)
                .Where(n => n is not ("bin" or "obj" or "artifacts"))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());

        // The retired W0/W1 defect: a duplicated stable-code home and a Contracts root dump.
        Assert.False(Directory.Exists(Path.Combine(domain, "Errors")),
            "Domain/Errors must stay retired; the canonical home is Contracts/Errors");
        Assert.False(File.Exists(Path.Combine(contracts, "TaxErrorCodes.cs")),
            "Contracts root must not resurrect the stable-code dump");
    }

    /// <summary>Application stays a shallow two-folder surface (zero CQRS ceremony).</summary>
    [Fact]
    public void Application_stays_shallow_with_no_technical_axis_or_use_case_leaves()
    {
        var root = Repo();
        var application = Path.Combine(root, ModuleRoot, "Tooba.Tax.Application");
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
                $"Application/{banned} must not appear: Tax owns zero endpoint-reachable requests");
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
        var infra = Path.Combine(Repo(), ModuleRoot, "Tooba.Tax.Infrastructure");
        Assert.Empty(Directory.GetFiles(infra, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(
            new[] { "Adapters", "DependencyInjection", "Events", "Outbox", "Persistence" },
            Directory.GetDirectories(infra)
                .Select(Path.GetFileName!)
                .Where(n => n is not ("bin" or "obj" or "artifacts"))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());

        Assert.True(File.Exists(Path.Combine(infra, "DependencyInjection", "TaxModule.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Outbox", "TaxOutboxRegistration.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Adapters", "TaxModuleMigration.cs")));
        Assert.True(File.Exists(Path.Combine(
            infra, "Persistence", "Migrations", "TaxDbContextModelSnapshot.cs")));
        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")),
            "migrations must live under Persistence/Migrations");
    }

    /// <summary>The error/localization surface has exactly one authoritative physical home.</summary>
    [Fact]
    public void Localization_surface_has_a_single_authoritative_home()
    {
        var root = Repo();
        var contracts = Path.Combine(root, ModuleRoot, "Tooba.Tax.Contracts");
        foreach (var relative in new[]
                 {
                     "Errors/TaxErrorCodes.cs",
                     "Errors/TaxErrorCatalogContributor.cs",
                     "Errors/TaxErrorResourceSet.cs",
                     "Resources/TaxErrors.resx",
                     "Resources/TaxErrors.fa.resx",
                 })
        {
            Assert.True(File.Exists(Path.Combine(contracts, relative.Replace('/', Path.DirectorySeparatorChar))),
                $"missing Contracts/{relative}");
        }

        var declarations = ProductionSources(root)
            .Count(p => File.ReadAllText(p).Contains("class TaxErrorCatalogContributor", StringComparison.Ordinal));
        Assert.Equal(1, declarations);

        var resourceSets = ProductionSources(root)
            .Count(p => File.ReadAllText(p).Contains("class TaxErrorResourceSet", StringComparison.Ordinal));
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

    /// <summary>The manifest carries the W2 pre-cert entry with honest allowlists.</summary>
    [Fact]
    public void Root_allowlists_and_forbidden_lists_match_the_manifest()
    {
        var root = Repo();
        using var manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, ManifestPath)).Replace("\uFEFF", string.Empty));

        // W2 recorded Tax in the pre-cert state; the W3 Certify wave promoted it into modules[].
        // The certified entry is the same structure record, so the allowlists below are unchanged.
        var module = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => string.Equals(m.GetProperty("module").GetString(), "Tax", StringComparison.Ordinal));
        Assert.True(module.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", module.GetProperty("lockVersion").GetString());
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "Tax", StringComparison.Ordinal));

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

    /// <summary>The canonical <c>/Modules/Tax/</c> solution grouping holds exactly five projects.</summary>
    [Fact]
    public void Solution_grouping_is_canonical_modules_tax()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src/backend/Tooba.slnx"));
        var folderStart = slnx.IndexOf("<Folder Name=\"/Modules/Tax/\">", StringComparison.Ordinal);
        Assert.True(folderStart >= 0, "missing /Modules/Tax/ solution folder");
        var folderEnd = slnx.IndexOf("</Folder>", folderStart, StringComparison.Ordinal);
        var group = slnx[folderStart..folderEnd];
        foreach (var project in Projects.Concat(["Tooba.Tax.Tests"]))
        {
            Assert.Contains($"Modules/Tax/{project}/{project}.csproj", group, StringComparison.Ordinal);
        }

        Assert.Equal(5, Regex.Matches(group, @"<Project Path=").Count);
        Assert.DoesNotContain("Tooba.Tax.Endpoints", group, StringComparison.Ordinal);
    }

    /// <summary>
    /// Every Tax <c>.cs</c> outside the Test project and outside <c>bin</c>/<c>obj</c>. The Test
    /// project is excluded because the durable guards necessarily name the retired identifiers as
    /// negative assertions.
    /// </summary>
    private static IEnumerable<string> ProductionSources(string root)
    {
        var testsRoot = Path.Combine(root, ModuleRoot, "Tooba.Tax.Tests") + Path.DirectorySeparatorChar;
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
