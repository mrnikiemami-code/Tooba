using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Settlement.Application.Payouts.Commands;
using Tooba.Settlement.Application.Payouts.Ports;
using Tooba.Settlement.Application.Payouts.Queries;
using Tooba.Settlement.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-SETTLEMENT-AMSC-001-W3-R1 — durable HTTP → request → handler → validator provenance set-equality
/// lock (Certify skill §6a hard gate).
///
/// The historical W3 guard proved the route count (10), the <c>ISender.Send</c> count (10) and the presence
/// of 10 hard-coded request names. Counts alone are not set equality: a changed or replaced dispatch can
/// preserve every count while silently invalidating the provenance matrix. This guard therefore derives the
/// <b>actual</b> dispatched request type of every shipped route from the endpoint source itself (supporting
/// both <c>Send(new T(...))</c> and <c>Send(variable)</c> by tracing the local construction), fails closed
/// when extraction is ambiguous, and asserts exact equality against the classified partition.
///
/// The parser is intentionally a pure function over source text so the mutation negative proof below can
/// exercise it against a deliberately corrupted copy without touching production.
/// </summary>
public sealed class SettlementModuleAmsc001W3R1SetEqualityGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Settlement";

    private const string SellerFile = "Tooba.Settlement.Endpoints/Seller/SettlementSellerEndpoints.cs";
    private const string AdminFile = "Tooba.Settlement.Endpoints/Admin/SettlementAdminEndpoints.cs";
    private const string ModuleFile = "Tooba.Settlement.Endpoints/SettlementEndpointModule.cs";
    private const string ValidatorsFile = "Tooba.Settlement.Application/Validation/SettlementRequestValidators.cs";
    private const string GridQueryFile = "Tooba.Settlement.Application/Payouts/Queries/QueryAdminPayoutGridQuery.cs";
    private const string GridPolicyFile = "Tooba.Settlement.Application/Payouts/Queries/AdminPayoutGridQueryPolicy.cs";

    private static readonly string[] ValidatorRequired =
    [
        "RequestSellerPayoutCommand", "ProcessAdminPayoutCommand", "RetryAdminPayoutCommand",
        "QueryAdminPayoutGridQuery",
    ];

    private static readonly string[] NoValidatorRequired =
    [
        "GetSellerSettlementBalanceQuery", "ListSellerSettlementEntriesQuery",
        "ListSellerSettlementStatementsQuery", "ListSellerPayoutRequestsQuery",
        "ListAdminSettlementBalancesQuery", "ListAdminPayoutQueueQuery",
    ];

    /// <summary>Exact expected audience → verb → path → dispatched request map (10 rows).</summary>
    private static readonly (string Audience, string Verb, string Path, string Request)[] ExpectedRoutes =
    [
        ("seller", "Get", "/v1/seller/settlement/balance", "GetSellerSettlementBalanceQuery"),
        ("seller", "Get", "/v1/seller/settlement/entries", "ListSellerSettlementEntriesQuery"),
        ("seller", "Get", "/v1/seller/settlement/statements", "ListSellerSettlementStatementsQuery"),
        ("seller", "Get", "/v1/seller/settlement/payout-requests", "ListSellerPayoutRequestsQuery"),
        ("seller", "Post", "/v1/seller/settlement/payout-requests", "RequestSellerPayoutCommand"),
        ("admin", "Get", "/v1/admin/settlement/balances", "ListAdminSettlementBalancesQuery"),
        ("admin", "Get", "/v1/admin/settlement/payout-queue", "ListAdminPayoutQueueQuery"),
        ("admin", "Post", "/v1/admin/settlement/payout-queue/query", "QueryAdminPayoutGridQuery"),
        ("admin", "Post", "/v1/admin/settlement/payout-requests/{payoutRequestId:guid}/process", "ProcessAdminPayoutCommand"),
        ("admin", "Post", "/v1/admin/settlement/payout-requests/{payoutRequestId:guid}/retry", "RetryAdminPayoutCommand"),
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

        Assert.Equal(10, derived.Count);
        Assert.Equal(expectedMap.Length, actualMap.Length);
        for (var i = 0; i < expectedMap.Length; i++)
        {
            Assert.Equal(expectedMap[i].Audience, actualMap[i].Audience);
            Assert.Equal(expectedMap[i].Verb, actualMap[i].Verb);
            Assert.Equal(expectedMap[i].Path, actualMap[i].Path);
            Assert.Equal(expectedMap[i].Request, actualMap[i].Request);
        }

        // Every shipped route dispatches through exactly one ISender.Send call site.
        Assert.Equal(10, derived.Count(d => d.SendCount == 1));
    }

    [Fact]
    public void Dispatched_request_set_equals_the_4_required_plus_6_exempt_partition_with_no_orphan_or_duplicate()
    {
        var root = Repo();
        var derived = DeriveDispatches(root);

        var dispatched = derived.Select(d => d.Request).ToArray();
        var classified = ValidatorRequired.Concat(NoValidatorRequired).ToArray();

        // Exact set equality, derived from endpoint code — not adopted from SoT or a static array.
        Assert.Equal(
            classified.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            dispatched.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray());

        // No duplicate, orphan or unclassified dispatch.
        Assert.Equal(dispatched.Length, dispatched.Distinct(StringComparer.Ordinal).Count());
        Assert.Empty(dispatched.Except(classified, StringComparer.Ordinal));
        Assert.Empty(classified.Except(dispatched, StringComparer.Ordinal));

        // The partition is a real partition: disjoint halves, exact union.
        Assert.Empty(ValidatorRequired.Intersect(NoValidatorRequired, StringComparer.Ordinal));
        Assert.Equal(10, ValidatorRequired.Length + NoValidatorRequired.Length);

        // One-to-one: 10 routes → 10 sends → 10 actual requests → 10 matching handlers.
        var applicationRoot = Path.Combine(root, ModuleRoot, "Tooba.Settlement.Application");
        foreach (var request in dispatched)
        {
            var declared = Directory.EnumerateFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
                .Count(p => File.ReadAllText(p).Contains($"record {request}", StringComparison.Ordinal));
            Assert.Equal(1, declared);

            var handlers = Directory.EnumerateFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
                .Count(p => File.ReadAllText(p).Contains($"IRequestHandler<{request},", StringComparison.Ordinal));
            Assert.Equal(1, handlers);
        }
    }

    [Fact]
    public void Validator_targets_derived_from_concrete_definitions_equal_the_four_required_requests()
    {
        var root = Repo();
        var validators = File.ReadAllText(Path.Combine(root, ModuleRoot, ValidatorsFile));

        var targets = Regex.Matches(validators, @"class\s+\w+Validator\s*:\s*AbstractValidator<(\w+)>")
            .Select(m => m.Groups[1].Value)
            .ToArray();

        Assert.Equal(
            ValidatorRequired.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            targets.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray());

        // No validator exists for an exempt request, and every exempt request is genuinely validator-free.
        foreach (var exempt in NoValidatorRequired)
        {
            Assert.DoesNotContain($"AbstractValidator<{exempt}>", validators, StringComparison.Ordinal);
        }

        // Validators emit stable machine codes only.
        Assert.DoesNotContain("WithMessage(", validators, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------------------
    // 2. Provenance re-check for the six exemptions
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Exempt_requests_rest_on_route_guid_constraints_and_server_derived_actors_only()
    {
        var root = Repo();
        var seller = File.ReadAllText(Path.Combine(root, ModuleRoot, SellerFile));
        var admin = File.ReadAllText(Path.Combine(root, ModuleRoot, AdminFile));

        // Every identifier route segment in the module carries the :guid constraint, so a malformed value
        // is rejected by routing before any handler runs.
        foreach (var (_, file) in new[] { ("seller", seller), ("admin", admin) })
        {
            foreach (Match route in Regex.Matches(file, @"group\.Map(Get|Post|Put|Patch|Delete)\(""(?<path>[^""]+)"""))
            {
                var path = route.Groups["path"].Value;
                var idSegments = Regex.Matches(path, @"\{([^}]+)\}").Select(m => m.Groups[1].Value).ToArray();
                Assert.All(idSegments, seg => Assert.EndsWith(":guid", seg, StringComparison.Ordinal));
            }
        }

        // The four AUTH_SCOPED_QUERY seller requests bind their only argument from the module authorizer
        // seam (server-derived), never from a client body.
        Assert.Contains("await authorizer.RequireAuthorizedAsync(context, cancellationToken)", seller, StringComparison.Ordinal);
        Assert.Contains("new GetSellerSettlementBalanceQuery(sellerPartyId)", seller, StringComparison.Ordinal);
        Assert.Contains("new ListSellerSettlementEntriesQuery(sellerPartyId)", seller, StringComparison.Ordinal);
        Assert.Contains("new ListSellerSettlementStatementsQuery(sellerPartyId)", seller, StringComparison.Ordinal);
        Assert.Contains("new ListSellerPayoutRequestsQuery(sellerPartyId)", seller, StringComparison.Ordinal);

        // The two NO_INPUT admin requests are parameterless — no bound input exists at all.
        Assert.Contains("new ListAdminSettlementBalancesQuery()", admin, StringComparison.Ordinal);
        Assert.Contains("new ListAdminPayoutQueueQuery()", admin, StringComparison.Ordinal);

        // The admin authorizer seam returns a single server-derived actor id.
        Assert.Contains("await authorizer.RequireAuthorizedAsync(context, cancellationToken)", admin, StringComparison.Ordinal);

        // No client-readable identity shortcut exists in either endpoint file.
        Assert.DoesNotContain("X-Tooba-Dev-Actor-User-Id", seller, StringComparison.Ordinal);
        Assert.DoesNotContain("X-Tooba-Dev-Actor-User-Id", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpContext.Request.Headers", seller, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpContext.Request.Headers", admin, StringComparison.Ordinal);
    }

    [Fact]
    public void Grid_query_request_is_validated_by_the_validator_and_the_module_owned_runtime_policy()
    {
        var root = Repo();
        var query = File.ReadAllText(Path.Combine(root, ModuleRoot, GridQueryFile));

        // The module-owned policy is actually executed inside the handler (not merely declared).
        Assert.Contains("AdminPayoutGridQueryPolicy.Instance.Normalize(request.Request)", query, StringComparison.Ordinal);

        // Policy rejections are mapped to a typed semantic error, never to message-text classification.
        Assert.Contains("catch (GridQueryValidationException ex)", query, StringComparison.Ordinal);
        Assert.Contains("new SemanticError(ex.ErrorCode)", query, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", query, StringComparison.Ordinal);

        // The allowlists that make the policy a real transport boundary are present.
        var policy = File.ReadAllText(Path.Combine(root, ModuleRoot, GridPolicyFile));
        Assert.Contains("AllowedFields", policy, StringComparison.Ordinal);
        Assert.Contains("OperatorsByField", policy, StringComparison.Ordinal);
        Assert.Contains("GridQueryValidationException.FilterFieldInvalid()", policy, StringComparison.Ordinal);
        Assert.Contains("GridQueryValidationException.FilterOperatorInvalid()", policy, StringComparison.Ordinal);
        Assert.Contains("GridQueryValidationException.AdvancedFieldInvalid()", policy, StringComparison.Ordinal);

        // And the transport-shape validator exists for the same request.
        var validatorSource = File.ReadAllText(Path.Combine(root, ModuleRoot, ValidatorsFile));
        Assert.Contains("AbstractValidator<QueryAdminPayoutGridQuery>", validatorSource, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------------------
    // 3. Mutation negative proof (in-memory; no production file is modified or committed)
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Replacing_one_dispatched_request_while_preserving_the_send_count_fails_the_set_equality_gate()
    {
        var root = Repo();
        var seller = File.ReadAllText(Path.Combine(root, ModuleRoot, SellerFile));

        // The mutation: swap a seller GET dispatch for a request that is already part of the matrix. The
        // Send count stays 10 and the *set* of dispatched request types stays identical, so a count-only or
        // set-only guard cannot see the corruption. The exact route→request map can.
        var mutated = seller.Replace(
            "new ListSellerSettlementEntriesQuery(sellerPartyId)",
            "new ListSellerPayoutRequestsQuery(sellerPartyId)",
            StringComparison.Ordinal);
        Assert.NotEqual(seller, mutated);

        // Legacy count check still passes: the demonstrated gap in the historical W3 guard.
        Assert.Equal(
            Regex.Matches(seller, @"sender\.Send\(").Count,
            Regex.Matches(mutated, @"sender\.Send\(").Count);

        var mutatedDispatch = ParseDispatchedRequest(mutated, "SellerEntriesAsync", SellerFile);
        Assert.Equal("ListSellerPayoutRequestsQuery", mutatedDispatch);

        // Set equality alone is blind to this mutation...
        var mutatedSet = new[] { mutatedDispatch, "ListSellerPayoutRequestsQuery" }
            .Distinct(StringComparer.Ordinal).ToArray();
        Assert.Equal(["ListSellerPayoutRequestsQuery"], mutatedSet);

        // ...but the exact route→request assertion fails.
        var derived = ParseRoutes("seller", SellerFile, mutated).ToArray();
        var mapping = derived
            .Select(r => (r.Path, Request: ParseDispatchedRequest(mutated, r.Handler, SellerFile)))
            .ToArray();
        var expected = ExpectedRoutes.Where(x => x.Audience == "seller")
            .Select(x => (x.Path, x.Request))
            .ToArray();

        Assert.NotEqual(
            expected.OrderBy(x => x.Path, StringComparer.Ordinal).ToArray(),
            mapping.OrderBy(x => x.Path, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void Inline_dispatch_is_read_directly_and_an_untraceable_variable_send_fails_closed()
    {
        var root = Repo();
        var seller = File.ReadAllText(Path.Combine(root, ModuleRoot, SellerFile));

        // Every shipped Settlement dispatch is an inline construction read directly from the Send argument.
        Assert.DoesNotContain("var command = new", seller, StringComparison.Ordinal);
        Assert.Equal("RequestSellerPayoutCommand", ParseDispatchedRequest(seller, "SellerRequestPayoutAsync", SellerFile));

        // A variable dispatch (the other supported shape) is traced to its local construction.
        const string variableSource = """
            private static async Task<IResult> VariableAsync(ISender sender)
            {
                var command = new RequestSellerPayoutCommand(Guid.Empty, Guid.Empty, 1m, "k");
                return await sender.Send(command);
            }
            """;
        Assert.Equal("RequestSellerPayoutCommand", ParseDispatchedRequest(variableSource, "VariableAsync", "synthetic"));

        // ...but an untraceable variable dispatch must throw rather than silently yield an unknown request.
        var untraceable = variableSource.Replace(
            "new RequestSellerPayoutCommand(", "BuildPayoutCommand(", StringComparison.Ordinal);
        Assert.NotEqual(variableSource, untraceable);
        Assert.Throws<InvalidOperationException>(
            () => ParseDispatchedRequest(untraceable, "VariableAsync", "synthetic"));

        // A handler with no ISender.Send at all must also fail closed.
        Assert.Throws<InvalidOperationException>(
            () => ParseDispatchedRequest(
                "private static async Task<IResult> NothingAsync() { return null; }",
                "NothingAsync",
                "synthetic"));
    }

    // ---------------------------------------------------------------------------------------------
    // 4. Certification truth still holds and the stable-code split is untouched
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Validators_are_discoverable_through_the_real_cqrs_registration()
    {
        // Non-circular proof: the four transport validators are resolved from a real container built by
        // the canonical foundation registration for the Settlement Application assembly — not by reflecting
        // over the validator source file.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(RequestSellerPayoutCommand).Assembly);
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IValidator<RequestSellerPayoutCommand>>());
        Assert.NotNull(provider.GetService<IValidator<ProcessAdminPayoutCommand>>());
        Assert.NotNull(provider.GetService<IValidator<RetryAdminPayoutCommand>>());
        Assert.NotNull(provider.GetService<IValidator<QueryAdminPayoutGridQuery>>());

        // The exempt requests genuinely have no registered validator.
        Assert.Null(provider.GetService<IValidator<GetSellerSettlementBalanceQuery>>());
        Assert.Null(provider.GetService<IValidator<ListSellerSettlementEntriesQuery>>());
        Assert.Null(provider.GetService<IValidator<ListSellerSettlementStatementsQuery>>());
        Assert.Null(provider.GetService<IValidator<ListSellerPayoutRequestsQuery>>());
        Assert.Null(provider.GetService<IValidator<ListAdminSettlementBalancesQuery>>());
        Assert.Null(provider.GetService<IValidator<ListAdminPayoutQueueQuery>>());

        // And the canonical validation behavior is installed exactly once in the pipeline.
        var behaviors = provider.GetServices<MediatR.IPipelineBehavior<RequestSellerPayoutCommand, Result<PayoutRequestSnapshot>>>()
            .ToArray();
        Assert.Equal(1, behaviors.Count(b => b.GetType().IsGenericType
            && b.GetType().GetGenericTypeDefinition() == typeof(ValidationBehavior<,>)));
    }

    [Fact]
    public void Certification_truth_and_stable_code_split_are_preserved()
    {
        Assert.Equal(17, SettlementErrorCodes.HttpReachable.Count);
        Assert.Equal(1, SettlementErrorCodes.PlatformFaults.Count);
        Assert.Equal(18, SettlementErrorCodes.HttpReachable.Count + SettlementErrorCodes.PlatformFaults.Count);

        // No raw API-result or ad-hoc fault classification in the endpoint surface.
        var root = Repo();
        foreach (var file in new[] { SellerFile, AdminFile })
        {
            var text = File.ReadAllText(Path.Combine(root, ModuleRoot, file));
            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.BadRequest", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Problem", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ProblemDetails", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ex.Message", text, StringComparison.Ordinal);
            Assert.Contains("api.From(", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Manifest_root_rule_coverage_is_intentional_and_not_widened_for_numeric_equality()
    {
        var root = Repo();

        using var manifest = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root, "docs/architecture/tmar-module-structure-manifests.json")));
        var entry = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Single(m => string.Equals(m.GetProperty("module").GetString(), "Settlement", StringComparison.Ordinal));
        var manifested = entry.GetProperty("projects").EnumerateArray()
            .Select(p => p.GetProperty("projectName").GetString()!)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        // The three manifested projects are exactly the ones that carry root-rule coverage.
        Assert.Equal(
            ["Tooba.Settlement.Application", "Tooba.Settlement.Endpoints", "Tooba.Settlement.Infrastructure"],
            manifested);

        // The six solution projects intentionally exceed the three manifested projects; the manifest is a
        // root-rule map, not a project inventory, so it must not be widened to force numeric equality.
        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        var block = Regex.Match(slnx, @"<Folder Name=""/Modules/Settlement/"">(?<body>.*?)</Folder>", RegexOptions.Singleline)
            .Groups["body"].Value;
        var solutionProjects = Regex.Matches(block, @"Modules/Settlement/(?<name>[^/]+)/")
            .Select(m => m.Groups["name"].Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(6, solutionProjects.Length);
        Assert.Equal(3, manifested.Length);

        // Every manifested project is a real solution project (root-rule map ⊆ project inventory).
        foreach (var project in manifested)
        {
            Assert.Contains(project, solutionProjects, StringComparer.Ordinal);
        }

        Assert.Contains("Tooba.Settlement.Domain", solutionProjects, StringComparer.Ordinal);
        Assert.Contains("Tooba.Settlement.Contracts", solutionProjects, StringComparer.Ordinal);
        Assert.Contains("Tooba.Settlement.Tests", solutionProjects, StringComparer.Ordinal);

        foreach (var project in new[] { "Tooba.Settlement.Domain", "Tooba.Settlement.Contracts" })
        {
            var projectRoot = Path.Combine(root, ModuleRoot, project);
            Assert.Empty(Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.TopDirectoryOnly));
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Deterministic, fail-closed source parser
    // ---------------------------------------------------------------------------------------------

    private sealed record RouteEntry(string Audience, string Verb, string Path, string Handler);

    private sealed record Dispatch(string Audience, string Verb, string Path, string Handler, string Request, int SendCount);

    private static IReadOnlyList<Dispatch> DeriveDispatches(string root)
    {
        var module = File.ReadAllText(Path.Combine(root, ModuleRoot, ModuleFile));
        var prefixes = Regex.Matches(module, @"MapGroup\(""(?<prefix>[^""]+)""\)")
            .Select(m => m.Groups["prefix"].Value)
            .ToArray();
        Assert.Equal(2, prefixes.Length);

        var audiences = new (string Audience, string Prefix, string File)[]
        {
            ("seller", prefixes[0], SellerFile),
            ("admin", prefixes[1], AdminFile),
        };

        var dispatches = new List<Dispatch>();
        foreach (var (audience, prefix, file) in audiences)
        {
            var source = File.ReadAllText(Path.Combine(root, ModuleRoot, file));
            foreach (var route in ParseRoutes(audience, file, source))
            {
                var body = ExtractHandlerBody(source, route.Handler);
                var sendCount = Regex.Matches(body, @"sender\.Send\(").Count;
                var request = ParseDispatchedRequest(source, route.Handler, file);
                dispatches.Add(new Dispatch(
                    audience, route.Verb, prefix + route.Path, route.Handler, request, sendCount));
            }
        }

        return dispatches;
    }

    private static IEnumerable<RouteEntry> ParseRoutes(string audience, string file, string source)
    {
        var matches = Regex.Matches(
            source,
            @"group\.Map(?<verb>Get|Post|Put|Patch|Delete)\(""(?<path>[^""]+)""\s*,\s*(?<handler>[A-Za-z_][A-Za-z0-9_]*)\)");

        if (matches.Count == 0)
        {
            throw new InvalidOperationException($"no route mappings parsed from {file}");
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
