using Xunit;

namespace Tooba.Shipping.Tests.Admin;

/// <summary>
/// TB-SHIPPING-MVP-GEOGRAPHY-ADMIN-056 — the authorized Geography admin read must stay admin-guarded,
/// return stable ids plus display labels, and never expose a DbContext or an anonymous route. The Host
/// must wire it in the established way.
/// </summary>
public sealed class GeographyAdminReadOwnershipTests
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

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine([RepoRoot(), .. parts]));

    private static string EndpointsSource() =>
        Read("src", "backend", "Modules", "Geography", "Tooba.Geography.Endpoints", "Admin", "GeographyAdminEndpoints.cs");

    [Fact]
    public void Every_geography_admin_route_is_mapped_and_authorized()
    {
        var source = EndpointsSource();

        Assert.Contains("MapGroup(\"/v1/admin\")", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/geography/countries\"", source, StringComparison.Ordinal);
        Assert.Contains("/geography/countries/{countryId:guid}/administrative-areas\"", source, StringComparison.Ordinal);
        Assert.Contains("/geography/countries/{countryId:guid}/cities\"", source, StringComparison.Ordinal);
        Assert.Contains("/geography/cities/{cityId:guid}/service-areas\"", source, StringComparison.Ordinal);

        // Four read routes, four authorization calls: no route may be reachable anonymously.
        Assert.Equal(4, CountOccurrences(source, "group.MapGet("));
        Assert.Equal(4, CountOccurrences(source, "authorizer.RequireAuthorizedAsync("));
        Assert.DoesNotContain("AllowAnonymous", source, StringComparison.Ordinal);

        // Contracts-only: no persistence type may leak through the endpoints project.
        Assert.DoesNotContain("DbContext", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IQueryable", source, StringComparison.Ordinal);

        // Existing platform permission only; no invented capability.
        Assert.Contains("public const string Read = \"fulfillment.view\";", source, StringComparison.Ordinal);

        // Labels come from the label seam and fall back to the stable code, never to a guessed name.
        Assert.Contains("IGeographyLabelLookup", source, StringComparison.Ordinal);
        Assert.Contains("Label(texts, x.CountryId, x.CountryCode)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Geography_admin_authorizer_is_fail_closed_and_lives_outside_the_frozen_host_trees()
    {
        var authorizer = Read(
            "src", "backend", "Modules", "Geography", "Tooba.Geography.Endpoints", "Admin", "GeographyAdminAuthorizer.cs");

        Assert.Contains("IAdminPanelAccess", authorizer, StringComparison.Ordinal);
        Assert.Contains("IAuthorizationService", authorizer, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.AdminAuthorizationUnavailable", authorizer, StringComparison.Ordinal);
        Assert.Contains("FoundationErrorCodes.AdminAuthorizationDenied", authorizer, StringComparison.Ordinal);

        // Host Admin/Security file sets are AMSC-frozen; the geography read must not add files there.
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
                "HostGeographyAdminAuthorizer.cs")));
    }

    [Fact]
    public void Host_maps_the_geography_read_and_keeps_the_storefront_route_unchanged()
    {
        var program = Read("src", "backend", "Host", "Tooba.Host", "Program.cs");

        Assert.Contains("app.MapGeographyEndpoints();", program, StringComparison.Ordinal);
        Assert.Contains(
            "Tooba.Geography.Endpoints.Admin.IGeographyAdminAuthorizer, Tooba.Geography.Endpoints.Admin.GeographyAdminAuthorizer",
            program,
            StringComparison.Ordinal);
        Assert.Contains("using Tooba.Geography.Endpoints.Admin;", program, StringComparison.Ordinal);

        // The legacy Order storefront geography route keeps its authority; nothing here replaces it.
        var storefront = Read(
            "src", "backend", "Modules", "Order", "Tooba.Order.Endpoints", "Storefront", "StorefrontOrderEndpoints.cs");
        Assert.Contains("MapGet(\"/geography/provinces\"", storefront, StringComparison.Ordinal);
    }

    [Fact]
    public void Shipping_create_path_validates_destination_through_geography_contracts_only()
    {
        var validator = Read(
            "src", "backend", "Modules", "Shipping", "Tooba.Shipping.Infrastructure", "Rates", "GeographyDestinationScopeValidator.cs");

        Assert.Contains("IGeographyLookup", validator, StringComparison.Ordinal);
        Assert.Contains("GetByIdsAsync", validator, StringComparison.Ordinal);
        Assert.Contains("ShippingErrorCodes.DestinationScopeInvalid", validator, StringComparison.Ordinal);
        Assert.DoesNotContain("GeographyDbContext", validator, StringComparison.Ordinal);
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
