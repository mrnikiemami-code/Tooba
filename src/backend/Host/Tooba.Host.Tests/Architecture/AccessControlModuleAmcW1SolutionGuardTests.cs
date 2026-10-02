using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-ACCESSCONTROL-AMC-001-W1 — VS solution grouping for AccessControl.</summary>
public sealed class AccessControlModuleAmcW1SolutionGuardTests
{
    [Fact]
    public void AccessControl_projects_are_grouped_under_Modules_AccessControl_including_Endpoints()
    {
        var root = Repo();
        var slnx = File.ReadAllText(Path.Combine(root, "src", "backend", "Tooba.slnx"));
        Assert.Contains("<Folder Name=\"/Modules/AccessControl/\">", slnx, StringComparison.Ordinal);
        Assert.Contains("Modules/AccessControl/Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj", slnx, StringComparison.Ordinal);
        Assert.Equal(1, Count(slnx, "Modules/AccessControl/Tooba.AccessControl.Application/Tooba.AccessControl.Application.csproj"));
        Assert.Equal(1, Count(slnx, "Modules/AccessControl/Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj"));

        // Must not remain as loose entries under the flat /Modules/ dump without the dedicated folder.
        var flatModulesStart = slnx.IndexOf("<Folder Name=\"/Modules/\">", StringComparison.Ordinal);
        var flatModulesEnd = slnx.IndexOf("</Folder>", flatModulesStart, StringComparison.Ordinal);
        var flatWindow = slnx.Substring(flatModulesStart, flatModulesEnd - flatModulesStart);
        Assert.DoesNotContain("Modules/AccessControl/", flatWindow, StringComparison.Ordinal);

        var sot = File.ReadAllText(Path.Combine(root, "docs", "architecture", "tmar-current-state.json"));
        Assert.Contains("\"accessControlModuleAmc001W1\"", sot, StringComparison.Ordinal);
        Assert.Contains("ACCESSCONTROL_SOLUTION_GROUP_APPLIED", sot, StringComparison.Ordinal);
    }

    private static int Count(string haystack, string needle)
    {
        var n = 0;
        for (var i = 0; (i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0; i += needle.Length)
            n++;
        return n;
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
