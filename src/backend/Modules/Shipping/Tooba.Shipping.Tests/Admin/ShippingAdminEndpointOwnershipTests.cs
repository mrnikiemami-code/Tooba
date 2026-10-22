using Xunit;

namespace Tooba.Shipping.Tests.Admin;

/// <summary>
/// TB-SHIPPING-MVP-ADMIN-LIVE-WIRING-055 — live admin surface ownership: routes stay under the
/// admin convention, every route is authorized through the module authorizer seam with an existing
/// platform permission, and the route service code stays authoritative over any body value.
/// </summary>
public sealed class ShippingAdminEndpointOwnershipTests
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
        "ShippingRateCoverageEndpoints.cs"));

    private static string CompositionSource() => File.ReadAllText(Path.Combine(
        RepoRoot(),
        "src",
        "backend",
        "Modules",
        "Shipping",
        "Tooba.Shipping.Endpoints",
        "ShippingEndpointModule.cs"));

    [Fact]
    public void Every_admin_route_is_mapped_and_authorized_with_existing_permissions()
    {
        var source = EndpointsSource();

        Assert.Contains("MapGroup(\"/v1/admin\")", CompositionSource(), StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/shipping-services/{serviceCode}/rates\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/shipping-services/{serviceCode}/rates\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/shipping-services/{serviceCode}/rates/{ratePolicyId:guid}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPatch(\"/shipping-services/{serviceCode}/rates/{ratePolicyId:guid}/active\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/shipping-services/{serviceCode}/coverage-grants\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPatch(", source, StringComparison.Ordinal);
        Assert.Contains("coverage-grants/{coverageGrantId:guid}/active", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/shipping-services/{serviceCode}/method-coverage\"", source, StringComparison.Ordinal);

        // Seven routes, seven authorization calls: no mapped route may be reachable anonymously.
        var mapCalls = CountOccurrences(source, "group.Map");
        var authorizerCalls = CountOccurrences(source, "authorizer.RequireAuthorizedAsync(");
        Assert.Equal(7, mapCalls);
        Assert.Equal(7, authorizerCalls);

        // Permissions are existing catalog ids only; no invented capability strings.
        Assert.Contains("public const string View = \"fulfillment.view\";", source, StringComparison.Ordinal);
        Assert.Contains("public const string Manage = \"fulfillment.manage\";", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowAnonymous", source, StringComparison.Ordinal);

        // The route parameter — not the body — is the service identity handed to the application layer.
        Assert.Contains("new CreateShippingRatePolicyCommand(serviceCode, body)", source, StringComparison.Ordinal);
        Assert.Contains("new UpdateShippingRatePolicyCommand(ratePolicyId, serviceCode, body)", source, StringComparison.Ordinal);
        Assert.Contains("new SetShippingRatePolicyActiveCommand(ratePolicyId, serviceCode, body.IsActive)", source, StringComparison.Ordinal);
        Assert.Contains("new CreateShippingCoverageGrantCommand(serviceCode, body)", source, StringComparison.Ordinal);
        Assert.Contains("new SetShippingCoverageGrantActiveCommand(coverageGrantId, serviceCode, body.IsActive)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_wires_the_endpoints_and_a_fail_closed_authorizer_without_migrator_registration()
    {
        var program = File.ReadAllText(Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));

        Assert.Contains("app.MapShippingEndpoints();", program, StringComparison.Ordinal);
        Assert.Contains(
            "Tooba.Shipping.Endpoints.Admin.IShippingAdminAuthorizer, Tooba.Shipping.Endpoints.Admin.ShippingAdminAuthorizer",
            program,
            StringComparison.Ordinal);
        Assert.Contains(
            "typeof(Tooba.Shipping.Application.Rates.Commands.CreateShippingRatePolicyCommand).Assembly",
            program,
            StringComparison.Ordinal);

        // Activation wiring must not register a schema migrator, so startup DDL stays off.
        Assert.DoesNotContain("IModuleSchemaMigrator<ShippingDbContext>", program, StringComparison.Ordinal);

        // The Host Admin/Security file sets are AMSC-frozen (17/19 files), so the module keeps the
        // panel-gate implementation on the shared platform seam instead of adding a Host adapter.
        Assert.False(
            File.Exists(Path.Combine(
                RepoRoot(),
                "src",
                "backend",
                "Host",
                "Tooba.Host",
                "Admin",
                "Access",
                "Authorizers",
                "HostShippingAdminAuthorizer.cs")));

        var authorizer = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "backend",
            "Modules",
            "Shipping",
            "Tooba.Shipping.Endpoints",
            "Admin",
            "ShippingAdminAuthorizer.cs"));

        Assert.Contains("IAdminPanelAccess", authorizer, StringComparison.Ordinal);
        Assert.Contains("IAuthorizationService", authorizer, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.AdminAuthorizationUnavailable", authorizer, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.AdminAuthorizationDenied", authorizer, StringComparison.Ordinal);
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
