using System.Xml.Linq;
using Xunit;

namespace Tooba.Order.Tests.Architecture;

/// <summary>
/// نگهبان معماری برش کامل‌بودن سفارش Admin: مالکیت Order، قرارداد-only بیگانه، و بدون بازگشت به Host.
/// </summary>
public sealed class OrderCompletenessArchitectureGuardTests
{
    private const string CompletenessSlice = "/Admin/Completeness/";

    [Fact]
    public void Domain_csproj_has_no_application_infrastructure_or_endpoints_reference()
    {
        var refs = ProjectRefs("Tooba.Order.Domain");
        Assert.DoesNotContain(refs, x => x.Contains("Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, x => x.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, x => x.Contains("Endpoints", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Application_and_endpoints_do_not_reference_host_or_infrastructure()
    {
        foreach (var project in new[] { "Tooba.Order.Application", "Tooba.Order.Endpoints" })
        {
            var refs = ProjectRefs(project);
            Assert.DoesNotContain(refs, x => x.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(refs, x => x.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Application_foreign_project_references_are_contracts_only()
    {
        var foreignModules = new[]
        {
            "Catalog.", "Party.", "Payment.", "Fulfillment.", "Returns.",
            "Settlement.", "Identity.", "OperatorProfile.", "Inventory.", "Cart.", "Offer.", "Pricing.", "Tax.",
        };
        var forbidden = ProjectRefs("Tooba.Order.Application")
            .Where(reference => foreignModules.Any(m => reference.Contains(m, StringComparison.OrdinalIgnoreCase)))
            .Where(reference => !reference.Contains(".Contracts", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.True(forbidden.Length == 0, "Order.Application foreign non-contract refs: " + string.Join("; ", forbidden));
    }

    [Fact]
    public void Completeness_slice_reads_foreign_data_through_contracts_only()
    {
        var violations = CompletenessSources()
            .Where(x =>
                x.Text.Contains("Tooba.Fulfillment.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Returns.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Settlement.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Identity.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.OperatorProfile.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Catalog.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Party.Application", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.AccessControl.Application", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToList();
        Assert.True(violations.Count == 0, "completeness slice foreign application leaks: " + string.Join("; ", violations));
    }

    [Fact]
    public void Completeness_slice_never_touches_foreign_dbcontext()
    {
        var violations = CompletenessSources()
            .Where(x =>
                x.Text.Contains("CatalogDbContext", StringComparison.Ordinal)
                || x.Text.Contains("FulfillmentDbContext", StringComparison.Ordinal)
                || x.Text.Contains("ReturnsDbContext", StringComparison.Ordinal)
                || x.Text.Contains("SettlementDbContext", StringComparison.Ordinal)
                || x.Text.Contains("IdentityDbContext", StringComparison.Ordinal)
                || x.Text.Contains("PaymentDbContext", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToList();
        Assert.True(violations.Count == 0, "completeness slice foreign DbContext leaks: " + string.Join("; ", violations));
    }

    [Fact]
    public void Application_slice_has_no_dbcontext_and_no_platform_http_exception()
    {
        var violations = Sources("Tooba.Order.Application")
            .Concat(Sources("Tooba.Order.Endpoints"))
            .Where(x =>
                x.Text.Contains("OrderDbContext", StringComparison.Ordinal)
                || x.Text.Contains("PlatformHttpException", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToList();
        Assert.True(violations.Count == 0, "DbContext/PlatformHttpException in Order Application/Endpoints: "
            + string.Join("; ", violations));
    }

    [Fact]
    public void Endpoints_delegate_every_completeness_route_to_isender()
    {
        var endpoints = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Endpoints", "AdminOrderCompletenessEndpoints.cs"));
        Assert.Contains("ISender sender", endpoints, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory api", endpoints, StringComparison.Ordinal);
        Assert.Equal(6, CountOccurrences(endpoints, "sender.Send("));
        Assert.Equal(6, CountOccurrences(endpoints, "auth.RequirePermissionAsync"));
        Assert.DoesNotContain("IAdminOrderCompletenessStore", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (", endpoints, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", endpoints, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_no_longer_owns_the_completeness_composer_or_invoice_semantics()
    {
        var hostAdmin = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Admin");
        Assert.False(File.Exists(Path.Combine(hostAdmin, "AdminOrderCompletenessComposer.cs")));
        Assert.False(File.Exists(Path.Combine(hostAdmin, "InvoiceHeaderSemantics.cs")));
        Assert.False(File.Exists(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host.Tests", "AdminOrderCompletenessLegacyTestHelpers.cs")));
        Assert.True(File.Exists(Path.Combine(OrderRoot(), "Tooba.Order.Domain", "InvoiceHeaderSemantics.cs")));
    }

    [Fact]
    public void Completeness_use_cases_live_in_folder_aligned_namespaces()
    {
        foreach (var source in CompletenessSources())
        {
            var normalized = source.Path.Replace('\\', '/');
            var folderIndex = normalized.IndexOf(CompletenessSlice, StringComparison.Ordinal);
            if (folderIndex < 0)
            {
                continue;
            }

            var relative = normalized[(folderIndex + CompletenessSlice.Length)..];
            var segments = relative.Split('/');
            if (segments.Length < 2)
            {
                continue;
            }

            var expected = "namespace Tooba.Order.Application.Admin.Completeness."
                + string.Join('.', segments[..^1]);
            Assert.Contains(expected, source.Text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Owning_modules_register_the_new_history_and_actor_contract_adapters()
    {
        var expectations = new (string Module, string Path, string Needle)[]
        {
            ("Fulfillment", "Tooba.Fulfillment.Infrastructure/DependencyInjection/FulfillmentModule.cs", "IFulfillmentHistoryReader"),
            ("Returns", "Tooba.Returns.Infrastructure/DependencyInjection/ReturnsModule.cs", "IReturnHistoryReader"),
            ("Settlement", "Tooba.Settlement.Infrastructure/DependencyInjection/SettlementModule.cs", "ISettlementHistoryReader"),
            ("Identity", "Tooba.Identity.Infrastructure/IdentityModule.cs", "IActorContactLookup"),
            ("OperatorProfile", "Tooba.OperatorProfile.Infrastructure/OperatorProfileModule.cs", "IActorDisplayLookup"),
        };
        foreach (var (module, path, needle) in expectations)
        {
            var file = Path.Combine(RepoRoot(), "src", "backend", "Modules", module, path.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(file), $"missing module registration file: {file}");
            Assert.Contains(needle, File.ReadAllText(file), StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Pre-R2C monoliths (<c>OrderDomain.cs</c>, <c>CheckoutDirectory.cs</c>) are outside this slice and
    /// stay on the residual list; the completeness slice itself must not add new oversized files.
    /// </summary>
    [Fact]
    public void No_completeness_slice_source_exceeds_800_loc()
    {
        var violations = CompletenessSources()
            .Where(x => File.ReadAllLines(Path.Combine(RepoRoot(), x.Path)).Length > 800)
            .Select(x => x.Path)
            .ToList();
        Assert.True(violations.Count == 0, "completeness slice >800 LOC: " + string.Join("; ", violations));
    }

    [Fact]
    public void Order_has_no_type_forwarding_artifacts()
    {
        Assert.DoesNotContain(AllOrderSources(), x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));
        Assert.Empty(Directory.EnumerateFiles(OrderRoot(), "TypeForwarders.cs", SearchOption.AllDirectories));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private static string OrderRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Order");

    private static IReadOnlyList<(string Path, string Text)> CompletenessSources() =>
        AllOrderSources()
            .Where(x => x.Path.Replace('\\', '/').Contains(CompletenessSlice, StringComparison.Ordinal)
                || x.Path.Replace('\\', '/').EndsWith("Admin/AdminOrderCompletenessStore.cs", StringComparison.Ordinal))
            .ToList();

    private static IReadOnlyList<(string Path, string Text)> Sources(string project) =>
        Collect(Path.Combine(OrderRoot(), project));

    private static IReadOnlyList<(string Path, string Text)> AllOrderSources() =>
        Collect(OrderRoot())
            .Where(x => !x.Path.Replace('\\', '/').Contains("/Tooba.Order.Tests/", StringComparison.Ordinal))
            .ToList();

    private static IReadOnlyList<(string Path, string Text)> Collect(string root) =>
        Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Select(x => x.Replace('\\', '/'))
            .Where(x => !x.Contains("/obj/", StringComparison.Ordinal) && !x.Contains("/bin/", StringComparison.Ordinal))
            .Where(x => !x.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase))
            .Select(x => (Path.GetRelativePath(RepoRoot(), x), File.ReadAllText(x)))
            .ToList();

    private static int CountOccurrences(string text, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    private static IReadOnlyList<string> ProjectRefs(string projectFolder)
    {
        var csproj = Path.Combine(OrderRoot(), projectFolder, projectFolder + ".csproj");
        return XDocument.Load(csproj).Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
