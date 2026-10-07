using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PRICING-AMSC-001-W2 — scoped structure gate (tooba-architecture-structure).
/// Pins the capability-first shallow physical tree, the exact path↔namespace rule for all five
/// production projects, the manifest root allowlists/forbidden lists, the canonical
/// <c>/Modules/Pricing/</c> solution grouping and the absence of stale/duplicate physical copies of
/// the relocated Contracts localization surface. Behavior is unchanged by W2; this guard only locks
/// the physical shape.
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
        "Tooba.Pricing.Endpoints",
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

    /// <summary>Endpoints keeps only the composition entry at root; the localized surface moved to Contracts.</summary>
    [Fact]
    public void Endpoints_root_holds_only_the_composition_entry()
    {
        var endpoints = Path.Combine(Repo(), ModuleRoot, "Tooba.Pricing.Endpoints");
        Assert.Equal(
            new[] { "PricingEndpointModule.cs" },
            Directory.GetFiles(endpoints, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());

        // W2 relocated the error catalog/resource surface into Contracts so Pricing owns one
        // self-contained code+text boundary; the Endpoints duplicates must stay retired.
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Errors")));
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Resources")));
        Assert.False(File.Exists(Path.Combine(endpoints, "PricingErrorCatalogContributor.cs")));
        Assert.False(File.Exists(Path.Combine(endpoints, "PricingErrorResources.cs")));
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

    /// <summary>Manifest physical allowlists/forbidden lists match disk for every Pricing project.</summary>
    [Fact]
    public void Root_allowlists_and_forbidden_lists_match_the_manifest()
    {
        var root = Repo();
        using var manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, ManifestPath)).Replace("\uFEFF", string.Empty));
        // Promoted by TB-TMAR-PRICING-AMSC-001-W3 from preCertModules to the certified modules
        // array (structureCertified true). The structural allowlists asserted below are unchanged.
        var module = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => string.Equals(m.GetProperty("module").GetString(), "Pricing", StringComparison.Ordinal));
        Assert.True(module.GetProperty("structureCertified").GetBoolean());
        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "Pricing", StringComparison.Ordinal));

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

    /// <summary>The canonical <c>/Modules/Pricing/</c> solution grouping holds all six projects.</summary>
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
    }

    /// <summary>Endpoints stays a thin Application/Contracts/BuildingBlocks consumer.</summary>
    [Fact]
    public void Endpoints_import_hygiene_stays_application_contracts_and_buildingblocks_only()
    {
        var csproj = File.ReadAllText(Path.Combine(
            Repo(), ModuleRoot, "Tooba.Pricing.Endpoints", "Tooba.Pricing.Endpoints.csproj"));
        Assert.Contains("Tooba.Pricing.Application.csproj", csproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.Pricing.Contracts.csproj", csproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.BuildingBlocks.csproj", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Pricing.Infrastructure.csproj", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Pricing.Domain.csproj", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", csproj, StringComparison.Ordinal);
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
