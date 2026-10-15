# TB-SHIPPING-FULL-OPERATIONS-074 — summary

Parent: TB-SHIPPING-FULL-OPERATIONS-073 · Channel: tooba-main · Worker: tooba-worker-01
Mode: `BOUNDED_TIERED_RATE_ADMIN_UI_AND_PACKAGING_FOUNDATION`
Expected-Base-SHA: `d6c9d5ec2d21331c34cf22f414ecdc1bea5d4606` · Stop-State: `STOP_FOR_ARCHITECT`

## Overall status

- **Mission A — COMPLETE.** The «شرایط مرسوله» tier editor works end to end over the real
  Development UI on the *existing* per-service «نرخ و پوشش» dialog, using the canonical 073
  condition contract.
- **Mission B (packaging cost) — NOT STARTED.** No packaging fee contract, domain, persistence,
  migration, admin API or UI was written. Graded honestly rather than faked.

## Mission A — what was built

A compact «شرایط مرسوله» section was added to the existing
`shipping-rates-coverage-dialog.tsx`, plus a new cohesive model file
`shipping-rate-tier-model.ts`. Nothing about the 073 pricing semantics was redefined: the editor
reads and writes exactly `condition: { weightGrams: { min, max }, itemCount: { min, max } }` or
`null` for an unconditioned rule.

- **Two independent opt-in axes.** Weight and item count each have their own enable checkbox, so
  unconditioned, weight-only, count-only and both-intersected pricing are all expressible.
- **Grams on the wire, kilograms for humans.** `parseKilogramsToGrams` converts with string and
  integer arithmetic only. `1.999` becomes exactly `1999`; `2.0005` is refused rather than rounded,
  which is what stops the 1999/2000 boundary from ever collapsing.
- **Half-open labels.** Every range is labelled «از … شامل، تا … غیرشامل»; an empty upper bound is
  described as «بدون کران بالا», never as zero.
- **Lossless round trip.** `conditionFormFromPolicy` maps a stored condition into the form and
  `conditionFormToWrite` maps it back, so saving an existing unconditioned rate leaves it
  unconditioned (proven live at the database level).
- **Explicit non-equivalence.** The count hint states that the count is the physical shipment item
  count, not cart lines and not an inferred cart quantity.
- **Stable error mapping.** `shipping.rate_policy.tier_overlap`, `...scope_duplicate`,
  `shipping.validation.tier_range_invalid`, `...tier_min_negative`, `...tier_out_of_range` map to
  adjacent Persian guidance next to the field.
- **Rate vs coverage untouched.** Coverage still only authorizes a destination; the tier section
  prices a shipment and never claims a live Checkout quote.

## Ownership / boundary

| Concern | Owner |
| --- | --- |
| Tier form model, exact conversions, validation, Persian descriptions | `shipping-rate-tier-model.ts` (new, 300 LOC) |
| Wire mapping of the persisted condition | `shipping-rates-coverage-api.ts` (existing typed adapter) |
| Editor UI + row rendering | `shipping-rates-coverage-dialog.tsx` (existing feature surface) |

No new page, no new admin route, no parallel client or rule engine, no `design-system/` change, no
backend change, and no cross-feature import. The dialog stays at 733 LOC, below the 800 threshold.

## Live proof (Development only)

Target positively identified as Development: `/__platform-commerce` →
`edition=SingleStore, deploymentId=dev-singlestore, tenantId=store-alpha`; frontend
`http://127.0.0.1:3000`, host `http://127.0.0.1:5088`, database `tooba_alpha`. No production or
staging write.

1. Created adjacent weight tiers `[0,2000)` and `[2000,5000)` grams at real identity
   tipax / `standard` / Alborz; both persisted and re-rendered with exact boundary text.
2. A deliberately overlapping `[1500,3000)` write returned `400 shipping.rate_policy.tier_overlap`
   with Persian guidance and no partial write.
3. Editing and saving a pre-existing unconditioned row left all four tier columns `NULL`.
4. Toggled a tier active→inactive successfully.
5. Deactivated both task-owned synthetic fixtures via the approved API; no active synthetic fixture
   remains and the 068/069 parity rows are untouched.

Full log: `log-live-ui-proof.txt`. Screenshot: `tier-editor-live.png`.

## Tests actually run

- Focused frontend tests (4 files, including the new tier model and API suites): **53 pass, 0 fail**.
- `npx tsc --noEmit`: **clean**.
- Architecture guards (import-boundary, feature-boundary, flat-folder, test-discovery): **7 pass**.
- `frontend-source-size`: 5 failures, **all pre-existing** in unmodified baseline god-files that
  this task never touched; none of this task's files appear in the list.

## Compatibility

- Existing unconditioned rates keep their behavior and are rendered explicitly as unconditioned.
- `optionCode`, destination scope, free-above-threshold and currency handling are unchanged; the
  update path still echoes the stored scope and option identity so nothing can be silently re-pointed.
- The API client sends `condition` alongside the existing fields, so a pre-074 client body without
  `condition` still means unconditioned.

## Remaining blockers / handoff

- **Mission B is open.** A packaging fee needs a Shipping-owned Contracts/Domain/persistence
  representation, an additive EF migration, an authorized admin read/write surface, deterministic
  precedence (service/option/scope), currency and zero-default semantics, and an explicit decision
  on whether free shipping waives packaging. It must not be hidden inside `chargeAmount`, must not
  be silently charged by existing Checkout, and must not be marked complete merely because a UI
  input exists without a persisted owner. None of that was attempted here.
- No delete surface exists for rate policies; cleanup used deactivation plus the id ledger above.
- Commercial Shipping/Checkout readiness is **not** claimed.
- `Worker PASS != Architect ACCEPT`.
