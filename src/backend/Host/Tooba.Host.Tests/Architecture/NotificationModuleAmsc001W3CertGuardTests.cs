using Tooba.Notification.Contracts.Errors;
using Tooba.Notification.Application.Validators;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-NOTIFICATION-AMSC-001-W3 — certification durable guard.
/// Locks the ARCH-COMPLETE-002 certified structure: capability-first Application tree, exact
/// path↔namespace, root allowlists, canonical fault/localization seams, manifest promotion, and
/// zero foreign coupling. Never weakens the W1/W2 guards; adds the certification-level assertions.
/// </summary>
public sealed class NotificationModuleAmsc001W3CertGuardTests
{
    private static readonly string[] ProductionProjects =
    [
        "Tooba.Notification.Contracts",
        "Tooba.Notification.Domain",
        "Tooba.Notification.Application",
        "Tooba.Notification.Infrastructure",
        "Tooba.Notification.Endpoints",
    ];

    [Fact]
    public void Application_is_capability_first_with_no_technical_axis_roots()
    {
        var app = Path.Combine(Repo(), "src/backend/Modules/Notification/Tooba.Notification.Application");
        var dirs = Directory.EnumerateDirectories(app)
            .Select(d => Path.GetFileName(d)!)
            .Where(n => n is not "bin" and not "obj" and not "artifacts" && !n.StartsWith('.'))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(new[] { "Composition", "Customer", "Models", "Ports", "Rendering", "Seller", "Validators" }, dirs);
        Assert.Equal(0, Directory.GetFiles(app, "*.cs", SearchOption.TopDirectoryOnly).Length);

        // Technical-axis request trees must never return at Application root.
        Assert.False(Directory.Exists(Path.Combine(app, "Commands")));
        Assert.False(Directory.Exists(Path.Combine(app, "Queries")));
        Assert.False(Directory.Exists(Path.Combine(app, "Errors")));
        Assert.False(Directory.Exists(Path.Combine(app, "Services")));
    }

    [Fact]
    public void Capability_leaves_carry_the_ten_endpoint_requests()
    {
        foreach (var file in new[]
                 {
                     "Customer/Commands/MarkCustomerNotificationRead/MarkCustomerNotificationReadCommand.cs",
                     "Customer/Commands/MarkAllCustomerNotificationsRead/MarkAllCustomerNotificationsReadCommand.cs",
                     "Customer/Commands/DismissCustomerNotification/DismissCustomerNotificationCommand.cs",
                     "Customer/Queries/ListCustomerNotifications/ListCustomerNotificationsQuery.cs",
                     "Customer/Queries/GetCustomerUnreadNotificationCount/GetCustomerUnreadNotificationCountQuery.cs",
                     "Seller/Commands/MarkSellerNotificationRead/MarkSellerNotificationReadCommand.cs",
                     "Seller/Commands/MarkAllSellerNotificationsRead/MarkAllSellerNotificationsReadCommand.cs",
                     "Seller/Commands/DismissSellerNotification/DismissSellerNotificationCommand.cs",
                     "Seller/Queries/ListSellerNotifications/ListSellerNotificationsQuery.cs",
                     "Seller/Queries/GetSellerUnreadNotificationCount/GetSellerUnreadNotificationCountQuery.cs",
                 })
        {
            var text = Read($"src/backend/Modules/Notification/Tooba.Notification.Application/{file}");
            Assert.Contains("IRequestHandler<", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Path_namespace_alignment_is_exact_for_all_production_files()
    {
        var violations = new List<string>();
        foreach (var project in ProductionProjects)
        {
            var projectPath = Path.Combine(Repo(), "src", "backend", "Modules", "Notification", project);
            var rootFull = Path.GetFullPath(projectPath);
            foreach (var file in Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories))
            {
                var relative = file[rootFull.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (relative.StartsWith($"obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || relative.StartsWith($"bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                // EF generated migrations keep the historical namespaces; excluded by the repo-wide lock.
                if (relative.Contains($"Persistence{Path.DirectorySeparatorChar}Migrations", StringComparison.Ordinal)
                    || relative.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal))
                {
                    continue;
                }

                var dir = Path.GetDirectoryName(relative);
                var expected = string.IsNullOrEmpty(dir)
                    ? project
                    : project + "." + dir.Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.');
                var text = File.ReadAllText(file);
                var match = System.Text.RegularExpressions.Regex.Match(
                    text, @"^namespace\s+([A-Za-z0-9_.]+)", System.Text.RegularExpressions.RegexOptions.Multiline);
                if (!match.Success || !string.Equals(expected, match.Groups[1].Value, StringComparison.Ordinal))
                {
                    violations.Add($"{project}/{relative}");
                }
            }
        }

        Assert.True(violations.Count == 0, "namespace mismatches: " + string.Join("; ", violations));
    }

    [Fact]
    public void Manifest_promotes_notification_to_certified()
    {
        var manifest = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(Path.Combine(Repo(), "docs", "architecture", "tmar-module-structure-manifests.json")));
        var module = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("module").GetString() == "Notification");
        Assert.True(!module.Equals(default), "Notification manifest entry missing");
        Assert.True(module.GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", module.GetProperty("lockVersion").GetString());

        var uncertified = manifest.RootElement.GetProperty("uncertifiedHttpOwningModules").EnumerateArray()
            .Select(x => x.GetString()!).ToArray();
        Assert.DoesNotContain("Notification", uncertified);

        var names = module.GetProperty("projects").EnumerateArray()
            .Select(p => p.GetProperty("projectName").GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(new[]
        {
            "Tooba.Notification.Application", "Tooba.Notification.Contracts", "Tooba.Notification.Domain",
            "Tooba.Notification.Endpoints", "Tooba.Notification.Infrastructure", "Tooba.Notification.Tests",
        }, names);
    }

    [Fact]
    public void Sot_records_the_certification()
    {
        var state = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(Path.Combine(Repo(), "docs", "architecture", "tmar-current-state.json")));
        var block = state.RootElement.GetProperty("notificationModuleAmsc001W3");
        Assert.Equal("CERTIFIED", block.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", block.GetProperty("verdict").GetString());
        Assert.Equal("EXHAUSTIVE", block.GetProperty("validatorCoverageState").GetString());
        Assert.Equal("NONE", block.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.True(block.GetProperty("manifestCertified").GetBoolean());
    }

    [Fact]
    public void Every_route_maps_through_isender_and_api_response_factory()
    {
        var customer = Read("src/backend/Modules/Notification/Tooba.Notification.Endpoints/Customer/NotificationCustomerEndpoints.cs");
        var seller = Read("src/backend/Modules/Notification/Tooba.Notification.Endpoints/Seller/NotificationSellerEndpoints.cs");
        foreach (var text in new[] { customer, seller })
        {
            Assert.Equal(5, System.Text.RegularExpressions.Regex.Matches(text, "sender\\.Send\\(").Count);
            Assert.Contains("ApiResponseFactory api", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.BadRequest", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Problem", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Module_composition_registers_the_canonical_seams_once()
    {
        var module = Read("src/backend/Modules/Notification/Tooba.Notification.Infrastructure/DependencyInjection/NotificationModule.cs");
        Assert.Contains("IErrorResourceSet, NotificationErrorResourceSet", module, StringComparison.Ordinal);
        Assert.Equal(1, System.Text.RegularExpressions.Regex.Matches(module, "AddSingleton<IErrorResourceSet").Count);
        Assert.Contains("AddModuleSchemaMigrator<NotificationDbContext>", module, StringComparison.Ordinal);

        var endpointModule = Read("src/backend/Modules/Notification/Tooba.Notification.Endpoints/NotificationEndpointModule.cs");
        Assert.Contains("IErrorCatalogContributor, NotificationErrorCatalogContributor", endpointModule, StringComparison.Ordinal);
    }

    [Fact]
    public void Zero_foreign_application_infrastructure_domain_dependencies()
    {
        foreach (var project in ProductionProjects)
        {
            var refs = ProjectRefs(project);
            Assert.DoesNotContain(refs, r => System.Text.RegularExpressions.Regex.IsMatch(
                r, @"Tooba\.(Order|Payment|Fulfillment|Returns|Wallet|Support|Cart|Catalog|Identity|Media|Localization)\.(Application|Infrastructure|Domain)\."));
            Assert.DoesNotContain(refs, r => r.Contains("Tooba.Host", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Typed_fault_and_localization_seams_stay_canonical()
    {
        var op = Read("src/backend/Modules/Notification/Tooba.Notification.Application/Composition/NotificationOperation.cs");
        Assert.Contains("NotificationErrorCodes.IsKnown", op, StringComparison.Ordinal);

        var routes = Read("src/backend/Modules/Notification/Tooba.Notification.Contracts/Routes/NotificationTargetRoutes.cs");
        Assert.Contains("ContractOperationException(NotificationErrorCodes.TargetRouteUnsafe)", routes, StringComparison.Ordinal);

        var resources = Path.Combine(Repo(), "src/backend/Modules/Notification/Tooba.Notification.Contracts/Resources");
        Assert.True(File.Exists(Path.Combine(resources, "NotificationErrors.resx")));
        Assert.True(File.Exists(Path.Combine(resources, "NotificationErrors.fa.resx")));
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

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(Repo(), relativePath));

    private static IReadOnlyList<string> ProjectRefs(string project)
    {
        var doc = System.Xml.Linq.XDocument.Load(Path.Combine(
            Repo(), "src", "backend", "Modules", "Notification", project, $"{project}.csproj"));
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
