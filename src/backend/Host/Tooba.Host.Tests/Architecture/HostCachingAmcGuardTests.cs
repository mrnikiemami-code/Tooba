using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// Architect-direct AMC guard for Host/Caching.
/// The folder is a legal Host platform seam, but its retained surface is explicitly allowlisted.
/// </summary>
public sealed class HostCachingAmcGuardTests
{
    private static readonly string[] Allowlist =
    [
        "CacheHostOptions.cs",
        "CacheInstrumentation.cs",
        "CacheRegistration.cs",
        "MemoryToobaCache.cs",
    ];

    [Fact]
    public void Caching_folder_matches_exact_retained_allowlist()
    {
        var folder = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Caching");
        Assert.True(Directory.Exists(folder));

        var files = Directory.GetFiles(folder, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(Allowlist.OrderBy(x => x, StringComparer.Ordinal).ToArray(), files);
    }

    [Fact]
    public void Caching_surface_has_no_business_persistence_or_http_ownership()
    {
        foreach (var file in Allowlist)
        {
            var text = Read(file);

            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbSet<", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SaveChanges", text, StringComparison.Ordinal);
            Assert.DoesNotContain("FromSql", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapGet(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPost(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPut(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapDelete(", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Cache_telemetry_uses_canonical_meter_and_bounded_dimensions()
    {
        var text = Read("CacheInstrumentation.cs");

        Assert.Contains("ToobaTelemetry.Meter", text, StringComparison.Ordinal);
        Assert.Contains(""cache.provider"", text, StringComparison.Ordinal);
        Assert.Contains(""cache.namespace"", text, StringComparison.Ordinal);
        Assert.Contains(""cache.edition"", text, StringComparison.Ordinal);

        Assert.DoesNotContain(""tenant", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(""user", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("key.Value", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_registers_runtime_cache_foundation()
    {
        var program = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));

        Assert.Contains("AddOptions<CacheHostOptions>()", program, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<IValidateOptions<CacheHostOptions>, CacheOptionsValidator>()", program, StringComparison.Ordinal);
        Assert.Equal(
            1,
            Regex.Matches(program, @"\bAddToobaCache\s*\(\s*\)", RegexOptions.CultureInvariant).Count);
    }

    [Fact]
    public void Provider_policy_remains_memory_or_none_and_redis_is_rejected()
    {
        var text = Read("CacheHostOptions.cs");

        Assert.Contains("Memory", text, StringComparison.Ordinal);
        Assert.Contains("None", text, StringComparison.Ordinal);
        Assert.Contains("Redis", text, StringComparison.Ordinal);
        Assert.Contains("StackExchange", text, StringComparison.Ordinal);
    }

    private static string Read(string fileName) =>
        File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Caching", fileName));

    private static string FindRepoRoot()
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
