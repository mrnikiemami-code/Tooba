using System.Xml.Linq;
using Tooba.Catalog.Infrastructure;
using Tooba.Identity.Infrastructure;
using Tooba.ModuleContracts;
using Tooba.Offer.Infrastructure;
using Tooba.Party.Infrastructure;
using Tooba.PlatformProbe.Infrastructure;
using Tooba.Pricing.Infrastructure;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// قوانین مرز Modular Monolith را به‌صورت اجرایی شکست می‌دهد؛ فقط مستند نیستند.
/// تا وقتی فقط PlatformProbe وجود دارد، بخشی از قوانین لایه‌ای به‌صورت vacuous روی پروژه‌های آینده اعمال می‌شود.
/// </summary>
public sealed class ArchitectureBoundaryTests
{
    [Fact]
    public void Host_composes_modules_from_explicit_list()
    {
        Assert.Contains(ToobaModuleComposition.Modules, module => module is PlatformProbeModule);
        Assert.Contains(ToobaModuleComposition.Modules, module => module is IdentityModule);
        Assert.Contains(ToobaModuleComposition.Modules, module => module is PartyModule);
        Assert.Contains(ToobaModuleComposition.Modules, module => module is CatalogModule);
        Assert.Contains(ToobaModuleComposition.Modules, module => module is OfferModule);
        Assert.Contains(ToobaModuleComposition.Modules, module => module is PricingModule);
        Assert.All(ToobaModuleComposition.Modules, module => Assert.False(string.IsNullOrWhiteSpace(module.Name)));
        Assert.Contains(typeof(IToobaModule).Assembly.GetExportedTypes(), t => t == typeof(IToobaModule));
    }

    [Fact]
    public void No_global_business_dbcontext_exists()
    {
        var backend = Path.Combine(FindRepoRoot(), "src", "backend");
        foreach (var file in Directory.GetFiles(backend, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            Assert.DoesNotMatch(@"\b(class|record)\s+(ToobaDbContext|AppDbContext)\b", text);
            Assert.False(
                Path.GetFileName(file) is "ToobaDbContext.cs" or "AppDbContext.cs",
                file);
        }
    }

    [Fact]
    public void Domain_does_not_reference_infrastructure_or_host()
    {
        foreach (var project in LoadProjects())
        {
            if (!project.Name.EndsWith(".Domain", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var reference in project.References)
            {
                Assert.False(
                    reference.Name.Contains(".Infrastructure", StringComparison.Ordinal)
                    || reference.Name.Equals("Tooba.Host", StringComparison.Ordinal)
                    || reference.Name.Equals("Tooba.Persistence", StringComparison.Ordinal),
                    $"{project.Name} must not reference {reference.Name}");
            }
        }
    }

    [Fact]
    public void Application_does_not_reference_host()
    {
        foreach (var project in LoadProjects())
        {
            if (!project.Name.EndsWith(".Application", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.DoesNotContain(
                project.References,
                reference => reference.Name.Equals("Tooba.Host", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Module_infrastructure_does_not_reference_foreign_module_infrastructure_or_persistence()
    {
        foreach (var project in LoadProjects())
        {
            if (project.ModuleName is null || !IsModuleInfrastructureOrPersistence(project))
            {
                continue;
            }

            foreach (var reference in project.References)
            {
                if (reference.ModuleName is null || reference.ModuleName == project.ModuleName)
                {
                    continue;
                }

                Assert.False(
                    IsModuleInfrastructureOrPersistence(reference),
                    $"{project.Name} must not reference foreign module {reference.Name}");
            }
        }
    }

    [Fact]
    public void Non_host_projects_do_not_reference_foreign_module_infrastructure()
    {
        foreach (var project in LoadProjects())
        {
            if (project.Name is "Tooba.Host" || project.Name.EndsWith(".Tests", StringComparison.Ordinal))
            {
                continue;
            }

            var foreignInfra = project.References
                .Where(reference =>
                    reference.ModuleName is not null
                    && IsModuleInfrastructureOrPersistence(reference)
                    && reference.ModuleName != project.ModuleName)
                .Select(reference => reference.Name)
                .ToArray();

            Assert.True(
                foreignInfra.Length == 0,
                $"{project.Name} references foreign module internals: {string.Join(", ", foreignInfra)}");
        }
    }

    [Fact]
    public void Module_contracts_are_not_a_dumping_ground_or_persistence_leak()
    {
        var contracts = LoadProjects().Single(project => project.Name == "Tooba.ModuleContracts");
        Assert.DoesNotContain(
            contracts.References,
            reference =>
                reference.Name.Equals("Tooba.Host", StringComparison.Ordinal)
                || reference.Name.Contains(".Infrastructure", StringComparison.Ordinal)
                || reference.Name.Equals("Tooba.Persistence", StringComparison.Ordinal));
    }

    [Fact]
    public void Building_blocks_do_not_depend_on_host_or_modules()
    {
        foreach (var project in LoadProjects().Where(project =>
                     project.Name is "Tooba.BuildingBlocks" or "Tooba.Persistence"))
        {
            Assert.DoesNotContain(
                project.References,
                reference =>
                    reference.Name.Equals("Tooba.Host", StringComparison.Ordinal)
                    || reference.ModuleName is not null);
        }
    }

    private static IReadOnlyList<ProjectInfo> LoadProjects()
    {
        var backend = Path.Combine(FindRepoRoot(), "src", "backend");
        var projects = new List<ProjectInfo>();
        foreach (var path in Directory.GetFiles(backend, "*.csproj", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            projects.Add(Parse(path));
        }

        return projects;
    }

    private static ProjectInfo Parse(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var xml = XDocument.Load(path);
        var includes = xml.Descendants()
            .Where(el => el.Name.LocalName == "ProjectReference")
            .Select(el => el.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, value!)))
            .ToArray();

        return new ProjectInfo(
            name,
            path,
            ModuleNameOf(path),
            includes.Select(include => new ProjectInfo(
                Path.GetFileNameWithoutExtension(include),
                include,
                ModuleNameOf(include),
                Array.Empty<ProjectInfo>())).ToArray());
    }

    private static string? ModuleNameOf(string path)
    {
        var normalized = path.Replace('/', Path.DirectorySeparatorChar);
        var marker = $"{Path.DirectorySeparatorChar}Modules{Path.DirectorySeparatorChar}";
        var index = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return null;
        }

        var rest = normalized[(index + marker.Length)..];
        var module = rest.Split(Path.DirectorySeparatorChar)[0];
        return module.Equals("Tooba.ModuleContracts", StringComparison.OrdinalIgnoreCase)
            || module.Equals("Tooba.ModuleContracts.csproj", StringComparison.OrdinalIgnoreCase)
            ? null
            : module;
    }

    private static bool IsModuleInfrastructureOrPersistence(ProjectInfo project) =>
        project.ModuleName is not null
        && (project.Name.Contains(".Infrastructure", StringComparison.Ordinal)
            || project.Name.Contains(".Persistence", StringComparison.Ordinal));

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }

    /// <summary>
    /// فرادادهٔ پروژه برای بررسی جهت وابستگی بدون اجرای کسب‌وکار.
    /// </summary>
    private sealed record ProjectInfo(
        string Name,
        string Path,
        string? ModuleName,
        IReadOnlyList<ProjectInfo> References);
}
