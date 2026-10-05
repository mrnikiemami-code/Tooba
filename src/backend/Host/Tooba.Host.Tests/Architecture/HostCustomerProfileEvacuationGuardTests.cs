using System.Text.RegularExpressions;
using System.Xml.Linq;
using Tooba.BuildingBlocks.Results;
using Tooba.CustomerProfile.Application.Profile.Commands;
using Tooba.CustomerProfile.Application.Account.Models;
using Tooba.CustomerProfile.Application.Account.Queries;
using Tooba.CustomerProfile.Application.Profile.Queries;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-CUSTOMERPROFILE-EVACUATION-001 — Host/CustomerProfile ZERO + module-owned Development seed.
/// Also regresses parent R1 Result pipeline and Solution Folder grouping.
/// </summary>
public sealed class HostCustomerProfileEvacuationGuardTests
{
    [Fact]
    public void Host_CustomerProfile_folder_has_zero_production_cs_files()
    {
        var folder = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "CustomerProfile");
        var files = Directory.Exists(folder)
            ? Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories)
            : [];
        Assert.Empty(files);
        Assert.False(File.Exists(Path.Combine(folder, "CustomerProfileDevelopmentSeed.cs")));
    }

    [Fact]
    public void Module_owned_seed_is_canonical_path_namespace_and_guest_actor()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Modules",
            "CustomerProfile",
            "Tooba.CustomerProfile.Infrastructure",
            "Development",
            "CustomerProfileDevelopmentSeed.cs");
        Assert.True(File.Exists(path));
        var text = File.ReadAllText(path);
        Assert.Contains("namespace Tooba.CustomerProfile.Infrastructure.Development;", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Order.Application", text, StringComparison.Ordinal);
        Assert.Contains("StorefrontGuestActor.ActorId", text, StringComparison.Ordinal);
        Assert.Contains("using Tooba.Order.Contracts.Fulfillment;", text, StringComparison.Ordinal);

        var duplicates = Directory.EnumerateFiles(
                Path.Combine(FindRepoRoot(), "src", "backend"),
                "CustomerProfileDevelopmentSeed.cs",
                SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();
        Assert.Single(duplicates);
        Assert.Equal(path, duplicates[0], StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bootstrap_call_sites_use_module_owned_seed()
    {
        // The migration-only Host debt file was replaced by the neutral DevelopmentSchemaMigrator seam;
        // the CustomerProfile module-owned seed now runs through that seam only.
        var bootstrap = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Host",
            "Tooba.Host",
            "Development",
            "DevelopmentSchemaMigrator.cs"));
        Assert.Contains("using Tooba.CustomerProfile.Infrastructure.Development;", bootstrap, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Host.CustomerProfile;", bootstrap, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(bootstrap, @"CustomerProfileDevelopmentSeed\.ApplyAsync").Count);
    }

    [Fact]
    public void Parent_R1_result_pipeline_remains_canonical()
    {
        Assert.True(typeof(GetCustomerProfilePageQuery).IsAssignableTo(typeof(MediatR.IRequest<Result<CustomerProfilePage>>)));
        Assert.True(typeof(UpsertCustomerProfileCommand).IsAssignableTo(typeof(MediatR.IRequest<Result<CustomerProfilePage>>)));
        Assert.True(typeof(GetCustomerAccountDashboardQuery).IsAssignableTo(typeof(MediatR.IRequest<Result<CustomerDashboardPage>>)));

        var profile = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src", "backend", "Modules", "CustomerProfile", "Tooba.CustomerProfile.Endpoints",
            "Customer", "CustomerProfileEndpoints.cs"));
        var dashboard = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src", "backend", "Modules", "CustomerProfile", "Tooba.CustomerProfile.Endpoints",
            "CustomerDashboard", "CustomerAccountDashboardEndpoints.cs"));
        Assert.Contains("api.From(result)", profile, StringComparison.Ordinal);
        Assert.Contains("api.From(result)", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json(page)", profile, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json(page)", dashboard, StringComparison.Ordinal);
    }

    [Fact]
    public void Parent_R1_solution_grouping_remains_exact()
    {
        var doc = XDocument.Load(Path.Combine(FindRepoRoot(), "src", "backend", "Tooba.slnx"));
        var folders = doc.Root!.Elements("Folder").ToArray();
        var customerFolder = folders.Single(f =>
            string.Equals((string?)f.Attribute("Name"), "/Modules/CustomerProfile/", StringComparison.Ordinal));
        Assert.Equal(5, customerFolder.Elements("Project").Count());

        // Repaired in TB-TMAR-IDENTITY-AMSC-001-W2: the canonical layout nests every module under its own
        // /Modules/<Module>/ folder, so the flat /Modules/ folder no longer exists. Assert the stricter
        // property instead — no CustomerProfile project may appear under any other solution folder.
        foreach (var folder in folders)
        {
            if (ReferenceEquals(folder, customerFolder))
            {
                continue;
            }

            var name = (string?)folder.Attribute("Name") ?? string.Empty;
            var leaked = folder.Elements("Project")
                .Select(p => (string?)p.Attribute("Path") ?? string.Empty)
                .Where(p => p.Contains("/CustomerProfile/", StringComparison.Ordinal))
                .ToArray();
            Assert.True(leaked.Length == 0, $"CustomerProfile project leaked into '{name}': {string.Join(", ", leaked)}");
        }
    }

    private static string FindRepoRoot()
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
