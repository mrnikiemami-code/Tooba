using Xunit;
using System.Xml.Linq;

namespace Tooba.Offer.Tests.Architecture;

public sealed class OfferArchitectureGuardTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "ai", "TOOBA-PIPELINE-PROTOCOL.md"))
                || File.Exists(Path.Combine(dir.FullName, ".git", "HEAD")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private static string OfferRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Offer");

    [Fact]
    public void Domain_csproj_has_no_application_or_infrastructure_reference()
    {
        var refs = ProjectRefs("Tooba.Offer.Domain");
        Assert.DoesNotContain(refs, r => r.Contains("Application", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Endpoints", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Endpoints_csproj_does_not_reference_infrastructure()
    {
        var refs = ProjectRefs("Tooba.Offer.Endpoints");
        Assert.DoesNotContain(refs, r => r.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(refs, r => r.Contains("Tooba.Offer.Application", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_csproj_does_not_reference_host_or_endpoints()
    {
        var refs = ProjectRefs("Tooba.Offer.Application");
        Assert.DoesNotContain(refs, r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Endpoints", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void No_offer_production_source_exceeds_800_loc()
    {
        var root = OfferRoot();
        var violations = new List<string>();
        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var n = file.Replace('\\', '/');
            if (n.Contains("/bin/", StringComparison.Ordinal) || n.Contains("/obj/", StringComparison.Ordinal))
            {
                continue;
            }

            if (n.Contains("/Migrations/", StringComparison.OrdinalIgnoreCase) || n.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (n.Contains("/Tooba.Offer.Tests/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var loc = File.ReadAllLines(file).Length;
            if (loc > 800)
            {
                violations.Add($"{Path.GetRelativePath(RepoRoot(), file)}:{loc}");
            }
        }

        Assert.True(violations.Count == 0, "Offer >800 LOC: " + string.Join("; ", violations));
    }

    [Fact]
    public void Host_seller_panel_does_not_map_offer_routes()
    {
        var hostEndpoints = Path.Combine(
            RepoRoot(),
            "src",
            "backend",
            "Host",
            "Tooba.Host",
            "Seller",
            "SellerPanelEndpoints.cs");
        var text = File.ReadAllText(hostEndpoints);
        Assert.DoesNotContain("MapGet(\"/offers\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/offers\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPatch(\"/offers/", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Offer_reference_boundaries_remain_clean()
    {
        var contracts = Sources("Tooba.Offer.Contracts");
        Assert.DoesNotContain(contracts, x => x.Text.Contains("namespace Tooba.Offer.Domain", StringComparison.Ordinal));
        Assert.DoesNotContain(ProjectRefs("Tooba.Offer.Domain"), x => x.Contains("Tooba.Offer.Contracts", StringComparison.Ordinal));
        Assert.DoesNotContain(AllOfferSources(), x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));

        var application = Sources("Tooba.Offer.Application");
        Assert.Contains(application, x => x.Text.Contains("IRequestHandler<", StringComparison.Ordinal));
        Assert.Contains(application, x => x.Text.Contains("CreateOfferCommand", StringComparison.Ordinal));

        var infrastructureRefs = ProjectRefs("Tooba.Offer.Infrastructure");
        Assert.DoesNotContain(infrastructureRefs, x => x.Contains("Catalog.Application", StringComparison.Ordinal));
        Assert.DoesNotContain(infrastructureRefs, x => x.Contains("Party.Application", StringComparison.Ordinal));
    }

    [Fact]
    public void Offer_endpoint_and_host_writes_remain_in_cqrs()
    {
        var endpoint = File.ReadAllText(Path.Combine(
            OfferRoot(), "Tooba.Offer.Endpoints", "Seller", "OfferSellerEndpoints.cs"));
        Assert.Contains("ISender sender", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("panel.CreateOfferAsync", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("panel.PatchOfferAsync", endpoint, StringComparison.Ordinal);

        var composer = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller", "SellerPanelComposer.cs"));
        Assert.DoesNotContain("CreateOfferAsync(", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("PatchOfferAsync(", composer, StringComparison.Ordinal);
        Assert.DoesNotContain(".Activate(", composer, StringComparison.Ordinal);
        Assert.DoesNotContain(".SetReturnPolicy(", composer, StringComparison.Ordinal);
        Assert.DoesNotContain(".SetOrderQuantityLimits(", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("_offers.SaveChanges", composer, StringComparison.Ordinal);
    }

    [Fact]
    public void Offer_application_and_endpoints_do_not_use_dbcontext()
    {
        var violations = Sources("Tooba.Offer.Application")
            .Concat(Sources("Tooba.Offer.Endpoints"))
            .Where(x => x.Text.Contains("OfferDbContext", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToList();
        Assert.Empty(violations);
    }

    [Fact]
    public void Seller_offer_path_has_real_cqrs_and_owner_boundaries()
    {
        var endpoint = File.ReadAllText(Path.Combine(
            OfferRoot(), "Tooba.Offer.Endpoints", "Seller", "OfferSellerEndpoints.cs"));
        Assert.DoesNotContain("IOfferSellerPanel", string.Join('\n', AllOfferSources().Select(x => x.Text)), StringComparison.Ordinal);
        Assert.Contains("return Results.Json(await sender.Send(new ListSellerOffersQuery", endpoint, StringComparison.Ordinal);
        Assert.Contains("return Results.Json(await sender.Send(new GetOfferQuery", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("await sender.Send(new ListSellerOffersQuery", endpoint.Replace(
            "return Results.Json(await sender.Send(new ListSellerOffersQuery", string.Empty), StringComparison.Ordinal);
        foreach (var obsolete in new[] { "OfferRequests.cs", "OfferHandlers.cs", "OfferQueries.cs", "OfferQueryHandlers.cs" })
            Assert.False(File.Exists(Path.Combine(OfferRoot(), "Tooba.Offer.Application", "Commands", obsolete))
                         || File.Exists(Path.Combine(OfferRoot(), "Tooba.Offer.Application", "Queries", obsolete)));

        var composer = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Seller", "SellerPanelComposer.cs"));
        Assert.DoesNotContain("OfferDbContext", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("SellerOfferListItem", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("SellerOfferDetailPage", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("PricingDbContext", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("InventoryDbContext", composer, StringComparison.Ordinal);
    }

    [Fact]
    public void Offer_endpoints_do_not_string_match_expected_exceptions()
    {
        var endpoint = File.ReadAllText(Path.Combine(
            OfferRoot(), "Tooba.Offer.Endpoints", "Seller", "OfferSellerEndpoints.cs"));
        Assert.DoesNotContain("ex.Message is", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("when (ex.Message", endpoint, StringComparison.Ordinal);
    }

    [Fact]
    public void Offer_domain_and_application_have_no_persian_prose()
    {
        var violations = Sources("Tooba.Offer.Domain")
            .Concat(Sources("Tooba.Offer.Application"))
            .Concat(Sources("Tooba.Offer.Infrastructure"))
            .Where(x => x.Text.Any(ch => ch is >= '\u0600' and <= '\u06ff'))
            .Select(x => x.Path)
            .ToList();
        Assert.Empty(violations);
    }

    [Fact]
    public void Host_production_sources_do_not_reference_offer_persistence()
    {
        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var violations = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
            {
                var n = path.Replace('\\', '/');
                return !n.Contains("/bin/", StringComparison.Ordinal)
                       && !n.Contains("/obj/", StringComparison.Ordinal);
            })
            .Select(path => (Path: Path.GetRelativePath(RepoRoot(), path), Text: File.ReadAllText(path)))
            .Where(x =>
                x.Text.Contains("OfferDbContext", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Offer.Infrastructure.Persistence", StringComparison.Ordinal)
                || x.Text.Contains("_offers.Offers", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToList();
        Assert.True(violations.Count == 0, "Host Offer persistence leaks: " + string.Join("; ", violations));
    }

    [Fact]
    public void Foreign_modules_do_not_reference_offer_dbcontext()
    {
        var modulesRoot = Path.Combine(RepoRoot(), "src", "backend", "Modules");
        var violations = Directory.EnumerateFiles(modulesRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
            {
                var n = path.Replace('\\', '/');
                if (n.Contains("/bin/", StringComparison.Ordinal) || n.Contains("/obj/", StringComparison.Ordinal))
                {
                    return false;
                }

                if (n.Contains("/Modules/Offer/", StringComparison.OrdinalIgnoreCase)
                    || n.Contains("/Tooba.Offer.", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return true;
            })
            .Select(path => (Path: Path.GetRelativePath(RepoRoot(), path), Text: File.ReadAllText(path)))
            .Where(x =>
                x.Text.Contains("OfferDbContext", StringComparison.Ordinal)
                || x.Text.Contains("Tooba.Offer.Infrastructure.Persistence", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToList();
        Assert.True(violations.Count == 0, "Foreign Offer persistence leaks: " + string.Join("; ", violations));
    }

    [Fact]
    public void Extracted_host_surfaces_use_offer_query_gateway_not_persistence()
    {
        var host = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");
        var files = new[]
        {
            Path.Combine(host, "Admin", "AdminPanelComposer.cs"),
            Path.Combine(host, "Admin", "ProductWorkspaceComposer.cs"),
            Path.Combine(host, "Admin", "MerchandisingCampaignAdminEndpoints.cs"),
            Path.Combine(host, "Admin", "ReservationPolicyAdminComposer.cs"),
            Path.Combine(host, "Admin", "ReservationPolicyAdminEndpoints.cs"),
            Path.Combine(host, "Grid", "AdminProductGridQueryEngine.cs"),
            Path.Combine(host, "Grid", "AdminSellersGridQueryEngine.cs"),
            Path.Combine(host, "Storefront", "StorefrontComposer.cs"),
        };
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("OfferDbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Offer.Infrastructure.Persistence", text, StringComparison.Ordinal);
            Assert.DoesNotContain("_offers.Offers", text, StringComparison.Ordinal);
            Assert.Contains("IOfferQueryGateway", text, StringComparison.Ordinal);
        }
    }

    private static IReadOnlyList<(string Path, string Text)> Sources(string project) =>
        Directory.EnumerateFiles(Path.Combine(OfferRoot(), project), "*.cs", SearchOption.AllDirectories)
            .Where(x => !x.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(x => (Path.GetRelativePath(RepoRoot(), x), File.ReadAllText(x)))
            .ToList();

    private static IReadOnlyList<(string Path, string Text)> AllOfferSources() =>
        Directory.EnumerateFiles(OfferRoot(), "*.cs", SearchOption.AllDirectories)
            .Where(x => !x.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(x => !x.Contains($"{Path.DirectorySeparatorChar}Tooba.Offer.Tests{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(x => (Path.GetRelativePath(RepoRoot(), x), File.ReadAllText(x)))
            .ToList();

    private static IReadOnlyList<string> ProjectRefs(string projectFolder)
    {
        var csproj = Path.Combine(OfferRoot(), projectFolder, projectFolder + ".csproj");
        var doc = XDocument.Load(csproj);
        return doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => x.Length > 0)
            .ToArray();
    }
}
