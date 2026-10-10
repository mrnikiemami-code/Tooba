using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.UserPreference.Application.LocalePreferences.Commands;
using Tooba.UserPreference.Application.LocalePreferences.Queries;
using Tooba.UserPreference.Application.Models;
using Tooba.UserPreference.Application.UiPreferences.Commands;
using Tooba.UserPreference.Application.UiPreferences.Queries;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-USERPREFERENCE-AMSC-001-W3-R1 — durable HTTP → request → handler provenance
/// <b>set-equality</b> lock (Certify skill §6a hard gate).
/// <para>
/// The W3 certification guard proved the route count (6), the <c>ISender.Send</c> count (6) and the
/// presence of four request names. Counts are not set equality: a replaced dispatch can preserve every
/// count while silently invalidating the provenance matrix. This guard therefore derives the
/// <b>actual</b> dispatched request type of every shipped route from the endpoint source itself
/// (supporting both <c>Send(new T(...))</c> and <c>Send(variable)</c> by tracing the local
/// construction), fails closed when extraction is ambiguous, and asserts exact equality against the
/// classified partition.
/// </para>
/// <para>
/// This is test/evidence work only: it adds no production change, no new route, no validator and no
/// business rule. The parser is a pure function over source text so the mutation negative proof below
/// can exercise it against a deliberately corrupted copy without touching production files.
/// </para>
/// </summary>
public sealed class UserPreferenceModuleAmsc001W3R1SetEqualityGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/UserPreference";
    private const string ModuleFile = "Tooba.UserPreference.Endpoints/UserPreferenceEndpointModule.cs";
    private const string CustomerFile = "Tooba.UserPreference.Endpoints/Customer/UserPreferenceCustomerEndpoints.cs";
    private const string AdminFile = "Tooba.UserPreference.Endpoints/Admin/UserPreferenceAdminEndpoints.cs";
    private const string UiAdminFile = "Tooba.UserPreference.Endpoints/Admin/UiPreferenceAdminEndpoints.cs";

    private const string CustomerKey = "customer";
    private const string AdminOperatorKey = "admin-operator";
    private const string AdminUiKey = "admin-ui";

    /// <summary>Three transport validators exist; the actor-bearing inputs are validated.</summary>
    private static readonly string[] ValidatorRequired =
    [
        "UpsertUserPreferenceCommand",
        "UpsertUiPreferenceCommand",
        "GetUiPreferenceQuery",
    ];

    /// <summary>
    /// The single exemption: <c>GetUserPreferenceQuery</c> carries only the server-derived actor and no
    /// caller-controlled transport shape (no body, no route parameter, no query string).
    /// </summary>
    private static readonly string[] NoValidatorRequired = ["GetUserPreferenceQuery"];

    /// <summary>Exact expected audience → verb → full route → dispatched request map (6 rows).</summary>
    private static readonly (string Audience, string Verb, string Path, string Request)[] ExpectedRoutes =
    [
        ("customer", "Get", "/v1/customer/preferences/", "GetUserPreferenceQuery"),
        ("customer", "Put", "/v1/customer/preferences/", "UpsertUserPreferenceCommand"),
        ("admin-operator", "Get", "/v1/admin/operator/preferences/", "GetUserPreferenceQuery"),
        ("admin-operator", "Put", "/v1/admin/operator/preferences/", "UpsertUserPreferenceCommand"),
        ("admin-ui", "Get", "/v1/admin/ui-preferences/{key}", "GetUiPreferenceQuery"),
        ("admin-ui", "Put", "/v1/admin/ui-preferences/{key}", "UpsertUiPreferenceCommand"),
    ];

    // ---------------------------------------------------------------------------------------------
    // 1. Independent re-derivation from disk
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Shipped_routes_and_actual_dispatched_requests_match_the_classified_matrix_exactly()
    {
        var root = Repo();

        var derived = DeriveDispatches(root);

        // Exact route → request mapping. This is the assertion a within-set swap must fail.
        var actualMap = derived
            .Select(d => (d.Audience, d.Verb, d.Path, d.Request))
            .OrderBy(x => x.Path, StringComparer.Ordinal)
            .ThenBy(x => x.Verb, StringComparer.Ordinal)
            .ToArray();
        var expectedMap = ExpectedRoutes
            .OrderBy(x => x.Path, StringComparer.Ordinal)
            .ThenBy(x => x.Verb, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(6, derived.Count);
        Assert.Equal(expectedMap.Length, actualMap.Length);
        for (var i = 0; i < expectedMap.Length; i++)
        {
            Assert.Equal(expectedMap[i].Audience, actualMap[i].Audience);
            Assert.Equal(expectedMap[i].Verb, actualMap[i].Verb);
            Assert.Equal(expectedMap[i].Path, actualMap[i].Path);
            Assert.Equal(expectedMap[i].Request, actualMap[i].Request);
        }

        // Every shipped route dispatches through exactly one ISender.Send call site.
        Assert.Equal(6, derived.Count(d => d.SendCount == 1));
    }

    [Fact]
    public void Dispatched_request_set_equals_the_3_required_plus_1_exempt_partition_with_no_orphan_or_duplicate()
    {
        var root = Repo();
        var derived = DeriveDispatches(root);

        var dispatched = derived.Select(d => d.Request).ToArray();
        var classified = ValidatorRequired.Concat(NoValidatorRequired).ToArray();

        // Exact set equality, derived from endpoint code — not adopted from SoT or a static array.
        Assert.Equal(
            classified.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            dispatched.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray());

        // Four distinct request types for six routes: the task must NOT require six distinct requests.
        Assert.Equal(4, dispatched.Distinct(StringComparer.Ordinal).Count());

        // No orphan or unclassified dispatch, and no classified request is left undispatchable.
        Assert.Empty(dispatched.Except(classified, StringComparer.Ordinal));
        Assert.Empty(classified.Except(dispatched, StringComparer.Ordinal));

        // The partition is a real partition: disjoint halves, exact union.
        Assert.Empty(ValidatorRequired.Intersect(NoValidatorRequired, StringComparer.Ordinal));
        Assert.Equal(4, ValidatorRequired.Length + NoValidatorRequired.Length);

        // Two locale requests are genuinely shared across two audiences each (customer + admin-operator).
        Assert.Equal(2, dispatched.Count(x => x == "GetUserPreferenceQuery"));
        Assert.Equal(2, dispatched.Count(x => x == "UpsertUserPreferenceCommand"));
        Assert.Equal(1, dispatched.Count(x => x == "GetUiPreferenceQuery"));
        Assert.Equal(1, dispatched.Count(x => x == "UpsertUiPreferenceCommand"));

        // One-to-one: each dispatched request is declared exactly once with exactly one handler.
        var applicationRoot = Path.Combine(root, ModuleRoot, "Tooba.UserPreference.Application");
        foreach (var request in dispatched.Distinct(StringComparer.Ordinal))
        {
            var declared = Directory.EnumerateFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
                .Count(p => File.ReadAllText(p).Contains($"record {request}", StringComparison.Ordinal));
            Assert.Equal(1, declared);

            var handlers = Directory.EnumerateFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
                .Count(p => File.ReadAllText(p).Contains($"IRequestHandler<{request},", StringComparison.Ordinal));
            Assert.Equal(1, handlers);

            var irequest = Directory.EnumerateFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
                .Count(p => File.ReadAllText(p).Contains($"record {request}(", StringComparison.Ordinal)
                            && File.ReadAllText(p).Contains("IRequest<Result<", StringComparison.Ordinal));
            Assert.Equal(1, irequest);
        }
    }

    [Fact]
    public void Validator_targets_derived_from_concrete_definitions_equal_the_three_required_requests()
    {
        var root = Repo();
        var applicationRoot = Path.Combine(root, ModuleRoot, "Tooba.UserPreference.Application");

        var targets = Directory.EnumerateFiles(applicationRoot, "*Validator.cs", SearchOption.AllDirectories)
            .SelectMany(p => Regex.Matches(
                File.ReadAllText(p), @"class\s+\w+Validator\s*:\s*AbstractValidator<(?<t>\w+)>")
                .Select(m => m.Groups["t"].Value))
            .ToArray();

        Assert.Equal(
            ValidatorRequired.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            targets.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray());

        // No validator exists for the exempt request, and every exempt request is genuinely validator-free.
        foreach (var exempt in NoValidatorRequired)
        {
            Assert.Empty(Directory.EnumerateFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
                .Where(p => File.ReadAllText(p).Contains($"AbstractValidator<{exempt}>", StringComparison.Ordinal)));
        }

        // Validators emit stable machine codes only, never localized text.
        foreach (var file in Directory.EnumerateFiles(applicationRoot, "*Validator.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("WithMessage(", text, StringComparison.Ordinal);
            Assert.Contains("WithErrorCode(", text, StringComparison.Ordinal);
        }
    }

    // ---------------------------------------------------------------------------------------------
    // 2. Provenance re-check for the single exemption
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Exempt_request_rests_on_a_server_derived_actor_and_a_bodyless_route_only()
    {
        var root = Repo();
        var customer = File.ReadAllText(Path.Combine(root, ModuleRoot, CustomerFile));
        var admin = File.ReadAllText(Path.Combine(root, ModuleRoot, AdminFile));
        var uiAdmin = File.ReadAllText(Path.Combine(root, ModuleRoot, UiAdminFile));

        // GetUserPreferenceQuery is only ever constructed from a server-resolved actor. The customer
        // actor resolver never trusts a client-supplied identity outside Development/Testing.
        Assert.Contains("new GetUserPreferenceQuery(actor.Value)", customer, StringComparison.Ordinal);
        Assert.Contains("new GetUserPreferenceQuery(actor)", admin, StringComparison.Ordinal);
        Assert.Contains("actorResolver.ResolveActor(httpContext)", customer, StringComparison.Ordinal);
        Assert.Contains("await adminAuthorizer.RequireAuthorizedAsync(httpContext, cancellationToken)",
            admin, StringComparison.Ordinal);

        // The GET routes bind no client-controlled transport shape at all: no route parameter and no body.
        foreach (var text in new[] { customer, admin })
        {
            var getRoutes = Regex.Matches(text, @"group\.MapGet\(""(?<path>[^""]+)""")
                .Select(m => m.Groups["path"].Value)
                .ToArray();
            Assert.Single(getRoutes);
            Assert.Equal("/", getRoutes[0]);
            Assert.DoesNotContain("{", getRoutes[0], StringComparison.Ordinal);
        }

        // The only client-readable identity shortcut is the module-owned customer actor resolver's
        // Development/Testing-only header with an explicit guest fallback; it must not appear in the
        // admin surfaces.
        var resolver = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.UserPreference.Endpoints/Customer/UserPreferenceCustomerActorResolver.cs"));
        Assert.Contains("X-Tooba-Dev-Actor-User-Id", resolver, StringComparison.Ordinal);
        Assert.Contains("environment.IsDevelopment()", resolver, StringComparison.Ordinal);
        Assert.Contains("IsEnvironment(\"Testing\")", resolver, StringComparison.Ordinal);
        Assert.Contains("currentUser.IsAuthenticated", resolver, StringComparison.Ordinal);
        Assert.Contains("return null;", resolver, StringComparison.Ordinal);
        Assert.DoesNotContain("X-Tooba-Dev-Actor-User-Id", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("X-Tooba-Dev-Actor-User-Id", uiAdmin, StringComparison.Ordinal);

        // The UI GET route binds a route key with no :guid constraint, so a validator is required — and
        // it exists. This is why GetUiPreferenceQuery is VALIDATOR_REQUIRED while GetUserPreferenceQuery
        // is not.
        Assert.Contains("group.MapGet(\"/{key}\", GetAsync)", uiAdmin, StringComparison.Ordinal);
        Assert.Contains("RuleFor(x => x.Key).NotEmpty()", File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.UserPreference.Application/UiPreferences/Validators/GetUiPreferenceQueryValidator.cs")),
            StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------------------
    // 3. Mutation negative proof (in-memory; no production file is modified or committed)
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Replacing_one_dispatched_request_while_preserving_route_and_send_counts_fails_the_exact_map()
    {
        var root = Repo();
        var sources = ReadSources(root);
        var baseline = DeriveFrom(ReadModule(root), sources);

        // The mutation: swap the admin-operator GET dispatch for a request already present in the matrix.
        // Route count stays 6, the ISender.Send count stays 6, and the *set* of dispatched request types
        // stays identical — so a count-only or set-only guard cannot see the corruption. The exact
        // route→request map can.
        var mutatedAdmin = sources[AdminOperatorKey].Replace(
            "new GetUserPreferenceQuery(actor)",
            "new UpsertUserPreferenceCommand(actor, \"en\")",
            StringComparison.Ordinal);
        Assert.NotEqual(sources[AdminOperatorKey], mutatedAdmin);

        var mutatedSources = new Dictionary<string, string>(sources, StringComparer.Ordinal)
        {
            [AdminOperatorKey] = mutatedAdmin,
        };

        // Legacy count check still passes: the demonstrated gap in a count-based guard.
        Assert.Equal(
            Regex.Matches(sources[AdminOperatorKey], @"sender\.Send\(").Count,
            Regex.Matches(mutatedAdmin, @"sender\.Send\(").Count);

        var mutated = DeriveFrom(ReadModule(root), mutatedSources);

        // Route count is unchanged.
        Assert.Equal(baseline.Count, mutated.Count);
        Assert.Equal(6, mutated.Count);

        // Set equality alone is blind to this mutation: the distinct request set is identical.
        Assert.Equal(
            baseline.Select(d => d.Request).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal),
            mutated.Select(d => d.Request).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal));

        // ...but the exact route→request mapping differs, so the exact-map assertion fails.
        Assert.NotEqual(
            baseline.Select(d => (d.Path, d.Request)).OrderBy(x => x.Path, StringComparer.Ordinal).ToArray(),
            mutated.Select(d => (d.Path, d.Request)).OrderBy(x => x.Path, StringComparer.Ordinal).ToArray());

        // The mutation is exactly one row: the admin-operator GET row.
        var before = baseline.Single(d => d.Path == "/v1/admin/operator/preferences/" && d.Verb == "Get").Request;
        var after = mutated.Single(d => d.Path == "/v1/admin/operator/preferences/" && d.Verb == "Get").Request;
        Assert.Equal("GetUserPreferenceQuery", before);
        Assert.Equal("UpsertUserPreferenceCommand", after);
    }

    [Fact]
    public void Untraceable_dispatch_unknown_route_syntax_and_missing_send_all_fail_closed()
    {
        var root = Repo();
        var sources = ReadSources(root);

        // A variable dispatch whose construction cannot be traced must throw, not yield a silent default.
        var untraceable = sources[CustomerKey].Replace(
            "new GetUserPreferenceQuery(actor.Value)",
            "BuildQuery(actor.Value)",
            StringComparison.Ordinal);
        Assert.NotEqual(sources[CustomerKey], untraceable);
        Assert.Throws<InvalidOperationException>(
            () => ParseDispatchedRequest(untraceable, "GetAsync", CustomerKey));

        // A route mapping in a syntax the parser does not recognise must throw (unknown route syntax).
        Assert.Throws<InvalidOperationException>(() => ParseRoutes(
            CustomerKey,
            "public static class X { public static void Map(RouteGroupBuilder group) { group.MapGet(\"/\", async (ISender s) => { }); } }")
            .ToArray());

        // A handler with no ISender.Send at all must also fail closed.
        Assert.Throws<InvalidOperationException>(
            () => ParseDispatchedRequest(
                "private static async Task<IResult> NothingAsync() { return null; }",
                "NothingAsync",
                CustomerKey));

        // A module whose route-group prefix set is not the expected three groups must fail closed.
        Assert.Throws<InvalidOperationException>(() => DeriveFrom(
            "public static class M { public static void X(IEndpointRouteBuilder app) { app.MapGroup(\"/only\"); } }",
            sources));
    }

    // ---------------------------------------------------------------------------------------------
    // 4. Validators are discoverable through the real CQRS registration (non-circular)
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Validators_are_discoverable_through_the_real_cqrs_registration()
    {
        // Non-circular proof: the three transport validators are resolved from a real container built by
        // the canonical foundation registration for the UserPreference Application assembly — not by
        // reflecting over the validator source files.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(UpsertUserPreferenceCommand).Assembly);
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IValidator<UpsertUserPreferenceCommand>>());
        Assert.NotNull(provider.GetService<IValidator<UpsertUiPreferenceCommand>>());
        Assert.NotNull(provider.GetService<IValidator<GetUiPreferenceQuery>>());

        // The exempt request genuinely has no registered validator.
        Assert.Null(provider.GetService<IValidator<GetUserPreferenceQuery>>());

        // And the canonical validation behavior is installed exactly once in the pipeline.
        var behaviors = provider
            .GetServices<MediatR.IPipelineBehavior<UpsertUserPreferenceCommand, Result<PreferenceLocaleResponse>>>()
            .ToArray();
        Assert.Equal(1, behaviors.Count(b => b.GetType().IsGenericType
            && b.GetType().GetGenericTypeDefinition() == typeof(ValidationBehavior<,>)));
    }

    [Fact]
    public void Certification_truth_and_the_w3_wave_evidence_are_preserved()
    {
        var root = Repo();

        // The R1 guard is additive: the W3 certification guard and its evidence remain in place.
        Assert.True(File.Exists(Path.Combine(root,
            "src/backend/Host/Tooba.Host.Tests/Architecture/UserPreferenceModuleAmsc001W3CertGuardTests.cs")));
        Assert.True(Directory.Exists(Path.Combine(
            root, "docs/architecture/evidence/TB-TMAR-USERPREFERENCE-AMSC-001-W3")));

        // No production change was made by this bounded task: the six routes are still module-owned and
        // the Host still owns zero UserPreference routes.
        var module = File.ReadAllText(Path.Combine(root, ModuleRoot, ModuleFile));
        Assert.Equal(3, Regex.Matches(module, @"app\.MapGroup\(").Count);
        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("app.MapUserPreferenceModuleEndpoints();", program, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/v1/customer/preferences\"", program, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/v1/admin/operator/preferences\"", program, StringComparison.Ordinal);
        Assert.DoesNotContain("\"/v1/admin/ui-preferences\"", program, StringComparison.Ordinal);

        // No raw API-result or ad-hoc fault classification in the endpoint surface.
        foreach (var file in new[] { CustomerFile, AdminFile, UiAdminFile })
        {
            var text = File.ReadAllText(Path.Combine(root, ModuleRoot, file));
            Assert.Contains("api.From(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.BadRequest", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Problem", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ProblemDetails", text, StringComparison.Ordinal);
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Deterministic, fail-closed source parser
    // ---------------------------------------------------------------------------------------------

    private sealed record RouteEntry(string Audience, string Verb, string Path, string Handler);

    private sealed record Dispatch(string Audience, string Verb, string Path, string Handler, string Request, int SendCount);

    private static Dictionary<string, string> ReadSources(string root) =>
        new(StringComparer.Ordinal)
        {
            [CustomerKey] = File.ReadAllText(Path.Combine(root, ModuleRoot, CustomerFile)),
            [AdminOperatorKey] = File.ReadAllText(Path.Combine(root, ModuleRoot, AdminFile)),
            [AdminUiKey] = File.ReadAllText(Path.Combine(root, ModuleRoot, UiAdminFile)),
        };

    private static string ReadModule(string root) =>
        File.ReadAllText(Path.Combine(root, ModuleRoot, ModuleFile));

    private static IReadOnlyList<Dispatch> DeriveDispatches(string root) =>
        DeriveFrom(ReadModule(root), ReadSources(root));

    private static IReadOnlyList<Dispatch> DeriveFrom(string moduleSource, Dictionary<string, string> sources)
    {
        var prefixes = Regex.Matches(moduleSource, @"MapGroup\(""(?<prefix>[^""]+)""\)")
            .Select(m => m.Groups["prefix"].Value)
            .ToArray();
        if (prefixes.Length != 3)
        {
            throw new InvalidOperationException(
                $"expected exactly 3 UserPreference route groups, found {prefixes.Length}; failing closed");
        }

        var audiences = new (string Audience, string Prefix, string Key)[]
        {
            (CustomerKey, prefixes[0], CustomerKey),
            (AdminOperatorKey, prefixes[1], AdminOperatorKey),
            (AdminUiKey, prefixes[2], AdminUiKey),
        };

        var dispatches = new List<Dispatch>();
        foreach (var (audience, prefix, key) in audiences)
        {
            if (!sources.TryGetValue(key, out var source))
            {
                throw new InvalidOperationException($"missing source for {key}; failing closed");
            }

            foreach (var route in ParseRoutes(audience, source))
            {
                var body = ExtractHandlerBody(source, route.Handler);
                var sendCount = Regex.Matches(body, @"sender\.Send\(").Count;
                var request = ParseDispatchedRequest(source, route.Handler, key);
                dispatches.Add(new Dispatch(
                    audience, route.Verb, prefix + route.Path, route.Handler, request, sendCount));
            }
        }

        return dispatches;
    }

    private static IEnumerable<RouteEntry> ParseRoutes(string audience, string source)
    {
        var matches = Regex.Matches(
            source,
            @"group\.Map(?<verb>Get|Post|Put|Patch|Delete)\(""(?<path>[^""]+)""\s*,\s*(?<handler>[A-Za-z_][A-Za-z0-9_]*)\)");

        if (matches.Count == 0)
        {
            throw new InvalidOperationException(
                $"{audience}: no route mappings parsed; unknown route syntax cannot be certified");
        }

        foreach (Match match in matches)
        {
            yield return new RouteEntry(
                audience,
                match.Groups["verb"].Value,
                match.Groups["path"].Value,
                match.Groups["handler"].Value);
        }
    }

    /// <summary>
    /// Derives the request type actually passed to <c>ISender.Send</c> inside <paramref name="handler"/>.
    /// Supports both <c>Send(new T(...))</c> and <c>Send(variable)</c> by tracing the local construction.
    /// Fails closed (throws) whenever the dispatch cannot be determined with certainty.
    /// </summary>
    private static string ParseDispatchedRequest(string source, string handler, string file)
    {
        var body = ExtractHandlerBody(source, handler);

        var sendIndex = body.IndexOf("sender.Send(", StringComparison.Ordinal);
        if (sendIndex < 0)
        {
            throw new InvalidOperationException(
                $"{file}:{handler} has no ISender.Send call; dispatch provenance cannot be derived");
        }

        var argument = body[(sendIndex + "sender.Send(".Length)..].TrimStart();

        // Case A — inline construction: Send(new T(...), ...)
        if (argument.StartsWith("new ", StringComparison.Ordinal))
        {
            return ReadTypeName(argument["new ".Length..], file, handler);
        }

        // Case B — variable dispatch: trace the local construction in the same handler body.
        var identifier = ReadIdentifier(argument);
        if (identifier.Length == 0)
        {
            throw new InvalidOperationException(
                $"{file}:{handler} dispatches an unrecognised expression: '{argument[..Math.Min(60, argument.Length)]}'");
        }

        var construction = Regex.Match(
            body,
            $@"(?:var|[A-Za-z_][A-Za-z0-9_<>.,\s]*?)\s+{Regex.Escape(identifier)}\s*=\s*new\s+(?<type>[A-Za-z_][A-Za-z0-9_.]*)",
            RegexOptions.Singleline);
        if (!construction.Success)
        {
            throw new InvalidOperationException(
                $"{file}:{handler} dispatches variable '{identifier}' whose construction cannot be traced; failing closed");
        }

        return construction.Groups["type"].Value;
    }

    private static string ReadTypeName(string tail, string file, string handler)
    {
        var match = Regex.Match(tail, @"^(?<type>[A-Za-z_][A-Za-z0-9_.]*)");
        if (!match.Success)
        {
            throw new InvalidOperationException($"{file}:{handler} has an unreadable constructed request type");
        }

        var type = match.Groups["type"].Value;
        return type.Contains('.') ? type[(type.LastIndexOf('.') + 1)..] : type;
    }

    private static string ReadIdentifier(string tail)
    {
        var match = Regex.Match(tail, @"^(?<id>[A-Za-z_][A-Za-z0-9_]*)");
        return match.Success ? match.Groups["id"].Value : string.Empty;
    }

    /// <summary>Extracts the brace-balanced body of a handler method by its declared name.</summary>
    private static string ExtractHandlerBody(string source, string handler)
    {
        var signature = Regex.Match(
            source,
            $@"[A-Za-z_][A-Za-z0-9_<>.,\s]*?\s{Regex.Escape(handler)}\s*\(");
        if (!signature.Success)
        {
            throw new InvalidOperationException($"handler '{handler}' not found");
        }

        var open = source.IndexOf('{', signature.Index + signature.Length);
        if (open < 0)
        {
            throw new InvalidOperationException($"handler '{handler}' has no body");
        }

        var depth = 0;
        for (var i = open; i < source.Length; i++)
        {
            switch (source[i])
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth == 0)
                    {
                        return source[open..(i + 1)];
                    }

                    break;
            }
        }

        throw new InvalidOperationException($"handler '{handler}' body is not brace-balanced");
    }

    private static string Repo()
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
