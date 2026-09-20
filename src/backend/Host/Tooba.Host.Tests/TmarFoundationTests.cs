using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// اثبات foundation CQRS و قفل بدهی جدید TMAR (با baseline صریح).
/// </summary>
public sealed class TmarFoundationTests
{
    [Fact]
    public void MediatR_package_is_exactly_12_5_0()
    {
        var csproj = Path.Combine(FindRepoRoot(), "src", "backend", "BuildingBlocks", "Tooba.BuildingBlocks", "Tooba.BuildingBlocks.csproj");
        var xml = File.ReadAllText(csproj);
        Assert.Contains("Include=\"MediatR\" Version=\"12.5.0\"", xml);
        Assert.DoesNotContain("Version=\"13.", xml);
    }

    [Fact]
    public async Task MediatR_pipeline_validates_and_handles()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation();
        await using var provider = services.BuildServiceProvider();
        var sender = provider.GetRequiredService<ISender>();

        var ok = await sender.Send(new FoundationPingCommand("alpha"));
        Assert.Equal("pong:alpha", ok);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => sender.Send(new FoundationPingCommand("")));
        Assert.Contains(ex.Errors, e => e.ErrorCode == "foundation.ping.name_required");
    }

    [Fact]
    public void Clock_and_id_abstractions_support_fixed_fakes()
    {
        var fixedNow = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        IClock clock = new FixedClock(fixedNow);
        Assert.Equal(fixedNow, clock.UtcNow);

        var id = Guid.Parse("11111111-1111-7111-8111-111111111111");
        IIdGenerator ids = new FixedIdGenerator(id);
        Assert.Equal(id, ids.NewId());
        Assert.Throws<InvalidOperationException>(() => ids.NewId());

        IIdGenerator uuid = new UuidV7IdGenerator();
        Assert.NotEqual(Guid.Empty, uuid.NewId());
    }

    [Fact]
    public void Semantic_error_is_locale_agnostic()
    {
        var error = new SemanticError("catalog.category.invalid_slug", new Dictionary<string, string?> { ["slug"] = "x" });
        var ex = new SemanticException(error);
        Assert.Equal("catalog.category.invalid_slug", ex.Message);
        Assert.Equal("x", error.Arguments["slug"]);
    }

    [Fact]
    public void App_to_app_edges_do_not_expand_beyond_baseline()
    {
        var baseline = LoadJsonBaseline("tmar-app-to-app-edges.json");
        var allowed = baseline.GetProperty("edges").EnumerateArray().Select(e => e.GetString()!).ToHashSet(StringComparer.Ordinal);
        var actual = ScanAppToAppEdges().ToHashSet(StringComparer.Ordinal);
        var extra = actual.Except(allowed).OrderBy(x => x).ToArray();
        Assert.True(extra.Length == 0, "NEW App→App edges: " + string.Join("; ", extra));
    }

    [Fact]
    public void Host_write_sites_do_not_expand_beyond_baseline()
    {
        var baseline = LoadJsonBaseline("tmar-host-write-files.json");
        var allowed = baseline.GetProperty("files").EnumerateArray().Select(e => e.GetString()!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hostRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var actual = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(hostRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            if (!Regex.IsMatch(text, @"\bSaveChangesAsync\b|\bBeginTransaction\b")
                && !(Regex.IsMatch(text, @"\bDbContext\b") && Regex.IsMatch(text, @"\.(Add|Remove)\(")))
            {
                continue;
            }

            actual.Add(Path.GetRelativePath(hostRoot, file).Replace('\\', '/'));
        }

        var extra = actual.Except(allowed).OrderBy(x => x).ToArray();
        Assert.True(extra.Length == 0, "NEW Host write sites: " + string.Join("; ", extra));
    }

    [Fact]
    public void Module_business_MediatR_handlers_do_not_live_in_Infrastructure()
    {
        var backend = Path.Combine(FindRepoRoot(), "src", "backend", "Modules");
        var offenders = new List<string>();
        foreach (var file in Directory.GetFiles(backend, "*.cs", SearchOption.AllDirectories))
        {
            var normalized = file.Replace('\\', '/');
            if (normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!Regex.IsMatch(normalized, @"/[^/]+\.Infrastructure/", RegexOptions.IgnoreCase))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            if (!Regex.IsMatch(text, @"IRequestHandler\s*<"))
            {
                continue;
            }

            offenders.Add(Path.GetRelativePath(FindRepoRoot(), file).Replace('\\', '/'));
        }

        Assert.True(offenders.Count == 0, "Business MediatR handlers in Infrastructure: " + string.Join("; ", offenders));
    }

    [Fact]
    public void Host_IMemoryCache_sites_do_not_expand_beyond_baseline()
    {
        var baseline = LoadJsonBaseline("tmar-host-imemory-files.json");
        var allowed = baseline.GetProperty("files").EnumerateArray().Select(e => e.GetString()!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        // Approved provider boundary always allowed.
        allowed.Add("CacheRegistration.cs");
        allowed.Add("MemoryToobaCache.cs");
        var hostRoot = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var actual = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(hostRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            if (!text.Contains("IMemoryCache", StringComparison.Ordinal) && !text.Contains("MemoryCache", StringComparison.Ordinal))
            {
                continue;
            }

            actual.Add(Path.GetRelativePath(hostRoot, file).Replace('\\', '/'));
        }

        var extra = actual.Except(allowed).OrderBy(x => x).ToArray();
        Assert.True(extra.Length == 0, "NEW Host IMemoryCache sites: " + string.Join("; ", extra));
    }

    private static JsonElement LoadJsonBaseline(string fileName)
    {
        var path = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host.Tests", "Baselines", fileName);
        return JsonDocument.Parse(File.ReadAllText(path)).RootElement;
    }

    private static IEnumerable<string> ScanAppToAppEdges()
    {
        var backend = Path.Combine(FindRepoRoot(), "src", "backend");
        foreach (var path in Directory.GetFiles(backend, "*.Application.csproj", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var name = Path.GetFileNameWithoutExtension(path);
            var xml = XDocument.Load(path);
            foreach (var include in xml.Descendants().Where(e => e.Name.LocalName == "ProjectReference")
                         .Select(e => e.Attribute("Include")?.Value)
                         .Where(v => !string.IsNullOrWhiteSpace(v)))
            {
                var refName = Path.GetFileNameWithoutExtension(include!);
                if (refName.EndsWith(".Application", StringComparison.Ordinal) && !refName.Equals(name, StringComparison.Ordinal))
                {
                    yield return $"{name} -> {refName}";
                }
            }
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "architecture", "TMAR-architecture-locks.md"))
                || Directory.Exists(Path.Combine(dir.FullName, "src", "backend")))
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "src", "backend")))
                {
                    return dir.FullName;
                }
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo_root_not_found");
    }
}
