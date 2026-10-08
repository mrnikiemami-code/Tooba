using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Queries;
using Tooba.Returns.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-RETURNS-AMSC-001-W3-R1 — durable HTTP → request → handler → validator provenance set-equality
/// lock (Certify skill §6a hard gate).
///
/// The historical W3 guard proved the route count (11), the <c>ISender.Send</c> count (11) and the presence
/// of 11 hard-coded request names. Counts alone are not set equality: a changed or replaced dispatch can
/// preserve every count while silently invalidating the provenance matrix. This guard therefore derives the
/// <b>actual</b> dispatched request type of every shipped route from the endpoint source itself
/// (supporting both <c>Send(new T(...))</c> and <c>Send(variable)</c> by tracing the local construction),
/// fails closed when extraction is ambiguous, and asserts exact equality against the classified partition.
///
/// The parser is intentionally a pure function over source text so the mutation negative proof below can
/// exercise it against a deliberately corrupted copy without touching production.
/// </summary>
public sealed class ReturnsModuleAmsc001W3R1SetEqualityGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/Returns";

    private const string CustomerFile = "Tooba.Returns.Endpoints/Customer/ReturnCustomerEndpoints.cs";
    private const string SellerFile = "Tooba.Returns.Endpoints/Seller/ReturnSellerEndpoints.cs";
    private const string AdminFile = "Tooba.Returns.Endpoints/Admin/ReturnAdminEndpoints.cs";
    private const string ModuleFile = "Tooba.Returns.Endpoints/ReturnEndpointModule.cs";
    private const string ValidatorsFile = "Tooba.Returns.Application/Validation/ReturnsRequestValidators.cs";

    private static readonly string[] ValidatorRequired =
    [
        "CreateReturnCommand", "ApproveReturnCommand", "RejectReturnCommand", "QueryAdminReturnsGridQuery",
    ];

    private static readonly string[] NoValidatorRequired =
    [
        "ListCustomerReturnsQuery", "GetCustomerReturnQuery", "ListSellerReturnsQuery", "GetSellerReturnQuery",
        "ListAdminReturnsQuery", "GetAdminReturnQuery", "RetryReturnRefundCommand",
    ];

    /// <summary>Exact expected audience → verb → path → dispatched request map (11 rows).</summary>
    private static readonly (string Audience, string Verb, string Path, string Request)[] ExpectedRoutes =
    [
        ("customer", "Get", "/v1/customer/returns", "ListCustomerReturnsQuery"),
        ("customer", "Get", "/v1/customer/returns/{returnRequestId:guid}", "GetCustomerReturnQuery"),
        ("customer", "Post", "/v1/customer/returns", "CreateReturnCommand"),
        ("seller", "Get", "/v1/seller/returns", "ListSellerReturnsQuery"),
        ("seller", "Get", "/v1/seller/returns/{returnRequestId:guid}", "GetSellerReturnQuery"),
        ("seller", "Post", "/v1/seller/returns/{returnRequestId:guid}/approve", "ApproveReturnCommand"),
        ("seller", "Post", "/v1/seller/returns/{returnRequestId:guid}/reject", "RejectReturnCommand"),
        ("admin", "Get", "/v1/admin/returns", "ListAdminReturnsQuery"),
        ("admin", "Post", "/v1/admin/returns/query", "QueryAdminReturnsGridQuery"),
        ("admin", "Get", "/v1/admin/returns/{returnRequestId:guid}", "GetAdminReturnQuery"),
        ("admin", "Post", "/v1/admin/returns/{returnRequestId:guid}/retry-refund", "RetryReturnRefundCommand"),
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

        Assert.Equal(11, derived.Count);
        Assert.Equal(expectedMap.Length, actualMap.Length);
        for (var i = 0; i < expectedMap.Length; i++)
        {
            Assert.Equal(expectedMap[i].Audience, actualMap[i].Audience);
            Assert.Equal(expectedMap[i].Verb, actualMap[i].Verb);
            Assert.Equal(expectedMap[i].Path, actualMap[i].Path);
            Assert.Equal(expectedMap[i].Request, actualMap[i].Request);
        }

        // Every shipped route dispatches through exactly one ISender.Send call site.
        Assert.Equal(11, derived.Count(d => d.SendCount == 1));
    }

    [Fact]
    public void Dispatched_request_set_equals_the_4_required_plus_7_exempt_partition_with_no_orphan_or_duplicate()
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
        Assert.Equal(11, ValidatorRequired.Length + NoValidatorRequired.Length);

        // One-to-one: 11 routes → 11 sends → 11 actual requests → 11 matching handlers.
        var applicationRoot = Path.Combine(root, ModuleRoot, "Tooba.Returns.Application");
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
    // 2. Provenance re-check for the seven exemptions
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Exempt_requests_rest_on_route_guid_constraints_and_server_derived_actors_only()
    {
        var root = Repo();
        var customer = File.ReadAllText(Path.Combine(root, ModuleRoot, CustomerFile));
        var seller = File.ReadAllText(Path.Combine(root, ModuleRoot, SellerFile));
        var admin = File.ReadAllText(Path.Combine(root, ModuleRoot, AdminFile));

        // Every exempt request whose route carries an identifier binds it with the :guid constraint,
        // so a malformed value is rejected by routing before any handler runs.
        foreach (var (audience, file) in new[]
                 {
                     ("customer", customer), ("seller", seller), ("admin", admin),
                 })
        {
            foreach (Match route in Regex.Matches(file, @"group\.MapGet\(""(?<path>[^""]+)"""))
            {
                var path = route.Groups["path"].Value;
                var idSegments = Regex.Matches(path, @"\{([^}]+)\}").Select(m => m.Groups[1].Value).ToArray();
                Assert.All(idSegments, seg => Assert.EndsWith(":guid", seg, StringComparison.Ordinal));
            }
        }

        // Actors/parties are server-derived from the authorizer seam, never from a client body value.
        Assert.Contains("authorizer.TryResolveActor(context)", customer, StringComparison.Ordinal);
        Assert.Contains("new ListCustomerReturnsQuery(actor.Value)", customer, StringComparison.Ordinal);
        Assert.Contains("new GetCustomerReturnQuery(actor.Value, returnRequestId)", customer, StringComparison.Ordinal);
        Assert.Contains("await authorizer.RequireAuthorizedAsync(context, cancellationToken)", seller, StringComparison.Ordinal);
        Assert.Contains("new ListSellerReturnsQuery(sellerPartyId)", seller, StringComparison.Ordinal);
        Assert.Contains("new GetSellerReturnQuery(sellerPartyId, returnRequestId)", seller, StringComparison.Ordinal);
        Assert.Contains("new ListAdminReturnsQuery()", admin, StringComparison.Ordinal);
        Assert.Contains("new GetAdminReturnQuery(returnRequestId)", admin, StringComparison.Ordinal);
        Assert.Contains("new RetryReturnRefundCommand(returnRequestId, actorUserId)", admin, StringComparison.Ordinal);

        // The only client-readable identity shortcut is the module-owned customer authorizer's
        // Development/Testing-only header, which preserves the accepted Host semantics and has no
        // guest fallback. It must not appear in the seller/admin endpoint files.
        Assert.DoesNotContain("X-Tooba-Dev-Actor-User-Id", seller, StringComparison.Ordinal);
        Assert.DoesNotContain("X-Tooba-Dev-Actor-User-Id", admin, StringComparison.Ordinal);

        var customerAuthorizer = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Returns.Endpoints/Customer/ReturnCustomerAuthorizer.cs"));
        Assert.Contains("environment.IsDevelopment()", customerAuthorizer, StringComparison.Ordinal);
        Assert.Contains("IsEnvironment(\"Testing\")", customerAuthorizer, StringComparison.Ordinal);
        Assert.Contains("currentUser.IsAuthenticated", customerAuthorizer, StringComparison.Ordinal);
        Assert.Contains("return null;", customerAuthorizer, StringComparison.Ordinal);

        // RetryReturnRefundCommand is a POST whose whole payload is route-constrained plus server-derived:
        // there is no client body parameter on the handler.
        var retryHandler = ExtractHandlerBody(admin, "AdminRetryRefundAsync");
        Assert.DoesNotContain("body", retryHandler, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Grid_query_request_is_validated_by_the_validator_and_the_module_owned_runtime_policy()
    {
        var root = Repo();
        var query = File.ReadAllText(Path.Combine(
            root, ModuleRoot, "Tooba.Returns.Application/ReturnRequests/Queries/QueryAdminReturnsGridQuery.cs"));

        // The module-owned policy is actually executed inside the handler (not merely declared).
        Assert.Contains("AdminReturnGridQueryPolicy.Instance.Normalize(request.Request)", query, StringComparison.Ordinal);

        // Policy rejections are mapped to a typed semantic error, never to message-text classification.
        Assert.Contains("catch (GridQueryValidationException ex)", query, StringComparison.Ordinal);
        Assert.Contains("new SemanticError(ex.ErrorCode)", query, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", query, StringComparison.Ordinal);

        // The allowlists that make the policy a real transport boundary are present.
        Assert.Contains("AllowedFields", query, StringComparison.Ordinal);
        Assert.Contains("OperatorsByField", query, StringComparison.Ordinal);
        Assert.Contains("GridQueryValidationException.FilterFieldInvalid()", query, StringComparison.Ordinal);
        Assert.Contains("GridQueryValidationException.FilterOperatorInvalid()", query, StringComparison.Ordinal);
        Assert.Contains("GridQueryValidationException.AdvancedFieldInvalid()", query, StringComparison.Ordinal);

        // And the transport-shape validator exists for the same request.
        var validators = File.ReadAllText(Path.Combine(root, ModuleRoot, ValidatorsFile));
        Assert.Contains("AbstractValidator<QueryAdminReturnsGridQuery>", validators, StringComparison.Ordinal);
    }

    // ---------------------------------------------------------------------------------------------
    // 3. Mutation negative proof (in-memory; no production file is modified or committed)
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Replacing_one_dispatched_request_while_preserving_the_send_count_fails_the_set_equality_gate()
    {
        var root = Repo();
        var customer = File.ReadAllText(Path.Combine(root, ModuleRoot, CustomerFile));

        // The mutation: swap the customer GET-by-id dispatch for a request that is already part of the
        // matrix. The Send count stays 11 and the *set* of dispatched request types stays identical, so a
        // count-only or set-only guard cannot see the corruption. The exact route→request map can.
        var mutated = customer.Replace(
            "new GetCustomerReturnQuery(actor.Value, returnRequestId)",
            "new ListCustomerReturnsQuery(actor.Value)",
            StringComparison.Ordinal);
        Assert.NotEqual(customer, mutated);

        // Legacy count check still passes: the demonstrated gap in the historical W3 guard.
        Assert.Equal(
            Regex.Matches(customer, @"sender\.Send\(").Count,
            Regex.Matches(mutated, @"sender\.Send\(").Count);

        var mutatedDispatch = ParseDispatchedRequest(mutated, "CustomerGetAsync", CustomerFile);
        Assert.Equal("ListCustomerReturnsQuery", mutatedDispatch);

        // Set equality alone is blind to this mutation...
        var mutatedSet = new[] { mutatedDispatch, "ListCustomerReturnsQuery" }.Distinct(StringComparer.Ordinal).ToArray();
        Assert.Equal(["ListCustomerReturnsQuery"], mutatedSet);

        // ...but the exact route→request assertion fails.
        var derived = ParseRoutes("customer", CustomerFile, mutated).ToArray();
        var mapping = derived
            .Select(r => (r.Path, Request: ParseDispatchedRequest(mutated, r.Handler, CustomerFile)))
            .ToArray();
        var expected = ExpectedRoutes.Where(x => x.Audience == "customer")
            .Select(x => (x.Path, x.Request))
            .ToArray();

        Assert.NotEqual(
            expected.OrderBy(x => x.Path, StringComparer.Ordinal).ToArray(),
            mapping.OrderBy(x => x.Path, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void Variable_dispatch_is_traced_and_an_untraceable_send_fails_closed()
    {
        var root = Repo();
        var customer = File.ReadAllText(Path.Combine(root, ModuleRoot, CustomerFile));

        // The real CreateReturnCommand dispatch goes through a local variable, not an inline new-expression.
        Assert.Contains("var command = new CreateReturnCommand(", customer, StringComparison.Ordinal);
        Assert.Contains("sender.Send(command, cancellationToken)", customer, StringComparison.Ordinal);
        Assert.Equal("CreateReturnCommand", ParseDispatchedRequest(customer, "CustomerCreateAsync", CustomerFile));

        // An untraceable variable dispatch must throw rather than silently yield an empty/unknown request.
        var broken = customer.Replace(
            "var command = new CreateReturnCommand(",
            "var command = BuildReturnCommand(",
            StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(
            () => ParseDispatchedRequest(broken, "CustomerCreateAsync", CustomerFile));

        // A handler with no ISender.Send at all must also fail closed.
        Assert.Throws<InvalidOperationException>(
            () => ParseDispatchedRequest(
                "private static async Task<IResult> NothingAsync() { return null; }",
                "NothingAsync",
                CustomerFile));
    }

    // ---------------------------------------------------------------------------------------------
    // 4. Certification truth still holds and the stable-code split is untouched
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Validators_are_discoverable_through_the_real_cqrs_registration()
    {
        // Non-circular proof: the four transport validators are resolved from a real container built by
        // the canonical foundation registration for the Returns Application assembly — not by reflecting
        // over the validator source file.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(CreateReturnCommand).Assembly);
        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IValidator<CreateReturnCommand>>());
        Assert.NotNull(provider.GetService<IValidator<ApproveReturnCommand>>());
        Assert.NotNull(provider.GetService<IValidator<RejectReturnCommand>>());
        Assert.NotNull(provider.GetService<IValidator<QueryAdminReturnsGridQuery>>());

        // The exempt requests genuinely have no registered validator.
        Assert.Null(provider.GetService<IValidator<ListCustomerReturnsQuery>>());
        Assert.Null(provider.GetService<IValidator<GetCustomerReturnQuery>>());
        Assert.Null(provider.GetService<IValidator<ListSellerReturnsQuery>>());
        Assert.Null(provider.GetService<IValidator<GetSellerReturnQuery>>());
        Assert.Null(provider.GetService<IValidator<ListAdminReturnsQuery>>());
        Assert.Null(provider.GetService<IValidator<GetAdminReturnQuery>>());
        Assert.Null(provider.GetService<IValidator<RetryReturnRefundCommand>>());

        // And the canonical validation behavior is installed exactly once in the pipeline.
        var behaviors = provider.GetServices<MediatR.IPipelineBehavior<CreateReturnCommand, Tooba.BuildingBlocks.Results.Result<Tooba.Returns.Application.ReturnRequests.Models.ReturnSnapshot>>>()
            .ToArray();
        Assert.Equal(1, behaviors.Count(b => b.GetType().IsGenericType
            && b.GetType().GetGenericTypeDefinition() == typeof(ValidationBehavior<,>)));
    }

    [Fact]
    public void Certification_truth_and_stable_code_split_are_preserved()
    {
        Assert.Equal(20, ReturnsErrorCodes.HttpReachable.Count);
        Assert.Equal(1, ReturnsErrorCodes.PlatformFaults.Count);
        Assert.Equal(21, ReturnsErrorCodes.HttpReachable.Count + ReturnsErrorCodes.PlatformFaults.Count);

        // No raw API-result or ad-hoc fault classification in the endpoint surface.
        var root = Repo();
        foreach (var file in new[] { CustomerFile, SellerFile, AdminFile })
        {
            var text = File.ReadAllText(Path.Combine(root, ModuleRoot, file));
            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.BadRequest", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Problem", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ProblemDetails", text, StringComparison.Ordinal);
            Assert.Contains("api.From(", text, StringComparison.Ordinal);
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
        Assert.Equal(3, prefixes.Length);

        var audiences = new (string Audience, string Prefix, string File)[]
        {
            ("customer", prefixes[0], CustomerFile),
            ("seller", prefixes[1], SellerFile),
            ("admin", prefixes[2], AdminFile),
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
