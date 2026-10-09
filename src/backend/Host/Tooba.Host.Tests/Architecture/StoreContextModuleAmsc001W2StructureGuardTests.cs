using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-STORECONTEXT-AMSC-001-W2 — durable capability-first structure lock for StoreContext: an
/// INTERNAL_ONLY platform context with no Endpoints/Application/Domain project, a shallow
/// capability tree, the module composition entry under DependencyInjection/, exact path↔namespace,
/// enforced root allowlists, canonical /Modules/StoreContext/ solution grouping, a Contracts-only
/// boundary, zero foreign coupling and the Persian documentation standard.
/// </summary>
public sealed class StoreContextModuleAmsc001W2StructureGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/StoreContext";

    private static readonly string[] Projects =
    [
        "Tooba.StoreContext.Contracts",
        "Tooba.StoreContext.Infrastructure",
    ];

    private static readonly string[] ProductionFiles =
    [
        "Tooba.StoreContext.Contracts/Current/StoreCommerceContext.cs",
        "Tooba.StoreContext.Infrastructure/DependencyInjection/StoreContextModule.cs",
        "Tooba.StoreContext.Infrastructure/Current/StoreCommerceContextAccessor.cs",
    ];

    /// <summary>
    /// StoreContext is a platform context provider: it owns no HTTP route, no application use case,
    /// no domain model and no Host folder, and must stay that way.
    /// </summary>
    [Fact]
    public void StoreContext_remains_internal_only_with_no_endpoints_application_or_domain_project()
    {
        foreach (var absent in new[]
                 {
                     "Tooba.StoreContext.Application",
                     "Tooba.StoreContext.Domain",
                     "Tooba.StoreContext.Endpoints",
                     "Tooba.StoreContext.Tests",
                 })
        {
            Assert.False(Directory.Exists(Path.Combine(Repo(), ModuleRoot, absent)), absent);
        }

        Assert.False(Directory.Exists(Path.Combine(Repo(), "src", "backend", "Host", "Tooba.Host", "StoreContext")));

        var storeContextSources = ProductionSources();
        Assert.DoesNotContain(storeContextSources, x => x.Text.Contains("MediatR", StringComparison.Ordinal));
        Assert.DoesNotContain(storeContextSources, x => x.Text.Contains("IRequest", StringComparison.Ordinal));
        Assert.DoesNotContain(storeContextSources, x => x.Text.Contains("ISender", StringComparison.Ordinal));
        Assert.DoesNotContain(storeContextSources, x => x.Text.Contains("MapGroup", StringComparison.Ordinal));
        Assert.DoesNotContain(storeContextSources, x => x.Text.Contains("DbContext", StringComparison.Ordinal));

        using var state = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(Repo(), "docs", "architecture", "tmar-current-state.json")));
        var historical = state.RootElement.GetProperty("storeContext");
        Assert.Equal("PLATFORM_CONTEXT_REFERENCE_PATTERN", historical.GetProperty("state").GetString());
        Assert.Equal("INTERNAL_ONLY", historical.GetProperty("httpApplicability").GetString());
        Assert.Equal("NOT_APPLICABLE", historical.GetProperty("endpointOwnership").GetString());
        Assert.Equal("NOT_APPLICABLE_NO_APPLICATION_USE_CASE", historical.GetProperty("cqrs").GetString());
    }

    /// <summary>
    /// The module composition entry lives under DependencyInjection/ like the newest ARCH-COMPLETE-002
    /// certified modules, so both project roots are empty.
    /// </summary>
    [Fact]
    public void StoreContext_composition_entry_lives_under_dependency_injection_and_roots_are_empty()
    {
        var contracts = Path.Combine(Repo(), ModuleRoot, "Tooba.StoreContext.Contracts");
        var infra = Path.Combine(Repo(), ModuleRoot, "Tooba.StoreContext.Infrastructure");

        Assert.Empty(Directory.GetFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.GetFiles(infra, "*.cs", SearchOption.TopDirectoryOnly));

        Assert.True(File.Exists(Path.Combine(infra, "DependencyInjection", "StoreContextModule.cs")));
        Assert.False(File.Exists(Path.Combine(infra, "StoreContextModule.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Current", "StoreCommerceContextAccessor.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Current", "StoreCommerceContext.cs")));

        var module = File.ReadAllText(Path.Combine(infra, "DependencyInjection", "StoreContextModule.cs"));
        Assert.Contains("namespace Tooba.StoreContext.Infrastructure.DependencyInjection;", module, StringComparison.Ordinal);
        Assert.Contains("public sealed class StoreContextModule : IToobaModule", module, StringComparison.Ordinal);
        Assert.Contains("public string Name => \"StoreContext\";", module, StringComparison.Ordinal);
    }

    /// <summary>
    /// Capability-first shallow tree: the capability folder is the primary axis, no technical request
    /// axis exists, and no leaf folder is empty or single-purpose ceremony.
    /// </summary>
    [Fact]
    public void StoreContext_tree_is_capability_first_shallow_with_no_empty_or_technical_axis_folder()
    {
        var contracts = Path.Combine(Repo(), ModuleRoot, "Tooba.StoreContext.Contracts");
        var infra = Path.Combine(Repo(), ModuleRoot, "Tooba.StoreContext.Infrastructure");

        Assert.Equal(new[] { "Current" }, Folders(contracts));
        Assert.Equal(new[] { "Current", "DependencyInjection" }, Folders(infra));

        foreach (var forbidden in new[] { "Commands", "Queries", "Models", "Validators", "Handlers", "Features" })
        {
            Assert.DoesNotContain(forbidden, Folders(contracts));
            Assert.DoesNotContain(forbidden, Folders(infra));
        }

        foreach (var project in Projects)
        {
            var projectPath = Path.Combine(Repo(), ModuleRoot, project);
            foreach (var folder in Directory.GetDirectories(projectPath, "*", SearchOption.AllDirectories)
                         .Where(d => !IsGenerated(d)))
            {
                var sources = Directory.GetFiles(folder, "*.cs", SearchOption.TopDirectoryOnly);
                Assert.True(sources.Length > 0, $"empty capability folder {Relative(folder)}");
            }
        }

        Assert.Equal(3, ProductionSources().Count);
    }

    /// <summary>Every production namespace equals its path-derived namespace.</summary>
    [Fact]
    public void StoreContext_production_path_equals_namespace_exactly()
    {
        var violations = new List<string>();
        foreach (var project in Projects)
        {
            var projectPath = Path.Combine(Repo(), ModuleRoot, project);
            var rootFull = Path.GetFullPath(projectPath);
            foreach (var file in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
            {
                var relative = file[rootFull.Length..]
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (relative.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || relative.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || Path.GetFileName(relative).StartsWith("GlobalUsings", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var dir = Path.GetDirectoryName(relative);
                var expected = string.IsNullOrEmpty(dir)
                    ? project
                    : project + "." + dir.Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.');
                var match = Regex.Match(File.ReadAllText(file), @"^namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Multiline);
                if (!match.Success || !string.Equals(expected, match.Groups[1].Value, StringComparison.Ordinal))
                {
                    violations.Add($"{relative}: expected {expected}, got {(match.Success ? match.Groups[1].Value : "<none>")}");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }

    /// <summary>Root allowlists match disk exactly and the forbidden root regressions are absent.</summary>
    [Fact]
    public void StoreContext_root_allowlists_match_disk_and_forbidden_roots_absent()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs/architecture/tmar-module-structure-manifests.json")));

        var entries = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => string.Equals(m.GetProperty("module").GetString(), "StoreContext", StringComparison.Ordinal))
            .ToArray();
        Assert.Single(entries);

        var entry = entries[0];
        Assert.True(entry.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entry.GetProperty("lockVersion").GetString());
        Assert.Equal(Projects.Length, entry.GetProperty("projects").GetArrayLength());
        Assert.DoesNotContain(
            doc.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "StoreContext", StringComparison.Ordinal));

        foreach (var project in entry.GetProperty("projects").EnumerateArray())
        {
            var projectName = project.GetProperty("projectName").GetString()!;
            var projectPath = Path.Combine(Repo(), ModuleRoot, projectName);
            Assert.True(Directory.Exists(projectPath), projectPath);

            var allowlist = project.GetProperty("rootAllowlist").EnumerateArray()
                .Select(x => x.GetString()!).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var actual = Directory.GetFiles(projectPath, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Assert.Equal(allowlist, actual);

            Assert.False(string.IsNullOrWhiteSpace(
                project.GetProperty("rootAllowlistJustification").GetString()));

            foreach (var forbidden in project.GetProperty("forbiddenRootFiles").EnumerateArray())
            {
                Assert.False(File.Exists(Path.Combine(projectPath, forbidden.GetString()!)));
            }

            foreach (var forbidden in project.GetProperty("forbiddenTopLevelFolders").EnumerateArray())
            {
                Assert.False(Directory.Exists(Path.Combine(projectPath, forbidden.GetString()!)));
            }
        }

        var infra = entry.GetProperty("projects").EnumerateArray()
            .Single(p => string.Equals(p.GetProperty("projectName").GetString(), "Tooba.StoreContext.Infrastructure", StringComparison.Ordinal));
        var forbiddenRoots = infra.GetProperty("forbiddenRootFiles").EnumerateArray()
            .Select(x => x.GetString()!).ToArray();
        Assert.Contains("StoreContextModule.cs", forbiddenRoots);
        Assert.Contains("StoreCommerceContextAccessor.cs", forbiddenRoots);
    }

    /// <summary>Both projects are grouped under the canonical /Modules/StoreContext/ solution folder.</summary>
    [Fact]
    public void StoreContext_projects_grouped_under_Modules_StoreContext_solution_folder()
    {
        var doc = XDocument.Load(Path.Combine(Repo(), "src", "backend", "Tooba.slnx"));
        var folder = doc.Root!.Elements("Folder").SingleOrDefault(f =>
            string.Equals((string?)f.Attribute("Name"), "/Modules/StoreContext/", StringComparison.Ordinal));
        Assert.True(folder is not null, "missing /Modules/StoreContext/ solution folder");

        var nested = folder!.Elements("Project")
            .Select(p => Path.GetFileNameWithoutExtension((string)p.Attribute("Path")!))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(Projects.OrderBy(x => x, StringComparer.Ordinal).ToArray(), nested);

        foreach (var project in nested)
        {
            var path = Path.Combine(Repo(), ModuleRoot, project, project + ".csproj");
            Assert.True(File.Exists(path), path);
        }
    }

    /// <summary>
    /// Contracts-only boundary: the Contracts project references the foundation only, and no
    /// StoreContext project reaches a foreign module layer, DbContext or alias workaround.
    /// </summary>
    [Fact]
    public void StoreContext_keeps_a_contracts_only_boundary_with_zero_foreign_layer_coupling()
    {
        var contractsCsproj = File.ReadAllText(Path.Combine(
            Repo(), ModuleRoot, "Tooba.StoreContext.Contracts", "Tooba.StoreContext.Contracts.csproj"));
        Assert.Contains("Tooba.BuildingBlocks", contractsCsproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.StoreContext.Infrastructure", contractsCsproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.StoreContext.Application", contractsCsproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.StoreContext.Domain", contractsCsproj, StringComparison.Ordinal);

        var infraCsproj = File.ReadAllText(Path.Combine(
            Repo(), ModuleRoot, "Tooba.StoreContext.Infrastructure", "Tooba.StoreContext.Infrastructure.csproj"));
        Assert.Contains("Tooba.StoreContext.Contracts", infraCsproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.ModuleContracts", infraCsproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", infraCsproj, StringComparison.Ordinal);

        var foreignLayer = new Regex(
            @"Tooba\.(?!StoreContext\b|BuildingBlocks\b|ModuleContracts\b|Persistence\b)[A-Za-z]+\.(Application|Domain|Infrastructure|Endpoints)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        foreach (var source in ProductionSources())
        {
            Assert.DoesNotContain("global using", source.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("TypeForwardedTo", source.Text, StringComparison.Ordinal);
            Assert.DoesNotMatch(@"^\s*using\s+[A-Za-z0-9_.]+\s*=", source.Text);
            var match = foreignLayer.Match(source.Text);
            Assert.False(match.Success, $"{source.Path}: foreign module layer {match.Value}");
        }
    }

    /// <summary>No ad-hoc presentation/logging mechanism may appear in the module.</summary>
    [Fact]
    public void StoreContext_uses_no_ad_hoc_result_logging_or_telemetry_mechanism()
    {
        foreach (var source in ProductionSources())
        {
            Assert.DoesNotContain("Results.Json", source.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.BadRequest", source.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Problem", source.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("ProblemDetails", source.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("Console.WriteLine", source.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("Debug.WriteLine", source.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("ILogger", source.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("ActivitySource", source.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", source.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("Exception.Message", source.Text, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// The Contracts boundary keeps the accepted currency semantics: exactly the three positional
    /// members Market/DefaultCurrency/SalesChannel and no invented currency model.
    /// </summary>
    [Fact]
    public void StoreContext_contract_keeps_default_currency_semantics_only()
    {
        var contract = File.ReadAllText(Path.Combine(
            Repo(), ModuleRoot, "Tooba.StoreContext.Contracts", "Current", "StoreCommerceContext.cs"));

        Assert.Contains("public sealed record StoreCommerceContext(", contract, StringComparison.Ordinal);
        Assert.Contains("string? Market,", contract, StringComparison.Ordinal);
        Assert.Contains("string? DefaultCurrency,", contract, StringComparison.Ordinal);
        Assert.Contains("string? SalesChannel);", contract, StringComparison.Ordinal);
        Assert.DoesNotContain("string? Currency", contract, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowedCurrencies", contract, StringComparison.Ordinal);
        Assert.DoesNotContain("SettlementCurrency", contract, StringComparison.Ordinal);
        Assert.DoesNotContain("PaymentCurrency", contract, StringComparison.Ordinal);

        foreach (var seam in new[]
                 {
                     "public interface ICurrentStoreCommerceContext",
                     "public interface IStoreCommerceContextAssigner",
                     "public interface IWorkerStoreCommerceContextFactory",
                 })
        {
            Assert.Contains(seam, contract, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// TB-TMAR-STORECONTEXT-AMSC-001-W1 durable lock: every public member of every StoreContext
    /// production file carries Persian XML documentation, not name-echo English.
    /// </summary>
    [Fact]
    public void StoreContext_production_members_carry_persian_xml_documentation()
    {
        var persian = new Regex(@"[\u0600-\u06FF]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        foreach (var relative in ProductionFiles)
        {
            var text = File.ReadAllText(Path.Combine(Repo(), ModuleRoot, relative));
            var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();

            var summaryCount = 0;
            for (var i = 0; i < lines.Length; i++)
            {
                if (!lines[i].TrimStart().StartsWith("/// <summary>", StringComparison.Ordinal))
                {
                    continue;
                }

                summaryCount++;
                var prose = i + 1 < lines.Length ? lines[i + 1] : string.Empty;
                Assert.True(persian.IsMatch(prose),
                    $"{relative}: summary at line {i + 1} has no Persian prose: '{prose}'");
            }

            Assert.True(summaryCount > 0, $"{relative}: no XML summary found");
            Assert.Contains(lines, l => l.TrimStart().StartsWith("///", StringComparison.Ordinal) && persian.IsMatch(l));
        }

        // The two architectural invariants must stay explicitly documented.
        var contract = File.ReadAllText(Path.Combine(
            Repo(), ModuleRoot, "Tooba.StoreContext.Contracts", "Current", "StoreCommerceContext.cs"));
        Assert.Contains("DefaultCurrency", contract, StringComparison.Ordinal);
        Assert.Contains(
            contract.Split('\n').Where(l => l.TrimStart().StartsWith("///", StringComparison.Ordinal)),
            l => l.Contains("DefaultCurrency", StringComparison.Ordinal) && persian.IsMatch(l));

        var accessor = File.ReadAllText(Path.Combine(
            Repo(), ModuleRoot, "Tooba.StoreContext.Infrastructure", "Current", "StoreCommerceContextAccessor.cs"));
        Assert.Contains("HttpContext", accessor, StringComparison.Ordinal);
        Assert.Contains("AsyncLocal", accessor, StringComparison.Ordinal);
    }

    private static List<(string Path, string Text)> ProductionSources()
    {
        var root = Path.Combine(Repo(), ModuleRoot);
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(p => !IsGenerated(p))
            .Select(p => (Path: Relative(p), Text: File.ReadAllText(p)))
            .OrderBy(x => x.Path, StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsGenerated(string path)
    {
        var segments = path.Replace('\\', '/').Split('/');
        return segments.Contains("bin", StringComparer.OrdinalIgnoreCase)
            || segments.Contains("obj", StringComparer.OrdinalIgnoreCase);
    }

    private static string[] Folders(string projectPath) =>
        Directory.GetDirectories(projectPath)
            .Select(Path.GetFileName!)
            .Where(n => n is not ("bin" or "obj"))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

    private static string Relative(string path) =>
        Path.GetRelativePath(Repo(), path).Replace('\\', '/');

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
