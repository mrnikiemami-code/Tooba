using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Host.Order;
using Tooba.Order.Contracts.Fulfillment;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-ORDER-AMC-001 / R1 — KEEP_AS_THIN_HOST_ORDER_STOREFRONT_ADAPTER, Contracts-only.
/// </summary>
public sealed class HostOrderAmcGuardTests
{
    private static readonly string[] Allowlist =
    [
        "HostOrderStorefrontActor.cs",
    ];

    private static readonly Regex ForbiddenForeignLayers = new(
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Payment|Reviews|Cart|Wallet|Support|ProductQnA|Preferences|Story|Wishlist|Fulfillment|Settlement|Notification|Returns|Promotion|Inventory|Pricing|Tax|Media|Content|User|OperatorProfile)\.(Application|Domain|Infrastructure|Persistence)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Order_folder_matches_exact_retained_allowlist()
    {
        var folder = Path.Combine(FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Order");
        Assert.True(Directory.Exists(folder), "Host/Order must remain PRESENT for KEEP disposition");

        var files = Directory.GetFiles(folder, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(Allowlist.OrderBy(x => x, StringComparer.Ordinal).ToArray(), files);
    }

    [Fact]
    public void Path_namespace_exact_and_contracts_only_thin_adapter_invariants()
    {
        var text = Read("HostOrderStorefrontActor.cs");
        var nsMatch = Regex.Match(text, @"^namespace\s+([^\s;{]+)", RegexOptions.Multiline);
        Assert.True(nsMatch.Success);
        Assert.Equal("Tooba.Host.Order", nsMatch.Groups[1].Value);

        Assert.Contains("class HostOrderStorefrontActor", text, StringComparison.Ordinal);
        Assert.Contains("class HostOrderStorefrontCheckoutIdentityGate", text, StringComparison.Ordinal);
        Assert.Contains("Tooba.Order.Contracts.Storefront", text, StringComparison.Ordinal);
        Assert.Contains("IOrderStorefrontActor", text, StringComparison.Ordinal);
        Assert.Contains("IOrderStorefrontCheckoutIdentityGate", text, StringComparison.Ordinal);
        Assert.Contains("StorefrontGuestActor.ActorId", text, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.CheckoutAuthenticationRequired", text, StringComparison.Ordinal);
        Assert.Contains("SemanticException", text, StringComparison.Ordinal);
        Assert.Contains("CheckoutIdentityGate", text, StringComparison.Ordinal);
        Assert.Contains("CartAccess", text, StringComparison.Ordinal);
        Assert.Contains("Tooba.Cart.Contracts", text, StringComparison.Ordinal);

        Assert.DoesNotContain("Tooba.Order.Application", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Order.Domain", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Order.Infrastructure", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Order.Persistence", text, StringComparison.Ordinal);
        Assert.DoesNotContain("StorefrontOrderException", text, StringComparison.Ordinal);
        Assert.DoesNotContain("StorefrontCheckoutService", text, StringComparison.Ordinal);
        Assert.DoesNotContain("StorefrontOrderErrors", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
        Assert.DoesNotContain("DbSet<", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
        Assert.DoesNotContain("message.Contains", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GetRequiredService", text, StringComparison.Ordinal);

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            Assert.False(ForbiddenForeignLayers.IsMatch(line), line);
        }
    }

    [Fact]
    public void Contracts_host_facing_seams_exist_with_exact_namespace()
    {
        var root = FindRepoRoot();
        var contracts = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Order", "Tooba.Order.Contracts", "Storefront",
            "OrderStorefrontActorContracts.cs"));
        Assert.Contains("namespace Tooba.Order.Contracts.Storefront", contracts, StringComparison.Ordinal);
        Assert.Contains("interface IOrderStorefrontActor", contracts, StringComparison.Ordinal);
        Assert.Contains("interface IOrderStorefrontCheckoutIdentityGate", contracts, StringComparison.Ordinal);

        var appPorts = File.ReadAllText(Path.Combine(
            root, "src", "backend", "Modules", "Order", "Tooba.Order.Application", "Storefront", "Ports",
            "StorefrontOrderPorts.cs"));
        Assert.DoesNotContain("interface IOrderStorefrontActor", appPorts, StringComparison.Ordinal);
        Assert.DoesNotContain("interface IOrderStorefrontCheckoutIdentityGate", appPorts, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_registers_order_storefront_adapters_from_contracts()
    {
        var program = File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.Contains("Tooba.Host.Order.HostOrderStorefrontActor", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Host.Order.HostOrderStorefrontCheckoutIdentityGate", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Order.Contracts.Storefront.IOrderStorefrontActor", program, StringComparison.Ordinal);
        Assert.Contains(
            "Tooba.Order.Contracts.Storefront.IOrderStorefrontCheckoutIdentityGate",
            program,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Tooba.Order.Application.Storefront.Ports.IOrderStorefrontActor",
            program,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Saved_address_unauthenticated_production_rejects_with_foundation_auth_code()
    {
        var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var env = new FakeHostEnvironment("Production");
        var actor = new HostOrderStorefrontActor(new CurrentAuthenticatedSession(), env, http);

        var ex = Assert.Throws<SemanticException>(() => actor.ResolvePlacementActor(usingSavedAddress: true));
        Assert.Equal(FoundationErrorCodes.CheckoutAuthenticationRequired, ex.Error.Code);
        Assert.Equal(StorefrontGuestActor.ActorId, actor.GuestActorId);
        Assert.Null(actor.TryResolveListActor());
    }

    private sealed class FakeHostEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Tooba.Host.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    [Fact]
    public void Sot_records_order_r1_keep_disposition()
    {
        var sot = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", "architecture", "tmar-current-state.json"));
        Assert.Contains("\"hostOrderAmcR1\"", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-ORDER-AMC-001-R1", sot, StringComparison.Ordinal);
        Assert.Contains("KEEP_AS_THIN_HOST_ORDER_STOREFRONT_ADAPTER", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_ORDER_AMC_001_R1_KEEP_THIN_HOST_ADAPTER", sot, StringComparison.Ordinal);
    }

    private static string Read(string fileName) =>
        File.ReadAllText(Path.Combine(
            FindRepoRoot(), "src", "backend", "Host", "Tooba.Host", "Order", fileName));

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
