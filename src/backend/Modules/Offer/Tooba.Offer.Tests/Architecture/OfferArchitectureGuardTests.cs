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
        Assert.DoesNotContain(refs, r => r.Contains("Inventory.Contracts", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, r => r.Contains("Pricing.Contracts", StringComparison.OrdinalIgnoreCase));
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
    public void Offer_price_and_inventory_routes_use_application_commands_only()
    {
        var endpoint = File.ReadAllText(Path.Combine(
            OfferRoot(), "Tooba.Offer.Endpoints", "Seller", "OfferSellerEndpoints.cs"));
        Assert.Contains("new SetOfferPriceCommand(", endpoint, StringComparison.Ordinal);
        Assert.Contains("new SetOfferInventoryCommand(", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("ISellerOfferPricingGateway", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("ISellerOfferInventoryGateway", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain(".SetPriceAsync(", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain(".SetInventoryAsync(", endpoint, StringComparison.Ordinal);

        var price = Sources("Tooba.Offer.Application")
            .Single(x => x.Path.Replace('\\', '/').EndsWith(
                "/Commands/SetOfferPrice/SetOfferPriceCommand.cs", StringComparison.Ordinal));
        var inventory = Sources("Tooba.Offer.Application")
            .Single(x => x.Path.Replace('\\', '/').EndsWith(
                "/Commands/SetOfferInventory/SetOfferInventoryCommand.cs", StringComparison.Ordinal));
        Assert.Contains("IRequestHandler<SetOfferPriceCommand", price.Text, StringComparison.Ordinal);
        Assert.Contains("pricing.SetPriceAsync(", price.Text, StringComparison.Ordinal);
        Assert.Contains("IRequestHandler<SetOfferInventoryCommand", inventory.Text, StringComparison.Ordinal);
        Assert.Contains("inventory.SetInventoryAsync(", inventory.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Offer_foreign_project_references_are_contracts_only()
    {
        foreach (var project in new[] { "Tooba.Offer.Application", "Tooba.Offer.Infrastructure" })
        {
            var forbidden = ProjectRefs(project)
                .Where(reference =>
                    reference.Contains("Catalog.", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("Inventory.", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("Party.", StringComparison.OrdinalIgnoreCase)
                    || reference.Contains("Pricing.", StringComparison.OrdinalIgnoreCase))
                .Where(reference => !reference.Contains(".Contracts", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Assert.True(forbidden.Length == 0, $"{project} foreign non-contract refs: {string.Join("; ", forbidden)}");
        }
    }

    [Fact]
    public void Offer_has_no_type_forwarding_artifacts()
    {
        Assert.DoesNotContain(AllOfferSources(), x => x.Text.Contains("TypeForwardedTo", StringComparison.Ordinal));
        Assert.Empty(Directory.EnumerateFiles(OfferRoot(), "TypeForwarders.cs", SearchOption.AllDirectories));
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
        Assert.Contains("api.From(result)", endpoint, StringComparison.Ordinal);
        Assert.Contains("api.Created(", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json(await sender.Send", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json(result, statusCode: StatusCodes.Status201Created)", endpoint, StringComparison.Ordinal);
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
    public void Offer_endpoints_use_central_api_response_factory_not_local_mappers()
    {
        var endpoint = File.ReadAllText(Path.Combine(
            OfferRoot(), "Tooba.Offer.Endpoints", "Seller", "OfferSellerEndpoints.cs"));
        Assert.Contains("ApiResponseFactory api", endpoint, StringComparison.Ordinal);
        Assert.Contains("api.From(", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("ToSemanticError", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("AcceptLanguage", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("AcceptLanguage.Contains", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("Guid.NewGuid()", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("exception.Message", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json(new ProblemDetails", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("StartsWith(\"en\"", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("switch (result.FirstError", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("static IHttpContextAccessor", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestServices.Get", endpoint, StringComparison.Ordinal);
    }

    [Fact]
    public void Offer_application_handlers_return_result_not_expected_semantic_exception()
    {
        var application = Sources("Tooba.Offer.Application");
        Assert.Contains(application, x => x.Text.Contains("IRequest<Result<", StringComparison.Ordinal));
        var throws = application
            .Where(x => x.Text.Contains("throw new SemanticException", StringComparison.Ordinal)
                        || x.Text.Contains("throw OfferReadModelComposer.NotFound()", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToList();
        Assert.True(throws.Count == 0, "expected SemanticException throws remain: " + string.Join("; ", throws));
    }

    [Fact]
    public void Offer_domain_expected_failures_use_result_not_semantic_exception_control_flow()
    {
        var domain = File.ReadAllText(Path.Combine(
            OfferRoot(), "Tooba.Offer.Domain", "Aggregates", "SellerOffer.cs"));
        Assert.Contains("public Result Activate(", domain, StringComparison.Ordinal);
        Assert.Contains("public Result SetOrderQuantityLimits(", domain, StringComparison.Ordinal);
        Assert.DoesNotContain("throw new SemanticException", domain, StringComparison.Ordinal);
    }

    [Fact]
    public void OfferEndpointLocalizer_is_deleted_and_resources_exist()
    {
        Assert.False(File.Exists(Path.Combine(
            OfferRoot(), "Tooba.Offer.Endpoints", "Seller", "OfferEndpointLocalizer.cs")));
        Assert.True(File.Exists(Path.Combine(
            OfferRoot(), "Tooba.Offer.Endpoints", "Resources", "OfferErrors.resx")));
        Assert.True(File.Exists(Path.Combine(
            OfferRoot(), "Tooba.Offer.Endpoints", "Resources", "OfferErrors.fa.resx")));
        Assert.True(File.Exists(Path.Combine(
            OfferRoot(), "Tooba.Offer.Endpoints", "Errors", "OfferErrorCatalogContributor.cs")));
    }

    [Fact]
    public void Offer_domain_and_application_do_not_throw_PlatformHttpException()
    {
        var violations = Sources("Tooba.Offer.Domain")
            .Concat(Sources("Tooba.Offer.Application"))
            .Where(x => x.Text.Contains("PlatformHttpException", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToList();
        Assert.True(violations.Count == 0, "PlatformHttpException in Offer Domain/Application: " + string.Join("; ", violations));
    }

    [Fact]
    public void SafeErrorMapper_has_no_ClassifySemanticCode_heuristic()
    {
        var mapper = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "backend",
            "BuildingBlocks",
            "Tooba.BuildingBlocks",
            "Presentation",
            "Errors",
            "SafeErrorMapper.cs"));
        Assert.DoesNotContain("ClassifySemanticCode", mapper, StringComparison.Ordinal);
        Assert.DoesNotContain(".not_found", mapper, StringComparison.Ordinal);
        Assert.DoesNotContain("cannot_activate", mapper, StringComparison.Ordinal);
        Assert.Contains("IErrorDefinitionCatalog", mapper, StringComparison.Ordinal);
    }

    [Fact]
    public void Global_handler_uses_central_exception_presentation()
    {
        var handler = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Errors", "ToobaExceptionHandler.cs"));
        Assert.Contains("IExceptionPresentationService", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("ISafeErrorMapper", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("AcceptLanguage", handler, StringComparison.Ordinal);
    }

    [Fact]
    public void Offer_handlers_and_endpoints_do_not_start_raw_activities_or_parse_traceparent()
    {
        var violations = Sources("Tooba.Offer.Application")
            .Concat(Sources("Tooba.Offer.Endpoints"))
            .Where(x =>
                x.Text.Contains("StartActivity(", StringComparison.Ordinal)
                || x.Text.Contains("traceparent", StringComparison.OrdinalIgnoreCase)
                || x.Text.Contains("AsyncLocal<", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToList();
        Assert.True(violations.Count == 0, "raw tracing/correlation in Offer handlers/endpoints: " + string.Join("; ", violations));
    }

    [Fact]
    public void Offer_cross_module_gateways_use_module_call_tracer_facade()
    {
        var tracing = Path.Combine(OfferRoot(), "Tooba.Offer.Infrastructure", "Adapters", "Tracing");
        Assert.True(Directory.Exists(tracing));
        var text = string.Join('\n', Directory.EnumerateFiles(tracing, "*.cs").Select(File.ReadAllText));
        Assert.Contains("IModuleCallTracer", text, StringComparison.Ordinal);
        Assert.Contains("TracedCatalogVariantLookup", text, StringComparison.Ordinal);
        Assert.Contains("TracedPartyLookup", text, StringComparison.Ordinal);
        Assert.Contains("TracedPriceLookupGateway", text, StringComparison.Ordinal);
        Assert.Contains("TracedSellerOfferInventoryGateway", text, StringComparison.Ordinal);
        Assert.DoesNotContain("exception.Message", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SetTag(\"request", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_registers_tracing_behavior_once_and_single_opentelemetry_pipeline()
    {
        var tmar = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "BuildingBlocks", "Tooba.BuildingBlocks", "TmarFoundation.cs"));
        Assert.Equal(1, CountOccurrences(tmar, "typeof(Observability.Tracing.TracingBehavior<,>)"));

        var program = File.ReadAllText(Path.Combine(
            RepoRoot(), "src", "backend", "Host", "Tooba.Host", "Program.cs"));
        Assert.Equal(1, CountOccurrences(program, "AddOpenTelemetry()"));
        Assert.Contains("AddOfferModuleCallTracing()", program, StringComparison.Ordinal);
        Assert.Contains("UseToobaCorrelationId()", program, StringComparison.Ordinal);
        Assert.Contains("RequestObservabilityEnrichmentMiddleware", program, StringComparison.Ordinal);
    }

    /// <summary>
    /// TB-TMAR-OFFER-HOST-RESIDUE-REPAIR-001 — Host may not own Offer business selection policy or
    /// hidden Offer type aliases; the primary-offer rule is Offer-owned behind a Contracts port.
    /// </summary>
    [Fact]
    public void Host_owns_no_offer_selection_policy_or_hidden_offer_alias()
    {
        var hostRoot = Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host");

        Assert.False(File.Exists(Path.Combine(hostRoot, "Storefront", "StorefrontPrimaryOfferResolver.cs")));
        Assert.False(File.Exists(Path.Combine(hostRoot, "OfferGlobalUsings.cs")));
        Assert.False(Directory.Exists(Path.Combine(hostRoot, ".tmp-t014-test-out")));

        var hostSources = Directory.EnumerateFiles(hostRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
            {
                var n = path.Replace('\\', '/');
                return !n.Contains("/bin/", StringComparison.Ordinal)
                       && !n.Contains("/obj/", StringComparison.Ordinal);
            })
            .Select(path => (Path: Path.GetRelativePath(RepoRoot(), path), Text: File.ReadAllText(path)))
            .ToArray();

        Assert.DoesNotContain(hostSources, x => x.Text.Contains("global using OfferStatus", StringComparison.Ordinal));
        Assert.DoesNotContain(hostSources, x => x.Text.Contains("global using SalesChannel", StringComparison.Ordinal));
        Assert.DoesNotContain(hostSources, x => x.Text.Contains("global using Tooba.Offer", StringComparison.Ordinal));

        // The equivalent primary-offer ordering rule must not survive anywhere in Host.
        Assert.DoesNotContain(
            hostSources,
            x => x.Text.Contains("OrderByDescending(candidate => candidate.AvailableUnits > 0)", StringComparison.Ordinal));

        // Host consumes the Offer Contracts port, never the Application implementation.
        var composer = hostSources.Single(x => x.Path.EndsWith(
            "Host/Tooba.Host/Storefront/StorefrontComposer.cs".Replace('/', Path.DirectorySeparatorChar),
            StringComparison.Ordinal));
        Assert.Contains("IPrimaryOfferSelectionPolicy", composer.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Offer.Application", composer.Text, StringComparison.Ordinal);
        Assert.Contains("Tooba.Offer.Contracts.Ports", composer.Text, StringComparison.Ordinal);

        // Contracts owns the port; Application owns the implementation.
        Assert.True(File.Exists(Path.Combine(
            OfferRoot(), "Tooba.Offer.Contracts", "Ports", "IPrimaryOfferSelectionPolicy.cs")));
        Assert.True(File.Exists(Path.Combine(
            OfferRoot(), "Tooba.Offer.Application", "Policies", "PrimaryOfferSelectionPolicy.cs")));

        // The allowed thin Host security adapter stays thin and business-free.
        var adapter = File.ReadAllText(Path.Combine(hostRoot, "Seller", "HostOfferSellerAuthorizer.cs"));
        Assert.Contains("IOfferSellerAuthorizer", adapter, StringComparison.Ordinal);
        Assert.DoesNotContain("PrimaryOfferSelectionPolicy", adapter, StringComparison.Ordinal);

        // No Offer -> Host reference in the Offer production projects.
        foreach (var project in new[] { "Tooba.Offer.Domain", "Tooba.Offer.Application", "Tooba.Offer.Contracts", "Tooba.Offer.Infrastructure", "Tooba.Offer.Endpoints" })
        {
            Assert.DoesNotContain(ProjectRefs(project), r => r.Contains("Tooba.Host", StringComparison.OrdinalIgnoreCase));
        }
    }

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
    public void Offer_production_sources_do_not_bypass_clock_or_id_generator()
    {
        var violations = AllOfferSources()
            .Where(x =>
                x.Text.Contains("DateTimeOffset.UtcNow", StringComparison.Ordinal)
                || x.Text.Contains("DateTime.UtcNow", StringComparison.Ordinal)
                || x.Text.Contains("Guid.NewGuid()", StringComparison.Ordinal))
            .Select(x => x.Path)
            .ToList();
        Assert.True(violations.Count == 0, "direct time/id bypass in Offer: " + string.Join("; ", violations));
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

                if (n.Contains(".Tests/", StringComparison.OrdinalIgnoreCase)
                    || n.Contains("/Tests/", StringComparison.OrdinalIgnoreCase))
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
