using System.Xml.Linq;
using Xunit;

namespace Tooba.Offer.Tests.Architecture;

/// <summary>
/// ARCH-MODULE-PHYSICAL-001 — reference-complete modules must place production files
/// under approved physical responsibility folders; namespace/docs alone are insufficient.
/// </summary>
public sealed class OfferPhysicalStructureGuardTests
{
    private static readonly string[] AllowedDomainFolders =
    [
        "Aggregates", "Entities", "ValueObjects", "Policies", "Events", "Errors"
    ];

    private static readonly string[] AllowedApplicationFolders =
    [
        "UseCases", "Commands", "Queries", "Mappings", "Ports", "Validators", "Dtos", "ReadModels", "Policies"
    ];

    private static readonly string[] AllowedContractsFolders =
    [
        "Ports", "Commands", "Events", "Dtos", "Errors"
    ];

    private static readonly string[] AllowedInfrastructureFolders =
    [
        "Persistence", "Repositories", "Adapters", "Outbox", "Events", "DependencyInjection"
    ];

    private static readonly string[] AllowedEndpointsFolders =
    [
        "Admin", "Storefront", "Seller", "Errors", "Resources"
    ];

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "ai", "TOOBA-PIPELINE-PROTOCOL.md"))
                || File.Exists(Path.Combine(dir.FullName, ".git", "HEAD")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private static string OfferRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Offer");

    [Fact]
    public void ARCH_MODULE_PHYSICAL_001_projects_exist_on_disk()
    {
        string[] projects =
        [
            "Tooba.Offer.Domain",
            "Tooba.Offer.Application",
            "Tooba.Offer.Contracts",
            "Tooba.Offer.Infrastructure",
            "Tooba.Offer.Endpoints",
            "Tooba.Offer.Tests"
        ];

        foreach (var project in projects)
        {
            var csproj = Path.Combine(OfferRoot(), project, project + ".csproj");
            Assert.True(File.Exists(csproj), "missing physical project: " + csproj);
        }
    }

    [Fact]
    public void ARCH_MODULE_PHYSICAL_001_no_flat_dumping_ground_at_project_roots()
    {
        AssertNoRootDump("Tooba.Offer.Domain", AllowedDomainFolders, allowRootFiles: Array.Empty<string>());
        AssertNoRootDump("Tooba.Offer.Application", AllowedApplicationFolders, allowRootFiles: Array.Empty<string>());
        AssertNoRootDump("Tooba.Offer.Contracts", AllowedContractsFolders, allowRootFiles: Array.Empty<string>());
        AssertNoRootDump(
            "Tooba.Offer.Infrastructure",
            AllowedInfrastructureFolders,
            allowRootFiles: Array.Empty<string>());
        AssertNoRootDump(
            "Tooba.Offer.Endpoints",
            AllowedEndpointsFolders,
            allowRootFiles: ["OfferEndpointModule.cs"]);
    }

    [Fact]
    public void ARCH_MODULE_PHYSICAL_001_production_files_live_under_approved_folders()
    {
        var violations = new List<string>();
        CheckProjectFiles("Tooba.Offer.Domain", AllowedDomainFolders, violations, allowRootFiles: Array.Empty<string>());
        CheckProjectFiles("Tooba.Offer.Application", AllowedApplicationFolders, violations, allowRootFiles: Array.Empty<string>());
        CheckProjectFiles("Tooba.Offer.Contracts", AllowedContractsFolders, violations, allowRootFiles: Array.Empty<string>());
        CheckProjectFiles(
            "Tooba.Offer.Infrastructure",
            AllowedInfrastructureFolders,
            violations,
            allowRootFiles: Array.Empty<string>());
        CheckProjectFiles(
            "Tooba.Offer.Endpoints",
            AllowedEndpointsFolders,
            violations,
            allowRootFiles: ["OfferEndpointModule.cs"]);

        Assert.True(violations.Count == 0, "physical layout violations:\n" + string.Join("\n", violations));
    }

    [Fact]
    public void ARCH_MODULE_PHYSICAL_001_namespaces_align_with_physical_folders()
    {
        var violations = new List<(string File, string Namespace, string Expected)>();
        ExpectNs("Tooba.Offer.Domain", "Aggregates", "Tooba.Offer.Domain.Aggregates", violations);
        ExpectNs("Tooba.Offer.Domain", "Events", "Tooba.Offer.Domain.Events", violations);
        ExpectNs("Tooba.Offer.Application", "Ports", "Tooba.Offer.Application.Ports", violations);
        ExpectNs("Tooba.Offer.Contracts", "Ports", "Tooba.Offer.Contracts.Ports", violations);
        ExpectNs("Tooba.Offer.Contracts", "Dtos", "Tooba.Offer.Contracts.Dtos", violations);
        ExpectNs("Tooba.Offer.Infrastructure", "Adapters", "Tooba.Offer.Infrastructure.Adapters", violations);
        ExpectNs("Tooba.Offer.Infrastructure", "Outbox", "Tooba.Offer.Infrastructure.Outbox", violations);
        ExpectNs(
            "Tooba.Offer.Infrastructure",
            "DependencyInjection",
            "Tooba.Offer.Infrastructure.DependencyInjection",
            violations);
        ExpectNs("Tooba.Offer.Endpoints", "Seller", "Tooba.Offer.Endpoints.Seller", violations);

        Assert.True(
            violations.Count == 0,
            "namespace/path mismatches:\n"
            + string.Join("\n", violations.Select(v => $"{v.File}: ns={v.Namespace} expected={v.Expected}")));
    }

    [Fact]
    public void ARCH_MODULE_PHYSICAL_001_no_empty_ceremonial_folders()
    {
        string[] ceremonialCandidates =
        [
            Path.Combine(OfferRoot(), "Tooba.Offer.Application", "UseCases"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Application", "Validators"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Application", "Dtos"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Domain", "Entities"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Domain", "ValueObjects"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Domain", "Policies"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Domain", "Errors"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Contracts", "Commands"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Contracts", "Events"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Infrastructure", "Repositories"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Endpoints", "Admin"),
            Path.Combine(OfferRoot(), "Tooba.Offer.Endpoints", "Storefront")
        ];

        var empties = ceremonialCandidates
            .Where(Directory.Exists)
            .Where(d => !Directory.EnumerateFileSystemEntries(d).Any())
            .ToArray();

        Assert.True(empties.Length == 0, "empty ceremonial folders: " + string.Join("; ", empties));
    }

    [Fact]
    public void ARCH_MODULE_PHYSICAL_001_offer_routes_absent_from_host_seller_endpoints()
    {
        var hostEndpoints = Path.Combine(
            RepoRoot(),
            "src",
            "backend",
            "Host",
            "Tooba.Host",
            "Seller",
            "SellerPanelEndpoints.cs");
        var text = File.ReadAllText(hostEndpoints);
        Assert.DoesNotContain("MapGet(\"/offers\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/offers\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPatch(\"/offers/", text, StringComparison.Ordinal);

        var moduleEndpoints = Path.Combine(
            OfferRoot(),
            "Tooba.Offer.Endpoints",
            "Seller",
            "OfferSellerEndpoints.cs");
        Assert.True(File.Exists(moduleEndpoints), "Offer seller endpoints must live in module Endpoints project");
    }

    private static void AssertNoRootDump(string project, string[] allowedFolders, string[] allowRootFiles)
    {
        var root = Path.Combine(OfferRoot(), project);
        var rootCs = Directory.EnumerateFiles(root, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(n => n is not null && !allowRootFiles.Contains(n, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        Assert.True(rootCs.Length == 0, $"{project} root dumping-ground: " + string.Join(", ", rootCs));

        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            var name = Path.GetFileName(dir);
            if (name is "bin" or "obj")
            {
                continue;
            }

            if (!allowedFolders.Contains(name, StringComparer.OrdinalIgnoreCase)
                && !name.StartsWith("Tooba.", StringComparison.Ordinal))
            {
                // Tests project has more folders; this helper is for production projects only.
            }
        }
    }

    private static void CheckProjectFiles(
        string project,
        string[] allowedFolders,
        List<string> violations,
        string[] allowRootFiles)
    {
        var root = Path.Combine(OfferRoot(), project);
        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var n = file.Replace('\\', '/');
            if (n.Contains("/bin/", StringComparison.Ordinal) || n.Contains("/obj/", StringComparison.Ordinal))
            {
                continue;
            }

            var rel = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (!rel.Contains('/', StringComparison.Ordinal))
            {
                if (!allowRootFiles.Contains(Path.GetFileName(file), StringComparer.OrdinalIgnoreCase))
                {
                    violations.Add($"{project}/{rel}: flat root file");
                }

                continue;
            }

            var top = rel.Split('/')[0];
            if (!allowedFolders.Contains(top, StringComparer.OrdinalIgnoreCase))
            {
                violations.Add($"{project}/{rel}: top folder '{top}' not in approved set");
            }
        }
    }

    private static void ExpectNs(
        string project,
        string folder,
        string expectedNamespace,
        List<(string File, string Namespace, string Expected)> violations)
    {
        var dir = Path.Combine(OfferRoot(), project, folder);
        if (!Directory.Exists(dir))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
        {
            var n = file.Replace('\\', '/');
            if (n.Contains("/bin/", StringComparison.Ordinal) || n.Contains("/obj/", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            var match = System.Text.RegularExpressions.Regex.Match(
                text,
                @"^namespace\s+([A-Za-z0-9_.]+)\s*;",
                System.Text.RegularExpressions.RegexOptions.Multiline);
            if (!match.Success)
            {
                violations.Add((Path.GetRelativePath(RepoRoot(), file), "<missing>", expectedNamespace));
                continue;
            }

            var ns = match.Groups[1].Value;
            if (!ns.StartsWith(expectedNamespace, StringComparison.Ordinal))
            {
                violations.Add((Path.GetRelativePath(RepoRoot(), file), ns, expectedNamespace));
            }
        }
    }
}
