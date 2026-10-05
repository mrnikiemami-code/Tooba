using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-IDENTITY-AMC-001-W1 — VS /Modules/Identity/ solution grouping.
/// Repaired in TB-TMAR-IDENTITY-AMSC-001-W2: the repository now groups every module under its own
/// nested <c>/Modules/&lt;Module&gt;/</c> folder, so the flat <c>/Modules/</c> folder no longer exists.
/// The assertion is therefore made strictly against the nested folder and against every other folder
/// in the solution, instead of against a flat list that the canonical layout removed.
/// </summary>
public sealed class IdentityModuleAmcW1SolutionGuardTests
{
    private static readonly string[] ExpectedProjects =
    [
        "Modules/Identity/Tooba.Identity.Domain/Tooba.Identity.Domain.csproj",
        "Modules/Identity/Tooba.Identity.Contracts/Tooba.Identity.Contracts.csproj",
        "Modules/Identity/Tooba.Identity.Application/Tooba.Identity.Application.csproj",
        "Modules/Identity/Tooba.Identity.Infrastructure/Tooba.Identity.Infrastructure.csproj",
        "Modules/Identity/Tooba.Identity.Endpoints/Tooba.Identity.Endpoints.csproj",
    ];

    [Fact]
    public void Identity_projects_group_under_Modules_Identity_in_slnx()
    {
        var doc = XDocument.Load(Path.Combine(Repo(), "src", "backend", "Tooba.slnx"));
        var folders = doc.Root!.Elements("Folder").ToArray();

        var identityFolder = folders.Single(f =>
            string.Equals((string?)f.Attribute("Name"), "/Modules/Identity/", StringComparison.Ordinal));

        var paths = identityFolder.Elements("Project")
            .Select(p => (string?)p.Attribute("Path") ?? string.Empty)
            .ToArray();
        Assert.Equal(ExpectedProjects, paths);

        // No Identity project may live outside /Modules/Identity/ (no loose flat entry, no stray group).
        foreach (var folder in folders)
        {
            if (ReferenceEquals(folder, identityFolder))
            {
                continue;
            }

            var name = (string?)folder.Attribute("Name") ?? string.Empty;
            var leaked = folder.Elements("Project")
                .Select(p => (string?)p.Attribute("Path") ?? string.Empty)
                .Where(p => p.Contains("/Identity/", StringComparison.Ordinal))
                .ToArray();
            Assert.True(leaked.Length == 0, $"Identity project leaked into '{name}': {string.Join(", ", leaked)}");
        }

        var slnx = File.ReadAllText(Path.Combine(Repo(), "src", "backend", "Tooba.slnx"));
        Assert.Contains("/Modules/Identity/", slnx, StringComparison.Ordinal);
    }

    private static string Repo()
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
