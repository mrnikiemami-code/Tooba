using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Support.Tests.Architecture;

public sealed class SupportArchitectureGuardTests
{
    private static readonly string[] AllowedDomainFolders = ["Aggregates", "Entities", "ValueObjects", "Events", "Policies"];
    private static readonly string[] AllowedApplicationFolders = ["Ports", "Models", "Commands", "Queries", "Errors"];
    private static readonly string[] AllowedInfrastructureFolders =
        ["Persistence", "Directories", "Adapters", "Seeds", "Messaging", "DependencyInjection", "Migrations"];
    private static readonly string[] AllowedEndpointsFolders = ["Customer", "Seller", "Admin", "Errors", "Resources"];

    private static readonly HashSet<string> HostDbContextAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "Program.cs",
        "ModuleMigrationRegistry.cs",
        "SupportDevelopmentSeedHost.cs",
        "ProductWorkspaceDevelopmentBootstrap.cs",
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

        throw new InvalidOperationException("repo root not found");
    }

    private static string SupportRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Support");

    [Fact]
    public void Support_references_Notification_Contracts_only()
    {
        var refs = ProjectRefs("Tooba.Support.Infrastructure");
        Assert.Contains(refs, r => r.Contains("Notification.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(refs, r => r.Contains("Notification.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Notification.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Notification.Infrastructure", StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(
            Sources("Tooba.Support.Infrastructure"),
            x => x.Text.Contains("using Tooba.Notification.Application", StringComparison.Ordinal)
                 || x.Text.Contains("using Tooba.Notification.Domain", StringComparison.Ordinal)
                 || x.Text.Contains("INotificationDirectory", StringComparison.Ordinal));
        Assert.Contains(
            Sources("Tooba.Support.Infrastructure"),
            x => x.Text.Contains("INotificationCreationPort", StringComparison.Ordinal));
    }

    [Fact]
    public void Support_golden_boundaries_and_physical_layout_remain_clean()
    {
        Assert.DoesNotContain(ProjectRefs("Tooba.Support.Domain"), x => x.Contains("Contracts", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));

        var directory = File.ReadAllText(Path.Combine(SupportRoot(), "Tooba.Support.Infrastructure", "Directories", "SupportDirectory.cs"));
        Assert.Contains("IClock", directory, StringComparison.Ordinal);
        Assert.Contains("IIdGenerator", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new SystemUtcClock()", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new UuidV7IdGenerator()", directory, StringComparison.Ordinal);

        AssertNoRootDump("Tooba.Support.Domain", AllowedDomainFolders);
        AssertNoRootDump("Tooba.Support.Application", AllowedApplicationFolders);
        AssertNoRootDump("Tooba.Support.Infrastructure", AllowedInfrastructureFolders);
        AssertNoRootDump("Tooba.Support.Endpoints", AllowedEndpointsFolders);
        AssertNamespacesAlign("Tooba.Support.Domain", "Tooba.Support.Domain");
        AssertNamespacesAlign("Tooba.Support.Application", "Tooba.Support.Application");
        AssertNamespacesAlign("Tooba.Support.Infrastructure", "Tooba.Support.Infrastructure");
        AssertNamespacesAlign("Tooba.Support.Endpoints", "Tooba.Support.Endpoints");

        var bypass = AllProductionSources()
            .Where(x => x.Text.Contains("DateTimeOffset.UtcNow", StringComparison.Ordinal)
                        || x.Text.Contains("DateTime.UtcNow", StringComparison.Ordinal)
                        || x.Text.Contains("Guid.NewGuid()", StringComparison.Ordinal)
                        || x.Text.Contains("UuidV7.New()", StringComparison.Ordinal)
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
            .Where(x => !x.Msg.StartsWith("support.", StringComparison.Ordinal))
            .Select(x => $"{x.Path}:{x.Msg}")
            .ToList();
        Assert.True(localized.Count == 0, "localized exception prose: " + string.Join("; ", localized));
    }

    [Fact]
    public void Support_endpoints_cqrs_and_host_ownership_are_enforced()
    {
        Assert.True(Directory.Exists(Path.Combine(SupportRoot(), "Tooba.Support.Endpoints")));
        Assert.True(File.Exists(Path.Combine(SupportRoot(), "Tooba.Support.Endpoints", "Tooba.Support.Endpoints.csproj")));

        var customer = File.ReadAllText(Path.Combine(SupportRoot(), "Tooba.Support.Endpoints", "Customer", "SupportCustomerEndpoints.cs"));
        var seller = File.ReadAllText(Path.Combine(SupportRoot(), "Tooba.Support.Endpoints", "Seller", "SupportSellerEndpoints.cs"));
        var admin = File.ReadAllText(Path.Combine(SupportRoot(), "Tooba.Support.Endpoints", "Admin", "SupportAdminEndpoints.cs"));
        var module = File.ReadAllText(Path.Combine(SupportRoot(), "Tooba.Support.Endpoints", "SupportEndpointModule.cs"));

        foreach (var endpoint in new[] { customer, seller, admin })
        {
            Assert.Contains("ISender sender", endpoint, StringComparison.Ordinal);
            Assert.Contains("ApiResponseFactory", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("ISupportDirectory", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("exception.Message", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("new { title", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("SupportDbContext", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (InvalidOperationException", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (PlatformHttpException", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("Rejected(", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("Missing()", endpoint, StringComparison.Ordinal);
        }

        Assert.Contains("MapGroup(\"/v1/customer/support\")", module, StringComparison.Ordinal);
        Assert.Contains("MapGroup(\"/v1/seller/support\")", module, StringComparison.Ordinal);
        Assert.Contains("MapGroup(\"/v1/admin/support\")", module, StringComparison.Ordinal);
        Assert.Contains("ListCustomerTicketsQuery", customer, StringComparison.Ordinal);
        Assert.Contains("CreateCustomerTicketCommand", customer, StringComparison.Ordinal);
        Assert.Contains("ListSellerTicketsQuery", seller, StringComparison.Ordinal);
        Assert.Contains("CreateSellerTicketCommand", seller, StringComparison.Ordinal);
        Assert.Contains("support.view", seller, StringComparison.Ordinal);
        Assert.Contains("support.create", seller, StringComparison.Ordinal);
        Assert.Contains("support.reply", seller, StringComparison.Ordinal);
        Assert.Contains("ListAdminTicketsQuery", admin, StringComparison.Ordinal);
        Assert.Contains("PatchAdminTicketCommand", admin, StringComparison.Ordinal);
        Assert.Contains("GetSupportDemoPreviewQuery", admin, StringComparison.Ordinal);
        Assert.Contains("demo-preview", admin, StringComparison.Ordinal);

        var endpointRefs = ProjectRefs("Tooba.Support.Endpoints");
        Assert.Contains(endpointRefs, r => r.Contains("Support.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(endpointRefs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointRefs, r => r.Contains("Host", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointRefs, r => r.Contains("DbContext", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointRefs, r => r.Contains("AccessControl.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointRefs, r => r.Contains("AccessControl.Domain", StringComparison.OrdinalIgnoreCase));

        var application = Sources("Tooba.Support.Application").ToList();
        Assert.Contains(application, x => x.Text.Contains("ListCustomerTicketsQuery", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("CreateCustomerTicketCommand", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("ReplySellerTicketCommand", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("PatchAdminTicketCommand", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("GetSupportDemoPreviewQuery", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("IRequestHandler<", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("using MediatR", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("SupportExceptionMapper", StringComparison.Ordinal));
        Assert.DoesNotContain(application, x =>
            x.Text.Contains("StartsWith(\"support.\"", StringComparison.Ordinal)
            || (x.Text.Contains(".Contains(\"", StringComparison.Ordinal) && x.Path.Contains("ExceptionMapper", StringComparison.Ordinal)));

        Assert.False(File.Exists(Path.Combine(SupportRoot(), "Tooba.Support.Application", "Commands", "SupportCommands.cs")));
        Assert.False(File.Exists(Path.Combine(SupportRoot(), "Tooba.Support.Application", "Queries", "SupportQueries.cs")));

        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        Assert.False(File.Exists(Path.Combine(hostRoot, "Support", "SupportEndpoints.cs")));
        Assert.True(File.Exists(Path.Combine(hostRoot, "Support", "SupportDevelopmentSeedHost.cs")));
        Assert.True(File.Exists(Path.Combine(hostRoot, "Customer", "HostSupportCustomerAuthorizer.cs")));
        Assert.True(File.Exists(Path.Combine(hostRoot, "Seller", "HostSupportSellerAuthorizer.cs")));
        Assert.True(File.Exists(Path.Combine(hostRoot, "Admin", "HostSupportAdminAuthorizer.cs")));

        var hostCustomer = File.ReadAllText(Path.Combine(hostRoot, "Customer", "HostSupportCustomerAuthorizer.cs"));
        var hostSeller = File.ReadAllText(Path.Combine(hostRoot, "Seller", "HostSupportSellerAuthorizer.cs"));
        var hostAdmin = File.ReadAllText(Path.Combine(hostRoot, "Admin", "HostSupportAdminAuthorizer.cs"));
        Assert.Contains("X-Tooba-Dev-Actor-User-Id", hostCustomer, StringComparison.Ordinal);
        Assert.Contains("SellerPanelAccess.RequireAuthorizedAsync", hostSeller, StringComparison.Ordinal);
        Assert.Contains("SellerAuthorizationDenied", hostSeller, StringComparison.Ordinal);
        Assert.Contains("GetEffectiveAccessAsync", hostSeller, StringComparison.Ordinal);
        Assert.Contains("AdminPanelAccess.RequireAuthorizedAsync", hostAdmin, StringComparison.Ordinal);
        Assert.Contains("AuthorizationDecisionKind.Unavailable", hostAdmin, StringComparison.Ordinal);
        Assert.DoesNotContain("ISupportDirectory", hostCustomer, StringComparison.Ordinal);
        Assert.DoesNotContain("ISupportDirectory", hostSeller, StringComparison.Ordinal);
        Assert.DoesNotContain("ISupportDirectory", hostAdmin, StringComparison.Ordinal);

        var programCs = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));
        Assert.Contains("MapSupportEndpoints()", programCs, StringComparison.Ordinal);
        Assert.Contains("AddSupportEndpointPresentation()", programCs, StringComparison.Ordinal);
        Assert.Contains("HostSupportCustomerAuthorizer", programCs, StringComparison.Ordinal);
        Assert.Contains("HostSupportSellerAuthorizer", programCs, StringComparison.Ordinal);
        Assert.Contains("HostSupportAdminAuthorizer", programCs, StringComparison.Ordinal);
        Assert.Contains("CreateCustomerTicketCommand", programCs, StringComparison.Ordinal);

        var hostHits = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                           && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains("SupportDbContext", StringComparison.Ordinal))
            .Where(path => !HostDbContextAllowlist.Contains(Path.GetFileName(path)))
            .ToList();
        Assert.True(hostHits.Count == 0, "Host SupportDbContext allowlist: " + string.Join("; ", hostHits));
    }

    private static void AssertNoRootDump(string project, string[] allowedFolders)
    {
        var root = Path.Combine(SupportRoot(), project);
        var rootCs = Directory.EnumerateFiles(root, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(name => name is not null
                           && !name.EndsWith("EndpointModule.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.True(rootCs.Length == 0, $"{project} root dumping-ground: " + string.Join(", ", rootCs));
        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            var name = Path.GetFileName(dir);
            if (name is "bin" or "obj" or "artifacts" || name.StartsWith('.')) continue;
            Assert.Contains(name, allowedFolders);
        }
    }

    private static void AssertNamespacesAlign(string projectFolder, string nsPrefix)
    {
        var violations = new List<string>();
        foreach (var (path, text) in Sources(projectFolder))
        {
            var ns = Regex.Match(text, @"^namespace\s+([\w.]+)", RegexOptions.Multiline).Groups[1].Value;
            if (string.IsNullOrEmpty(ns) || !ns.StartsWith(nsPrefix, StringComparison.Ordinal))
            {
                violations.Add($"{path}: ns={ns}");
                continue;
            }

            var rel = path.Replace('\\', '/');
            var marker = projectFolder.Replace('\\', '/') + "/";
            var idx = rel.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0) continue;
            var under = rel[(idx + marker.Length)..];
            var folder = under.Split('/')[0];
            if (folder.EndsWith(".cs", StringComparison.Ordinal)) continue;
            var expected = nsPrefix + "." + folder;
            if (!ns.StartsWith(expected, StringComparison.Ordinal))
                violations.Add($"{path}: ns={ns} expectedPrefix={expected}");
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<(string Path, string Text)> AllProductionSources() =>
        Sources("Tooba.Support.Domain")
            .Concat(Sources("Tooba.Support.Application"))
            .Concat(Sources("Tooba.Support.Infrastructure"))
            .Concat(Sources("Tooba.Support.Endpoints"));

    private static IEnumerable<(string Path, string Text)> Sources(string projectFolder)
    {
        var root = Path.Combine(SupportRoot(), projectFolder);
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
        var csproj = Path.Combine(SupportRoot(), projectFolder, projectFolder + ".csproj");
        var doc = XDocument.Load(csproj);
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
