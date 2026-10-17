# TB-SHIPPING-STOREFRONT-STRICT-RATE-CUTOVER-163

Date: 2026-10-17
Base: 727cdb46111446cf1a15364de73ebacb03660e5f
Mode: BOUNDED_OWNER_CORRECT_DEFAULT_OFF_STOREFRONT_RATE_INTEGRATION
Verdict: **PARTIAL** (implementation + focused fail-closed tests complete; isolated authenticated HTTP acceptance not executed inside timebox)

## What was implemented (code, not a report)

New default-OFF gate that makes the **Shipping-owned** rate authoritative for the customer-facing storefront
shipping projection/draft/payment-draft path.

| Item | Value |
| --- | --- |
| Config section | `Tooba:ShippingOwnedRateAuthority` |
| Key | `Enabled` |
| Default | `false` in `appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json` |
| Absent-is-false | yes (`Enabled` defaults to `bool` default) |
| Separate from observe-only shadow | yes (`Tooba:ShippingRateAuthority` / operational shadow untouched) |

Implementation points:

- `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/OwnedRate/StorefrontShippingOwnedRateOptions.cs`
- `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/OwnedRate/StorefrontShippingOwnedRateResolver.cs`
  - OFF: returns `Disabled` **before** touching the lookup or Geography (zero owner calls).
  - ON: for each legacy-eligible canonical method (`service:option`) resolves the authoritative destination
    (explicit Geography ids, else deterministic `ResolveLegacyAsync`; never from free text), then calls the
    existing `IShippingRateQuoteLookup.QuoteAsync` with `ShippingOptionMatchMode.StrictOwnedOption` and
    `Parcel = null` (no fabricated measurement).
  - Accepts a method only when `ShippingQuote.IsQuoted` **and** currency matches the cart currency **and**
    amount is non-negative. Otherwise the stable reason is kept (`shipping.coverage.method_restricted`,
    `shipping.quote.measurement_required`, `shipping.quote.owned_option_not_priced`,
    `shipping.currency.mismatch`, `shipping.scope.ambiguous`, ...) or an Order-layer code
    (`shipping.owned_rate.owner_unavailable|method_identity_incomplete|destination_unresolved|quote_failed`).
- `StorefrontShippingService.LoadEligibleMethodsAsync` (ON only): replaces each legacy `PriceAmount` with the
  authoritative amount, **removes** methods without an authoritative quote from the selectable list, and
  exposes them via `UnavailableMethods` with the stable reason. Nothing downstream can price them.
- `StorefrontShippingProjection.UnavailableMethods` (additive, trailing, `null` when OFF).
- `ProjectStorefrontShippingQuery` forwards `CityName`/`Destination` to the service (higher-precision
  authoritative destination; inert while OFF).
- `SaveSelectionAsync` uses the same authoritative list, so a rejected method cannot be selected, its
  amount never enters draft `ShippingAmount`, group `ShippingAmount`, or the payment draft
  (`ProjectPaymentDraftAsync` replays the persisted draft only). Group charges stay exactly once per
  `CartShippingGroupPlanner` (unchanged).
- DI: `OrderModule` registers options + resolver; the service dependency is trailing/optional so every
  existing composition (tests, other hosts) resolves to the exact legacy path.

Explicitly **not** done (out of scope / forbidden): no new rate engine, no new Shipping Admin writer, no
schema/migration, no Host DbContext/business pricing, no fallback to `Tooba:ShippingMethods`, provider tariff,
service-level row, or sibling option when ON.

## Quote provenance matrix

| Case (ON) | Owner result | Customer-visible result | Persisted draft/payment |
| --- | --- | --- | --- |
| `tipax:standard` exact option-owned rule = 205000 IRR | `Quoted` 205000 | exactly 205000 | 205000 (never legacy price) |
| `tipax:express` sibling option | `Quoted` own amount | own amount only | own amount |
| shared-only (service-level) row only | `OwnedOptionNotPriced` | method removed + reason | cannot be selected/saved |
| Restricted coverage denies destination | `CoverageMethodRestricted` | method removed + reason | cannot be selected/saved |
| absent declaration (`Unrestricted`) + owned rule | `Quoted` | owned amount | owned amount |
| absent declaration, no owned rule | `OwnedOptionNotPriced` | method removed + reason | cannot be selected/saved |
| conditioned rule, no trusted measurement | `MeasurementRequired` | method removed + reason | cannot be selected/saved |
| currency mismatch | Order rejects | method removed + `shipping.owned_rate.currency_mismatch` | cannot be selected/saved |
| owner missing / throws | Order rejects | method removed + `owner_unavailable`\* | cannot be selected/saved |

\* `owner_unavailable` for a missing port; `shipping.owned_rate.quote_failed` for a real lookup fault.

Never: zero/missing quote → FREE; generic/provider rate; guessed weight/dimensions.

## Tests

New focused suite `StorefrontShippingOwnedRateResolverTests` (10 tests, all passing):

1. `Gate_off_performs_zero_owner_calls_and_keeps_legacy_path` — OFF parity, 0 lookup calls, 0 geography calls.
2. `Gate_on_prices_each_exact_option_and_never_inherits_sibling_option` — 205000 `standard`; 480000 `express`;
   every request asserts `StrictOwnedOption` + `Parcel == null`.
3. `Legacy_shared_only_row_is_unquotable_with_stable_reason`.
4. `Restricted_coverage_denial_fails_closed`.
5. `Measurement_required_fails_closed_without_fabricating_parcel`.
6. `Currency_mismatch_is_rejected`.
7. `Unresolved_destination_fails_closed_without_any_quote`.
8. `Owner_failure_is_fail_closed_and_never_free`.
9. `Missing_service_identity_is_rejected_before_any_quote`.
10. `One_rejected_option_does_not_poison_the_quoted_sibling`.

Regression (unchanged behavior, OFF):
`dotnet test Tooba.Order.Tests --filter "FullyQualifiedName~Storefront"` → **210/210 passed**
(includes `ShippingGroupIdentityContinuityTests`, `StorefrontShippingDraftRecoveryTests`,
`StorefrontPaymentDraftTests`, `StorefrontStartPaymentComposerTests`, one-level label tests).

Build: `dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj` → 0 errors.

## Unverified (honest)

- Isolated authenticated HTTP acceptance (projection → selection save → GET reread → payment draft with the
  seed 205000 rate and per-group fees) was **not** executed: the 45-minute timebox was consumed by the
  bounded implementation and focused tests. OFF/ON are both covered deterministically at the
  resolver/service boundary, but no disposable loopback Host/DB/browser run was captured.
- Two-seller/same-method independence and "group charges exactly once" are covered by the existing
  unchanged group planner/continuity tests, not by a new owner-rate-specific multi-seller fixture.
- `@frontend` UI surfacing of `UnavailableMethods` was not built (backend contract only; no visual change
  while the gate is OFF).

## Cleanup

No containers, ports, shared services, databases, PIDs, migrations, or workers were created or modified.
