using Xunit;

namespace Tooba.Shipping.Tests.Admin;

/// <summary>
/// TB-SHIPPING-FULL-OPERATIONS-071 — ownership of the operating-schedule admin surface: routes stay
/// under the admin convention, every route is authorized through the module authorizer seam with an
/// existing platform permission, and the route service code stays authoritative over any body value.
/// </summary>
public sealed class ShippingOperatingScheduleEndpointOwnershipTests
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

    private static string ScheduleEndpointsSource() => File.ReadAllText(Path.Combine(
        RepoRoot(),
        "src",
        "backend",
        "Modules",
        "Shipping",
        "Tooba.Shipping.Endpoints",
        "Admin",
        "ShippingOperatingScheduleEndpoints.cs"));

    [Fact]
    public void Every_schedule_route_is_mapped_and_authorized_with_existing_permissions()
    {
        var source = ScheduleEndpointsSource();

        Assert.Contains("MapGet(\"/shipping-services/{serviceCode}/operating-schedules\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/shipping-services/{serviceCode}/operating-schedules\"", source, StringComparison.Ordinal);
        Assert.Contains("operating-schedules/{scheduleId:guid}/active", source, StringComparison.Ordinal);

        // Three routes, three authorization calls: no mapped route may be reachable anonymously.
        Assert.Equal(3, CountOccurrences(source, "group.Map"));
        Assert.Equal(3, CountOccurrences(source, "authorizer.RequireAuthorizedAsync("));

        // Permissions are the existing catalog ids already declared by the module; no invented capability.
        Assert.Contains("ShippingAdminPermissions.View", source, StringComparison.Ordinal);
        Assert.Contains("ShippingAdminPermissions.Manage", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowAnonymous", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json", source, StringComparison.Ordinal);

        // The route parameter - not the body - is the service identity handed to the application layer.
        Assert.Contains("new GetShippingOperatingSchedulesQuery(serviceCode)", source, StringComparison.Ordinal);
        Assert.Contains("new UpsertShippingOperatingScheduleCommand(serviceCode, body)", source, StringComparison.Ordinal);
        Assert.Contains(
            "new SetShippingOperatingScheduleActiveCommand(scheduleId, serviceCode, body.IsActive)",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void The_schedule_surface_is_mapped_under_the_same_admin_group_as_rates()
    {
        var module = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "backend",
            "Modules",
            "Shipping",
            "Tooba.Shipping.Endpoints",
            "ShippingEndpointModule.cs"));

        Assert.Contains("var admin = app.MapGroup(\"/v1/admin\");", module, StringComparison.Ordinal);
        Assert.Contains("ShippingRateCoverageEndpoints.Map(admin);", module, StringComparison.Ordinal);
        Assert.Contains("ShippingOperatingScheduleEndpoints.Map(admin);", module, StringComparison.Ordinal);
    }

    [Fact]
    public void The_schedule_directory_is_registered_in_module_owned_di_without_a_host_edit()
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
            "services.AddScoped<IShippingOperatingScheduleDirectory, ShippingOperatingScheduleDirectory>();",
            module,
            StringComparison.Ordinal);

        // Activation wiring must not register a schema migrator, so startup DDL stays off.
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.DoesNotContain("IModuleSchemaMigrator<ShippingDbContext>", program, StringComparison.Ordinal);
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
