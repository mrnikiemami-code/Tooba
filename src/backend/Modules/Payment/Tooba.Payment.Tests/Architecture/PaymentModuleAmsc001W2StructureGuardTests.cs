using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Payment.Tests.Architecture;

/// <summary>
/// TB-TMAR-PAYMENT-AMSC-001-W2 — scoped structure gate (tooba-architecture-structure).
/// Pins the capability-first shallow Application tree (zero per-use-case request leaves, zero
/// technical-axis-first root), the manifest root allowlists/forbidden lists, exact path↔namespace
/// equality for all five module projects, the canonical /Modules/Payment/ solution grouping and the
/// Endpoints import hygiene. Behavior is unchanged by W2; this guard only locks the physical shape.
/// </summary>
public sealed class PaymentModuleAmsc001W2StructureGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Payment";
    private const string ManifestPath = "docs/architecture/tmar-module-structure-manifests.json";

    private static readonly string[] ApplicationCapabilities = ["Admin", "Storefront", "Webhooks", "Reconciliation"];
    private static readonly string[] ApplicationSharedFolders = ["Composition", "Models", "Ports", "Validators"];

    [Fact]
    public void Application_root_is_capability_first_with_no_technical_axis_root()
    {
        var applicationRoot = Path.Combine(Repo(), ModuleRoot, "Tooba.Payment.Application");

        var rootFolders = Directory.GetDirectories(applicationRoot)
            .Select(Path.GetFileName!)
            .Where(name => name is not ("bin" or "obj" or "artifacts") && !name.StartsWith('.'))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            ApplicationCapabilities.Concat(ApplicationSharedFolders).OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            rootFolders);

        foreach (var banned in new[] { "Commands", "Queries", "Orchestration" })
        {
            Assert.False(Directory.Exists(Path.Combine(applicationRoot, banned)),
                $"technical-axis-first Application/{banned} must not return");
        }

        foreach (var capability in ApplicationCapabilities)
        {
            var capabilityRoot = Path.Combine(applicationRoot, capability);
            Assert.True(Directory.Exists(capabilityRoot), capability);
            Assert.NotEmpty(Directory.GetDirectories(capabilityRoot));
        }
    }

    [Fact]
    public void Capability_axes_are_flat_with_zero_per_use_case_request_leaves()
    {
        var root = Repo();
        foreach (var project in new[] { "Tooba.Payment.Application", "Tooba.Payment.Endpoints" })
        {
            var projectDir = Path.Combine(root, ModuleRoot, project);
            foreach (var dir in Directory.EnumerateDirectories(projectDir, "*", SearchOption.AllDirectories))
            {
                if (dir.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || dir.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                var files = Directory.EnumerateFiles(dir, "*.cs", SearchOption.TopDirectoryOnly).ToList();
                if (files.Count != 1 || Directory.EnumerateDirectories(dir).Any())
                {
                    continue;
                }

                // A per-use-case leaf is a FOLDER named after one Command/Query/UseCase wrapping a single
                // source file. Shared technical axes (Commands/, Queries/, Validators/, Models/, Ports/,
                // Orchestration/) legitimately hold request-named files and are the canonical shape.
                var folderName = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar));
                Assert.False(
                    folderName.EndsWith("Command", StringComparison.Ordinal)
                        || folderName.EndsWith("Query", StringComparison.Ordinal)
                        || folderName.EndsWith("UseCase", StringComparison.Ordinal),
                    $"per-use-case request leaf folder {dir}");
            }
        }
    }

    [Fact]
    public void Request_sources_are_colocated_on_the_capability_axes()
    {
        var applicationRoot = Path.Combine(Repo(), ModuleRoot, "Tooba.Payment.Application");
        var expected = new (string Capability, string Axis, int Count)[]
        {
            ("Admin", "Commands", 3),
            ("Admin", "Models", 1),
            ("Admin", "Queries", 2),
            ("Admin", "Validators", 5),
            ("Storefront", "Commands", 6),
            ("Storefront", "Queries", 4),
            ("Storefront", "Validators", 9),
            ("Storefront", "Models", 1),
            ("Storefront", "Orchestration", 2),
            ("Webhooks", "Commands", 1),
            ("Webhooks", "Validators", 1),
            ("Reconciliation", "Commands", 1),
        };

        foreach (var (capability, axis, count) in expected)
        {
            var axisRoot = Path.Combine(applicationRoot, capability, axis);
            Assert.True(Directory.Exists(axisRoot), $"{capability}/{axis}");
            Assert.Equal(count, Directory.GetFiles(axisRoot, "*.cs").Length);
            Assert.Empty(Directory.GetDirectories(axisRoot));
        }

        // The shared Application/Validators folder keeps exactly the two cross-capability helpers.
        Assert.Equal(2, Directory.GetFiles(Path.Combine(applicationRoot, "Validators"), "*.cs").Length);
        Assert.Empty(Directory.GetDirectories(Path.Combine(applicationRoot, "Validators")));
    }

    [Fact]
    public void Root_allowlists_and_forbidden_lists_match_the_manifest()
    {
        var root = Repo();
        using var manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, ManifestPath)).Replace("\uFEFF", string.Empty));
        var module = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => m.GetProperty("module").GetString() == "Payment");

        foreach (var project in module.GetProperty("projects").EnumerateArray())
        {
            var projectName = project.GetProperty("projectName").GetString()!;
            var projectPath = Path.Combine(root, ModuleRoot, projectName);

            var allowlist = project.GetProperty("rootAllowlist").EnumerateArray().Select(x => x.GetString()!)
                .OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var actualRoot = Directory.GetFiles(projectPath, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Assert.Equal(allowlist, actualRoot);

            foreach (var forbidden in project.GetProperty("forbiddenRootFiles").EnumerateArray())
            {
                Assert.False(File.Exists(Path.Combine(projectPath, forbidden.GetString()!)),
                    $"{projectName} resurrected forbidden root file {forbidden.GetString()}");
            }

            foreach (var forbiddenFolder in project.GetProperty("forbiddenTopLevelFolders").EnumerateArray())
            {
                Assert.False(Directory.Exists(Path.Combine(projectPath, forbiddenFolder.GetString()!)),
                    $"{projectName} resurrected forbidden top-level folder {forbiddenFolder.GetString()}");
            }
        }
    }

    [Fact]
    public void Path_derived_namespaces_are_exact()
    {
        var root = Repo();
        foreach (var project in new[]
                 {
                     "Tooba.Payment.Contracts", "Tooba.Payment.Domain",
                     "Tooba.Payment.Application", "Tooba.Payment.Infrastructure",
                     "Tooba.Payment.Endpoints",
                 })
        {
            var projectPath = Path.Combine(root, ModuleRoot, project);
            foreach (var file in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
            {
                var relative = file[Path.GetFullPath(projectPath).Length..]
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (relative.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || relative.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || Path.GetFileName(relative).StartsWith("GlobalUsings", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // EF Persistence/Migrations designer/snapshot exemption follows the repository lock.
                if (relative.Contains($"Persistence{Path.DirectorySeparatorChar}Migrations", StringComparison.Ordinal))
                {
                    continue;
                }

                var dir = Path.GetDirectoryName(relative);
                var expected = string.IsNullOrEmpty(dir)
                    ? project
                    : project + "." + dir.Replace(Path.DirectorySeparatorChar, '.')
                        .Replace(Path.AltDirectorySeparatorChar, '.');
                var match = Regex.Match(
                    File.ReadAllText(file).TrimStart('\uFEFF'),
                    @"^namespace\s+([A-Za-z0-9_.]+)",
                    RegexOptions.Multiline);
                Assert.True(match.Success, $"no namespace in {relative}");
                Assert.Equal(expected, match.Groups[1].Value);
            }
        }
    }

    [Fact]
    public void Solution_grouping_is_canonical_modules_payment()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src/backend/Tooba.slnx"));
        var folderStart = slnx.IndexOf("<Folder Name=\"/Modules/Payment/\">", StringComparison.Ordinal);
        Assert.True(folderStart >= 0, "missing /Modules/Payment/ solution folder");
        var folderEnd = slnx.IndexOf("</Folder>", folderStart, StringComparison.Ordinal);
        var group = slnx[folderStart..folderEnd];
        foreach (var project in new[]
                 {
                     "Tooba.Payment.Domain", "Tooba.Payment.Contracts",
                     "Tooba.Payment.Application", "Tooba.Payment.Infrastructure",
                     "Tooba.Payment.Endpoints", "Tooba.Payment.Tests",
                 })
        {
            Assert.Contains($"Modules/Payment/{project}/{project}.csproj", group, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Endpoints_import_hygiene_stays_application_and_buildingblocks_only()
    {
        var csproj = File.ReadAllText(Path.Combine(
            Repo(), ModuleRoot, "Tooba.Payment.Endpoints", "Tooba.Payment.Endpoints.csproj"));
        Assert.Contains("Tooba.Payment.Application.csproj", csproj, StringComparison.Ordinal);
        Assert.Contains("Tooba.BuildingBlocks.csproj", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Payment.Infrastructure.csproj", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Payment.Domain.csproj", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", csproj, StringComparison.Ordinal);
    }

    [Fact]
    public void No_stale_or_duplicate_physical_copy_of_the_moved_surface_remains()
    {
        var applicationRoot = Path.Combine(Repo(), ModuleRoot, "Tooba.Payment.Application");
        foreach (var stale in new[]
                 {
                     "Commands/CompleteSandboxPayment/CompleteSandboxPaymentCommand.cs",
                     "Queries/QueryAdminPaymentsGrid/QueryAdminPaymentsGridQuery.cs",
                     "Validators/Storefront/InitiateStorefrontPaymentCommandValidator.cs",
                     "Orchestration/StorefrontPaymentOrchestrator.cs",
                     "Models/StorefrontPaymentDtos.cs",
                 })
        {
            Assert.False(File.Exists(Path.Combine(applicationRoot, stale.Replace('/', Path.DirectorySeparatorChar))),
                $"stale W2 copy remains: {stale}");
        }

        // Single authoritative home for each moved responsibility.
        Assert.True(File.Exists(Path.Combine(
            applicationRoot, "Storefront", "Orchestration", "StorefrontPaymentOrchestrator.cs")));
        Assert.True(File.Exists(Path.Combine(
            applicationRoot, "Storefront", "Models", "StorefrontPaymentDtos.cs")));
        Assert.True(File.Exists(Path.Combine(
            applicationRoot, "Reconciliation", "Commands", "ReconcileStalePaymentsCommand.cs")));
    }

    private static string Repo()
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
