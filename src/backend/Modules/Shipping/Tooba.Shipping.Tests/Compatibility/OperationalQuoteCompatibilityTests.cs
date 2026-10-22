using Xunit;

namespace Tooba.Shipping.Tests.Compatibility;

/// <summary>
/// Shipping operational-quote boundary guards.
/// </summary>
/// <remarks>
/// <para>
/// <b>History.</b> TB-SHIPPING-FULL-OPERATIONS-076 introduced this class when the operational quote seam
/// was brand new and had no external consumer at all; its assertion
/// <c>No_module_outside_shipping_references_the_operational_quote_types</c> therefore expressed a
/// <em>temporary</em> "nobody outside Shipping may mention these types" rule.
/// </para>
/// <para>
/// <b>Why that rule became stale.</b> TB-079 Mission A explicitly authorized a real, read-only,
/// default-OFF operational shadow on the Order storefront shipping projection through
/// <c>Tooba.Shipping.Contracts</c> only, and TB-080 recorded that bounded scope as accepted. The
/// historical blanket prohibition therefore contradicts an approved architecture decision. It is replaced
/// here — not deleted — by the narrower and stronger invariant those decisions approved:
/// </para>
/// <list type="bullet">
///   <item>only the reviewed observation/comparison pair plus the single Order DI registration may
///   consume the operational Contracts; any fourth production consumer fails;</item>
///   <item>no module outside Shipping may reference Shipping Domain/Application/Infrastructure/Endpoints
///   implementation types — including <em>inside</em> the three approved files;</item>
///   <item>the Order edge stays registration-only and observation-only (no quote call, no observation call
///   from the composition root);</item>
///   <item>the approved observation files use public Contracts only, never an implementation import;</item>
///   <item>the gate defaults stay OFF and the real projection keeps discarding the observation, so nothing
///   reaches customer amount, payment draft, paid snapshot, delivery promise or capacity.</item>
/// </list>
/// <para>
/// The detector is deliberately separate from the file enumeration so the negative cases below can prove it
/// fails on a forbidden expansion instead of merely asserting that today's allowed files exist. String
/// matching is paired with exact source review; it is not claimed as a general data-flow proof.
/// </para>
/// </remarks>
public sealed class OperationalQuoteCompatibilityTests
{
    [Fact]
    public void Only_the_reviewed_order_observation_files_consume_the_operational_contracts()
    {
        var sources = BackendProductionSources();

        var consumers = sources
            .Where(source => !OperationalContractBoundary.IsShippingModule(source.RelativePath))
            .Where(OperationalContractBoundary.ConsumesOperationalContracts)
            .Select(source => source.RelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        // The reviewed set is source-proven: TB-079 Mission A authorized exactly this observation pair and
        // the single Order composition edge; TB-080 accepted that bounded scope.
        Assert.Equal(
            OperationalContractBoundary.ApprovedConsumers.OrderBy(path => path, StringComparer.Ordinal).ToArray(),
            consumers);

        // No unexpected consumer and no implementation reference anywhere outside Shipping.
        Assert.Empty(OperationalContractBoundary.Analyze(sources));
    }

    [Fact]
    public void Approved_observation_files_use_only_public_contracts_and_never_an_implementation_import()
    {
        var sources = BackendProductionSources().ToDictionary(source => source.RelativePath, StringComparer.Ordinal);

        foreach (var path in new[]
                 {
                     OperationalContractBoundary.ApprovedComparerPath,
                     OperationalContractBoundary.ApprovedObserverPath,
                 })
        {
            var text = sources[path].Text;

            // Every Shipping import in the observation pair must be the public Contracts surface.
            foreach (var line in text.Split('\n'))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("using Tooba.Shipping.", StringComparison.Ordinal))
                {
                    continue;
                }

                Assert.StartsWith("using Tooba.Shipping.Contracts.", trimmed, StringComparison.Ordinal);
            }

            // Contracts-only observation: no evaluator/engine/persistence type may be reached from here.
            Assert.Empty(OperationalContractBoundary.ImplementationReferences(text));
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IQueryable", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_order_module_edge_is_registration_only()
    {
        var sources = BackendProductionSources().ToDictionary(source => source.RelativePath, StringComparer.Ordinal);
        var registration = sources[OperationalContractBoundary.ApprovedRegistrationPath].Text;

        // The only operational Contracts reference is the DI factory argument.
        var operationalLines = CodeLines(registration)
            .Where(OperationalContractBoundary.ConsumesOperationalContracts)
            .ToArray();
        Assert.NotEmpty(operationalLines);
        Assert.All(operationalLines, line => Assert.Contains("GetService<", line, StringComparison.Ordinal));

        // Composition must not become an execution path.
        Assert.DoesNotContain("QuoteAsync(", registration, StringComparison.Ordinal);
        Assert.DoesNotContain(".ObserveAsync(", registration, StringComparison.Ordinal);
        Assert.DoesNotContain("ShippingOperationalQuoteRequest", registration, StringComparison.Ordinal);

        // The registration is unique: one factory for the observer, one options section.
        Assert.Equal(
            1,
            CountOccurrences(
                registration,
                "new Application.Storefront.Shipping.OperationalShadow.StorefrontShippingOperationalShadowObserver("));
        Assert.Contains(
            "StorefrontShippingOperationalShadowOptions.SectionName",
            registration,
            StringComparison.Ordinal);
    }

    [Fact]
    public void The_real_projection_discards_the_observation_and_the_gate_stays_off_by_default()
    {
        var sources = BackendProductionSources().ToDictionary(source => source.RelativePath, StringComparer.Ordinal);

        var options = sources[
            "src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/OperationalShadow/StorefrontShippingOperationalShadowOptions.cs"].Text;
        Assert.Contains("Tooba:ShippingOperationalShadow", options, StringComparison.Ordinal);
        Assert.Contains("public bool Enabled { get; set; }", options, StringComparison.Ordinal);
        Assert.Contains("public bool PrerequisitesConfirmed { get; set; }", options, StringComparison.Ordinal);
        Assert.DoesNotContain("Enabled { get; set; } = true", options, StringComparison.Ordinal);
        Assert.DoesNotContain("PrerequisitesConfirmed { get; set; } = true", options, StringComparison.Ordinal);

        var projection = sources[
            "src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/Queries/ProjectStorefrontShipping/ProjectStorefrontShippingQuery.cs"].Text;

        // The observation is awaited and its value is never bound to anything.
        Assert.Contains("await operationalShadow.ObserveAsync(", projection, StringComparison.Ordinal);
        Assert.DoesNotContain("= await operationalShadow.ObserveAsync(", projection, StringComparison.Ordinal);

        // No path from observation to the customer-facing result.
        Assert.Contains("return result;", projection, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectedShippingAmount =", projection, StringComparison.Ordinal);
        Assert.DoesNotContain("MinimumDeliveryDate =", projection, StringComparison.Ordinal);
    }

    // ---------- negative detection proof (the replacement must fail on expansion) ----------

    [Fact]
    public void Detector_fails_for_an_unexpected_fourth_consumer_outside_the_reviewed_set()
    {
        var sources = BackendProductionSources()
            .Append(new OperationalBoundarySource(
                "src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/RateAuthority/StorefrontShippingRateShadowEvaluator.cs",
                "using Tooba.Shipping.Contracts.Operations;\n\nnamespace X;\ninternal static class Y { }\n"))
            .ToArray();

        var violations = OperationalContractBoundary.Analyze(sources);

        var violation = Assert.Single(violations);
        Assert.Equal(OperationalContractBoundary.UnexpectedConsumerRule, violation.Rule);
        Assert.Contains("StorefrontShippingRateShadowEvaluator.cs", violation.RelativePath, StringComparison.Ordinal);
    }

    [Fact]
    public void Detector_fails_for_an_implementation_reference_inside_an_otherwise_allowed_file()
    {
        var observerPath = OperationalContractBoundary.ApprovedObserverPath;
        var polluted = BackendProductionSources()
            .Select(source => source.RelativePath == observerPath
                ? source with
                {
                    // A single illegal line inside a permitted file must not be tolerated.
                    Text = source.Text
                           + "\n// injected for the negative case\n"
                           + "internal static class Smuggled\n{\n"
                           + "    internal static int Peek() =>\n"
                           + "        typeof(Tooba.Shipping.Domain.Operations.ShippingOperationalQuoteEvaluator).Name.Length;\n"
                           + "}\n",
                }
                : source)
            .ToArray();

        var violations = OperationalContractBoundary.Analyze(polluted);

        Assert.Contains(
            violations,
            v => v.Rule == OperationalContractBoundary.ForeignImplementationRule
                 && v.RelativePath == observerPath
                 && v.Detail.Contains("Tooba.Shipping.Domain", StringComparison.Ordinal));
    }

    [Fact]
    public void Detector_fails_for_an_evaluator_reference_inside_an_allowed_comparer_file()
    {
        var comparerPath = OperationalContractBoundary.ApprovedComparerPath;
        var polluted = BackendProductionSources()
            .Select(source => source.RelativePath == comparerPath
                ? source with
                {
                    Text = source.Text
                           + "\n// injected for the negative case\n"
                           + "internal static class SmuggledEvaluator\n{\n"
                           + "    private static readonly System.Type T = typeof(ShippingRatePolicyEngine);\n"
                           + "}\n",
                }
                : source)
            .ToArray();

        var violations = OperationalContractBoundary.Analyze(polluted);

        Assert.Contains(
            violations,
            v => v.Rule == OperationalContractBoundary.ForeignImplementationRule
                 && v.RelativePath == comparerPath
                 && v.Detail.Contains("ShippingRatePolicyEngine", StringComparison.Ordinal));
    }

    [Fact]
    public void Detector_fails_for_a_non_registration_use_in_the_order_module()
    {
        var registrationPath = OperationalContractBoundary.ApprovedRegistrationPath;
        var polluted = BackendProductionSources()
            .Select(source => source.RelativePath == registrationPath
                ? source with
                {
                    Text = source.Text
                           + "\ninternal sealed class SmuggledLookup\n{\n"
                           + "    internal async Task Peek(CancellationToken ct)\n"
                           + "    {\n"
                           + "        var lookup = _sp.GetRequiredService<Tooba.Shipping.Contracts.Operations.IShippingOperationalQuoteLookup>();\n"
                           + "        await lookup.QuoteAsync(default!, ct);\n"
                           + "    }\n"
                           + "}\n",
                }
                : source)
            .ToArray();

        var violations = OperationalContractBoundary.Analyze(polluted);

        Assert.Contains(
            violations,
            v => v.Rule == OperationalContractBoundary.RegistrationOnlyRule
                 && v.RelativePath == registrationPath);
    }

    [Fact]
    public void Detector_fails_for_a_peer_module_reaching_shipping_implementation()
    {
        var sources = BackendProductionSources()
            .Append(new OperationalBoundarySource(
                "src/backend/Modules/Geography/Tooba.Geography.Application/Leak.cs",
                "namespace X;\ninternal sealed class Leak\n{\n    private readonly Tooba.Shipping.Infrastructure.Persistence.ShippingDbContext _db = null!;\n}\n"))
            .ToArray();

        var violations = OperationalContractBoundary.Analyze(sources);

        Assert.NotEmpty(violations);
        Assert.All(violations, violation =>
        {
            Assert.Equal(OperationalContractBoundary.ForeignImplementationRule, violation.Rule);
            Assert.Contains("Leak.cs", violation.RelativePath, StringComparison.Ordinal);
        });
    }

    // ---------- preserved historical invariants ----------

    [Fact]
    public void The_legacy_read_seams_keep_their_exact_shape()
    {
        var contractsAssembly = typeof(Contracts.Rates.IShippingRateQuoteLookup).Assembly;
        var interfaces = contractsAssembly
            .GetTypes()
            .Where(x => x.IsInterface)
            .Select(x => x.Name)
            .ToArray();

        // The existing seams are untouched: still present, still one method each, still read-only.
        Assert.Contains("IShippingRateQuoteLookup", interfaces);
        Assert.Contains("IShippingPackagingFeeLookup", interfaces);
        Assert.Contains("IShippingShipmentCostLookup", interfaces);
        Assert.Contains("IShippingOperationalQuoteLookup", interfaces);

        // No write seam is exposed to another module by this slice.
        Assert.DoesNotContain("IShippingOperationalQuoteWriter", interfaces);
        Assert.DoesNotContain("IShippingCapacityReservation", interfaces);

        var rate = typeof(Contracts.Rates.IShippingRateQuoteLookup);
        Assert.Single(rate.GetMethods());

        var operational = typeof(Contracts.Operations.IShippingOperationalQuoteLookup);
        Assert.Single(operational.GetMethods());
    }

    [Fact]
    public void The_operational_contract_never_carries_a_success_shaped_zero()
    {
        // Every money field on the composite result is nullable: "not quoted" cannot be expressed as 0.
        var moneyFields = new[]
        {
            nameof(Contracts.Operations.ShippingOperationalQuote.FreightAmount),
            nameof(Contracts.Operations.ShippingOperationalQuote.PackagingAmount),
            nameof(Contracts.Operations.ShippingOperationalQuote.TotalServiceExpense),
            nameof(Contracts.Operations.ShippingOperationalQuote.AmountPayableAtCheckout),
            nameof(Contracts.Operations.ShippingOperationalQuote.AmountCollectOnDelivery),
        };

        foreach (var field in moneyFields)
        {
            var property = typeof(Contracts.Operations.ShippingOperationalQuote).GetProperty(field);
            Assert.NotNull(property);
            Assert.Equal(typeof(decimal?), property!.PropertyType);
        }
    }

    [Fact]
    public void The_operational_slice_adds_no_migration_and_no_money_movement()
    {
        // A read-only composition needs no schema change: the migration chain stays exactly as 075 left it.
        Assert.False(
            File.Exists(Path.Combine(
                RepoRoot(),
                "src",
                "backend",
                "Modules",
                "Shipping",
                "Tooba.Shipping.Infrastructure",
                "Persistence",
                "Migrations",
                "20261015090000_OperationalQuote.cs")));

        // TB-190-R1: the historical blanket "no BeginTransaction text anywhere" rule was stale because the
        // architect authorized exactly one module-owned authoring replacement transaction. The replacement
        // guard below is narrower AND stronger: it proves exactly one reviewed transaction site, keeps the
        // operational quote read-only, and still forbids every startup-migration authority and every
        // operational money/capacity write inside Shipping.
        var violations = ShippingTransactionBoundary.Analyze(BackendProductionSources());

        Assert.Empty(violations.Where(violation =>
            violation.Rule is ShippingTransactionBoundary.StartupSchemaAuthorityRule
                or ShippingTransactionBoundary.OperationalWriteAuthorityRule));
    }

    [Fact]
    public void Exactly_one_reviewed_shipping_transaction_exists_and_it_is_the_staged_draft_replacement()
    {
        var violations = ShippingTransactionBoundary.Analyze(BackendProductionSources());

        // No unexpected transaction anywhere, and the reviewed boundary kept its one-begin shape with its
        // staged saves/commit/rollback statements and exactly the two typed callers.
        Assert.Empty(violations.Where(violation =>
            violation.Rule is ShippingTransactionBoundary.UnexpectedTransactionRule
                or ShippingTransactionBoundary.BoundaryShapeRule
                or ShippingTransactionBoundary.CallerUniverseRule));

        // The allowed boundary is not blanket-excluded by path or folder: its content is inspected too, and
        // it must remain a single begin/commit around two staged saves with a rollback path.
        var boundary = BackendProductionSources()
            .Single(source => source.RelativePath == ShippingTransactionBoundary.AllowedBoundaryPath);
        var code = CodeLines(boundary.Text).ToArray();

        Assert.Equal(1, CountOccurrences(string.Join('\n', code), "BeginTransactionAsync("));
        Assert.Equal(1, CountOccurrences(string.Join('\n', code), "CommitAsync("));
        Assert.True(
            CountOccurrences(string.Join('\n', code), "SaveChangesAsync(") >= 2,
            "both staged saves must live inside the reviewed boundary");
        Assert.True(
            CountOccurrences(string.Join('\n', code), "RollbackAsync(") >= 1,
            "the reviewed boundary must keep a rollback path for failure and cancellation");
    }

    // ---------- negative detection proof for the transaction / schema guard ----------

    [Fact]
    public void Transaction_guard_fails_for_an_unexpected_second_transaction_in_another_shipping_file()
    {
        var polluted = BackendProductionSources()
            .Append(new OperationalBoundarySource(
                "src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Authoring/ShippingAuthoringRateWriter.cs",
                "namespace X;\ninternal sealed class W\n{\n    internal void Go(Tooba.Shipping.Infrastructure.Persistence.ShippingDbContext db)\n    {\n        using var tx = db.Database.BeginTransaction();\n    }\n}\n"))
            .ToArray();

        var violations = ShippingTransactionBoundary.Analyze(polluted);

        var violation = Assert.Single(
            violations,
            candidate => candidate.Rule == ShippingTransactionBoundary.UnexpectedTransactionRule);
        Assert.Contains("ShippingAuthoringRateWriter.cs", violation.RelativePath, StringComparison.Ordinal);
        Assert.Contains("Go", violation.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Transaction_guard_fails_for_an_extra_transaction_inside_the_otherwise_allowed_file()
    {
        // Same reviewed FILE, different member: a filename-only exemption would let this pass.
        var polluted = BackendProductionSources()
            .Select(source => source.RelativePath == ShippingTransactionBoundary.AllowedBoundaryPath
                ? source with
                {
                    Text = source.Text
                           + "\n\ninternal sealed class SmuggledSecondBoundary\n{\n"
                           + "    internal void Extra()\n"
                           + "    {\n"
                           + "        using var nested = _context.Database.BeginTransaction();\n"
                           + "    }\n"
                           + "}\n",
                }
                : source)
            .ToArray();

        var violations = ShippingTransactionBoundary.Analyze(polluted);

        Assert.Contains(
            violations,
            violation => violation.Rule == ShippingTransactionBoundary.UnexpectedTransactionRule
                         && violation.RelativePath == ShippingTransactionBoundary.AllowedBoundaryPath
                         && violation.Detail.Contains("Extra", StringComparison.Ordinal));

        // And a SECOND begin added to the reviewed member itself must fail: the boundary is exactly one
        // begin/commit, not "at most one per file".
        var doubled = BackendProductionSources()
            .Select(source => source.RelativePath == ShippingTransactionBoundary.AllowedBoundaryPath
                ? source with
                {
                    Text = source.Text.Replace(
                        "                transaction = await context.Database.BeginTransactionAsync(cancellationToken);",
                        "                transaction = await context.Database.BeginTransactionAsync(cancellationToken);\n"
                        + "                var nested = await context.Database.BeginTransactionAsync(cancellationToken);",
                        StringComparison.Ordinal),
                }
                : source)
            .ToArray();
        Assert.NotEqual(
            BackendProductionSources().Single(source =>
                source.RelativePath == ShippingTransactionBoundary.AllowedBoundaryPath).Text,
            doubled.Single(source => source.RelativePath == ShippingTransactionBoundary.AllowedBoundaryPath).Text);

        var doubledViolations = ShippingTransactionBoundary.Analyze(doubled);
        Assert.Contains(
            doubledViolations,
            violation => violation.Rule == ShippingTransactionBoundary.BoundaryShapeRule);
    }

    [Fact]
    public void Transaction_guard_fails_for_an_operational_transaction_or_money_authority_leak()
    {
        var polluted = BackendProductionSources()
            .Append(new OperationalBoundarySource(
                "src/backend/Modules/Shipping/Tooba.Shipping.Application/Operations/ShippingOperationalMoneyWriter.cs",
                "namespace X;\ninternal sealed class W\n{\n    internal void Go()\n    {\n"
                + "        using var tx = _db.Database.BeginTransaction();\n"
                + "        result.SelectedShippingAmount = 1m;\n"
                + "        var reservation = new StorefrontCapacityReservation();\n"
                + "    }\n}\n"))
            .ToArray();

        var violations = ShippingTransactionBoundary.Analyze(polluted);

        Assert.Contains(violations, violation =>
            violation.Rule == ShippingTransactionBoundary.UnexpectedTransactionRule);
        Assert.Contains(violations, violation =>
            violation.Rule == ShippingTransactionBoundary.OperationalWriteAuthorityRule);
    }

    [Fact]
    public void Transaction_guard_fails_for_a_startup_migration_inside_the_allowed_file()
    {
        var polluted = BackendProductionSources()
            .Select(source => source.RelativePath == ShippingTransactionBoundary.AllowedBoundaryPath
                ? source with
                {
                    Text = source.Text
                           + "\n\ninternal sealed class SmuggledMigrator\n{\n"
                           + "    internal static void Up(Tooba.Shipping.Infrastructure.Persistence.ShippingDbContext db)\n"
                           + "    {\n"
                           + "        db.Database.MigrateAsync().GetAwaiter().GetResult();\n"
                           + "    }\n"
                           + "}\n",
                }
                : source)
            .ToArray();

        var violations = ShippingTransactionBoundary.Analyze(polluted);

        Assert.Contains(
            violations,
            violation => violation.Rule == ShippingTransactionBoundary.StartupSchemaAuthorityRule
                         && violation.RelativePath == ShippingTransactionBoundary.AllowedBoundaryPath);
    }

    [Fact]
    public void Transaction_guard_fails_when_the_reviewed_boundary_loses_its_single_transaction()
    {
        var allowed = BackendProductionSources()
            .Single(source => source.RelativePath == ShippingTransactionBoundary.AllowedBoundaryPath);

        var stripped = new[] { allowed with { Text = allowed.Text.Replace("BeginTransactionAsync", "OpenAsync") } };

        var violations = ShippingTransactionBoundary.Analyze(stripped);

        Assert.Contains(
            violations,
            violation => violation.Rule == ShippingTransactionBoundary.BoundaryShapeRule);
    }

    [Fact]
    public void Transaction_guard_fails_when_the_reviewed_boundary_loses_its_commit_or_rollback_or_staged_save()
    {
        var allowed = BackendProductionSources()
            .Single(source => source.RelativePath == ShippingTransactionBoundary.AllowedBoundaryPath);

        // (1) The commit statement disappears: a begin with no commit is not the reviewed shape.
        var withoutCommit = allowed with
        {
            Text = allowed.Text.Replace(
                "await transaction.CommitAsync(cancellationToken);",
                "await transaction.CompleteAsync(cancellationToken);",
                StringComparison.Ordinal),
        };
        Assert.NotEqual(allowed.Text, withoutCommit.Text);
        Assert.Contains(
            ShippingTransactionBoundary.Analyze([withoutCommit]),
            violation => violation.Rule == ShippingTransactionBoundary.BoundaryShapeRule
                         && violation.Detail.Contains("CommitAsync", StringComparison.Ordinal));

        // (2) The rollback path disappears: a failure would then leave the transaction open.
        var withoutRollback = allowed with
        {
            Text = allowed.Text.Replace("RollbackAsync(", "DiscardAsync(", StringComparison.Ordinal),
        };
        Assert.NotEqual(allowed.Text, withoutRollback.Text);
        Assert.Contains(
            ShippingTransactionBoundary.Analyze([withoutRollback]),
            violation => violation.Rule == ShippingTransactionBoundary.BoundaryShapeRule
                         && violation.Detail.Contains("RollbackAsync", StringComparison.Ordinal));

        // (3) One of the two REQUIRED staged saves disappears.
        var secondSave = string.Join(
            Environment.NewLine,
            "                draft.ApplyFinalRegionGroupOrder();",
            "                await context.SaveChangesAsync(cancellationToken);");
        var withoutSecondSave = allowed with
        {
            Text = allowed.Text.Replace(
                secondSave,
                "                draft.ApplyFinalRegionGroupOrder();",
                StringComparison.Ordinal),
        };
        Assert.NotEqual(allowed.Text, withoutSecondSave.Text);
        Assert.Contains(
            ShippingTransactionBoundary.Analyze([withoutSecondSave]),
            violation => violation.Rule == ShippingTransactionBoundary.BoundaryShapeRule
                         && violation.Detail.Contains("staged saves", StringComparison.Ordinal));
    }

    [Fact]
    public void Transaction_guard_fails_for_a_third_caller_or_a_callback_indirection_in_the_reviewed_file()
    {
        var allowed = BackendProductionSources()
            .Single(source => source.RelativePath == ShippingTransactionBoundary.AllowedBoundaryPath);

        // A third caller in the SAME reviewed file is outside the caller universe, even with no extra begin.
        var thirdCaller = allowed with
        {
            Text = allowed.Text
                   + "\n\ninternal sealed class SmuggledThirdCaller\n{\n"
                   + "    internal async Task<Tooba.BuildingBlocks.Results.Result<ShippingAuthoringDraft>> Extra("
                   + "ShippingAuthoringDraft draft)\n"
                   + "    {\n"
                   + "        return await SaveAuthoringReplacementAsync(draft, null, null, CancellationToken.None);\n"
                   + "    }\n"
                   + "}\n",
        };
        Assert.Contains(
            ShippingTransactionBoundary.Analyze([thirdCaller]),
            violation => violation.Rule == ShippingTransactionBoundary.CallerUniverseRule
                         && violation.Detail.Contains("Extra", StringComparison.Ordinal));

        // A delegate indirection from an ALLOWED caller still hides an unreviewed invocation path.
        var callback = allowed with
        {
            Text = allowed.Text.Replace(
                "return await SaveAuthoringReplacementAsync(",
                "return await InvokeBoundary(() => SaveAuthoringReplacementAsync(",
                StringComparison.Ordinal),
        };
        Assert.NotEqual(allowed.Text, callback.Text);
        Assert.Contains(
            ShippingTransactionBoundary.Analyze([callback]),
            violation => violation.Rule == ShippingTransactionBoundary.CallerUniverseRule
                         && violation.Detail.Contains("delegate/callback", StringComparison.Ordinal));
    }

    [Fact]
    public void Capacity_is_declared_unenforced_by_the_type_system()
    {
        // The only declared enforcement value is "not enforced", so no caller can read a numeric cap as a
        // real reservation until an atomic claim is designed and approved.
        var values = Enum.GetValues<Contracts.Operations.ShippingCapacityEnforcement>();

        Assert.Equal([Contracts.Operations.ShippingCapacityEnforcement.NotEnforced], values);
    }

    // ---------- helpers ----------

    /// <summary>Executable lines only: XML docs and line comments explain boundaries and must stay legal.</summary>
    internal static IEnumerable<string> CodeLines(string text) =>
        text.Split('\n')
            .Select(line => line.TrimStart())
            .Where(line => line.Length > 0
                           && !line.StartsWith("///", StringComparison.Ordinal)
                           && !line.StartsWith("//", StringComparison.Ordinal)
                           && !line.StartsWith("*", StringComparison.Ordinal)
                           && !line.StartsWith("/*", StringComparison.Ordinal));

    internal static int CountOccurrences(string haystack, string needle)
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

    private static OperationalBoundarySource[] BackendProductionSources()
    {
        var backend = Path.Combine(RepoRoot(), "src", "backend");
        return Directory
            .EnumerateFiles(backend, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedOrTestTree(path))
            .Select(path => new OperationalBoundarySource(
                Path.GetRelativePath(RepoRoot(), path).Replace('\\', '/'),
                File.ReadAllText(path)))
            .OrderBy(source => source.RelativePath, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsGeneratedOrTestTree(string path)
    {
        var relative = Path.GetRelativePath(RepoRoot(), path);
        var segments = relative.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment is "obj" or "bin")
               || segments.Any(segment => segment.EndsWith("Tests", StringComparison.Ordinal));
    }

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
}

/// <summary>One production source file considered by the operational-contract boundary detector.</summary>
internal sealed record OperationalBoundarySource(string RelativePath, string Text);

/// <summary>One detected breach of the approved operational-contract boundary.</summary>
internal sealed record OperationalBoundaryViolation(string RelativePath, string Rule, string Detail);

/// <summary>
/// Machine-checkable form of the TB-079/TB-080-approved operational shadow boundary, replacing the stale
/// TB-076 blanket prohibition. Kept side-effect free so the negative cases can feed it synthetic sources.
/// </summary>
internal static class OperationalContractBoundary
{
    public const string UnexpectedConsumerRule = "UNEXPECTED_OPERATIONAL_CONTRACT_CONSUMER";
    public const string ForeignImplementationRule = "FOREIGN_SHIPPING_IMPLEMENTATION_REFERENCE";
    public const string RegistrationOnlyRule = "ORDER_EDGE_NOT_REGISTRATION_ONLY";

    private const string ShippingModuleRoot = "src/backend/Modules/Shipping/";
    private const string ModulesRoot = "src/backend/Modules/";
    private const string HostRoot = "src/backend/Host/";

    public const string ApprovedComparerPath =
        "src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/OperationalShadow/StorefrontShippingOperationalShadowComparer.cs";

    public const string ApprovedObserverPath =
        "src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/OperationalShadow/StorefrontShippingOperationalShadowObserver.cs";

    public const string ApprovedRegistrationPath =
        "src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs";

    /// <summary>The reviewed observation/comparison pair plus the single Order DI registration.</summary>
    public static readonly string[] ApprovedConsumers =
    [
        ApprovedComparerPath,
        ApprovedObserverPath,
        ApprovedRegistrationPath,
    ];

    /// <summary>Host composition roots: they may compose modules, never compute module behavior.</summary>
    private static readonly string[] HostCompositionFiles =
    [
        "src/backend/Host/Tooba.Host/Program.cs",
        "src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs",
        "src/backend/Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs",
    ];

    /// <summary>Public Contracts surface an outside module may lawfully observe.</summary>
    private static readonly string[] OperationalContractMarkers =
    [
        "Tooba.Shipping.Contracts.Operations",
        "IShippingOperationalQuoteLookup",
        "ShippingOperationalQuoteRequest",
        "ShippingOperationalQuoteOutcome",
        "ShippingOperationalQuoteReasonCodes",
        "ShippingOperationalQuote",
        "operational-quote",
    ];

    /// <summary>Shipping implementation surface no outside module may reference.</summary>
    private static readonly string[] ForeignImplementationMarkers =
    [
        "Tooba.Shipping.Domain",
        "Tooba.Shipping.Application",
        "Tooba.Shipping.Infrastructure",
        "Tooba.Shipping.Endpoints",
        "ShippingOperationalQuoteEvaluator",
        "ShippingScheduleAvailabilityEvaluator",
        "ShippingRatePolicyEngine",
        "ShippingDbContext",
    ];

    /// <summary>Even the Host composition roots must never reach module domain logic.</summary>
    private static readonly string[] HostForbiddenImplementationMarkers =
    [
        "Tooba.Shipping.Domain",
        "ShippingOperationalQuoteEvaluator",
        "ShippingScheduleAvailabilityEvaluator",
        "ShippingRatePolicyEngine",
    ];

    public static bool ConsumesOperationalContracts(string code) =>
        OperationalContractMarkers.Any(marker => code.Contains(marker, StringComparison.Ordinal));

    public static bool IsShippingModule(string relativePath) =>
        relativePath.Replace('\\', '/').StartsWith(ShippingModuleRoot, StringComparison.Ordinal);

    public static bool ConsumesOperationalContracts(OperationalBoundarySource source) =>
        Code(source.Text).Any(ConsumesOperationalContracts);

    public static IReadOnlyList<string> ImplementationReferences(string text) =>
        ForeignImplementationMarkers
            .Where(marker => Code(text).Any(line => line.Contains(marker, StringComparison.Ordinal)))
            .ToArray();

    public static IReadOnlyList<OperationalBoundaryViolation> Analyze(IEnumerable<OperationalBoundarySource> sources)
    {
        var violations = new List<OperationalBoundaryViolation>();

        foreach (var source in sources)
        {
            var path = source.RelativePath.Replace('\\', '/');
            var code = Code(source.Text);

            // The owning module and non-backend paths are not this guard's business.
            if (path.StartsWith(ShippingModuleRoot, StringComparison.Ordinal))
            {
                continue;
            }

            var implementationMarkers = ForeignImplementationMarkers
                .Where(marker => code.Any(line => line.Contains(marker, StringComparison.Ordinal)))
                .ToArray();
            var consumes = code.Any(ConsumesOperationalContracts);

            if (path.StartsWith(ModulesRoot, StringComparison.Ordinal))
            {
                // A peer module may never reach Shipping implementation, inside or outside an approved file.
                foreach (var marker in implementationMarkers)
                {
                    violations.Add(new OperationalBoundaryViolation(
                        path,
                        ForeignImplementationRule,
                        $"module outside Shipping references Shipping implementation marker '{marker}'"));
                }
            }
            else if (path.StartsWith(HostRoot, StringComparison.Ordinal))
            {
                var isCompositionRoot = HostCompositionFiles.Contains(path, StringComparer.Ordinal);
                var hostViolations = isCompositionRoot
                    ? implementationMarkers.Where(marker =>
                        HostForbiddenImplementationMarkers.Contains(marker, StringComparer.Ordinal))
                    : implementationMarkers;
                foreach (var marker in hostViolations)
                {
                    violations.Add(new OperationalBoundaryViolation(
                        path,
                        ForeignImplementationRule,
                        $"Host references Shipping implementation marker '{marker}'"));
                }
            }

            if (!consumes)
            {
                continue;
            }

            if (!ApprovedConsumers.Contains(path, StringComparer.Ordinal))
            {
                violations.Add(new OperationalBoundaryViolation(
                    path,
                    UnexpectedConsumerRule,
                    "operational Contracts consumed outside the reviewed observation/registration set"));
                continue;
            }

            if (!string.Equals(path, ApprovedRegistrationPath, StringComparison.Ordinal))
            {
                continue;
            }

            var nonRegistration = code
                .Where(ConsumesOperationalContracts)
                .Where(line => !line.Contains("GetService<", StringComparison.Ordinal))
                .ToArray();
            if (nonRegistration.Length > 0
                || code.Any(line => line.Contains("QuoteAsync(", StringComparison.Ordinal)
                                    || line.Contains(".ObserveAsync(", StringComparison.Ordinal)))
            {
                violations.Add(new OperationalBoundaryViolation(
                    path,
                    RegistrationOnlyRule,
                    "the Order edge must only register the seam, never call the quote or the observer"));
            }
        }

        return violations;
    }

    private static string[] Code(string text) =>
        OperationalQuoteCompatibilityTests.CodeLines(text).ToArray();
}
