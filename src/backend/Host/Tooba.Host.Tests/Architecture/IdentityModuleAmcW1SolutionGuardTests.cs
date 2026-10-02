using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-IDENTITY-AMC-001-W1 — VS /Modules/Identity/ solution grouping.</summary>
public sealed class IdentityModuleAmcW1SolutionGuardTests
{
    [Fact]
    public void Identity_projects_group_under_Modules_Identity_in_slnx()
    {
        var root = Repo();
        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        Assert.Contains("/Modules/Identity/", slnx, StringComparison.Ordinal);
        Assert.Contains("Tooba.Identity.Domain", slnx, StringComparison.Ordinal);
        Assert.Contains("Tooba.Identity.Contracts", slnx, StringComparison.Ordinal);
        Assert.Contains("Tooba.Identity.Application", slnx, StringComparison.Ordinal);
        Assert.Contains("Tooba.Identity.Infrastructure", slnx, StringComparison.Ordinal);

        // Must not remain as loose entries under the flat /Modules/ list ahead of nested folders.
        var modulesFlat = slnx.IndexOf("<Folder Name=\"/Modules/\">", StringComparison.Ordinal);
        var identityGroup = slnx.IndexOf("<Folder Name=\"/Modules/Identity/\">", StringComparison.Ordinal);
        Assert.True(modulesFlat >= 0 && identityGroup > modulesFlat);
        var flatSection = slnx.Substring(modulesFlat, identityGroup - modulesFlat);
        Assert.DoesNotContain("Tooba.Identity.", flatSection, StringComparison.Ordinal);
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
