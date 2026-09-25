using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Tooba.Fulfillment.Tests.Architecture;

public sealed class FulfillmentArchitectureGuardTests
{
    private static readonly string[] AllowedDomainFolders = ["Aggregates", "Entities", "ValueObjects", "Events", "Policies"];
    private static readonly string[] AllowedApplicationFolders =
        ["Ports", "Models", "Shipping", "Commands", "Queries", "Errors", "Validators"];
    private static readonly string[] AllowedContractsFolders = ["Events", "Returns", "Errors", "History", "Operations", "Shipping"];
    private static readonly string[] AllowedInfrastructureFolders =
        ["Persistence", "Directories", "Adapters", "Events", "Messaging", "DependencyInjection", "Migrations",
            "Gateways", "Bridges", "Handlers", "Shipping", "Observability", "Queries", "Errors"];
    private static readonly string[] AllowedEndpointsFolders = ["Seller", "Admin", "Shipping", "Customer", "Errors", "Resources"];

    private static readonly HashSet<string> HostDbContextAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "Program.cs",
        "ProductWorkspaceDevelopmentBootstrap.cs",
        "ModuleMigrationRegistry.cs",
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

    private static string ModuleRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Fulfillment");

    [Fact]
    public void Infrastructure_references_foreign_Contracts_only()
    {
        var refs = ProjectRefs("Tooba.Fulfillment.Infrastructure");
        Assert.Contains(refs, r => r.Contains("Order.Contracts", StringComparison.Ordinal));
        Assert.Contains(refs, r => r.Contains("Inventory.Contracts", StringComparison.Ordinal));
        Assert.Contains(refs, r => r.Contains("Payment.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(refs, r => r.Contains("Order.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Inventory.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Payment.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Order.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Inventory.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Payment.Domain", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("using Tooba.Order.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("using Tooba.Inventory.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("using Tooba.Payment.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("OrderDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("InventoryDbContext", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("PaymentDbContext", StringComparison.Ordinal));
    }

    [Fact]
    public void Fulfillment_golden_boundaries_and_physical_layout_remain_clean()
    {
        Assert.DoesNotContain(ProjectRefs("Tooba.Fulfillment.Domain"), x => x.Contains("Tooba.Fulfillment.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));
        AssertNoRootDump("Tooba.Fulfillment.Domain", AllowedDomainFolders);
        AssertNoRootDump("Tooba.Fulfillment.Application", AllowedApplicationFolders);
        AssertNoRootDump("Tooba.Fulfillment.Contracts", AllowedContractsFolders);
        AssertNoRootDump("Tooba.Fulfillment.Infrastructure", AllowedInfrastructureFolders);
        AssertNoRootDump("Tooba.Fulfillment.Endpoints", AllowedEndpointsFolders);
        AssertNamespacesAlign("Tooba.Fulfillment.Domain", "Tooba.Fulfillment.Domain");
        AssertNamespacesAlign("Tooba.Fulfillment.Application", "Tooba.Fulfillment.Application");
        AssertNamespacesAlign("Tooba.Fulfillment.Contracts", "Tooba.Fulfillment.Contracts");
        AssertNamespacesAlign("Tooba.Fulfillment.Infrastructure", "Tooba.Fulfillment.Infrastructure");
        AssertNamespacesAlign("Tooba.Fulfillment.Endpoints", "Tooba.Fulfillment.Endpoints");

        var directory = File.ReadAllText(Path.Combine(ModuleRoot(), "Tooba.Fulfillment.Infrastructure", "Directories", "FulfillmentDirectory.cs"));
        Assert.Contains("IClock", directory, StringComparison.Ordinal);
        Assert.Contains("IIdGenerator", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new SystemUtcClock()", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("?? new UuidV7IdGenerator()", directory, StringComparison.Ordinal);

        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var hostHits = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains("FulfillmentDbContext", StringComparison.Ordinal))
            .Where(path => !HostDbContextAllowlist.Contains(Path.GetFileName(path)))
            .ToList();
        Assert.True(hostHits.Count == 0, "Host FulfillmentDbContext allowlist: " + string.Join("; ", hostHits));

        Assert.False(Directory.Exists(Path.Combine(hostRoot, "Fulfillment")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Fulfillment", "FulfillmentEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Fulfillment", "FulfillmentPanelComposer.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Admin", "ShippingServiceEndpoints.cs")));

        var hostGrid = File.ReadAllText(Path.Combine(hostRoot, "Grid", "AdminListGridPolicies.cs"));
        Assert.DoesNotContain("Fulfillments", hostGrid, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminFulfillmentWorkQueueRow", hostGrid, StringComparison.Ordinal);

        var orderOpsEndpoint = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src", "backend", "Modules", "Order", "Tooba.Order.Endpoints", "Admin", "Operations", "AdminOrderOperationsEndpoints.cs"));
        Assert.DoesNotContain("/v1/admin/shipping-methods", orderOpsEndpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("ListEnabledShippingMethodsTreeQuery", orderOpsEndpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("ListShippingMethodsAsync", orderOpsEndpoint, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(hostRoot, "Admin", "AdminOrderOperationsEndpoints.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Admin", "AdminOrderOperationsComposer.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Admin", "AdminOrderOperationsModels.cs")));

        Assert.False(
            File.Exists(Path.Combine(hostRoot, "Admin", "AdminFulfillmentWorkQueueComposer.cs")),
            "AdminFulfillmentWorkQueueComposer must be deleted — bulk ownership is Application-owned.");
        Assert.False(
            File.Exists(Path.Combine(hostRoot, "Admin", "HostAdminOrderFulfillmentOperations.cs")),
            "HostAdminOrderFulfillmentOperations must be deleted — Order owns IAdminOrderFulfillmentOperations.");

        var programCs = File.ReadAllText(Path.Combine(hostRoot, "Program.cs"));
        Assert.Contains("MapFulfillmentEndpoints()", programCs, StringComparison.Ordinal);
        Assert.DoesNotContain("MapShippingServiceEndpoints()", programCs, StringComparison.Ordinal);
        Assert.DoesNotContain("HostShippingServiceLanguageGate", programCs, StringComparison.Ordinal);
        Assert.DoesNotContain("IShippingServiceLanguageGate", programCs, StringComparison.Ordinal);
        Assert.DoesNotContain("FulfillmentPanelComposer", programCs, StringComparison.Ordinal);

        var orderOps = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src", "backend", "Modules", "Order", "Tooba.Order.Infrastructure", "Admin", "Fulfillment", "AdminOrderFulfillmentOperations.cs"));
        Assert.Contains("IAdminOrderFulfillmentOperations", orderOps, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", orderOps, StringComparison.Ordinal);
        Assert.DoesNotContain("AdminOrderOperationsComposer", orderOps, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", orderOps, StringComparison.Ordinal);
        Assert.DoesNotContain("MapFulfillmentException", orderOps, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex(@"[\u0600-\u06FF]"), orderOps);

        var languageGateImpl = Path.Combine(
            ModuleRoot(), "Tooba.Fulfillment.Infrastructure", "Shipping", "ShippingServiceLanguageGate.cs");
        Assert.True(File.Exists(languageGateImpl), "IShippingServiceLanguageGate must live in Fulfillment.Infrastructure.");
        var languageGateText = File.ReadAllText(languageGateImpl);
        Assert.Contains("IShippingServiceLanguageGate", languageGateText, StringComparison.Ordinal);
        Assert.Contains("ILanguageLookup", languageGateText, StringComparison.Ordinal);
        Assert.Contains("namespace Tooba.Fulfillment.Infrastructure.Shipping", languageGateText, StringComparison.Ordinal);
        Assert.DoesNotContain("ServiceProvider", languageGateText, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRequiredService", languageGateText, StringComparison.Ordinal);

        var fulfillmentModule = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.Fulfillment.Infrastructure", "DependencyInjection", "FulfillmentModule.cs"));
        Assert.Contains("IShippingServiceLanguageGate, ShippingServiceLanguageGate", fulfillmentModule, StringComparison.Ordinal);

        var treeQuery = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.Fulfillment.Application", "Queries", "ListEnabledShippingMethodsTree", "ListEnabledShippingMethodsTreeQuery.cs"));
        Assert.Contains("ListEnabledShippingMethodsTreeQuery", treeQuery, StringComparison.Ordinal);
        Assert.Contains("ListEnabledShippingMethodsTreeHandler", treeQuery, StringComparison.Ordinal);

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

        // TB-TMAR-FULFILLMENT-HOST-EVACUATION-001: Host is out; AccessControl/Order are Contracts-only.
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("Tooba.Host", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("Tooba.AccessControl.", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("Tooba.Order.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("aaaaaaaa-aaaa-4aaa-8aaa-000000000009", StringComparison.Ordinal));

        var silentCatch = AllProductionSources()
            .Where(x => Regex.IsMatch(x.Text, @"catch\s*\(\s*Exception\s*\)\s*\{|catch\s*\{\s*\}|catch\s*\([^)]+\)\s*\{\s*\}", RegexOptions.Multiline))
            .Select(x => x.Path)
            .ToList();
        Assert.True(silentCatch.Count == 0, "silent/empty catch: " + string.Join("; ", silentCatch));

        var localized = AllProductionSources()
            .SelectMany(x => Regex.Matches(x.Text, @"throw new \w+Exception\(\s*""([^""]*)""\s*\)")
                .Select(m => (x.Path, Msg: m.Groups[1].Value)))
            .Where(x => Regex.IsMatch(x.Msg, @"[\u0600-\u06FF]") || x.Msg.Contains(' ', StringComparison.Ordinal))
            .Where(x => !x.Msg.StartsWith("fulfillment.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("shipping_service.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("shipping_service_option.", StringComparison.Ordinal)
                        && !x.Msg.StartsWith("domain.", StringComparison.Ordinal))
            .Select(x => $"{x.Path}:{x.Msg}")
            .ToList();
        Assert.True(localized.Count == 0, "localized exception prose: " + string.Join("; ", localized));
    }

    [Fact]
    public void Fulfillment_endpoints_cqrs_and_host_ownership_are_enforced()
    {
        Assert.True(Directory.Exists(Path.Combine(ModuleRoot(), "Tooba.Fulfillment.Endpoints")));
        Assert.True(File.Exists(Path.Combine(ModuleRoot(), "Tooba.Fulfillment.Endpoints", "Tooba.Fulfillment.Endpoints.csproj")));

        var seller = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.Fulfillment.Endpoints", "Seller", "FulfillmentSellerEndpoints.cs"));
        var admin = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.Fulfillment.Endpoints", "Admin", "FulfillmentAdminEndpoints.cs"));
        var shipping = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.Fulfillment.Endpoints", "Shipping", "ShippingServiceEndpoints.cs"));
        var methods = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.Fulfillment.Endpoints", "Shipping", "ShippingMethodsEndpoints.cs"));
        var customer = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.Fulfillment.Endpoints", "Customer", "FulfillmentCustomerEndpoints.cs"));
        var module = File.ReadAllText(Path.Combine(
            ModuleRoot(), "Tooba.Fulfillment.Endpoints", "FulfillmentEndpointModule.cs"));

        foreach (var endpoint in new[] { seller, admin, shipping, methods, customer })
        {
            Assert.Contains("ISender sender", endpoint, StringComparison.Ordinal);
            Assert.Contains("ApiResponseFactory", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("exception.Message", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("new { title", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("FulfillmentDbContext", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("AdminListGridPolicies", endpoint, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (InvalidOperationException", endpoint, StringComparison.Ordinal);
        }

        Assert.Contains("MapGroup(\"/v1/seller\")", module, StringComparison.Ordinal);
        Assert.Contains("MapGroup(\"/v1/admin\")", module, StringComparison.Ordinal);
        Assert.Contains("MapGroup(\"/v1/customer\")", module, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/fulfillments\"", seller, StringComparison.Ordinal);
        Assert.Contains("SellerMutateFulfillmentCommand", seller, StringComparison.Ordinal);
        Assert.Contains("ExecuteAdminFulfillmentBulkCommand", admin, StringComparison.Ordinal);
        Assert.Contains("QueryAdminFulfillmentWorkQueueQuery", admin, StringComparison.Ordinal);
        Assert.Contains("ListShippingServicesQuery", shipping, StringComparison.Ordinal);
        Assert.Contains("CreateShippingServiceCommand", shipping, StringComparison.Ordinal);
        Assert.Contains("/v1/admin/shipping-methods", methods, StringComparison.Ordinal);
        Assert.Contains("ListEnabledShippingMethodsTreeQuery", methods, StringComparison.Ordinal);
        Assert.Contains("ListCustomerCheckoutFulfillmentsQuery", customer, StringComparison.Ordinal);

        var endpointRefs = ProjectRefs("Tooba.Fulfillment.Endpoints");
        Assert.Contains(endpointRefs, r => r.Contains("Fulfillment.Application", StringComparison.Ordinal));
        Assert.Contains(endpointRefs, r => r.Contains("Order.Contracts", StringComparison.Ordinal));
        Assert.Contains(endpointRefs, r => r.Contains("Cart.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(endpointRefs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointRefs, r => r.Contains("Host", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointRefs, r => r.Contains("Order.Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointRefs, r => r.Contains("AccessControl", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(endpointRefs, r => r.Contains("DbContext", StringComparison.OrdinalIgnoreCase));

        var application = Sources("Tooba.Fulfillment.Application").ToList();
        Assert.Contains(application, x => x.Text.Contains("ExecuteAdminFulfillmentBulkCommand", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("SellerMutateFulfillmentCommand", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("CreateShippingServiceCommand", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("ListEnabledShippingMethodsTreeQuery", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("QueryAdminFulfillmentWorkQueueQuery", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("AdminFulfillmentGridQueryPolicy", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("FulfillmentExceptionMapper", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("IRequestHandler<", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("using MediatR", StringComparison.Ordinal));
        Assert.DoesNotContain(application, x => x.Path.EndsWith("FulfillmentQueries.cs", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(application, x => x.Path.EndsWith("ExecuteAdminFulfillmentBulkCommand.cs", StringComparison.OrdinalIgnoreCase)
            && x.Path.Contains("/Commands/ExecuteAdminFulfillmentBulkCommand.cs", StringComparison.Ordinal));
        Assert.DoesNotContain(application, x => x.Path.Contains("/Commands/SellerMutateFulfillmentCommand.cs", StringComparison.Ordinal));
        Assert.DoesNotContain(application, x => x.Path.Contains("/Shipping/ShippingServiceWriteHandlers.cs", StringComparison.Ordinal));
        Assert.DoesNotContain(application, x => x.Path.Contains("/Shipping/ShippingServiceReadHandlers.cs", StringComparison.Ordinal));
        Assert.DoesNotContain(application, x => x.Path.Contains("/Shipping/ListEnabledShippingMethodsTreeQuery.cs", StringComparison.Ordinal));
        Assert.DoesNotContain(application, x =>
            x.Text.Contains("StartsWith(\"fulfillment.\"", StringComparison.Ordinal)
            || x.Text.Contains("StartsWith(\"shipping_service.\"", StringComparison.Ordinal)
            || x.Text.Contains("IsShippingSemantic", StringComparison.Ordinal)
            || x.Text.Contains(".Contains(\"", StringComparison.Ordinal) && x.Path.Contains("ExceptionMapper", StringComparison.Ordinal));

        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        Assert.False(File.Exists(Path.Combine(hostRoot, "Seller", "HostFulfillmentSellerAuthorizer.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Admin", "HostFulfillmentAdminAuthorizer.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "Customer", "HostFulfillmentCustomerAuthorizer.cs")));

        var hostFulfillment = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                           && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => Path.GetFileName(path).Contains("Fulfillment", StringComparison.Ordinal))
            .ToList();
        Assert.True(hostFulfillment.Count == 0, "Host Fulfillment-specific files: " + string.Join("; ", hostFulfillment));

        var endpointsRoot = Path.Combine(ModuleRoot(), "Tooba.Fulfillment.Endpoints");
        Assert.True(File.Exists(Path.Combine(endpointsRoot, "Admin", "FulfillmentAdminAuthorizer.cs")));
        Assert.True(File.Exists(Path.Combine(endpointsRoot, "Customer", "FulfillmentCustomerAuthorizer.cs")));
        Assert.True(File.Exists(Path.Combine(endpointsRoot, "Seller", "FulfillmentSellerAuthorizer.cs")));
        Assert.Contains("AddFulfillmentEndpointPresentation", module, StringComparison.Ordinal);
    }

    private static void AssertNoRootDump(string project, string[] allowedFolders)
    {
        var root = Path.Combine(ModuleRoot(), project);
        var rootCs = Directory.EnumerateFiles(root, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(name => name is not null
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

    // TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-STRUCTURE-001: exact path-derived namespace equality.
    // The previous loose StartsWith(prefix) acceptance is replaced so a capability file cannot be
    // flattened into root or re-declared under a wrong namespace silently.
    private static void AssertNamespacesAlign(string projectFolder, string nsPrefix)
    {
        var violations = new List<string>();
        foreach (var (path, text) in Sources(projectFolder))
        {
            var ns = Regex.Match(text, @"^namespace\s+([\w.]+)", RegexOptions.Multiline).Groups[1].Value;
            if (string.IsNullOrEmpty(ns))
            {
                violations.Add($"{path}: ns=<none>");
                continue;
            }

            var rel = path.Replace('\\', '/');
            var marker = projectFolder.Replace('\\', '/') + "/";
            var idx = rel.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0) continue;
            var under = rel[(idx + marker.Length)..];
            var dir = under.Contains('/') ? under[..under.LastIndexOf('/')] : string.Empty;
            var expected = string.IsNullOrEmpty(dir)
                ? nsPrefix
                : nsPrefix + "." + dir.Replace("/", ".");
            if (!string.Equals(ns, expected, StringComparison.Ordinal))
                violations.Add($"{path}: ns={ns} expected={expected}");
        }

        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }

    [Fact]
    public void Fulfillment_root_allowlists_and_forbidden_flattened_files_are_enforced()
    {
        Assert.Equal([], RootCs("Tooba.Fulfillment.Application"));
        Assert.Equal([], RootCs("Tooba.Fulfillment.Domain"));
        Assert.Equal([], RootCs("Tooba.Fulfillment.Contracts"));
        Assert.Equal([], RootCs("Tooba.Fulfillment.Infrastructure"));
        Assert.Equal(["FulfillmentEndpointModule.cs"], RootCs("Tooba.Fulfillment.Endpoints"));

        foreach (var flattened in new[]
        {
            "FulfillmentContracts.cs", "FulfillmentHandlers.cs", "FulfillmentRequests.cs",
            "FulfillmentQueries.cs", "FulfillmentQueryHandlers.cs", "FulfillmentCommands.cs",
            "FulfillmentErrorCodes.cs", "FulfillmentModels.cs",
        })
        {
            Assert.False(File.Exists(Path.Combine(ModuleRoot(), "Tooba.Fulfillment.Application", flattened)),
                $"flattened Application root file {flattened}");
        }

        foreach (var flattened in new[]
        {
            "FulfillmentSellerEndpoints.cs", "FulfillmentAdminEndpoints.cs", "FulfillmentCustomerEndpoints.cs",
            "ShippingServiceEndpoints.cs", "ShippingMethodsEndpoints.cs",
            "IFulfillmentSellerAuthorizer.cs", "IFulfillmentAdminAuthorizer.cs", "IFulfillmentCustomerAuthorizer.cs",
        })
        {
            Assert.False(File.Exists(Path.Combine(ModuleRoot(), "Tooba.Fulfillment.Endpoints", flattened)),
                $"flattened Endpoints root file {flattened}");
        }

        foreach (var flattened in new[]
        {
            "FulfillmentModule.cs", "FulfillmentDbContext.cs", "FulfillmentDirectory.cs",
            "FulfillmentOutboxRegistration.cs", "AdminFulfillmentWorkQueueQueryEngine.cs",
            "FulfillmentErrorCatalogContributor.cs",
        })
        {
            Assert.False(File.Exists(Path.Combine(ModuleRoot(), "Tooba.Fulfillment.Infrastructure", flattened)),
                $"flattened Infrastructure root file {flattened}");
        }
    }

    [Fact]
    public void Fulfillment_rejects_namespace_alias_workarounds_and_type_forwarding()
    {
        Assert.DoesNotContain(AllProductionSources(), x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));

        // Self-module short aliases (e.g. `using AppModels = Tooba.Fulfillment.Application.Models;`) are
        // legitimate import ergonomics and are not a namespace workaround. Only foreign-module
        // Application/Infrastructure/Domain aliases are rejected.
        var foreignAliases = AllProductionSources()
            .SelectMany(x => Regex.Matches(
                    x.Text, @"^\s*using\s+[A-Za-z0-9_]+\s*=\s*Tooba\.(?!Fulfillment\.)[A-Za-z0-9_.]+(Application|Infrastructure|Domain)",
                    RegexOptions.Multiline)
                .Select(m => $"{x.Path}: {m.Value.Trim()}"))
            .ToList();
        Assert.True(foreignAliases.Count == 0, "foreign-module alias workaround: " + string.Join("; ", foreignAliases));

        foreach (var project in new[]
        {
            "Tooba.Fulfillment.Application", "Tooba.Fulfillment.Contracts",
            "Tooba.Fulfillment.Domain", "Tooba.Fulfillment.Endpoints", "Tooba.Fulfillment.Infrastructure",
        })
        {
            var root = Path.Combine(ModuleRoot(), project);
            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.TopDirectoryOnly))
            {
                Assert.DoesNotContain("GlobalUsings", Path.GetFileName(file), StringComparison.Ordinal);
            }
        }
    }

    private static string[] RootCs(string project) =>
        Directory.EnumerateFiles(Path.Combine(ModuleRoot(), project), "*.cs", SearchOption.TopDirectoryOnly)
            .Select(path => Path.GetFileName(path)!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();


    private static IEnumerable<(string Path, string Text)> AllProductionSources() =>
        Sources("Tooba.Fulfillment.Domain")
            .Concat(Sources("Tooba.Fulfillment.Application"))
            .Concat(Sources("Tooba.Fulfillment.Contracts"))
            .Concat(Sources("Tooba.Fulfillment.Infrastructure"))
            .Concat(Sources("Tooba.Fulfillment.Endpoints"));

    private static IEnumerable<(string Path, string Text)> Sources(string projectFolder)
    {
        var root = Path.Combine(ModuleRoot(), projectFolder);
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
        var csproj = Path.Combine(ModuleRoot(), projectFolder, projectFolder + ".csproj");
        var doc = XDocument.Load(csproj);
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
