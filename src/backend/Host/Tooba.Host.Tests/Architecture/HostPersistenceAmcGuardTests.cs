using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-PERSISTENCE-AMC-001 — KEEP_AS_GENERIC_HOST_PLATFORM_INFRASTRUCTURE.
/// </summary>
public sealed class HostPersistenceAmcGuardTests
{
    [Fact]
    public void Host_persistence_folder_retains_exactly_one_platform_resolver()
    {
        var root = FindRepoRoot();
        var persistence = Path.Combine(root, "src/backend/Host/Tooba.Host/Persistence");
        Assert.True(Directory.Exists(persistence));

        var files = Directory.EnumerateFiles(persistence, "*.cs", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "DatabaseConnectionResolver.cs" }, files);
    }

    [Fact]
    public void Path_namespace_exact_and_no_foreign_module_layers()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "src/backend/Host/Tooba.Host/Persistence/DatabaseConnectionResolver.cs");
        var text = File.ReadAllText(path);

        var nsMatch = Regex.Match(text, @"^namespace\s+([^\s;{]+)", RegexOptions.Multiline);
        Assert.True(nsMatch.Success);
        Assert.Equal("Tooba.Host.Persistence", nsMatch.Groups[1].Value);

        Assert.Contains("IDatabaseConnectionResolver", text, StringComparison.Ordinal);
        Assert.Contains("platform.connection.unconfigured", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ILogger", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Console.Write", text, StringComparison.Ordinal);

        var foreign = new Regex(
            @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Payment|Reviews|Cart)\.(Application|Domain|Infrastructure)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            Assert.False(foreign.IsMatch(line), line);
        }
    }

    [Fact]
    public void Program_registers_building_blocks_seam_and_sot_keep_disposition()
    {
        var root = FindRepoRoot();
        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("IDatabaseConnectionResolver, DatabaseConnectionResolver", program, StringComparison.Ordinal);
        Assert.Contains("using Tooba.Host.Persistence;", program, StringComparison.Ordinal);

        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostPersistenceAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("KEEP_AS_GENERIC_HOST_PLATFORM_INFRASTRUCTURE", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-PERSISTENCE-AMC-001", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_PERSISTENCE_AMC_001_KEEP_PLATFORM", sot, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
