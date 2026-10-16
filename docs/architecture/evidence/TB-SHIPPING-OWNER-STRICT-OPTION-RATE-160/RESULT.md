# TB-SHIPPING-OWNER-STRICT-OPTION-RATE-160 — Result (PASS)

Base SHA: `cff0f19d36ab73870345a3f542f01c459894a1a6` (HEAD == origin/main at claim).
Scope: Shipping owner semantics only. **No Order/storefront wiring, no gate enablement.**

## What was implemented

### 1. Additive opt-in contract (`Tooba.Shipping.Contracts.Rates`)

```text
enum ShippingOptionMatchMode { LegacyFallback = 0, StrictOwnedOption = 1 }
ShippingQuoteRequest.OptionMatchMode (init-only, default LegacyFallback)
ShippingReasonCodes.OwnedOptionNotPriced = "shipping.quote.owned_option_not_priced"
```

* `OptionMatchMode` is an **init-only property**, not a positional parameter, so the record's constructor
  signature is unchanged — every existing caller (`StorefrontShippingRateShadowEvaluator`,
  `ShippingShipmentCostLookup`, tests) compiles and behaves identically, and no serialization shape moved.
* `StrictOwnedOption` is a named mode rather than a bare `bool`, so the semantics are self-describing and
  extensible without a second boolean later.

### 2. Owner engine change (`ShippingRatePolicyEngine.Quote`)

```startLine:72:endLine:92:src/backend/Modules/Shipping/Tooba.Shipping.Domain/Rates/ShippingRatePolicyEngine.cs
        var serviceCode = request.ServiceCode?.Trim() ?? string.Empty;
        var optionCode = request.OptionCode?.Trim();
        var strictOwnedOption = request.OptionMatchMode == ShippingOptionMatchMode.StrictOwnedOption;
        var destination = new ShippingDestinationScope( /* ... */ );

        var active = (policies ?? Array.Empty<ShippingRatePolicy>())
            .Where(x => x.IsActive)
            .Where(x => string.Equals(x.ServiceCode, serviceCode, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var covering = active
            .Where(x => x.Covers(destination))
            .Where(x => strictOwnedOption
                ? MatchesOwnedOptionExactly(x, optionCode)
                : x.MatchesOption(optionCode))
            .ToArray();
```

with

```startLine:222:endLine:223:src/backend/Modules/Shipping/Tooba.Shipping.Domain/Rates/ShippingRatePolicyEngine.cs
    private static bool MatchesOwnedOptionExactly(ShippingRatePolicy policy, string? optionCode)
        => string.Equals(policy.OptionCode, optionCode, StringComparison.OrdinalIgnoreCase);
```

* Strict mode requires **exact option equality**, so a service-level row (`OptionCode == null`) can never
  satisfy a concrete option request, and a sibling option row can never satisfy it either (the TB-159
  defect). No first-match behaviour is introduced.
* The existing `candidates` option-precedence step is provably a no-op in strict mode (all surviving rows
  share one option identity), so the documented legacy order *option → scope depth → size tier* is
  untouched; a comment now records that.
* Fail-closed reason selection: when strict mode finds nothing while the service *does* have active rows
  covering the destination, the result is the established `UnknownService` outcome with the new stable
  reason `shipping.quote.owned_option_not_priced`; the generic `shipping.service.not_priced` is kept when
  the service genuinely has no rows. No new enum member was added to `ShippingServiceabilityOutcome`
  (deliberate: keeps the Contracts enum additive-stable for existing consumers, and the stable reason code
  already carries the distinction for logs/telemetry/Admin). Amount stays `0m` with a null currency and
  `IsQuoted == false` — **zero is never "free"**; `QuotedFreeAboveThreshold` remains the only FREE signal.

### 3. Honest limit on coverage ownership (report, not a code change)

`ShippingCoverageGrant` and `ShippingCoverageGrantWriteRequest(string ServiceCode, ShippingDestinationScopeWrite Scope)`
have **no option dimension at all** — coverage is authored and stored per *service + destination*. The SQL
pre-filter in `ShippingRateQuoteLookup` is also service+country. Therefore:

* A coverage grant can never be "owned by a sibling/provider row" in a way strict mode could reject — that
  ownership datum simply does not exist in the schema.
* What strict mode *does* guarantee: a quote cannot be produced without an active coverage grant for the
  same service (§engine `Covered` check), and it cannot be produced from a row not owned by the exact
  option.
* Remaining prerequisite (unchanged from TB-159, still open): decide whether customer-purchasable methods
  must carry their own coverage grants, and whether coverage eventually needs an option dimension. Adding
  that dimension is a schema change and was correctly left out of this bounded task.

## Proof matrix (focused owner tests, real engine + real adapter)

| Required case | Test | Result |
| --- | --- | --- |
| (a) provider-level only, strict `tipax:standard` fails | `Strict_mode_refuses_provider_level_row_as_a_method_price` | PASS — `UnknownService` + `owned_option_not_priced`, `IsQuoted=false`, amount `0` |
| (b) `tipax:express` row cannot price `tipax:standard` | `Strict_mode_never_lets_a_sibling_option_price_this_option` | PASS |
| (c) standard-owned works for standard, express-owned for express | `Strict_mode_prices_the_owned_option_and_never_the_sibling` | PASS — 205,000 / 260,000 |
| (d) mixed shared + owned choose strictly owned | `Mixed_shared_and_owned_rows_keep_owned_precedence_and_diverge_only_on_legacy_fallback` | PASS — strict 205,000; legacy express still 185,000 (unchanged on purpose) |
| (e) strict without owned coverage fails closed | `Strict_mode_without_coverage_fails_closed` | PASS — `NotCovered` + `coverage.missing` |
| (f) conditioned row without measurement fails closed | `Strict_mode_conditioned_row_without_measurement_fails_closed_and_quotes_with_it` | PASS — `MeasurementRequired`, then quotes with a declared weight |
| (g) optionless/legacy unchanged | `Optionless_methods_and_legacy_requests_are_unchanged` | PASS — equality asserted between modes |
| (h) ambiguity/conflict handling | `Strict_mode_keeps_ambiguity_and_currency_conflicts_fail_closed` | PASS — `ScopeConflict`, `ConditionConflict`, `CurrencyMismatch` |
| strict flag survives the read adapter | `Strict_flag_survives_the_rate_quote_lookup_adapter` (in-memory `ShippingDbContext`, provider row + owned row + grant persisted) | PASS |

No strictness downgrade path exists: `LegacyFallback` is only ever the *absent* value, and every
non-strict caller in the repository is unchanged (verified by the 70/70 Order storefront test run).

## Verification runs

See `test-runs-160.txt` (verbatim). Summary:

* `Tooba.Shipping.Infrastructure` build: **succeeded, 0 errors**.
* `Tooba.Shipping.Tests` build: **succeeded, 0 errors**.
* New strict tests: **9/9 passed**.
* Focused Shipping set (`Rates` + `ShippingMethodFinancial*` + `Compatibility`): **105 passed / 1 failed**.
  The single failure is **pre-existing**: `OperationalQuoteCompatibilityTests.No_module_outside_shipping_references_the_operational_quote_types`
  scans committed source for operational-quote markers outside Shipping and finds four **unmodified**
  Order files (the pre-existing operational shadow seam). None of them is in this task's diff
  (`git status --porcelain` proves it).
* `Tooba.Host` build (downstream consumer compile): **succeeded, 0 errors**.
* `Tooba.Order.Tests --filter StorefrontShipping`: **70/70 passed**.
* Frontend untouched → no frontend typecheck.

## Scope / safety compliance

* No Order quote path wiring, no storefront pricing change, no feature-gate enablement, no Admin redesign.
* No FREE/COD/provider-quote invention; no tax/settlement/PSP change; no `Results.Json`; Contracts-only.
* No schema or migration change (none was needed; the coverage option dimension is reported, not improvised).
* Shared `:3000`/`:5088`/`:5432`, `tooba_alpha`, `store-alpha`, containers, RabbitMQ/Redis untouched; no
  restart, reset, rebase, stash or force-push. Pre-existing unrelated dirty paths
  (`src/frontend/app/admin/admin-fulfillment-work-queue-screen.tsx`, two `docs/architecture/evidence`
  `host-stdout-*.txt` files) were preserved and **not** committed.

## Remaining prerequisites before any customer-facing cutover (unchanged, for the Architect)

1. Coverage-grant policy for customer-purchasable methods (and whether coverage needs an option dimension).
2. Trusted parcel/measurement source for conditioned policies, or an explicit `MeasurementRequired` UX.
3. Only then: Order storefront seam behind its own default-off gate, requesting
   `ShippingOptionMatchMode.StrictOwnedOption`, plus authenticated-customer runtime proof.

STOP_FOR_ARCHITECT. No automatic next task.
