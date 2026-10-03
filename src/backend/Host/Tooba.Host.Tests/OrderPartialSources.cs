using System.IO;

namespace Tooba.Host.Tests;

/// <summary>
/// Reads an Order partial-file family as one logical source.
/// Guards must assert the whole cohesive surface after de-godding splits, not a single shard;
/// reading only the base file silently drops symbols that moved into a cohesive partial.
/// </summary>
internal static class OrderPartialSources
{
    /// <summary>Concatenates every file matching <paramref name="searchPattern"/> in a repo-relative directory (order-stable).</summary>
    internal static string ReadAll(string repoRelativeDirectory, string searchPattern)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, repoRelativeDirectory);
            if (Directory.Exists(candidate))
            {
                return Concat(candidate, searchPattern);
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(repoRelativeDirectory);
    }

    /// <summary>Concatenates every file matching <paramref name="searchPattern"/> in an absolute directory (order-stable).</summary>
    internal static string ReadAllAbsolute(string absoluteDirectory, string searchPattern) =>
        Concat(absoluteDirectory, searchPattern);

    private static string Concat(string directory, string searchPattern) =>
        string.Concat(Directory
            .GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(File.ReadAllText));
}
