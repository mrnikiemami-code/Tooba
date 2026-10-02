using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-ACCESSCONTROL-STRUCTURE-REPAIR-001 — capability-first shallow Application tree;
/// rejects technical-axis-first + single-file request leaf regression.
/// </summary>
public sealed class AccessControlStructureRepair001GuardTests
{
    private static readonly string[] Capabilities =
    [
        "Roles",
        "Assignments",
        "Permissions",
        "Ceiling",
        "Access",
        "Bootstrap",
    ];

    [Fact]
    public void AccessControl_application_is_capability_first_shallow_without_single_file_request_leaves()
    {
        var root = Repo();
        var app = Path.Combine(root,
            "src", "backend", "Modules", "AccessControl", "Tooba.AccessControl.Application");

        Assert.False(Directory.Exists(Path.Combine(app, "Commands")),
            "Technical-axis-first Application/Commands must not return");
        Assert.False(Directory.Exists(Path.Combine(app, "Queries")),
            "Technical-axis-first Application/Queries must not return");

        foreach (var capability in Capabilities)
        {
            Assert.True(Directory.Exists(Path.Combine(app, capability)),
                $"Missing capability root {capability}");
        }

        Assert.True(Directory.Exists(Path.Combine(app, "Development", "Seller")),
            "Development/Seller capability must remain");

        // Shared/cross-capability roots allowed
        Assert.True(Directory.Exists(Path.Combine(app, "Composition")));
        Assert.True(Directory.Exists(Path.Combine(app, "Exceptions")));
        Assert.True(Directory.Exists(Path.Combine(app, "Models")));
        Assert.True(Directory.Exists(Path.Combine(app, "Ports")));
        Assert.True(Directory.Exists(Path.Combine(app, "Validators")));
        Assert.True(File.Exists(Path.Combine(app, "Permissions", "PermissionCatalog.cs")));

        var requestAxes = new[] { "Commands", "Queries" };
        foreach (var capability in Capabilities)
        {
            foreach (var axis in requestAxes)
            {
                var axisDir = Path.Combine(app, capability, axis);
                if (!Directory.Exists(axisDir))
                    continue;

                // No per-use-case subfolders under capability technical axes
                Assert.Empty(Directory.GetDirectories(axisDir));

                foreach (var file in Directory.EnumerateFiles(axisDir, "*.cs", SearchOption.TopDirectoryOnly))
                {
                    var relative = Path.GetRelativePath(app, file).Replace('\\', '/');
                    var expectedNs = "Tooba.AccessControl.Application." +
                                     Path.GetDirectoryName(relative)!.Replace('\\', '/').Replace('/', '.');
                    var text = File.ReadAllText(file);
                    Assert.Contains($"namespace {expectedNs};", text, StringComparison.Ordinal);
                }
            }

            var validatorsDir = Path.Combine(app, capability, "Validators");
            if (!Directory.Exists(validatorsDir))
                continue;
            Assert.Empty(Directory.GetDirectories(validatorsDir));
            foreach (var file in Directory.EnumerateFiles(validatorsDir, "*.cs", SearchOption.TopDirectoryOnly))
            {
                var relative = Path.GetRelativePath(app, file).Replace('\\', '/');
                var expectedNs = "Tooba.AccessControl.Application." +
                                 Path.GetDirectoryName(relative)!.Replace('\\', '/').Replace('/', '.');
                var text = File.ReadAllText(file);
                Assert.Contains($"namespace {expectedNs};", text, StringComparison.Ordinal);
            }
        }

        // Shared Validators root must not reintroduce capability dump folders
        var sharedValidators = Path.Combine(app, "Validators");
        foreach (var banned in new[] { "Role", "Assignment", "Ceiling", "Permissions" })
        {
            Assert.False(Directory.Exists(Path.Combine(sharedValidators, banned)),
                $"Shared Validators/{banned} must not return; validators belong under capability roots");
        }
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
