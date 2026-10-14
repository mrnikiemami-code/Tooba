using Xunit;

namespace Tooba.Shipping.Tests.Admin;

/// <summary>
/// TB-SHIPPING-MVP-DESTINATION-LIVE-067 — the customer storefront Geography read must be public but
/// read-only: it exists so a storefront customer can select stable country/area/city ids without ever
/// being handed admin capability. It must not reuse the admin authorizer, must expose only GET routes,
/// must stay bounded to the minimum levels the shipping screen needs, and must not leak persistence.
/// </summary>
public sealed class GeographyStorefrontReadOwnershipTests
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

    private static string StorefrontSource() =>
        Read("src", "backend", "Modules", "Geography", "Tooba.Geography.Endpoints", "Storefront", "GeographyStorefrontEndpoints.cs");

    [Fact]
    public void Customer_geography_read_is_public_read_only_and_never_reuses_admin_authorization()
    {
        var source = StorefrontSource();

        Assert.Contains("MapGroup(\"/v1/storefront\")", source, StringComparison.Ordinal);

        // Only GET routes exist: no write surface is exposed to anonymous customers.
        Assert.DoesNotContain("MapPost", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPut", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapDelete", source, StringComparison.Ordinal);

        // The admin authorization seam must not appear anywhere in the customer surface.
        Assert.DoesNotContain("GeographyAdminPermissions", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RequireAuthorizedAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IGeographyAdminAuthorizer", source, StringComparison.Ordinal);

        // Contracts-only: no persistence type and no unbounded query may leak through HTTP.
        Assert.DoesNotContain("DbContext", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IQueryable", source, StringComparison.Ordinal);

        // Bounded to the levels the storefront shipping screen actually needs.
        Assert.Contains("/geography/places/countries", source, StringComparison.Ordinal);
        Assert.Contains("/administrative-areas", source, StringComparison.Ordinal);
        Assert.Contains("/cities", source, StringComparison.Ordinal);
        Assert.DoesNotContain("/service-areas", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Customer_city_read_is_scoped_by_its_parent_area_so_hierarchy_is_preserved()
    {
        var source = StorefrontSource();

        // The city route accepts the parent area bound and forwards it to the Contracts lookup.
        Assert.Contains("Guid? administrativeAreaId = null", source, StringComparison.Ordinal);
        Assert.Contains("ListCitiesAsync(countryId, administrativeAreaId, cancellationToken)", source, StringComparison.Ordinal);

        // Labels come from the label seam and fall back to the stable code, never to a guessed name.
        Assert.Contains("IGeographyLabelLookup", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_geography_read_stays_the_authorized_four_route_surface()
    {
        var admin = Read("src", "backend", "Modules", "Geography", "Tooba.Geography.Endpoints", "Admin", "GeographyAdminEndpoints.cs");

        Assert.Contains("MapGroup(\"/v1/admin\")", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGroup(\"/v1/storefront\")", admin, StringComparison.Ordinal);
        Assert.Equal(4, CountOccurrences(admin, "group.MapGet("));

        // The customer route lives in its own file and is registered from Host explicitly.
        var program = Read("src", "backend", "Host", "Tooba.Host", "Program.cs");
        Assert.Contains("app.MapGeographyEndpoints();", program, StringComparison.Ordinal);
        Assert.Contains("app.MapGeographyStorefrontEndpoints();", program, StringComparison.Ordinal);
        Assert.Contains("using Tooba.Geography.Endpoints.Storefront;", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Legacy_storefront_province_route_keeps_its_authority()
    {
        var storefront = Read(
            "src", "backend", "Modules", "Order", "Tooba.Order.Endpoints", "Storefront", "StorefrontOrderEndpoints.cs");

        Assert.Contains("MapGet(\"/geography/provinces\"", storefront, StringComparison.Ordinal);
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
