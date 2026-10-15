# SHIPPING-FULL-OPERATIONS-073 — Weight / Quantity Pricing Foundation

- **Task:** `TB-SHIPPING-FULL-OPERATIONS-073`
- **Parent:** `TB-SHIPPING-FULL-OPERATIONS-072`
- **Mode:** `BOUNDED_WEIGHT_QUANTITY_PRICING_DOMAIN_AND_PERSISTENCE`
- **Channel:** `tooba-main` · **Worker:** `tooba-worker-01` · **Agent:** cursor
- **Expected-Base-SHA:** `b6ec9c5926ff85a45920e8dd5d0883cb82877ad5`

## 1. Scope and ownership

This task adds the **weight / item-count pricing foundation** to the Shipping module only.
It is the pricing *condition* layer — not packaging fees, not freight payer mode, not atomic
capacity enforcement, and explicitly **not** a Checkout pricing cutover.

| Capability | Owner | Status after 073 |
| --- | --- | --- |
| Service / option identity | Fulfillment | unchanged (read-only seam) |
| Place identity | Geography | unchanged (read-only seam) |
| Rate policy, coverage, tiers | **Shipping** | **extended** |
| Schedules, capacity | Shipping | unchanged (071/072, config-only capacity) |
| Delivery promise / snapshot | Order | unchanged |
| Money handling | Payment / Settlement | unchanged |

No foreign `DbContext`, no cross-module FK, no `Results.Json`, no privilege bypass. All new
Contracts, Domain, Application, Infrastructure, Endpoints, and Tests changes live under
`src/backend/Modules/Shipping/**`.

## 2. Tier semantics (normative)

### 2.1 Unit and representation

- **Weight** is a non-negative `long` in **grams** (`ShippingWeightUnit.Gram`). Integer grams are
  chosen deliberately so tier boundaries are exact and no floating/rounding error can leak into a
  commercial decision.
- **Item count** is an `int` counting **shipment items** — the number of physical units that get
  packed. It is **not** the cart line count and **not** the summed `Quantity` of the cart.
- Both axes are **optional**. A rule with neither axis is an *unconditioned* rule and behaves
  exactly as every pre-073 row already did.

### 2.2 Half-open interval `[Min, Max)`

`ShippingNumericTier` is a half-open interval: **`Min` is inclusive, `Max` is exclusive**.

- `[0, 2000)` and `[2000, 5000)` do not overlap and leave no gap: `2000` is priced exactly once,
  in the second tier.
- Only the upper bound may be open (`Max == null` ⇒ "from `Min` upward"). `Min` is always concrete;
  "up to 2000" is expressed as `Min = 0`.
- Two adjacent half-open intervals are **not** an overlap (`Overlaps` uses `Min < otherEnd &&
  other.Min < thisEnd`).

### 2.3 Multi-axis conditions are an intersection

When both `Weight` and `Count` are present on one rule, a parcel must fall inside **both** intervals
to match. This is an intersection, never a union.

### 2.4 Documented boundaries

| Axis | Tier A | Tier B | Boundary proof |
| --- | --- | --- | --- |
| Weight (g) | `[0, 2000)` | `[2000, 5000)` | 1999 → A, 2000 → B |
| Count | `[1, 6)` | `[6, 11)` | 5 → A, 6 → B |

### 2.5 Fail-closed measurement rules

- A **conditioned** rule never matches a request that **omitted** the required axis.
  Outcome: `MeasurementRequired` / `shipping.quote.measurement_required`.
- A measurement **outside every declared tier** is reported distinctly as
  `ConditionNotMatched` / `shipping.quote.condition_not_matched` — "we can't price without a size"
  is a different fact from "this size is in no tier".
- Shipping **never derives** weight or count from cart subtotal or cart line count.

### 2.6 Zero weight / zero count

`0` is a real measurement. A `[0, 2000)` weight tier matches `0 g`. A `[0, 6)` count tier matches
`0` items; a `[1, 6)` count tier deliberately does **not** — zero items is not a shipment.

## 3. Deterministic precedence

Precedence is resolved in this exact order, and **never** by row order:

```text
1. Option identity   → option-specific rule beats service-level rule (service-level = documented fallback)
2. Destination depth → deepest level wins (ServiceArea > City > AdministrativeArea > Country)
3. Measurement tier  → only INSIDE the winning identity+scope group do weight/count tiers refine
```

Consequences:

- A weight tier **never** overrides option identity. A `standard` weight tier cannot price an
  `express` request.
- A weight tier **never** overrides destination depth. A city row prices even if it is conditioned,
  while an area row with a "better" weight fit loses.
- Tiers refine only among rows that already share **both** option and destination scope.

Ambiguity is reported, never silently resolved:

- Two equally-specific **unconditioned** rows → `ScopeConflict` / `shipping.scope.ambiguous`.
- Two **conditioned** tiers matching the same parcel → `ConditionConflict` /
  `shipping.quote.condition_ambiguous`.

### 3.1 Backward-compatible fallback

If a group has one unconditioned base row and conditioned exception rows, the unconditioned row
remains the general rule for measurements no tier declares. This is exactly how the pre-existing
9 rate policies keep their behaviour: their `condition` columns are `NULL` ⇒ unconditioned.

## 4. Persistence

Additive migration `20261015063838_WeightAndCountRateTiers` (Shipping module scope):

- Adds nullable `min_weight_grams`, `max_weight_grams` (`bigint`) and `min_item_count`,
  `max_item_count` (`integer`) to `shipping.rate_policies`.
- Replaces the unique index `ux_rate_policies_service_option_scope` with a tier-aware version
  covering the four new columns, using `NULLS NOT DISTINCT` (PostgreSQL 15+) so two `NULL` tier
  columns on the same identity still collide as before.
- Adds check constraints:
  - `ck_rate_policies_weight_tier_coherent` — `min >= 0`, `min <= 1_000_000_000`, `max > min`, `max <= 1_000_000_000`.
  - `ck_rate_policies_count_tier_coherent` — `min >= 0`, `min <= 1_000_000`, `max > min`, `max <= 1_000_000`.
  - `ck_rate_policies_tier_bounds_paired` — `max` set implies `min` set, per axis.

**No existing row is rewritten or deleted.** The 9 pre-existing rate policies and 7 coverage grants
survive with `NULL` tier columns and unchanged results.

### 4.1 Rollback

`Down` drops the three check constraints, drops the four columns, and restores the original
six-column unique index. Reverting is loss-free for pre-073 rows.

## 5. Admin API integration

The existing Shipping admin CQRS paths were extended rather than replaced:

- `ShippingRatePolicyWriteRequest` / `ShippingRatePolicyAdminItem` carry an optional
  `ShippingRateConditionWrite` (`ShippingNumericTierWrite` per axis).
- `ShippingAdminValidators` reject negative bounds (`shipping.validation.tier_min_negative`),
  reversed / zero-width ranges (`shipping.validation.tier_range_invalid`), and oversized bounds
  (`shipping.validation.tier_out_of_range`).
- `ShippingRateCoverageDirectory` gained tier-aware scope comparison and
  `TierOverlapsExistingAsync`, so a new/updated tier is refused when it would collide with an
  existing tier on the same identity+scope. Exact duplicates are refused as
  `shipping.rate_policy.scope_duplicate`; partial overlaps as `shipping.rate_policy.tier_overlap`.
- The domain aggregate validates independently, so the invariant holds regardless of caller.

## 6. Verification

### 6.1 Unit tests (`Tooba.Shipping.Tests`, 192 passing)

`Rates/ShippingWeightCountRateTierTests.cs` covers:

- 1999/2000 and 5/6 exact boundaries; open upper bound; zero weight / zero count.
- Missing measurement (`MeasurementRequired`) vs. out-of-range (`ConditionNotMatched`).
- A weight-only request does not satisfy a count condition.
- Option identity and destination depth beat tiers; city row wins over area row.
- Two matching tiers → `ConditionConflict`; weight+count is an intersection.
- Reversed / zero-width / negative / oversized tiers refused with stable codes.
- Overlap detection is symmetric and respects adjacency and the two-dimensional partition.
- Coverage stays a service+destination decision for tiered rows.
- Currency equality and the free-shipping threshold still apply to tiered rows.
- Legacy unconditioned rows keep exact prior behaviour, with and without a parcel.

`Admin/ShippingWeightCountTierAdminTests.cs` covers admin write/read roundtrip and the
validation refusals.

### 6.2 Live Development proof (`tooba_alpha`, host `:5088`)

`probe-tiers.ps1` + `log-live-roundtrip.txt` prove against the running Development host:

- Positive target provenance: `/__platform-commerce` reports `dev-singlestore`, `store-alpha`,
  `tenant-alpha`, `127.0.0.1`.
- Baseline: 3 pre-existing `tipax` rows, all `condition = null`.
- `[0,2000)` and `[2000,5000)` grams tiers persisted and read back with their condition.
- Cross-axis ambiguity on the same identity is refused (`shipping.rate_policy.tier_overlap`).
- A count tier on a distinct identity is accepted; a combined weight+count tier on a disjoint
  weight axis is accepted.
- Overlapping, duplicate, reversed, zero-width, negative and unknown-option writes are all refused
  with stable `400` codes and no partial write.
- Unauthenticated read is `401` (`admin.actor.missing`).
- Rollback ledger records every synthetic row; all were deleted, restoring the pristine
  9-row baseline.

### 6.3 Reserved for later reviewed tasks

Explicitly **not** delivered here (no pretending otherwise):

- Packaging fees.
- Freight prepaid vs. collect-on-delivery payer modes.
- Schedule availability gating of a quote.
- **Atomic** capacity enforcement (capacity remains configuration-only).
- The 074 admin tier editor (frontend).

## 7. Numeric limits

| Limit | Value |
| --- | --- |
| Weight bound | `0 … 1_000_000_000` grams |
| Count bound | `0 … 1_000_000` items |
| Open upper bound | `Max = null` |

## 8. Changed paths

```text
src/backend/Modules/Shipping/Tooba.Shipping.Contracts/Admin/ShippingAdminContracts.cs
src/backend/Modules/Shipping/Tooba.Shipping.Contracts/Errors/ShippingErrorCodes.cs
src/backend/Modules/Shipping/Tooba.Shipping.Contracts/Rates/ShippingRatePolicyContracts.cs
src/backend/Modules/Shipping/Tooba.Shipping.Domain/Aggregates/ShippingRatePolicy.cs
src/backend/Modules/Shipping/Tooba.Shipping.Domain/Rates/ShippingRatePolicyEngine.cs
src/backend/Modules/Shipping/Tooba.Shipping.Application/Validation/ShippingAdminValidators.cs
src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Persistence/ShippingDbContext.cs
src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Persistence/Migrations/20261015063838_WeightAndCountRateTiers.cs
src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Persistence/Migrations/20261015063838_WeightAndCountRateTiers.Designer.cs
src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Persistence/Migrations/ShippingDbContextModelSnapshot.cs
src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Rates/ShippingRateCoverageDirectory.cs
src/backend/Modules/Shipping/Tooba.Shipping.Tests/Rates/ShippingWeightCountRateTierTests.cs
src/backend/Modules/Shipping/Tooba.Shipping.Tests/Admin/ShippingWeightCountTierAdminTests.cs
src/backend/Modules/Shipping/Tooba.Shipping.Tests/Persistence/ShippingPersistenceModelTests.cs
```

## 9. Authority boundary

Checkout / Order / Payment / Settlement are untouched. No quote authority was activated, no paid
snapshot mutated, no commercial tariff invented, no nationwide coverage grant created. The quote
engine remains consumed internally; Shipping still exposes only its existing admin endpoints.
