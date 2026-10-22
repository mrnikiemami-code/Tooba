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

    /// <summary>Exact count of the certified endpoint-reachable request universe (W3 gate + TB-188 additive read).</summary>
    private const int CertifiedRequestCount = 17;

    [Fact]
    public void Shipped_route_universe_equals_the_classified_manifest_exactly()
    {
        var rows = ManifestRows();
        var shipped = ShippedRoutes();

        // Exact set equality, both directions: no unclassified live route, no phantom manifest route.
        Assert.Equal(
            rows.Select(row => $"{row.Verb} {row.Path}").OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            shipped.Select(route => $"{route.Verb} {route.Path}").OrderBy(x => x, StringComparer.Ordinal).ToArray());

        // Each route pattern and each request is classified exactly once; duplicates are not "coverage".
        Assert.Equal(rows.Count, rows.Select(row => $"{row.Verb} {row.Path}").Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(rows.Count, rows.Select(row => row.Request).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(CertifiedRequestCount, rows.Count);
    }

    [Fact]
    public void Every_shipped_route_dispatches_through_ISender_to_its_classified_request()
    {
        var rows = ManifestRows();
        foreach (var route in ShippedRoutes())
        {
            var row = rows.Single(candidate => candidate.Verb == route.Verb && candidate.Path == route.Path);
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
        var reachable = ShippedRoutes().Select(route => route.Request).OrderBy(x => x, StringComparer.Ordinal).ToArray();
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
            // the route template carries no parameter, and the handler binds no route/query/body.
            Assert.Equal(NoValidatorRequired, row.Classification);
            Assert.Null(row.Validator);
            Assert.False(validators.ContainsKey(row.Request), $"{row.Request} declares NO_VALIDATOR_REQUIRED but has a validator");
            Assert.DoesNotContain("{", row.Path, StringComparison.Ordinal);

            var route = shipped.Single(candidate => candidate.Verb == row.Verb && candidate.Path == row.Path);
            Assert.DoesNotContain("context.Request.Query", route.HandlerSource, StringComparison.Ordinal);
            Assert.DoesNotContain("ReadFromJsonAsync", route.HandlerSource, StringComparison.Ordinal);
            Assert.DoesNotContain("[FromRoute]", route.HandlerSource, StringComparison.Ordinal);
            Assert.DoesNotContain("[FromQuery]", route.HandlerSource, StringComparison.Ordinal);
            Assert.Contains($"new {row.Request}()", route.HandlerSource, StringComparison.Ordinal);
        }
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

        // The single authorized additive read is the only exception; every other row keeps a validator.
        Assert.Equal(new[] { "GetShippingAuthoringContextQuery" }, declared);
        Assert.Equal(17, ManifestRows().Count);
        Assert.Equal(16, ManifestRows().Count(row => row.Classification == ValidatorRequired));
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
        var classifiedRoutes = rows.Select(row => $"{row.Verb} {row.Path}").ToHashSet(StringComparer.Ordinal);
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
                item.GetProperty("route").GetString()!.Split(' ', 2)[0],
                item.GetProperty("route").GetString()!.Split(' ', 2)[1],
                item.GetProperty("handler").GetString()!,
                item.GetProperty("classification").GetString()!,
                item.TryGetProperty("validator", out var validator) ? validator.GetString() : null))
            .ToArray();
    }

    private static JsonDocument ManifestDocument()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot(), ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        return JsonDocument.Parse(text);
    }

    private static IReadOnlyList<ShippedRoute> ShippedRoutes()
    {
        var routes = new List<ShippedRoute>();
        foreach (var path in EndpointSourceFiles())
        {
            var text = File.ReadAllText(path);
            foreach (Match match in RouteRegex().Matches(text))
            {
                var handler = match.Groups["handler"].Value;
                var body = HandlerBody(text, handler);
                var request = RequestConstruction().Match(body);
                Assert.True(request.Success, $"{Path.GetFileName(path)}:{handler} does not dispatch a classified request");
                routes.Add(new ShippedRoute(
                    match.Groups["verb"].Value.ToUpperInvariant(),
                    $"/v1/admin{match.Groups["path"].Value}",
                    request.Groups["request"].Value,
                    handler,
                    body));
            }
        }

        return routes;
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

    private sealed record ManifestRow(string Request, string Verb, string Path, string Handler, string Classification, string? Validator);

    private sealed record ShippedRoute(string Verb, string Path, string Request, string Handler, string HandlerSource);
}
