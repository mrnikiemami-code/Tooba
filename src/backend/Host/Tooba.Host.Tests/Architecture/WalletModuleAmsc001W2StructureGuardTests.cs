using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-WALLET-AMSC-001-W2 — structure wave durable guard.
/// Locks the capability-first shallow Wallet tree, the exact path↔namespace boundary, the enforced
/// per-project root allowlists, the canonical <c>/Modules/Wallet/</c> solution grouping, the clean
/// physical copies, the INTERNAL move of the directory into Persistence (no invented Directories
/// folder), the <c>Contracts/Errors/Resources</c> localization home with its locked logical names, and
/// the manifest structure authority recorded for the module. Behavior and schema are untouched.
/// </summary>
public sealed class WalletModuleAmsc001W2StructureGuardTests
{
    private const string ModuleRootRelative = "src/backend/Modules/Wallet";

    private static readonly string[] ProductionProjects =
    [
        "Tooba.Wallet.Contracts",
        "Tooba.Wallet.Domain",
        "Tooba.Wallet.Application",
        "Tooba.Wallet.Infrastructure",
        "Tooba.Wallet.Endpoints",
    ];

    [Fact]
    public void Application_is_capability_first_and_has_no_single_file_request_leaf_folders()
    {
        var application = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Wallet.Application");

        // Capability-first primary axis with the technical axes secondary and shared seams at root.
        Assert.True(Directory.Exists(Path.Combine(application, "Customer", "Commands")));
        Assert.True(Directory.Exists(Path.Combine(application, "Customer", "Queries")));
        Assert.True(Directory.Exists(Path.Combine(application, "Customer", "Models")));
        Assert.True(Directory.Exists(Path.Combine(application, "Admin", "Commands")));
        Assert.True(Directory.Exists(Path.Combine(application, "Admin", "Queries")));
        Assert.True(Directory.Exists(Path.Combine(application, "Admin", "Models")));
        Assert.True(Directory.Exists(Path.Combine(application, "Payments", "Models")));
        Assert.True(Directory.Exists(Path.Combine(application, "Refunds", "Models")));
        Assert.True(Directory.Exists(Path.Combine(application, "Composition")));
        Assert.True(Directory.Exists(Path.Combine(application, "Ports")));
        Assert.True(Directory.Exists(Path.Combine(application, "Validation")));

        // The retired technical-axis-first request tree must stay retired.
        Assert.False(Directory.Exists(Path.Combine(application, "Commands")));
        Assert.False(Directory.Exists(Path.Combine(application, "Queries")));

        // A single-file folder is only acceptable for a technical axis or a shared seam (Commands,
        // Queries, Models, Ports, Validators, Validation, Composition). A folder named after one
        // request/use case is a single-file request leaf and must not exist.
        string[] axisOrSeam = ["Commands", "Queries", "Models", "Ports", "Validators", "Validation", "Composition"];
        foreach (var dir in Directory.EnumerateDirectories(application, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(dir);
            if (name is "bin" or "obj" or "artifacts") continue;
            var files = Directory.GetFiles(dir, "*.cs", SearchOption.TopDirectoryOnly);
            if (files.Length == 1 && !axisOrSeam.Contains(name, StringComparer.Ordinal))
            {
                Assert.Fail($"single-file request/use-case leaf folder: {dir}");
            }
        }

        // No use-case-named leaf folder survives anywhere under the capability tree.
        var requestLeaves = Directory.EnumerateDirectories(application, "*", SearchOption.AllDirectories)
            .Where(dir => dir.Contains($"{Path.DirectorySeparatorChar}Commands{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                          || dir.Contains($"{Path.DirectorySeparatorChar}Queries{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.Empty(requestLeaves);
    }

    [Fact]
    public void Infrastructure_uses_Persistence_for_the_directory_and_Persistence_Migrations_for_the_migration()
    {
        var infrastructure = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Wallet.Infrastructure");

        Assert.True(File.Exists(Path.Combine(infrastructure, "Persistence", "WalletDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(infrastructure, "Persistence", "WalletDirectory.Customer.cs")));
        Assert.True(File.Exists(Path.Combine(infrastructure, "Persistence", "WalletDirectory.Admin.cs")));
        Assert.True(File.Exists(Path.Combine(infrastructure, "Persistence", "WalletDirectory.Payments.cs")));
        Assert.True(File.Exists(Path.Combine(infrastructure, "Persistence", "WalletDirectory.Refunds.cs")));
        Assert.True(File.Exists(Path.Combine(infrastructure, "Persistence", "WalletDbContext.cs")));
        Assert.True(File.Exists(Path.Combine(infrastructure, "Persistence", "Migrations", "20260827180000_InitialWallet.cs")));
        Assert.True(File.Exists(Path.Combine(infrastructure, "Adapters", "WalletDevelopmentSeedBootstrap.cs")));
        Assert.True(File.Exists(Path.Combine(infrastructure, "DependencyInjection", "WalletModule.cs")));

        // The retired technical-axis roots must stay retired (no invented ceremony folder).
        Assert.False(Directory.Exists(Path.Combine(infrastructure, "Directories")));
        Assert.False(Directory.Exists(Path.Combine(infrastructure, "Development")));
        Assert.False(Directory.Exists(Path.Combine(infrastructure, "Migrations")));

        // The Directory keeps the Persistence-namespace identity (exact path↔namespace) and is split
        // by capability into cohesive partials so no single file mixes four capabilities.
        var directory = File.ReadAllText(Path.Combine(infrastructure, "Persistence", "WalletDirectory.cs"));
        Assert.Contains("namespace Tooba.Wallet.Infrastructure.Persistence;", directory, StringComparison.Ordinal);
        Assert.Contains("public sealed partial class WalletDirectory", directory, StringComparison.Ordinal);
        Assert.Contains("IWalletOrderPaymentPort", directory, StringComparison.Ordinal);
        Assert.Contains("IWalletRefundCreditPort", directory, StringComparison.Ordinal);

        var customer = File.ReadAllText(Path.Combine(infrastructure, "Persistence", "WalletDirectory.Customer.cs"));
        Assert.Contains("RedeemGiftCardForCustomerAsync", customer, StringComparison.Ordinal);
        var admin = File.ReadAllText(Path.Combine(infrastructure, "Persistence", "WalletDirectory.Admin.cs"));
        Assert.Contains("AdjustWalletForAdminAsync", admin, StringComparison.Ordinal);
        var payments = File.ReadAllText(Path.Combine(infrastructure, "Persistence", "WalletDirectory.Payments.cs"));
        Assert.Contains("SpendForOrderPaymentAsync", payments, StringComparison.Ordinal);
        var refunds = File.ReadAllText(Path.Combine(infrastructure, "Persistence", "WalletDirectory.Refunds.cs"));
        Assert.Contains("CreditRefundAsync", refunds, StringComparison.Ordinal);

        // Every capability partial stays well under the ARCH-SIZE-001 ceiling (no re-created god-file).
        foreach (var partial in Directory.GetFiles(Path.Combine(infrastructure, "Persistence"), "WalletDirectory*.cs"))
        {
            Assert.True(File.ReadAllLines(partial).Length < 300, Path.GetFileName(partial));
        }
    }

    [Fact]
    public void Endpoints_localization_lives_in_Contracts_Errors_Resources_with_locked_logical_names()
    {
        var endpoints = Path.Combine(Repo(), ModuleRootRelative, "Tooba.Wallet.Endpoints");

        Assert.True(File.Exists(Path.Combine(endpoints, "Contracts", "Errors", "WalletErrorCatalogContributor.cs")));
        Assert.True(File.Exists(Path.Combine(endpoints, "Contracts", "Errors", "Resources", "WalletErrorResources.cs")));
        Assert.True(File.Exists(Path.Combine(endpoints, "Contracts", "Errors", "Resources", "WalletErrors.resx")));
        Assert.True(File.Exists(Path.Combine(endpoints, "Contracts", "Errors", "Resources", "WalletErrors.fa.resx")));
        Assert.True(File.Exists(Path.Combine(endpoints, "WalletEndpointModule.cs")));
        Assert.True(File.Exists(Path.Combine(endpoints, "Admin", "WalletAdminEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(endpoints, "Customer", "WalletCustomerEndpoints.cs")));

        Assert.False(Directory.Exists(Path.Combine(endpoints, "Errors")));
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Resources")));

        var csproj = File.ReadAllText(Path.Combine(endpoints, "Tooba.Wallet.Endpoints.csproj"));
        Assert.Contains(
            "Tooba.Wallet.Endpoints.Contracts.Errors.Resources.WalletErrors.resources",
            csproj,
            StringComparison.Ordinal);
        Assert.Contains(
            "Tooba.Wallet.Endpoints.Contracts.Errors.Resources.WalletErrors.fa.resources",
            csproj,
            StringComparison.Ordinal);

        var resources = File.ReadAllText(Path.Combine(endpoints, "Contracts", "Errors", "Resources", "WalletErrorResources.cs"));
        Assert.Contains(
            "Tooba.Wallet.Endpoints.Contracts.Errors.Resources.WalletErrors",
            resources,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Path_namespace_alignment_is_exact_for_every_wallet_production_file()
    {
        var violations = new List<string>();
        foreach (var project in ProductionProjects)
        {
            var root = Path.GetFullPath(Path.Combine(Repo(), ModuleRootRelative, project));
            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                var relative = file[root.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (relative.StartsWith("obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                    || relative.StartsWith("bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                {
                    continue;
                }

                var dir = Path.GetDirectoryName(relative);
                var expected = string.IsNullOrEmpty(dir)
                    ? project
                    : project + "." + dir.Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.');
                var text = File.ReadAllText(file);
                var match = Regex.Match(text, @"^namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Multiline);
                if (!match.Success)
                {
                    violations.Add($"{project}/{relative}: no namespace");
                    continue;
                }

                if (!string.Equals(expected, match.Groups[1].Value, StringComparison.Ordinal))
                {
                    violations.Add($"{project}/{relative}: ns={match.Groups[1].Value} expected={expected}");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }

    [Fact]
    public void Solution_grouping_is_the_canonical_modules_wallet_folder_with_six_projects()
    {
        var slnx = File.ReadAllText(Path.Combine(Repo(), "src", "backend", "Tooba.slnx"));
        var match = Regex.Match(slnx, "<Folder Name=\"/Modules/Wallet/\">(?<body>.*?)</Folder>", RegexOptions.Singleline);
        Assert.True(match.Success, "/Modules/Wallet/ solution folder missing");
        var body = match.Groups["body"].Value;

        foreach (var project in new[]
                 {
                     "Tooba.Wallet.Contracts", "Tooba.Wallet.Domain", "Tooba.Wallet.Application",
                     "Tooba.Wallet.Infrastructure", "Tooba.Wallet.Endpoints", "Tooba.Wallet.Tests",
                 })
        {
            var occurrences = Regex.Matches(body, Regex.Escape($"Modules/Wallet/{project}/{project}.csproj")).Count;
            Assert.Equal(1, occurrences);
        }

        // No Wallet project may be registered outside the canonical folder.
        Assert.Equal(6, Regex.Matches(slnx, @"Modules/Wallet/Tooba\.Wallet\.[A-Za-z]+/").Count);
    }

    [Fact]
    public void Manifest_records_wallet_structure_authority_with_disk_accurate_allowlists()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs", "architecture", "tmar-module-structure-manifests.json")));

        // Wallet was promoted from preCertModules into the certified modules[] array by the W3
        // certify wave (structureCertified true); the structural allowlists below are unchanged.
        var wallet = doc.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => string.Equals(m.GetProperty("module").GetString(), "Wallet", StringComparison.Ordinal));

        Assert.True(wallet.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", wallet.GetProperty("lockVersion").GetString());
        Assert.Equal("TB-TMAR-WALLET-AMSC-001-W2", wallet.GetProperty("structureAuthorityTask").GetString());
        Assert.Equal("READY_FOR_CERTIFY_CONSUMED_BY_W3", wallet.GetProperty("structureHandoffState").GetString());
        Assert.Equal("779cd2f4f08cbc5bd90741384d8cef7d4a049e96", wallet.GetProperty("structureAuthorityCommit").GetString());

        var projects = wallet.GetProperty("projects").EnumerateArray()
            .ToDictionary(p => p.GetProperty("projectName").GetString()!, p => p);
        Assert.Equal(5, projects.Count);

        var root = Path.Combine(Repo(), ModuleRootRelative);
        foreach (var (project, entry) in projects)
        {
            var projectPath = Path.Combine(root, project);
            Assert.True(Directory.Exists(projectPath), project);

            var allowlist = entry.GetProperty("rootAllowlist").EnumerateArray()
                .Select(x => x.GetString()!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
            var actual = Directory.GetFiles(projectPath, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
            Assert.Equal(allowlist, actual);

            foreach (var forbiddenFolder in entry.GetProperty("forbiddenTopLevelFolders").EnumerateArray())
            {
                Assert.False(
                    Directory.Exists(Path.Combine(projectPath, forbiddenFolder.GetString()!)),
                    $"{project} still has forbidden top-level folder {forbiddenFolder.GetString()}");
            }
        }

        // Wallet was removed from uncertifiedHttpOwningModules by the W3 certify wave and its
        // pre-cert duplicate is gone (promotion is a move, not a copy).
        Assert.DoesNotContain(
            "Wallet",
            doc.RootElement.GetProperty("uncertifiedHttpOwningModules").EnumerateArray().Select(x => x.GetString()!));
        Assert.DoesNotContain(
            doc.RootElement.GetProperty("preCertModules").EnumerateArray(),
            m => string.Equals(m.GetProperty("module").GetString(), "Wallet", StringComparison.Ordinal));

        using var state = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs", "architecture", "tmar-current-state.json")));
        var certified = state.RootElement.GetProperty("structureLock").GetProperty("certifiedModules")
            .EnumerateArray().Select(x => x.GetString()!).ToArray();
        Assert.Contains("Wallet", certified);
    }

    [Fact]
    public void Wallet_w2_sot_record_records_the_structure_handoff()
    {
        using var state = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            Repo(), "docs", "architecture", "tmar-current-state.json")));
        var w2 = state.RootElement.GetProperty("walletAmsc001W2");
        Assert.Equal("TB-TMAR-WALLET-AMSC-001-W2", w2.GetProperty("task").GetString());
        Assert.Equal("tooba-architecture-structure", w2.GetProperty("skill").GetString());
        Assert.Equal("READY_FOR_CERTIFY", w2.GetProperty("structureState").GetString());
        Assert.Equal("779cd2f4", w2.GetProperty("commit").GetString());
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
