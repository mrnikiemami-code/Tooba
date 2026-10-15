using Xunit;

namespace Tooba.Shipping.Tests.Packaging;

/// <summary>
/// TB-SHIPPING-FULL-OPERATIONS-075 — ownership of the packaging admin surface: routes stay under the
/// admin convention, every route is authorized through the module authorizer seam with an existing
/// platform permission, and the route service code stays authoritative over any body value.
/// </summary>
public sealed class ShippingPackagingEndpointOwnershipTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private static string EndpointsSource() => File.ReadAllText(Path.Combine(
        RepoRoot(),
        "src",
        "backend",
        "Modules",
        "Shipping",
        "Tooba.Shipping.Endpoints",
        "Admin",
        "ShippingPackagingPolicyEndpoints.cs"));

    [Fact]
    public void Every_packaging_route_is_mapped_and_authorized_with_existing_permissions()
    {
        var source = EndpointsSource();

        Assert.Contains("MapGet(\"/shipping-services/{serviceCode}/packaging-policies\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/shipping-services/{serviceCode}/packaging-policies\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(", source, StringComparison.Ordinal);
        Assert.Contains("packaging-policies/{packagingPolicyId:guid}\"", source, StringComparison.Ordinal);
        Assert.Contains("packaging-policies/{packagingPolicyId:guid}/active\"", source, StringComparison.Ordinal);

        // Four routes, four authorization calls: no mapped route may be reachable anonymously.
        var mapCalls = CountOccurrences(source, "group.Map");
        var authorizerCalls = CountOccurrences(source, "authorizer.RequireAuthorizedAsync(");
        Assert.Equal(4, mapCalls);
        Assert.Equal(4, authorizerCalls);

        // Permissions are the existing catalog ids already declared by the module; no invented capability.
        Assert.Contains("ShippingAdminPermissions.View", source, StringComparison.Ordinal);
        Assert.Contains("ShippingAdminPermissions.Manage", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowAnonymous", source, StringComparison.Ordinal);

        // The route parameter — not the body — is the service identity handed to the application layer.
        Assert.Contains("new CreateShippingPackagingPolicyCommand(serviceCode, body)", source, StringComparison.Ordinal);
        Assert.Contains("new UpdateShippingPackagingPolicyCommand(packagingPolicyId, serviceCode, body)", source, StringComparison.Ordinal);
        Assert.Contains("new SetShippingPackagingPolicyActiveCommand(packagingPolicyId, serviceCode, body.IsActive)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void The_packaging_routes_are_mapped_by_the_module_under_the_admin_group()
    {
        var module = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "backend",
            "Modules",
            "Shipping",
            "Tooba.Shipping.Endpoints",
            "Admin",
            "ShippingRateCoverageEndpoints.cs"));

        Assert.Contains("var admin = app.MapGroup(\"/v1/admin\");", module, StringComparison.Ordinal);
        Assert.Contains("ShippingPackagingPolicyEndpoints.Map(admin);", module, StringComparison.Ordinal);
    }

    [Fact]
    public void The_module_registers_the_packaging_seams_and_still_no_schema_migrator()
    {
        var module = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "backend",
            "Modules",
            "Shipping",
            "Tooba.Shipping.Infrastructure",
            "DependencyInjection",
            "ShippingModule.cs"));

        Assert.Contains(
            "AddScoped<IShippingPackagingPolicyDirectory, ShippingPackagingPolicyDirectory>",
            module,
            StringComparison.Ordinal);
        Assert.Contains(
            "AddScoped<IShippingPackagingFeeLookup, ShippingPackagingFeeLookup>",
            module,
            StringComparison.Ordinal);
        Assert.Contains(
            "AddScoped<IShippingShipmentCostLookup, ShippingShipmentCostLookup>",
            module,
            StringComparison.Ordinal);
        Assert.DoesNotContain("AddModuleSchemaMigrator", module, StringComparison.Ordinal);
        Assert.DoesNotContain("Database.MigrateAsync", module, StringComparison.Ordinal);
    }

    [Fact]
    public void The_host_wires_the_packaging_command_assembly_without_touching_order_or_payment()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));

        // The packaging commands live in the Shipping.Application assembly already registered at 055, so
        // the Host needs no new registration and no Order/Checkout/Payment edit.
        Assert.Contains(
            "typeof(Tooba.Shipping.Application.Rates.Commands.CreateShippingRatePolicyCommand).Assembly",
            program,
            StringComparison.Ordinal);
        Assert.Contains("app.MapShippingEndpoints();", program, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = haystack.IndexOf(needle, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }
}
