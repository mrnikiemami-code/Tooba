using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-NOTIFICATION-AMSC-001-W3-R2 — Structure repair durable guard.
/// Locks the repaired capability-first shallow Application tree: ZERO child directories under the
/// four Customer/Seller request axes, all ten request sources + six validators directly colocated,
/// exact path↔namespace, no stale/duplicate leaf paths, and the shared capability folders intact.
/// Requires the tooba-architecture-structure skill; never weakens the W1/W3 guards.
/// </summary>
public sealed class NotificationModuleAmsc001W3R2StructureRepairGuardTests
{
    private static string App() => Path.Combine(Repo(), "src/backend/Modules/Notification/Tooba.Notification.Application");

    private static (string Axis, string[] Files)[] Axes() =>
    [
        ("Customer/Commands",
        [
            "MarkCustomerNotificationReadCommand.cs",
            "MarkCustomerNotificationReadCommandValidator.cs",
            "MarkAllCustomerNotificationsReadCommand.cs",
            "DismissCustomerNotificationCommand.cs",
            "DismissCustomerNotificationCommandValidator.cs",
        ]),
        ("Customer/Queries",
        [
            "ListCustomerNotificationsQuery.cs",
            "ListCustomerNotificationsQueryValidator.cs",
            "GetCustomerUnreadNotificationCountQuery.cs",
        ]),
        ("Seller/Commands",
        [
            "MarkSellerNotificationReadCommand.cs",
            "MarkSellerNotificationReadCommandValidator.cs",
            "MarkAllSellerNotificationsReadCommand.cs",
            "DismissSellerNotificationCommand.cs",
            "DismissSellerNotificationCommandValidator.cs",
        ]),
        ("Seller/Queries",
        [
            "ListSellerNotificationsQuery.cs",
            "ListSellerNotificationsQueryValidator.cs",
            "GetSellerUnreadNotificationCountQuery.cs",
        ]),
    ];

    private static readonly string[] StaleLeafDirectories =
    [
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
    ];

    [Fact]
    public void Request_axes_carry_zero_child_directories()
    {
        foreach (var (axis, _) in Axes())
        {
            var path = Path.Combine(App(), axis.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(Directory.Exists(path), $"missing request axis: {axis}");
            var children = Directory.EnumerateDirectories(path).ToArray();
            Assert.True(children.Length == 0, $"{axis} must have ZERO child directories, found: {string.Join("; ", children)}");
        }
    }

    [Fact]
    public void All_ten_requests_and_six_validators_are_colocated_on_their_axis()
    {
        foreach (var (axis, files) in Axes())
        {
            foreach (var file in files)
            {
                var path = Path.Combine(App(), axis.Replace('/', Path.DirectorySeparatorChar), file);
                Assert.True(File.Exists(path), $"missing colocated source: {axis}/{file}");
            }
        }
    }

    [Fact]
    public void Stale_use_case_leaf_directories_are_absent()
    {
        foreach (var leaf in StaleLeafDirectories)
        {
            Assert.False(Directory.Exists(Path.Combine(App(), leaf.Replace('/', Path.DirectorySeparatorChar))),
                "stale use-case leaf directory must not exist: " + leaf);
        }
    }

    [Fact]
    public void Request_axis_namespaces_are_exactly_the_axis_namespaces()
    {
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Customer/Commands"] = "Tooba.Notification.Application.Customer.Commands",
            ["Customer/Queries"] = "Tooba.Notification.Application.Customer.Queries",
            ["Seller/Commands"] = "Tooba.Notification.Application.Seller.Commands",
            ["Seller/Queries"] = "Tooba.Notification.Application.Seller.Queries",
        };

        var violations = new List<string>();
        foreach (var (axis, files) in Axes())
        {
            foreach (var file in files)
            {
                var text = File.ReadAllText(Path.Combine(App(), axis.Replace('/', Path.DirectorySeparatorChar), file));
                var match = System.Text.RegularExpressions.Regex.Match(
                    text, @"^namespace\s+([A-Za-z0-9_.]+)", System.Text.RegularExpressions.RegexOptions.Multiline);
                if (!match.Success || !string.Equals(expected[axis], match.Groups[1].Value, StringComparison.Ordinal))
                {
                    violations.Add($"{axis}/{file}: ns={(match.Success ? match.Groups[1].Value : "<none>")}");
                }
            }
        }

        Assert.True(violations.Count == 0, "namespace mismatches: " + string.Join("; ", violations));
    }

    [Fact]
    public void Capability_branches_hold_only_commands_queries_and_shared_folders()
    {
        var app = App();
        foreach (var capability in new[] { "Customer", "Seller" })
        {
            var dirs = Directory.EnumerateDirectories(Path.Combine(app, capability))
                .Select(d => Path.GetFileName(d)!)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
            Assert.Equal(new[] { "Commands", "Queries" }, dirs);
        }

        // Shared cross-capability folders stay exactly as the W2 capability map.
        var rootDirs = Directory.EnumerateDirectories(app)
            .Select(d => Path.GetFileName(d)!)
            .Where(n => n is not "bin" and not "obj" and not "artifacts" && !n.StartsWith('.'))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "Composition", "Customer", "Models", "Ports", "Rendering", "Seller", "Validators" }, rootDirs);
    }

    [Fact]
    public void Host_scan_anchor_is_repointed_to_the_flattened_namespace()
    {
        var program = File.ReadAllText(Path.Combine(Repo(), "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("typeof(Tooba.Notification.Application.Customer.Commands.MarkCustomerNotificationReadCommand).Assembly", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MarkCustomerNotificationRead.MarkCustomerNotificationReadCommand", program, StringComparison.Ordinal);
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
