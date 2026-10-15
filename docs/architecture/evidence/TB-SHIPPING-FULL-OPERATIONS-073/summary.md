# TB-SHIPPING-FULL-OPERATIONS-073 — Evidence Summary

**Status: PASS**

| Field | Value |
| --- | --- |
| Task | TB-SHIPPING-FULL-OPERATIONS-073 |
| Parent | TB-SHIPPING-FULL-OPERATIONS-072 |
| Mode | BOUNDED_WEIGHT_QUANTITY_PRICING_DOMAIN_AND_PERSISTENCE |
| Channel | tooba-main |
| Expected-Base-SHA | b6ec9c5926ff85a45920e8dd5d0883cb82877ad5 |
| Migration | 20261015063838_WeightAndCountRateTiers |
| Tests | 192 / 192 passing (`Tooba.Shipping.Tests`) |
| Live target | Development `tooba_alpha`, host `:5088`, tenant `store-alpha` |

## Deliverable

Explicit optional bounded **weight** (grams, non-negative integer) and **item-count**
(non-negative integer) conditions on option-/service-/geography-scoped rate rules, with half-open
`[Min, Max)` semantics, deterministic non-ambiguous precedence, fail-closed handling of missing
measurements, and a narrow additive migration. Pre-existing unconditioned rows are unchanged.

## Precedence (deterministic, never row order)

```text
option identity → destination depth → measurement tier
```

A measurement tier refines only *inside* the winning option+destination identity. Two equally
specific unconditioned rows ⇒ `ScopeConflict`. Two conditioned tiers matching one parcel ⇒
`ConditionConflict`.

## Boundary proofs

| Proof | Result |
| --- | --- |
| weight 1999 → `[0,2000)` = 120000 | PASS |
| weight 2000 → `[2000,5000)` = 260000 | PASS |
| count 5 → `[1,6)` = 100000 | PASS |
| count 6 → `[6,11)` = 180000 | PASS |
| open upper bound `[5000, ∞)` | PASS |
| missing measurement → `measurement_required` | PASS |
| outside every tier → `condition_not_matched` | PASS |
| weight+count intersection | PASS |
| overlapping/duplicate/reversed/zero-width/negative refused | PASS |
| legacy unconditioned rows unchanged | PASS |

## Live HTTP proof (Development `:5088`)

Full transcript: `log-live-roundtrip.txt`; driver: `probe-tiers.ps1`.

1. `GET /__platform-commerce` → `dev-singlestore`, `store-alpha`, `tenant-alpha`, `127.0.0.1`.
2. Baseline: 3 `tipax` rows, `condition = null`.
3. `POST [0,2000)` and `POST [2000,5000)` → `200`, persisted with condition.
4. Same-identity weight tier + count tier → `400 shipping.rate_policy.tier_overlap` (correct: the
   pair genuinely intersects).
5. Count tier on a distinct identity → `200`; combined weight+count on a disjoint weight axis → `200`.
6. Refusals: overlap `400 tier_overlap`, duplicate `400 scope_duplicate`, reversed/zero-width/negative
   `400 validation.failed`, unknown option `400 service_option.unknown`.
7. Unauthenticated read → `401 admin.actor.missing`.
8. Rollback ledger emptied; baseline restored to exactly **9** rate policies.

## Safety

- No Production/staging write. No scratch/parallel database. No global migrator.
- No Order/Checkout/Payment/Settlement/Fulfillment/Geography/Host production edits.
- No frontend change in 073. No new quote authority. No paid-snapshot mutation.
- All synthetic Development rows deleted; pre-existing 9 rate policies and 7 coverage grants intact.

## Reserved for later reviewed tasks

Packaging fees · freight payer modes (prepaid/collect) · schedule availability gating ·
**atomic** capacity enforcement (capacity stays configuration-only) · 074 tier editor UI.

## Artifacts

```text
docs/planning/shipping/SHIPPING-FULL-OPERATIONS-073.md
docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-073/probe-tiers.ps1
docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-073/log-live-roundtrip.txt
docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-073/log-tests.txt
docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-073/host-stdout-073.excerpt.txt
```
