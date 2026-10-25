# TB-SHIPPING-CAPTURED-MANUAL-QUOTE-215 — RESULT

Mode: `BOUNDED_EXISTING_AUTHORITY_CAPTURED_MANUAL_QUOTE`
Parent: `TB-SHIPPING-ADMIN-IMMUTABLE-REVISION-CAPTURE-214-R2`
Base-SHA: `4f7cde593115df7c5c8dfd64ea54986d5282113b`
Verdict: `READY_FOR_ARCHITECT_REVIEW` (STOP_FOR_ARCHITECT; no 216 envelope)

## 1. What was added (ONE pure capability, no live wiring)

`ShippingRatePolicyEngine.QuoteCapturedRevision(ShippingCapturedRevisionQuoteRequest)` — an additive
overload-free static method on the **existing** rate authority. All existing `Quote(...)` overloads and
their legacy behaviour are byte-unchanged (a focused regression subset proves it).

New cohesive file `Tooba.Shipping.Domain/Rates/ShippingCapturedRevisionQuote.cs` holds the typed
input/result shape:

- `ShippingCapturedRevisionIdentity` — the caller's trusted expected owner kind/tenant/seller plus
  draft id and source revision.
- `ShippingCapturedRevisionQuoteRequest` — expected identity, the exact decoded
  `ShippingAuthoringRevisionPayload`, a typed `ShippingDestinationScope` path (country + validated
  ancestors), and the caller-declared `GroupWeightGrams?`, `GroupItemCount?`,
  `FinalGroupGoodsAmountAfterDiscountAndTax?`, `CurrencyCode?`.
- `ShippingCapturedQuoteOutcome` (Quoted + 13 explicit not-quoted reasons),
  `ShippingCapturedQuoteReasonCodes`, `ShippingAmountTier` (decimal half-open mirror of the existing
  `ShippingNumericTier` for the amount axis), `ShippingCapturedQuoteMatchedConditions`,
  `ShippingCapturedQuoteSelection` (row/group identity, draft/revision/format provenance, raw type id,
  min+max delivery days, stored settings, stored week, matched tiers).
- `ShippingCapturedRevisionQuote` with `IsQuoted` and an explicit
  `Eligibility = ShippingCapturedQuoteEligibility.NotEvaluated` so a monetary answer can never be read as
  customer eligibility or activation.
- `ShippingCapturedRevisionQuoteRequest` never carries HTTP/browser/clock/DB input.

Evaluation order and rules (all deterministic, no implicit read):

1. **Identity first** — owner kind/tenant/seller, draft id and source revision must equal the caller's
   expectation, else `IdentityMismatch` and no money. No newest/latest fallback exists.
2. **Settings** — missing stored settings is `NotConfigured`; `CarrierApi` is
   `CalculationModeUnavailable` / `carrier_mode_unavailable`; `Free` is `CalculationModeUnavailable` /
   `free_not_implemented` (a real expense is never zeroed into a "free" claim); unknown enum values are
   `ConfigurationInvalid`.
3. **Input sanity** — negative measurements/amount and a structurally incomplete destination path
   (empty country id, empty node id, service area without city) fail closed.
4. **Group resolution** — a group matches when any of its stored `NodeKind`+`NodeId` areas equals the
   verified path node. Several matched areas in ONE group stay one group; zero groups is
   `CoverageMissing`; more than one group is `GroupAmbiguous` (no first/deepest/cheapest choice).
5. **Currency** — missing on either side is `CurrencyMissing`; a different currency is
   `CurrencyMismatch`. No FX.
6. **Three axes AND, rows are alternatives** — weight/count/amount tiers are half-open `[min, max)`;
   an absent axis is unconditional; an explicit `0` is a present measurement; a known axis that does not
   contain the declared measurement excludes the row outright (so it can never block a valid row by
   being "undetermined"); a known axis without its measurement is `MeasurementRequired`.
   Exactly one matching row wins, zero is `ConditionNotMatched`, more than one is `ConditionAmbiguous`
   (even with identical amounts). Invalid stored bounds/amounts/days are `ConfigurationInvalid`, never a
   normalised tier.
7. **Same row, both components** — freight and packaging are read from the winning row only (no
   scope-only packaging resolver, which would lose matched-row identity). `total = freight + packaging`;
   `Prepaid` → checkout = total and carrier collect = 0; `CollectOnDelivery` → checkout = packaging and
   carrier collect = freight (the existing module payer semantics; packaging is never carrier-collectable).
8. **Metadata only** — min+max delivery days, the stored week/timezone and settings travel as-is; nothing
   is converted into preparation/lead days or pickup windows and no localized/geographic label is guessed.

## 2. Guard hygiene (same task, no new R3 loop)

`ShippingTransactionBoundary` now names exactly one additional type-only member,
`DisposeCaptureTransactionAsync`, in `AllowedTypeMarkerMembers`. That member may mention
`IDbContextTransaction` and may only dispose it: `AllowedTransactionStartMembers` still contains only
`SaveAuthoringReplacementAsync` and `CaptureRevisionAsync`, so a `BeginTransaction` inside the disposal
helper is still a violation. A narrow proof was added (see §3, case 12) and the boundary diagnostic now
lists the exact allowed member set. No file-wide exemption was opened and `SaveAuthoringReplacementAsync`
plus its two callers are untouched.

## 3. Executed proof (only what actually ran)

Build: `Tooba.Shipping.Domain` → **0 errors**; `Tooba.Shipping.Tests` → **0 errors**.

`dotnet test --filter FullyQualifiedName~ShippingCapturedRevisionQuoteTests` → **11 executed / 11 passed**:

1. `Two_rows_in_the_same_area_take_freight_and_packaging_from_the_SAME_three_axis_match`
2. `Tiers_are_half_open_so_a_boundary_prices_once_and_touching_ranges_never_overlap`
3. `A_missing_measurement_for_a_known_axis_is_not_quoted_and_is_never_a_zero_rate`
4. `A_known_axis_that_does_not_match_excludes_its_row_before_any_missing_axis_is_considered`
5. `Two_matching_rows_are_ambiguous_even_when_their_amounts_are_identical`
6. `Missing_coverage_multiple_groups_currency_and_identity_are_all_rejected_without_money`
7. `An_incomplete_destination_path_or_incoherent_stored_bounds_fails_closed`
8. `Prepaid_pays_everything_at_checkout_and_collect_on_delivery_moves_only_the_freight`
9. `An_unconfigured_or_non_manual_method_is_not_quoted_and_never_a_zero_price`
10. `The_quote_carries_full_provenance_and_the_captured_payload_survives_a_later_edit`
11. `The_existing_legacy_quote_paths_are_unchanged`

`dotnet test --filter FullyQualifiedName~OperationalQuoteCompatibilityTests` (exact class, run once) →
**23 executed / 23 passed**, including the new
12. `Transaction_guard_allows_only_the_named_capture_disposal_helper_and_never_a_begin_inside_it`
    (baseline clean tree; a begin smuggled into the named disposal helper is rejected; any OTHER member
    mentioning the transaction type is rejected).

No whole-batch replay: no 119/43/18/8 suite, no guard/compatibility/structure/snapshot wildcard, no
provider/Postgres/browser/HTTP work in this task. Every number above counts only cases actually executed.

### Worked example (used in case 1 and case 8)

Captured group, currency `IRT`, final group goods amount after discount and tax `200 000.00`:

| row | weight (g) | item count | merchandise (IRT) | freight | packaging | delivery days |
|---|---|---|---|---|---|---|
| matched | `[0, 5000)` | `[1, 10]` | `[100000.25, 5000000.75)` | `120000.00` | `5000.00` | `1..3` |

Result: `Quoted`, total `125000.00` (freight `120000.00` + packaging `5000.00` **from the same row**).
`Prepaid` → payable at checkout `125000.00`, carrier collect `0`. `CollectOnDelivery` →
payable at checkout `5000.00`, carrier collect `120000.00` (the case-8 variant uses
`180000.50` + `9500.75` = `189501.25`).

## 4. Honest limits

- This is a **monetary primitive only**. It does not activate/publish a method, has no Contracts reader,
  no DI registration, no endpoint, no MediatR request, no Order/Storefront consumer and no live pointer.
  `Eligibility` is always `NotEvaluated`; operational readiness, customer eligibility, funded cost and
  provider/funder authority are NOT proved.
- The destination path is a typed node-key path; verifying that it really exists in Geography (and that
  the ancestors are the real ones) remains the future adapter's job. Arbitrary UUIDs are not trusted by
  this method, only structurally checked.
- The final goods amount after discount and tax is caller-declared; no tax/discount allocation is
  computed here and `CartSubtotalExclusive` is deliberately not used as a substitute.
- `CarrierApi` and `Free` stay explicitly unavailable (never zero-priced).
- Ambiguous rows and invalid stored bounds cannot be produced by today's authoring guard; the tests that
  cover them rebuild the captured payload with `with` to prove the evaluator fails closed on
  legacy/corrupt captured data instead of resolving it.
- No schema/migration/capture-code change, no shared database, no user-draft write, no API/flag/seed,
  no Host/FE restart, no gate change. TB185 stays paused, storefront gate stays default-OFF,
  PendingAllocation untouched, user draft/revision/unsaved tabs and Host 5088 / FE 3000 preserved.
- This is not a Storefront completion claim; a later slice must map this via Contracts and own
  publication/selection/commit plus the real amount authority.

## 5. Next concrete step (not issued)

A Contracts-only reader/selection edge that resolves an active captured revision + the group money
authority (tax-inclusive goods amount, measurements), then projection/selection/commit. Not another
audit.
