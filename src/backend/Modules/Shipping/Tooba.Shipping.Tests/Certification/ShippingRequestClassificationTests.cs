using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Shipping.Application.Rates.Commands;
using Tooba.Shipping.Application.Rates.Queries;
using Tooba.Shipping.Application.Validation;
using Xunit;

namespace Tooba.Shipping.Tests.Certification;

/// <summary>
/// TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3 — independent route/input/validator set-equality gate.
/// </summary>
/// <remarks>
/// The guard is deliberately derived from shipped sources, not from a frozen count or a hand-picked
/// type list: it re-reads every <c>group.Map*</c> call site, follows the handler body to the
/// <c>sender.Send(new ...)</c> construction, enumerates the real <see cref="IRequest{TResponse}"/>
/// universe of the Application assembly, and compares all three against the reviewed classification
/// manifest in <c>docs/architecture/shipping/shipping-request-classification.json</c>.
/// A new route, a new request or a dropped validator therefore fails the gate instead of silently
/// widening the certified surface.
/// </remarks>
public sealed class ShippingRequestClassificationTests
{
    private const string ValidatorRequired = "VALIDATOR_REQUIRED";
    private const string NoValidatorRequired = "NO_VALIDATOR_REQUIRED";
    private const string ManifestRelativePath = "docs/architecture/shipping/shipping-request-classification.json";

    /// <summary>Exact count of the certified endpoint-reachable request universe (W3 gate + TB-188 + TB-189).</summary>
    private const int CertifiedRequestCount = 26;

    /// <summary>Exact count of shipped routes: TB-190 pairs each draft CRUD route for admin and seller.</summary>
    private const int CertifiedRouteCount = 35;

    [Fact]
    public void Shipped_route_universe_equals_the_classified_manifest_exactly()
    {
        var rows = ManifestRows();
        var shipped = ShippedRoutes();
        var classifiedRoutes = rows.SelectMany(row => row.Routes).ToArray();

        // Exact set equality, both directions: no unclassified live route, no phantom manifest route.
        Assert.Equal(
            classifiedRoutes.Select(route => $"{route.Verb} {route.Path}").OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            shipped.Select(route => $"{route.Verb} {route.Path}").OrderBy(x => x, StringComparer.Ordinal).ToArray());

        // Each route pattern is classified exactly once; duplicates are not "coverage".
        Assert.Equal(classifiedRoutes.Length, classifiedRoutes.Select(route => $"{route.Verb} {route.Path}").Distinct(StringComparer.Ordinal).Count());
        // A request may be reachable through more than one classified route (one row per request, not per route).
        Assert.Equal(rows.Count, rows.Select(row => row.Request).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(CertifiedRequestCount, rows.Count);
        Assert.Equal(CertifiedRouteCount, classifiedRoutes.Length);
    }

    [Fact]
    public void Every_shipped_route_dispatches_through_ISender_to_its_classified_request()
    {
        var rows = ManifestRows();
        foreach (var route in ShippedRoutes())
        {
            var row = rows.Single(candidate => candidate.Routes.Any(declared =>
                declared.Verb == route.Verb && declared.Path == route.Path));
            Assert.Equal(row.Request, route.Request);

            // The real dispatch site is the handler body, so the classification cannot drift from it.
            Assert.Contains("MediatR.ISender sender", route.HandlerSource, StringComparison.Ordinal);
            Assert.DoesNotContain("IValidator", route.HandlerSource, StringComparison.Ordinal);
            Assert.DoesNotContain("ValidateAsync", route.HandlerSource, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Application_request_universe_equals_the_endpoint_reachable_universe()
    {
        var reachable = ShippedRoutes().Select(route => route.Request).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var declared = ApplicationRequests().OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var classified = ManifestRows().Select(row => row.Request).OrderBy(x => x, StringComparer.Ordinal).ToArray();

        Assert.Equal(classified, reachable);
        Assert.Equal(declared, reachable);
    }

    [Fact]
    public void Every_reachable_request_has_one_handler_and_the_declared_transport_ownership()
    {
        var rows = ManifestRows();
        var handlers = ApplicationHandlers();
        var validators = ApplicationValidators();
        var shipped = ShippedRoutes();

        foreach (var row in rows)
        {
            Assert.True(handlers.ContainsKey(row.Request), $"{row.Request} has no IRequestHandler");
            Assert.Equal(row.Handler, handlers[row.Request]);

            if (row.Classification == ValidatorRequired)
            {
                Assert.True(validators.TryGetValue(row.Request, out var actual), $"{row.Request} has no AbstractValidator");
                Assert.Equal(new[] { row.Validator }, actual!);
                continue;
            }

            // NO_VALIDATOR_REQUIRED is only legal when a validator would own nothing. Prove the
            // absence from real sources instead of trusting the label: no validator type exists,
            // every route template carries no parameter, and each handler binds no route/query/body.
            Assert.Equal(NoValidatorRequired, row.Classification);
            Assert.Null(row.Validator);
            Assert.False(validators.ContainsKey(row.Request), $"{row.Request} declares NO_VALIDATOR_REQUIRED but has a validator");

            foreach (var declared in row.Routes)
            {
                Assert.DoesNotContain("{", declared.Path, StringComparison.Ordinal);

                var route = shipped.Single(candidate => candidate.Verb == declared.Verb && candidate.Path == declared.Path);
                Assert.DoesNotContain("context.Request.Query", route.HandlerSource, StringComparison.Ordinal);
                Assert.DoesNotContain("ReadFromJsonAsync", route.HandlerSource, StringComparison.Ordinal);
                Assert.DoesNotContain("[FromRoute]", route.HandlerSource, StringComparison.Ordinal);
                Assert.DoesNotContain("[FromQuery]", route.HandlerSource, StringComparison.Ordinal);
                Assert.Contains($"new {row.Request}(", route.HandlerSource, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void The_shared_owner_read_has_both_route_provenance_paths_and_no_trusted_seller_input()
    {
        var owner = ManifestRows().Single(row => row.Request == "GetShippingAuthoringOwnerQuery");

        Assert.Equal(NoValidatorRequired, owner.Classification);
        Assert.Null(owner.Validator);
        Assert.Equal(
            [
                new ManifestRoute("GET", "/v1/admin/shipping-services/authoring-owner"),
                new ManifestRoute("GET", "/v1/seller/shipping-services/authoring-owner"),
            ],
            owner.Routes);

        var shipped = ShippedRoutes();
        var admin = shipped.Single(route => route.Path == "/v1/admin/shipping-services/authoring-owner");
        var seller = shipped.Single(route => route.Path == "/v1/seller/shipping-services/authoring-owner");

        // One use case, two real gates: never one permissive replacement route.
        Assert.Equal(owner.Request, admin.Request);
        Assert.Equal(owner.Request, seller.Request);
        Assert.Contains("RequireAdminOwnerAsync", admin.HandlerSource, StringComparison.Ordinal);
        Assert.Contains("RequireSellerOwnerAsync", seller.HandlerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("RequireAdminOwnerAsync", seller.HandlerSource, StringComparison.Ordinal);

        // The seller candidate header is only ever consumed by the canonical platform seam, never
        // parsed or copied into the request/response by Shipping.
        Assert.DoesNotContain("X-Tooba-Seller-Party", seller.HandlerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("Request.Headers", seller.HandlerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("SellerPartyId", seller.HandlerSource, StringComparison.Ordinal);
        Assert.Contains("new GetShippingAuthoringOwnerQuery(scope)", seller.HandlerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void No_validator_required_rows_are_declared_in_the_manifest_and_nowhere_else()
    {
        var manifest = ManifestDocument();
        var declared = manifest.RootElement.GetProperty("noValidatorRequiredRequests")
            .EnumerateArray()
            .Select(item => item.GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var fromRows = ManifestRows()
            .Where(row => row.Classification == NoValidatorRequired)
            .Select(row => row.Request)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(declared, fromRows);

        // Two additive authorized reads are the only exceptions; every other row keeps a validator.
        Assert.Equal(
            new[] { "GetShippingAuthoringContextQuery", "GetShippingAuthoringDraftsQuery", "GetShippingAuthoringOwnerQuery" },
            declared);
        Assert.Equal(26, ManifestRows().Count);
        Assert.Equal(23, ManifestRows().Count(row => row.Classification == ValidatorRequired));
    }

    [Fact]
    public void Nested_transport_validators_are_declared_and_actually_wired()
    {
        var manifest = ManifestDocument();
        var declared = manifest.RootElement.GetProperty("nestedTransportValidators")
            .EnumerateArray()
            .Select(item => item.GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        var reachable = ManifestRows().Select(row => row.Request).ToHashSet(StringComparer.Ordinal);
        var actual = ApplicationValidators()
            .Where(pair => !reachable.Contains(pair.Key))
            .SelectMany(pair => pair.Value)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(declared, actual);

        // A nested validator that no parent request validator wires would be dead classification.
        var validatorSources = ApplicationSourceFiles().Select(File.ReadAllText).ToArray();
        foreach (var nested in declared)
        {
            Assert.Contains(
                validatorSources,
                source => source.Contains($"SetValidator(new {nested}()", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Required_validators_resolve_through_the_real_cqs_foundation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(CreateShippingRatePolicyCommand).Assembly);
        using var provider = services.BuildServiceProvider();

        var assembly = typeof(CreateShippingRatePolicyCommand).Assembly;
        foreach (var row in ManifestRows())
        {
            var requestType = assembly.GetTypes().Single(type => type.Name == row.Request);
            Assert.True(requestType.IsAssignableTo(typeof(IBaseRequest)), $"{row.Request} is not a MediatR request");

            var resolved = provider.GetService(typeof(IValidator<>).MakeGenericType(requestType));
            if (row.Classification == NoValidatorRequired)
            {
                // A real CQRS foundation must not silently invent a validator for this row either.
                Assert.Null(resolved);
                continue;
            }

            Assert.NotNull(resolved);
            Assert.Equal(row.Validator, resolved!.GetType().Name);
            Assert.Contains(
                resolved.GetType().GetInterfaces(),
                contract => contract.IsGenericType
                            && contract.GetGenericTypeDefinition() == typeof(IValidator<>)
                            && contract.GetGenericArguments()[0] == requestType);
        }

        // The validation pipeline behavior must stay in front of the handlers.
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IPipelineBehavior<,>)
                          && descriptor.ImplementationType == typeof(ValidationBehavior<,>));
    }

    [Fact]
    public async Task Representative_valid_invalid_and_omitted_inputs_hit_the_declared_stable_codes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(CreateShippingRatePolicyCommand).Assembly);
        using var provider = services.BuildServiceProvider();

        var queryValidator = provider.GetRequiredService<IValidator<GetShippingMethodFinancialStatusQuery>>();

        var omitted = await queryValidator.ValidateAsync(new GetShippingMethodFinancialStatusQuery(string.Empty));
        Assert.False(omitted.IsValid);
        Assert.Contains(omitted.Errors, failure => failure.ErrorCode == ShippingValidationCodes.ServiceCodeRequired);

        var valid = await queryValidator.ValidateAsync(new GetShippingMethodFinancialStatusQuery("post"));
        Assert.True(valid.IsValid);

        var commandValidator = provider.GetRequiredService<IValidator<CreateShippingRatePolicyCommand>>();

        // A completely absent envelope is stopped by the NotNull envelope guard; over HTTP the endpoint
        // body reader already converts unreadable/absent JSON to shipping.validation.body_invalid, so
        // this default code never becomes the wire contract.
        var missingEnvelope = await commandValidator.ValidateAsync(new CreateShippingRatePolicyCommand("post", null!));
        Assert.False(missingEnvelope.IsValid);

        // A present but invalid envelope reaches the nested transport rules and keeps declared codes.
        var invalidEnvelope = await commandValidator.ValidateAsync(
            new CreateShippingRatePolicyCommand(
                "post",
                new Tooba.Shipping.Contracts.Admin.ShippingRatePolicyWriteRequest("post", null!, 25000m, "IRR", 2, 3, 500000m)));
        Assert.False(invalidEnvelope.IsValid);
        Assert.Contains(invalidEnvelope.Errors, failure => failure.ErrorCode == ShippingValidationCodes.ScopeRequired);
        Assert.All(invalidEnvelope.Errors, failure => Assert.False(string.IsNullOrWhiteSpace(failure.ErrorCode)));
    }

    [Fact]
    public void A_new_unclassified_route_or_request_is_reported_instead_of_silently_accepted()
    {
        var rows = ManifestRows();
        var classifiedRoutes = rows.SelectMany(row => row.Routes)
            .Select(route => $"{route.Verb} {route.Path}")
            .ToHashSet(StringComparer.Ordinal);
        var classifiedRequests = rows.Select(row => row.Request).ToHashSet(StringComparer.Ordinal);

        // Negative detection: the guard compares real universes, so any new route/request is uncovered.
        var injectedRoute = "POST /v1/admin/shipping-services/{serviceCode}/rates/archive";
        var uncoveredRoutes = new[] { injectedRoute }.Where(route => !classifiedRoutes.Contains(route)).ToArray();
        Assert.Equal(new[] { injectedRoute }, uncoveredRoutes);

        var injectedRequest = "ArchiveShippingRatePolicyCommand";
        var uncoveredRequests = new[] { injectedRequest }.Where(request => !classifiedRequests.Contains(request)).ToArray();
        Assert.Equal(new[] { injectedRequest }, uncoveredRequests);

        // And the classifier really produces that shape from source text, not only from literals.
        const string synthetic = """
            public static void Map(RouteGroupBuilder group)
            {
                group.MapPost("/shipping-services/{serviceCode}/rates/archive", ArchiveAsync);
            }
            """;
        var detected = RouteRegex().Matches(synthetic).Select(match => match.Groups["handler"].Value).ToArray();
        Assert.Equal(new[] { "ArchiveAsync" }, detected);
    }

    [Fact]
    public void Every_classified_request_is_traced_under_the_shipping_module_name()
    {
        var assembly = typeof(CreateShippingRatePolicyCommand).Assembly;
        foreach (var row in ManifestRows())
        {
            var requestType = assembly.GetTypes().Single(type => type.Name == row.Request);
            Assert.Equal("Shipping", MediatRTraceNaming.ResolveModule(requestType));
            Assert.Equal($"mediatr.Shipping.{row.Request}", MediatRTraceNaming.ResolveActivityName(requestType));
        }
    }

    private static IReadOnlyList<ManifestRow> ManifestRows()
    {
        var requests = ManifestDocument().RootElement.GetProperty("requests");
        return requests
            .EnumerateArray()
            .Select(item => new ManifestRow(
                item.GetProperty("request").GetString()!,
                ManifestRoutes(item),
                item.GetProperty("handler").GetString()!,
                item.GetProperty("classification").GetString()!,
                item.TryGetProperty("validator", out var validator) ? validator.GetString() : null))
            .ToArray();
    }

    /// <summary>
    /// A manifest row is keyed by request. Legacy rows carry one <c>route</c>; a request that is
    /// legitimately reachable through more than one route carries a <c>routes</c> array instead, so
    /// neither provenance path can be dropped or silently duplicated.
    /// </summary>
    private static ManifestRoute[] ManifestRoutes(JsonElement item)
    {
        if (item.TryGetProperty("routes", out var multiple))
        {
            Assert.False(
                item.TryGetProperty("route", out _),
                $"{item.GetProperty("request").GetString()} declares both route and routes");
            return multiple
                .EnumerateArray()
                .Select(text => text.GetString()!)
                .Select(SplitRoute)
                .ToArray();
        }

        return [SplitRoute(item.GetProperty("route").GetString()!)];
    }

    private static ManifestRoute SplitRoute(string route) =>
        new(route.Split(' ', 2)[0], route.Split(' ', 2)[1]);

    private static JsonDocument ManifestDocument()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot(), ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        return JsonDocument.Parse(text);
    }

    private static IReadOnlyList<ShippedRoute> ShippedRoutes()
    {
        var prefixes = MapperRoutePrefixes();
        var routes = new List<ShippedRoute>();
        foreach (var path in EndpointSourceFiles())
        {
            var text = File.ReadAllText(path);
            var matches = RouteRegex().Matches(text);
            if (matches.Count == 0)
            {
                continue;
            }

            // The physical group prefix is derived from the composition entry, never hardcoded here,
            // so a route mapped under /v1/seller cannot be mistaken for an /v1/admin route.
            var mapper = MapperClass(text, prefixes, path);
            var prefix = prefixes[mapper];
            foreach (Match match in matches)
            {
                var handler = match.Groups["handler"].Value;
                var body = HandlerBody(text, handler);
                var request = RequestConstruction().Match(body);
                Assert.True(request.Success, $"{Path.GetFileName(path)}:{handler} does not dispatch a classified request");
                routes.Add(new ShippedRoute(
                    match.Groups["verb"].Value.ToUpperInvariant(),
                    $"{prefix}{match.Groups["path"].Value}",
                    request.Groups["request"].Value,
                    handler,
                    body));
            }
        }

        return routes;
    }

    /// <summary>Maps each capability mapper to the concrete route-group prefix its file is mapped under.</summary>
    private static Dictionary<string, string> MapperRoutePrefixes()
    {
        var module = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "backend",
            "Modules",
            "Shipping",
            "Tooba.Shipping.Endpoints",
            "ShippingEndpointModule.cs"));

        var groups = GroupDeclaration().Matches(module)
            .ToDictionary(
                match => match.Groups["group"].Value,
                match => match.Groups["prefix"].Value,
                StringComparer.Ordinal);

        var prefixes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in MapperMapping().Matches(module))
        {
            var mapper = match.Groups["mapper"].Value;
            var group = match.Groups["group"].Value;
            Assert.True(groups.ContainsKey(group), $"{mapper} is mapped under an undeclared group {group}");
            Assert.False(prefixes.ContainsKey(mapper), $"{mapper} is mapped more than once");
            prefixes[mapper] = groups[group];
        }

        Assert.NotEmpty(prefixes);
        return prefixes;
    }

    private static string MapperClass(string text, IReadOnlyDictionary<string, string> prefixes, string path)
    {
        var declared = MapperClassDeclaration().Matches(text)
            .Select(match => match.Groups["mapper"].Value)
            .Where(prefixes.ContainsKey)
            .ToArray();

        Assert.True(
            declared.Length == 1,
            $"{Path.GetFileName(path)} must declare exactly one mapper that the composition entry maps, found {declared.Length}");
        return declared[0];
    }

    private static string HandlerBody(string text, string handler)
    {
        var declaration = Regex.Match(
            text,
            $@"private static async Task<IResult> {Regex.Escape(handler)}\(",
            RegexOptions.CultureInvariant);
        Assert.True(declaration.Success, $"{handler} has no endpoint handler declaration");

        var tail = text[declaration.Index..];
        var next = Regex.Match(tail[1..], @"\n\s*private static ", RegexOptions.CultureInvariant);
        return next.Success ? tail[..(next.Index + 1)] : tail;
    }

    private static string[] ApplicationRequests() =>
        ApplicationSourceFiles()
            .SelectMany(path => RequestDeclaration().Matches(File.ReadAllText(path)).Select(match => match.Groups["request"].Value))
            .ToArray();

    private static Dictionary<string, string> ApplicationHandlers()
    {
        var handlers = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in ApplicationSourceFiles())
        {
            foreach (Match match in HandlerDeclaration().Matches(File.ReadAllText(path)))
            {
                handlers[match.Groups["request"].Value] = match.Groups["handler"].Value;
            }
        }

        return handlers;
    }

    private static Dictionary<string, string[]> ApplicationValidators()
    {
        var validators = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var path in ApplicationSourceFiles())
        {
            foreach (Match match in ValidatorDeclaration().Matches(File.ReadAllText(path)))
            {
                var target = match.Groups["target"].Value;
                validators[target] = validators.TryGetValue(target, out var existing)
                    ? [.. existing, match.Groups["validator"].Value]
                    : [match.Groups["validator"].Value];
            }
        }

        return validators;
    }

    private static string[] EndpointSourceFiles() =>
        ProductionFiles(Path.Combine("src", "backend", "Modules", "Shipping", "Tooba.Shipping.Endpoints"));

    private static string[] ApplicationSourceFiles() =>
        ProductionFiles(Path.Combine("src", "backend", "Modules", "Shipping", "Tooba.Shipping.Application"));

    private static string[] ProductionFiles(string relative) =>
        Directory
            .EnumerateFiles(Path.Combine(RepoRoot(), relative.Replace('/', Path.DirectorySeparatorChar)), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

    private static Regex RouteRegex() => new(
        @"Map(?<verb>Get|Post|Put|Patch|Delete)\(\s*""(?<path>[^""]+)""\s*,\s*(?<handler>\w+)",
        RegexOptions.CultureInvariant);

    private static Regex GroupDeclaration() => new(
        @"var\s+(?<group>\w+)\s*=\s*app\.MapGroup\(\s*""(?<prefix>[^""]+)""\s*\)",
        RegexOptions.CultureInvariant);

    private static Regex MapperMapping() => new(
        @"(?<mapper>\w+)\.Map\(\s*(?<group>\w+)\s*\)",
        RegexOptions.CultureInvariant);

    private static Regex MapperClassDeclaration() => new(
        @"public static class (?<mapper>\w+)",
        RegexOptions.CultureInvariant);

    private static Regex RequestConstruction() => new(
        @"sender\.Send\(\s*new\s+(?<request>\w+)\s*\(",
        RegexOptions.CultureInvariant);

    private static Regex RequestDeclaration() => new(
        @"public sealed record (?<request>\w+)\s*\([\s\S]{0,600}?\)\s*:\s*IRequest<",
        RegexOptions.CultureInvariant);

    private static Regex HandlerDeclaration() => new(
        @"class (?<handler>\w+Handler)\s*\([\s\S]{0,800}?\bIRequestHandler<(?<request>\w+)\s*,",
        RegexOptions.CultureInvariant);

    private static Regex ValidatorDeclaration() => new(
        @"class (?<validator>\w+Validator)\s*:?\s*AbstractValidator<(?<target>\w+)>",
        RegexOptions.CultureInvariant);

    private static string RepoRoot()
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

    private sealed record ManifestRoute(string Verb, string Path);

    private sealed record ManifestRow(
        string Request,
        IReadOnlyList<ManifestRoute> Routes,
        string Handler,
        string Classification,
        string? Validator);

    private sealed record ShippedRoute(string Verb, string Path, string Request, string Handler, string HandlerSource);
}
