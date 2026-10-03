using Xunit;

namespace Tooba.Order.Tests.Architecture;

/// <summary>
/// TB-TMAR-ORDER-AMC-001-W4 — durable folder-granularity guard.
/// Capability-first shallow grouping is canonical for Order: a Commands/Queries leaf that contains a single
/// production source file is one-folder-per-request over-foldering, and a technical-axis-first
/// Application/Commands|Queries root is forbidden for a multi-capability module.
/// </summary>
public sealed class OrderApplicationFolderGranularityGuardTests
{
    [Fact]
    public void No_unjustified_single_file_command_or_query_leaf_folders()
    {
        var app = ApplicationRoot();
        var violations = new List<string>();

        foreach (var kindRoot in Directory.GetDirectories(app, "*", SearchOption.AllDirectories))
        {
            if (IsGenerated(kindRoot))
            {
                continue;
            }

            var kind = Path.GetFileName(kindRoot);
            if (!string.Equals(kind, "Commands", StringComparison.Ordinal)
                && !string.Equals(kind, "Queries", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var sub in Directory.GetDirectories(kindRoot))
            {
                var files = Directory.GetFiles(sub, "*.cs", SearchOption.TopDirectoryOnly);
                var nested = Directory.GetDirectories(sub);
                if (files.Length == 1 && nested.Length == 0)
                {
                    violations.Add(Relative(app, sub));
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Unjustified single-file Command/Query leaf folders: " + string.Join(", ", violations));
    }

    [Fact]
    public void No_technical_axis_first_command_or_query_root()
    {
        var app = ApplicationRoot();

        Assert.False(
            Directory.Exists(Path.Combine(app, "Commands")),
            "Technical-axis-first Application/Commands must not return");
        Assert.False(
            Directory.Exists(Path.Combine(app, "Queries")),
            "Technical-axis-first Application/Queries must not return");
    }

    private static bool IsGenerated(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static string Relative(string root, string path) =>
        Path.GetRelativePath(root, path).Replace('\\', '/');

    private static string ApplicationRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Order", "Tooba.Order.Application");

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "backend", "Tooba.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
