using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-NOTIFICATION-AMSC-001-W3-R3 — fresh certification durable guard.
/// Independent re-verification of the recertified current tree: current SoT/manifest certification
/// facts, the repaired shallow request tree, the canonical typed-fault/localization seams, the
/// exhaustive 6+4 validator matrix, Contracts-only boundaries and Host closure. Adds the
/// certification-level lock on top of the W3-R2 structure-repair guard; never weakens prior guards.
/// </summary>
public sealed class NotificationModuleAmsc001W3R3RecertGuardTests
{
    [Fact]
    public void Sot_records_the_current_recertification()
    {
        var state = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(Path.Combine(Repo(), "docs", "architecture", "tmar-current-state.json")));
        var block = state.RootElement.GetProperty("notificationModuleAmsc001W3R3");
        Assert.Equal("NOTIFICATION_AMSC_001_RECERTIFIED", block.GetProperty("state").GetString());
        Assert.Equal("COMPLETE_REFERENCE_PATTERN", block.GetProperty("verdict").GetString());
        Assert.Equal("ARCH-COMPLETE-002", block.GetProperty("lockVersion").GetString());
        Assert.Equal("CERTIFIED", block.GetProperty("structureState").GetString());
        Assert.Equal("TB-TMAR-NOTIFICATION-AMSC-001-W3-R2", block.GetProperty("structureGateSource").GetString());
        Assert.Equal("PROFESSIONAL_SHALLOW", block.GetProperty("folderGranularityState").GetString());
        Assert.Equal("EXHAUSTIVE", block.GetProperty("validatorCoverageState").GetString());
        Assert.Equal("ZERO", block.GetProperty("foreignAppInfraDomainCoupling").GetString());
        Assert.Equal("ZERO", block.GetProperty("crossModuleJoinState").GetString());
        Assert.Equal("ZERO", block.GetProperty("crossModulePersistenceState").GetString());
        Assert.True(block.GetProperty("structureCertified").GetBoolean());
        Assert.True(block.GetProperty("microserviceExtractable").GetBoolean());
        Assert.True(block.GetProperty("productionCodeChangedThisWave").GetBoolean() is false);
    }

    [Fact]
    public void Manifest_certification_facts_match_current_truth()
    {
        var manifest = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(Path.Combine(Repo(), "docs", "architecture", "tmar-module-structure-manifests.json")));
        var entries = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => m.GetProperty("module").GetString() == "Notification")
            .ToArray();
        Assert.Single(entries);
        Assert.True(entries[0].GetProperty("structureCertified").GetBoolean());
        Assert.Equal("ARCH-COMPLETE-002", entries[0].GetProperty("lockVersion").GetString());

        var uncertified = manifest.RootElement.GetProperty("uncertifiedHttpOwningModules").EnumerateArray()
            .Select(x => x.GetString()!)
            .ToArray();
        Assert.DoesNotContain("Notification", uncertified);

        // Flat request axes are compatible with the manifest: the Application entry forbids only the
        // root-level technical axes and carries no per-leaf path declarations.
        var app = entries[0].GetProperty("projects").EnumerateArray()
            .First(p => p.GetProperty("projectName").GetString() == "Tooba.Notification.Application");
        Assert.Contains("Commands", app.GetProperty("forbiddenTopLevelFolders").EnumerateArray().Select(x => x.GetString()!).ToArray());
        Assert.Contains("Queries", app.GetProperty("forbiddenTopLevelFolders").EnumerateArray().Select(x => x.GetString()!).ToArray());
    }

    [Fact]
    public void Repaired_shallow_request_tree_stays_flat()
    {
        var app = Path.Combine(Repo(), "src/backend/Modules/Notification/Tooba.Notification.Application");
        foreach (var axis in new[] { "Customer/Commands", "Customer/Queries", "Seller/Commands", "Seller/Queries" })
        {
            var path = Path.Combine(app, axis.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(Directory.Exists(path), "missing axis: " + axis);
            Assert.Empty(Directory.EnumerateDirectories(path));
        }

        // All ten requests still present directly on their axis.
        foreach (var file in new[]
                 {
                     "Customer/Commands/MarkCustomerNotificationReadCommand.cs",
                     "Customer/Commands/MarkAllCustomerNotificationsReadCommand.cs",
                     "Customer/Commands/DismissCustomerNotificationCommand.cs",
                     "Customer/Queries/ListCustomerNotificationsQuery.cs",
                     "Customer/Queries/GetCustomerUnreadNotificationCountQuery.cs",
                     "Seller/Commands/MarkSellerNotificationReadCommand.cs",
                     "Seller/Commands/MarkAllSellerNotificationsReadCommand.cs",
                     "Seller/Commands/DismissSellerNotificationCommand.cs",
                     "Seller/Queries/ListSellerNotificationsQuery.cs",
                     "Seller/Queries/GetSellerUnreadNotificationCountQuery.cs",
                 })
        {
            Assert.True(File.Exists(Path.Combine(app, file.Replace('/', Path.DirectorySeparatorChar))), "missing: " + file);
        }

        // Ten stale use-case leaf paths must never reappear (R2 drift cannot reappear).
        foreach (var leaf in new[]
                 {
                     "Customer/Commands/MarkCustomerNotificationRead",
                     "Customer/Commands/MarkAllCustomerNotificationsRead",
                     "Customer/Commands/DismissCustomerNotification",
                     "Customer/Queries/ListCustomerNotifications",
                     "Customer/Queries/GetCustomerUnreadNotificationCount",
                     "Seller/Commands/MarkSellerNotificationRead",
                     "Seller/Commands/MarkAllSellerNotificationsRead",
                     "Seller/Commands/DismissSellerNotification",
                     "Seller/Queries/ListSellerNotifications",
                     "Seller/Queries/GetSellerUnreadNotificationCount",
                 })
        {
            Assert.False(Directory.Exists(Path.Combine(app, leaf.Replace('/', Path.DirectorySeparatorChar))), "stale leaf resurrected: " + leaf);
        }
    }

    [Fact]
    public void Canonical_seams_and_boundaries_stay_intact()
    {
        var op = Read("src/backend/Modules/Notification/Tooba.Notification.Application/Composition/NotificationOperation.cs");
        Assert.Contains("catch (ContractOperationException ex) when (NotificationErrorCodes.IsKnown(ex.Code))", op, StringComparison.Ordinal);

        var module = Read("src/backend/Modules/Notification/Tooba.Notification.Infrastructure/DependencyInjection/NotificationModule.cs");
        Assert.Contains("IErrorResourceSet, NotificationErrorResourceSet", module, StringComparison.Ordinal);
        Assert.Contains("AddModuleSchemaMigrator<NotificationDbContext>", module, StringComparison.Ordinal);

        var codes = Read("src/backend/Modules/Notification/Tooba.Notification.Contracts/Errors/NotificationErrorCodes.cs");
        Assert.Contains("IsKnown", codes, StringComparison.Ordinal);

        // Foreign coupling stays zero: no foreign layer using in any Notification production project.
        foreach (var project in new[]
                 {
                     "Tooba.Notification.Contracts", "Tooba.Notification.Domain", "Tooba.Notification.Application",
                     "Tooba.Notification.Infrastructure", "Tooba.Notification.Endpoints",
                 })
        {
            var root = Path.Combine(Repo(), "src", "backend", "Modules", "Notification", project);
            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                Assert.False(System.Text.RegularExpressions.Regex.IsMatch(
                    text, @"using Tooba\.(Order|Payment|Fulfillment|Returns|Wallet|Support|Cart|Catalog|Identity|Media|Localization)\.(Application|Infrastructure|Domain)"),
                    "foreign layer using in " + file);
                Assert.DoesNotContain("using Tooba.Host", text, StringComparison.Ordinal);
            }
        }

        // Host closure preserved: composition + thin seller adapter only.
        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("MapNotificationEndpoints()", program, StringComparison.Ordinal);
        Assert.Contains("typeof(Tooba.Notification.Application.Customer.Commands.MarkCustomerNotificationReadCommand).Assembly", program, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(Repo(), "src", "backend", "Host", "Tooba.Host", "Security", "Seller", "HostNotificationSellerAuthorizer.cs")));
        Assert.False(Directory.Exists(Path.Combine(Repo(), "src", "backend", "Host", "Tooba.Host", "Notifications")));
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(Repo(), relativePath));

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
