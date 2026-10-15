# SHIPPING-FULL-OPERATIONS-074 — tiered rate admin UI (and packaging foundation)

Task: `TB-SHIPPING-FULL-OPERATIONS-074` · Parent: `...-073` · Timebox: 45 minutes HARD

## Scope decision

Two missions were authorized. Mission A (tier editor over the existing API) was executed and proven.
Mission B (packaging cost foundation) was **not started**, because a safe slice did not fit the
timebox and the task explicitly forbids marking packaging complete merely because a UI input exists
without a persisted owner. See the evidence summary for the exact open gap.

## Canonical contract reused (not redefined)

073 is accepted as canonical. This task consumes the already-proven condition contract verbatim:

```text
condition: { weightGrams: { min, max }, itemCount: { min, max } }   // or null
```

- `min` inclusive, `max` exclusive (`[min, max)`); `max = null` means no upper bound.
- Weight is integer grams; item count is a non-negative integer of physical shipment items.
- `null` condition (or a condition with no axis) is an unconditioned base rule.
- The 073 guard still owns overlap/duplicate refusal and the missing-measurement fail-closed rule.

No competing engine, no second DTO, no semantic change was introduced.

## Frontend design

### New file — `shipping-rate-tier-model.ts`

Pure presentation/validation layer, no fetching, so the tier semantics stay testable without a
server. It owns:

| Export | Responsibility |
| --- | --- |
| `parseKilogramsToGrams` | exact kg→g conversion, string/integer arithmetic only |
| `formatGramsAsKilograms` | exact g→kg text, trailing zeros trimmed |
| `parseItemCount` | non-negative integer items |
| `conditionFormFromPolicy` | stored condition → form, losslessly (`null` → untouched form) |
| `conditionFormToWrite` | form → wire shape, or `null` when nothing is enabled |
| `validateConditionForm` | half-open range, negative, ceiling and zero-width/reversed refusal |
| `describeConditionFa` / `conditionSummaryFa` | half-open + intersection wording, or explicit unconditioned |
| `tierErrorFa` | stable backend codes → adjacent Persian guidance |

The single most important design choice is the exact conversion. The API carries integer grams so
the boundary is unambiguous, while a human enters kilograms. Routing that through `Number` and
float multiplication would make `1.999` unreliable; instead the string is split and the fraction is
padded to three digits, so `1999` and `2000` can never collapse into each other.

### Changed — `shipping-rates-coverage-api.ts`

- `ShippingRatePolicyWriteRequest` gains `condition?: ShippingRateConditionWrite | null`.
- `ShippingRatePolicyAdminItem` gains `condition: ShippingRateConditionWrite | null`.
- `mapCondition`/`mapTierAxis` read the persisted condition in both camelCase and PascalCase; an
  absent or empty condition maps to `null`, never to an invented `[0, ...)` tier.
- Create and update send the exact wire shape.

### Changed — `shipping-rates-coverage-dialog.tsx`

- Each rate row now shows «شرایط مرسوله: …» plus a «پلهای» badge when conditioned.
- A `ConditionEditor` (two `ConditionAxis` rows) appears in both the create form and the inline edit
  form, with separate state so an edit form can never mirror another row's condition.
- Unconditioned rules render the explicit «بدون شرط اندازه — برای همهٔ مرسولهها».

The file remains under the 800 LOC architecture threshold and is not a baseline shrink-only entry.

## Behavior preserved

- Destination scope, `optionCode`, free-above-threshold and currency handling are untouched; the
  update path still echoes the stored scope and option identity.
- A pre-074 client body without `condition` still means unconditioned.
- Coverage still authorizes a destination only; the tier section prices a shipment and claims no
  live Checkout quote.

## Proof

Live Development UI proof, focused tests, typecheck and guard results are in
`docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-074/`.
