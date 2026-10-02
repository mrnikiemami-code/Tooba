using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-ACCESSCONTROL-STRUCTURE-RECERT-001 — re-cert after capability-first structure repair.</summary>
public sealed class AccessControlStructureRecert001GuardTests
{
    [Fact]
    public void AccessControl_structure_recertified_capability_first_complete_reference()
    {
        var root = Repo();
        var app = Path.Combine(root,
            "src", "backend", "Modules", "AccessControl", "Tooba.AccessControl.Application");

        Assert.False(Directory.Exists(Path.Combine(app, "Commands")));
        Assert.False(Directory.Exists(Path.Combine(app, "Queries")));
        foreach (var capability in new[] { "Roles", "Assignments", "Permissions", "Ceiling", "Access", "Bootstrap" })
            Assert.True(Directory.Exists(Path.Combine(app, capability)));

        Assert.True(File.Exists(Path.Combine(root,
            "src/backend/Host/Tooba.Host.Tests/Architecture/AccessControlStructureRepair001GuardTests.cs")));

        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        Assert.Contains("/Modules/AccessControl/", slnx, StringComparison.Ordinal);
        Assert.Contains("Tooba.AccessControl.Endpoints", slnx, StringComparison.Ordinal);

        foreach (var relative in new[]
                 {
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Admin/AccessControlAdminEndpoints.cs",
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Admin/AccessControlAdminSellerEndpoints.cs",
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Seller/AccessControlSellerEndpoints.cs",
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            Assert.Contains("api.From(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.AccessControl.Domain", text, StringComparison.Ordinal);
        }

        var manifest = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-module-structure-manifests.json"));
        Assert.Contains("\"module\": \"AccessControl\"", manifest, StringComparison.Ordinal);
        Assert.Contains("\"structureCertified\": true", manifest, StringComparison.Ordinal);

        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"accessControlStructureRecert001\"", sot, StringComparison.Ordinal);
        Assert.Contains("ACCESSCONTROL_STRUCTURE_RECERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ROOT_FINAL_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("\"currentHostCheckpoint\": \"HOST_ROOT_FINAL_CERTIFIED\"", sot, StringComparison.Ordinal);
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
