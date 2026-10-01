using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-CACHING-AMC-001 — KEEP_AS_GENERIC_HOST_CACHE_INFRASTRUCTURE.
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

    private static readonly Regex ForeignModuleLayers = new(
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Payment|Reviews|Cart|Wallet|Support|ProductQnA|Preferences|Story|Wishlist|Fulfillment|Settlement|Notification|Returns|Promotion|Inventory|Pricing|Tax|Media|Content|User|OperatorProfile)\.(Application|Domain|Infrastructure|Persistence)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

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
    public void Path_namespace_exact_and_platform_boundary_invariants()
    {
        foreach (var file in Allowlist)
        {
            var text = Read(file);
            var nsMatch = Regex.Match(text, @"^namespace\s+([^\s;{]+)", RegexOptions.Multiline);
            Assert.True(nsMatch.Success, file);
            Assert.Equal("Tooba.Host.Caching", nsMatch.Groups[1].Value);

            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbSet<", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SaveChanges", text, StringComparison.Ordinal);
            Assert.DoesNotContain("FromSql", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapGet(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPost(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapPut(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapDelete(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("HttpContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("StackExchange.Redis", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
            Assert.DoesNotContain("message.Contains", text, StringComparison.OrdinalIgnoreCase);

            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim();
                Assert.False(ForeignModuleLayers.IsMatch(line), $"{file}: {line}");
            }
        }
    }

    [Fact]
    public void Cache_telemetry_uses_canonical_meter_and_bounded_dimensions()
    {
        var text = Read("CacheInstrumentation.cs");

        Assert.Contains("ToobaTelemetry.Meter", text, StringComparison.Ordinal);
        Assert.Contains("cache.provider", text, StringComparison.Ordinal);
        Assert.Contains("cache.namespace", text, StringComparison.Ordinal);
        Assert.Contains("cache.edition", text, StringComparison.Ordinal);
        Assert.Contains("tooba.cache.type_mismatch", text, StringComparison.Ordinal);
        Assert.Contains("tooba.cache.factory.failure", text, StringComparison.Ordinal);
        Assert.Contains("tooba.cache.stampede.wait", text, StringComparison.Ordinal);

        Assert.DoesNotContain("tenant", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("user", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("key.Value", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ResourceId", text, StringComparison.Ordinal);
        Assert.DoesNotContain("payload", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Single_flight_retirement_rechecks_refcount_before_remove()
    {
        var text = Read("MemoryToobaCache.cs");
        Assert.Contains("class CacheInflightCoordinator", text, StringComparison.Ordinal);
        Assert.Contains("AfterRefCountZeroBeforeRecheckRemove", text, StringComparison.Ordinal);
        Assert.Contains("slot.Retired", text, StringComparison.Ordinal);
        Assert.Contains("lock (slot.Sync)", text, StringComparison.Ordinal);
        Assert.Contains("if (slot.RefCount != 0 || slot.Retired)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("CurrentCount == 1", text, StringComparison.Ordinal);
        Assert.DoesNotContain("CurrentCount==1", text, StringComparison.Ordinal);
        // Parent race: Interlocked.Decrement then unconditional TryRemove without recheck under lock.
        Assert.DoesNotContain("Interlocked.Decrement(ref slot.RefCount)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Deterministic_retirement_race_test_exists()
    {
        var tests = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host.Tests", "CacheFoundationTests.cs"));
        Assert.Contains("Inflight_retirement_does_not_remove_slot_after_new_attachment", tests, StringComparison.Ordinal);
        Assert.Contains("AfterRefCountZeroBeforeRecheckRemove", tests, StringComparison.Ordinal);
    }

    [Fact]
    public void Type_mismatch_removes_entry_and_does_not_cast_as_hit_null()
    {
        var text = Read("MemoryToobaCache.cs");
        Assert.Contains("TypeMismatch", text, StringComparison.Ordinal);
        Assert.Contains("box.Payload is T typed", text, StringComparison.Ordinal);
        Assert.DoesNotContain("box.Payload as T", text, StringComparison.Ordinal);
        Assert.Contains("NullSentinel", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Provider_parity_rejects_blank_tag_and_namespace_on_both_implementations()
    {
        var text = Read("MemoryToobaCache.cs");
        Assert.Equal(2, Regex.Matches(text, @"Cache tag is required for invalidation\.").Count);
        Assert.Equal(2, Regex.Matches(text, @"Cache namespace is required for invalidation\.").Count);
        Assert.Contains("class DisabledToobaCache", text, StringComparison.Ordinal);
        Assert.Contains("class MemoryToobaCache", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_registers_runtime_cache_foundation()
    {
        var program = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));

        Assert.Contains("using Tooba.Host.Caching;", program, StringComparison.Ordinal);
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

    [Fact]
    public void Sot_keep_disposition_and_exact_namespace_are_recorded()
    {
        var sot = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "architecture", "tmar-current-state.json"));
        Assert.Contains("\"hostCachingAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("KEEP_AS_GENERIC_HOST_CACHE_INFRASTRUCTURE", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-CACHING-AMC-001", sot, StringComparison.Ordinal);
        Assert.Contains("EXACT_Tooba.Host.Caching", sot, StringComparison.Ordinal);
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
