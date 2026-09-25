using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Settlement.Tests.Architecture;

public sealed class SettlementArchitectureGuardTests
{
    private static readonly string[] AllowedDomainFolders = ["Aggregates", "Entities", "ValueObjects", "Events"];
    private static readonly string[] AllowedApplicationFolders = ["Ports", "Models", "Queries", "Commands", "Errors", "Validators"];
    private static readonly string[] AllowedInfrastructureFolders =
        ["Persistence", "Directories", "Messaging", "DependencyInjection", "Bridges", "Gateways", "Handlers", "Adapters", "Adapters",
            "Observability", "Queries", "Errors", "Migrations"];
    private static readonly string[] AllowedEndpointsFolders = ["Seller", "Admin", "Errors", "Resources"];

    private static readonly HashSet<string> HostDbContextAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "MarketplaceDevelopmentBootstrap.cs",
        "ModuleMigrationRegistry.cs",
        "Program.cs",
    };

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "ai", "TOOBA-PIPELINE-PROTOCOL.md"))
                || Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo.root.not_found");
    }

    private static string SettlementRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Settlement");

    [Fact]
    public void Settlement_golden_boundaries_and_physical_layout_remain_clean()
    {
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));
        AssertNoRootDump("Tooba.Settlement.Domain", AllowedDomainFolders);
        AssertNoRootDump("Tooba.Settlement.Application", AllowedApplicationFolders);
        AssertNoRootDump("Tooba.Settlement.Infrastructure", AllowedInfrastructureFolders);
        AssertNoRootDump("Tooba.Settlement.Endpoints", AllowedEndpointsFolders);
        AssertNamespacesAlign("Tooba.Settlement.Domain", "Tooba.Settlement.Domain");
        AssertNamespacesAlign("Tooba.Settlement.Contracts", "Tooba.Settlement.Contracts");
        AssertNamespacesAlign("Tooba.Settlement.Application", "Tooba.Settlement.Application");
        AssertNamespacesAlign("Tooba.Settlement.Infrastructure", "Tooba.Settlement.Infrastructure");
        AssertNamespacesAlign("Tooba.Settlement.Endpoints", "Tooba.Settlement.Endpoints");

        var appRefs = ProjectRefs("Tooba.Settlement.Application");
        Assert.DoesNotContain(appRefs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(appRefs, r => r.Contains(".Application", StringComparison.Ordinal) && !r.Contains("Settlement.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(appRefs, r => r.Contains("Host", StringComparison.OrdinalIgnoreCase));

        var endpointRefs = ProjectRefs("Tooba.Settlement.Endpoints");
        Assert.Contains(endpointRefs, r => r.Contains("Settlement.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(endpointRefs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointRefs, r => r.Contains("Host", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointRefs, r => r.Contains("DbContext", StringComparison.OrdinalIgnoreCase));

        var infraRefs = ProjectRefs("Tooba.Settlement.Infrastructure");
        Assert.Contains(infraRefs, r => r.Contains("Party.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Payment.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Payment.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Payment.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Order.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Order.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Order.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Returns.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Returns.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Returns.Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Party.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Party.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(infraRefs, r => r.Contains("Party.Infrastructure", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(AllProductionSources(), x =>
            x.Text.Contains("PartyDbContext", StringComparison.Ordinal)
            || x.Text.Contains("PaymentDbContext", StringComparison.Ordinal)
            || x.Text.Contains("OrderDbContext", StringComparison.Ordinal)
            || x.Text.Contains("ReturnsDbContext", StringComparison.Ordinal));

        var directory = File.ReadAllText(Path.Combine(SettlementRoot(), "Tooba.Settlement.Infrastructure", "Directories", "SettlementDirectory.cs"));
        Assert.Contains("IClock", directory, StringComparison.Ordinal);
        Assert.Contains("IIdGenerator", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new SystemUtcClock()", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new UuidV7IdGenerator()", directory, StringComparison.Ordinal);

        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var hostSettlementDbHits = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                           && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains("SettlementDbContext", StringComparison.Ordinal))
            .Where(path => !HostDbContextAllowlist.Contains(Path.GetFileName(path)))
            .ToList();
        Assert.True(hostSettlementDbHits.Count == 0, "Host SettlementDbContext: " + string.Join("; ", hostSettlementDbHits));

        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Settlement")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Settlement", "SettlementEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Settlement", "SettlementAdminAccess.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Settlement", "SettlementPanelComposer.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Grid", "AdminPayoutGridQueryEngine.cs")));
        Assert.True(File.Exists(Path.Combine(SettlementRoot(), "Tooba.Settlement.Infrastructure", "Queries", "AdminPayoutGridQueryEngine.cs")));

        var hostGrid = File.ReadAllText(Path.Combine(hostRoot, "Grid", "AdminListGridPolicies.cs"));
        Assert.DoesNotContain("Payouts", hostGrid, StringComparison.Ordinal);
        Assert.DoesNotContain("PayoutRequestSnapshot", hostGrid, StringComparison.Ordinal);

        var bypass = AllProductionSources()
            .Where(x => x.Text.Contains("DateTimeOffset.UtcNow", StringComparison.Ordinal)
                        || x.Text.Contains("DateTime.UtcNow", StringComparison.Ordinal)
                        || x.Text.Contains("Guid.NewGuid()", StringComparison.Ordinal)
                        || x.Text.Contains("UuidV7.New()", StringComparison.Ordinal)
                        || x.Text.Contains("StartActivity(", StringComparison.Ordinal)
                        || x.Text.Contains("?? new SystemUtcClock()", StringComparison.Ordinal)
                        || x.Text.Contains("?? new UuidV7IdGenerator()", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToList();
        Assert.True(bypass.Count == 0, string.Join("; ", bypass));

        var silentCatch = AllProductionSources()
            .Where(x => Regex.IsMatch(x.Text, @"catch\s*\(\s*Exception\s*\)\s*\{|catch\s*\{\s*\}|catch\s*\([^)]+\)\s*\{\s*\}", RegexOptions.Multiline))
            .Select(x => x.Path)
            .ToList();
        Assert.True(silentCatch.Count == 0, "silent/empty catch: " + string.Join("; ", silentCatch));

        var localized = AllProductionSources()
            .SelectMany(x => Regex.Matches(x.Text, @"throw new \w+Exception\(\s*""([^""]*)""\s*\)")
                .Select(m => (x.Path, Msg: m.Groups[1].Value)))
            .Where(x => Regex.IsMatch(x.Msg, @"[\u0600-\u06FF]") || x.Msg.Contains(' ', StringComparison.Ordinal))
            .Where(x => !x.Msg.StartsWith("settlement.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("payout.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("domain.", StringComparison.Ordinal))
            .Select(x => $"{x.Path}:{x.Msg}")
            .ToList();
        Assert.True(localized.Count == 0, "localized exception prose: " + string.Join("; ", localized));
    }

    [Fact]
    public void Settlement_endpoints_cqrs_and_host_ownership_are_enforced()
    {
        Assert.True(Directory.Exists(Path.Combine(SettlementRoot(), "Tooba.Settlement.Endpoints")));
        Assert.True(File.Exists(Path.Combine(SettlementRoot(), "Tooba.Settlement.Endpoints", "Tooba.Settlement.Endpoints.csproj")));

        var seller = File.ReadAllText(Path.Combine(
            SettlementRoot(), "Tooba.Settlement.Endpoints", "Seller", "SettlementSellerEndpoints.cs"));
        var admin = File.ReadAllText(Path.Combine(
            SettlementRoot(), "Tooba.Settlement.Endpoints", "Admin", "SettlementAdminEndpoints.cs"));
        var module = File.ReadAllText(Path.Combine(
            SettlementRoot(), "Tooba.Settlement.Endpoints", "SettlementEndpointModule.cs"));

        foreach (var endpoint in new[] { seller, admin })
        {
            Assert.Contains("ISender sender", endpoint, StringComparison.Ordinal);
            Assert.Contains("ApiResponseFactory api", endpoint, StringComparison.Ordinal);
            Assert.Contains("api.From(", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("exception.Message", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("new { title", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("errorCode =", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("SettlementDbContext", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("AdminListGridPolicies", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("PlatformHttpException", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (InvalidOperationException", endpoint, StringComparison.Ordinal);
        }

        Assert.Contains("MapGroup(\"/v1/seller\")", module, StringComparison.Ordinal);
        Assert.Contains("MapGroup(\"/v1/admin\")", module, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/settlement/balance\"", seller, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/settlement/entries\"", seller, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/settlement/statements\"", seller, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/settlement/payout-requests\"", seller, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/settlement/payout-requests\"", seller, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/settlement/balances\"", admin, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/settlement/payout-queue\"", admin, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/settlement/payout-queue/query\"", admin, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/settlement/payout-requests/{payoutRequestId:guid}/process\"", admin, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/settlement/payout-requests/{payoutRequestId:guid}/retry\"", admin, StringComparison.Ordinal);

        var application = Sources("Tooba.Settlement.Application").ToList();
        Assert.Contains(application, x => x.Text.Contains("RequestSellerPayoutCommand", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("ProcessAdminPayoutCommand", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("RetryAdminPayoutCommand", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("GetSellerSettlementBalanceQuery", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("QueryAdminPayoutGridQuery", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("IRequestHandler<", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("using MediatR", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("AdminPayoutGridQueryPolicy", StringComparison.Ordinal));
        Assert.DoesNotContain(application, x => x.Path.EndsWith("SettlementCommands.cs", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(application, x => x.Path.EndsWith("SettlementQueries.cs", StringComparison.OrdinalIgnoreCase));

        var hostProgram = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.Contains("MapSettlementEndpoints()", hostProgram, StringComparison.Ordinal);
        Assert.Contains("ISettlementSellerAuthorizer", hostProgram, StringComparison.Ordinal);
        Assert.Contains("ISettlementAdminAuthorizer", hostProgram, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Host.Settlement;", hostProgram, StringComparison.Ordinal);
    }

    [Fact]
    public void Settlement_exception_mapper_is_stable_codes_only_without_prose_heuristics()
    {
        var mapperPath = Path.Combine(SettlementRoot(), "Tooba.Settlement.Application", "Errors", "SettlementExceptionMapper.cs");
        Assert.True(File.Exists(mapperPath));
        var mapper = File.ReadAllText(mapperPath);

        Assert.DoesNotContain(".Contains(", mapper, StringComparison.Ordinal);
        Assert.DoesNotContain("StartsWith(", mapper, StringComparison.Ordinal);
        Assert.DoesNotContain("StringComparison.OrdinalIgnoreCase", mapper, StringComparison.Ordinal);
        Assert.True(Regex.IsMatch(mapper, @"[\u0600-\u06FF]") == false, "Persian prose in SettlementExceptionMapper");

        Assert.False(
            Regex.IsMatch(
                mapper,
                @"default\s*:\s*(?:\{[^}]*?)?(?:return\s+new\s+SemanticError\(\s*SettlementErrorCodes\.PayoutRejected\s*\)|error\s*=\s*new\s+SemanticError\(\s*SettlementErrorCodes\.PayoutRejected)",
                RegexOptions.Singleline),
            "default branch must not map unknown IOE to settlement.payout.rejected");
        Assert.Contains("TryMapExact", mapper, StringComparison.Ordinal);
        Assert.Contains("default:", mapper, StringComparison.Ordinal);
        Assert.Contains("return false", mapper, StringComparison.Ordinal);
        Assert.Contains("throw exception", mapper, StringComparison.Ordinal);

        var messageHeuristics = Sources("Tooba.Settlement.Application")
            .Where(x => x.Path.Replace('\\', '/').Contains("/Errors/", StringComparison.Ordinal)
                        || x.Path.Replace('\\', '/').Contains("/Commands/", StringComparison.Ordinal)
                        || x.Path.Replace('\\', '/').Contains("/Queries/", StringComparison.Ordinal))
            .Where(x => Regex.IsMatch(
                x.Text,
                @"exception\.Message.*\.Contains\(|\.Message\s*\.Contains\(|text\.Contains\(|message\.StartsWith\(",
                RegexOptions.IgnoreCase))
            .Select(x => x.Path)
            .ToList();
        Assert.True(messageHeuristics.Count == 0, "message heuristics: " + string.Join("; ", messageHeuristics));
    }

    [Fact]
    public void Settlement_host_residue_is_exactly_two_thin_security_adapters()
    {
        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");

        var authorizers = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(p => File.ReadAllText(p).Contains("ISettlement", StringComparison.Ordinal)
                        && (File.ReadAllText(p).Contains("ISettlementAdminAuthorizer", StringComparison.Ordinal)
                            || File.ReadAllText(p).Contains("ISettlementSellerAuthorizer", StringComparison.Ordinal)))
            .Where(p => !p.EndsWith("Program.cs", StringComparison.Ordinal))
            .Select(p => Path.GetRelativePath(hostRoot, p).Replace('\\', '/'))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[] { "Admin/HostSettlementAdminAuthorizer.cs", "Seller/HostSettlementSellerAuthorizer.cs" },
            authorizers);

        // No Host Settlement business surface may exist.
        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Settlement")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Grid", "AdminPayoutGridQueryEngine.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Settlement", "SettlementPanelComposer.cs")));

        // Settlement -> Host dependency must remain ZERO.
        var settlementSources = new[]
        {
            "Tooba.Settlement.Domain", "Tooba.Settlement.Contracts",
            "Tooba.Settlement.Application", "Tooba.Settlement.Infrastructure", "Tooba.Settlement.Endpoints",
        };
        foreach (var project in settlementSources)
        {
            var csproj = Path.Combine(SettlementRoot(), project, project + ".csproj");
            if (!File.Exists(csproj)) continue;
            var refs = ProjectRefs(project);
            Assert.DoesNotContain(refs, r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));
        }

        var hostDependency = AllProductionSources()
            .Where(x => x.Text.Contains("Tooba.Host", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToArray();
        Assert.True(hostDependency.Length == 0, "Settlement -> Host: " + string.Join("; ", hostDependency));
    }

    private static void AssertNoRootDump(string project, string[] allowedFolders)
    {
        var root = Path.Combine(SettlementRoot(), project);
        var rootCs = Directory.EnumerateFiles(root, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(name => name is not null
                           && !name.StartsWith("GlobalUsings", StringComparison.OrdinalIgnoreCase)
                           && !name.EndsWith("EndpointModule.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.True(rootCs.Length == 0, $"{project} root dumping-ground: " + string.Join(", ", rootCs));
        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            var name = Path.GetFileName(dir);
            if (name is "bin" or "obj" or "artifacts") continue;
            Assert.Contains(name, allowedFolders);
        }
    }

    private static void AssertNamespacesAlign(string projectFolder, string nsPrefix)
    {
        var root = Path.Combine(SettlementRoot(), projectFolder);
        var rootFull = Path.GetFullPath(root);
        var violations = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var normalized = file.Replace('\\', '/');
            if (normalized.Contains("/bin/", StringComparison.Ordinal) || normalized.Contains("/obj/", StringComparison.Ordinal))
            {
                continue;
            }

            var relative = Path.GetRelativePath(rootFull, file).Replace('\\', '/');

            // GlobalUsings files aggregate imports and declare no namespace by design; they are
            // covered by the dedicated global-using guard.
            if (Path.GetFileName(relative).StartsWith("GlobalUsings", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // EF generated migrations + model snapshot keep their legitimate migrations namespace.
            if (relative.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase)
                || relative.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var dir = Path.GetDirectoryName(relative)?.Replace('\\', '/');
            var expected = string.IsNullOrEmpty(dir)
                ? nsPrefix
                : nsPrefix + "." + dir.Replace('/', '.');

            var text = File.ReadAllText(file);
            var match = Regex.Match(text, @"^namespace\s+([\w.]+)", RegexOptions.Multiline);
            if (!match.Success)
            {
                violations.Add($"{relative}: no namespace");
                continue;
            }

            // Exact equality only: a name that merely starts with the expected prefix is a violation.
            if (!string.Equals(match.Groups[1].Value, expected, StringComparison.Ordinal))
            {
                violations.Add($"{relative}: ns={match.Groups[1].Value} expected={expected}");
            }
        }

        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }

    [Fact]
    public void Settlement_root_allowlists_and_forbidden_flattened_files_are_enforced()
    {
        var expected = new (string Project, string[] Allowlist, string[] Forbidden)[]
        {
            ("Tooba.Settlement.Application", ["GlobalUsings.Domain.cs", "GlobalUsings.Layout.cs"], [
                "SettlementContracts.cs", "SettlementCommands.cs", "SettlementQueries.cs",
                "SettlementHandlers.cs", "SettlementErrorCodes.cs", "SettlementAdminModels.cs",
                "RequestSellerPayoutCommand.cs", "QueryAdminPayoutGridQuery.cs",
            ]),
            ("Tooba.Settlement.Endpoints", ["SettlementEndpointModule.cs"], [
                "SettlementSellerEndpoints.cs", "SettlementAdminEndpoints.cs",
                "ISettlementSellerAuthorizer.cs", "ISettlementAdminAuthorizer.cs",
            ]),
            ("Tooba.Settlement.Infrastructure", ["GlobalUsings.Domain.cs", "GlobalUsings.Layout.cs"], [
                "SettlementModule.cs", "SettlementDbContext.cs", "SettlementDirectory.cs",
                "SettlementOutboxRegistration.cs", "SettlementEventHandlers.cs",
                "AdminPayoutGridQueryEngine.cs",
            ]),
        };

        foreach (var (project, allowlist, forbidden) in expected)
        {
            var root = Path.Combine(SettlementRoot(), project);
            var actual = Directory.GetFiles(root, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
            Assert.Equal(allowlist.OrderBy(x => x, StringComparer.Ordinal).ToArray(), actual);

            foreach (var file in forbidden)
            {
                Assert.False(File.Exists(Path.Combine(root, file)), $"{project} forbidden root file {file}");
            }
        }
    }

    [Fact]
    public void Settlement_global_usings_are_approved_project_wide_imports_only()
    {
        var expected = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Tooba.Settlement.Application/GlobalUsings.Domain.cs"] =
            [
                "Tooba.Settlement.Domain.Aggregates",
                "Tooba.Settlement.Domain.Entities",
                "Tooba.Settlement.Domain.Events",
                "Tooba.Settlement.Domain.ValueObjects",
            ],
            ["Tooba.Settlement.Application/GlobalUsings.Layout.cs"] =
            [
                "Tooba.Settlement.Application.Ports",
            ],
            ["Tooba.Settlement.Infrastructure/GlobalUsings.Domain.cs"] =
            [
                "Tooba.Settlement.Domain.Aggregates",
                "Tooba.Settlement.Domain.Entities",
                "Tooba.Settlement.Domain.Events",
                "Tooba.Settlement.Domain.ValueObjects",
            ],
            ["Tooba.Settlement.Infrastructure/GlobalUsings.Layout.cs"] =
            [
                "Tooba.Settlement.Application.Ports",
                "Tooba.Settlement.Infrastructure.Directories",
                "Tooba.Settlement.Infrastructure.DependencyInjection",
                "Tooba.Settlement.Infrastructure.Messaging",
                "Tooba.Settlement.Infrastructure.Handlers",
                "Tooba.Settlement.Infrastructure.Observability",
                "Tooba.Settlement.Infrastructure.Bridges",
                "Tooba.Settlement.Infrastructure.Gateways",
            ],
        };

        var actual = Directory.EnumerateFiles(SettlementRoot(), "GlobalUsings*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(p => Path.GetRelativePath(SettlementRoot(), p).Replace('\\', '/'))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expected.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray(), actual);

        foreach (var (relative, usings) in expected)
        {
            var text = File.ReadAllText(Path.Combine(SettlementRoot(), relative));
            var aliases = Regex.Matches(text, @"global\s+using\s+([\w.]+)\s*;")
                .Select(m => m.Groups[1].Value)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
            Assert.Equal(usings.OrderBy(x => x, StringComparer.Ordinal).ToArray(), aliases);
            Assert.DoesNotContain("=", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Settlement_rejects_namespace_alias_workarounds_and_foreign_global_aliases()
    {
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));

        // No foreign-module global alias may be used to hide wrong physical namespaces.
        var foreignGlobalAlias = AllProductionSources()
            .Where(x => Regex.IsMatch(
                x.Text,
                @"global\s+using\s+Tooba\.(Order|Payment|Returns|Party|Cart|Offer|Wallet|Fulfillment|Notification|Support|Promotion|Inventory|Media)\.[\w.]*(Application|Infrastructure|Domain)"))
            .Select(x => x.Path)
            .ToArray();
        Assert.True(foreignGlobalAlias.Length == 0, "foreign global alias: " + string.Join("; ", foreignGlobalAlias));

        // No compatibility shim may preserve an old flattened Settlement path.
        var shims = AllProductionSources()
            .Where(x => Regex.IsMatch(x.Text, @"namespace\s+Tooba\.Settlement\.(Application|Infrastructure|Endpoints);", RegexOptions.Multiline)
                        && (x.Path.Contains("/Commands/", StringComparison.Ordinal)
                            || x.Path.Contains("/Queries/", StringComparison.Ordinal)
                            || x.Path.Contains("/Validators/", StringComparison.Ordinal)))
            .Select(x => x.Path)
            .ToArray();
        Assert.True(shims.Length == 0, "flattened-namespace shim: " + string.Join("; ", shims));
    }

    private static IEnumerable<(string Path, string Text)> AllProductionSources() =>
        Sources("Tooba.Settlement.Domain")
            .Concat(Sources("Tooba.Settlement.Application"))
            .Concat(Sources("Tooba.Settlement.Infrastructure"))
            .Concat(Sources("Tooba.Settlement.Endpoints"));

    private static IEnumerable<(string Path, string Text)> Sources(string projectFolder)
    {
        var root = Path.Combine(SettlementRoot(), projectFolder);
        if (!Directory.Exists(root))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var n = file.Replace('\\', '/');
            if (n.Contains("/bin/", StringComparison.Ordinal) || n.Contains("/obj/", StringComparison.Ordinal))
                continue;
            if (n.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase) || n.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
                continue;
            yield return (Path.GetRelativePath(RepoRoot(), file), File.ReadAllText(file));
        }
    }

    private static IReadOnlyList<string> ProjectRefs(string projectFolder)
    {
        var csproj = Path.Combine(SettlementRoot(), projectFolder, projectFolder + ".csproj");
        var doc = XDocument.Load(csproj);
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
