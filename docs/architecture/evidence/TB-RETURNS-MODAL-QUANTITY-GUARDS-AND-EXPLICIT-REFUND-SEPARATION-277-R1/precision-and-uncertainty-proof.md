# TB-277-R1 — precision and uncertainty proof

Every fact below is from the final code state. "Controlled" means a test double/port or an in-memory provider; no real
PSP, Wallet or Inventory effect was produced, and no row was written to the canonical database by any test.

## A — the owning unit's quantum is enforced where the row would be written

Source: `src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Directories/ReturnDirectory.Receipts.cs`

Ordering inside one receipt command (nothing is inserted before the gate):

1. courier-origin refusal (`return.receipt.origin_invalid`);
2. **exact stored occurrence replay** — a receipt already stored for the same `OccurrenceKey` with the same
   item/quantity/condition set answers the durable snapshot; a differing intent is `return.receipt.conflict`. This runs
   *before* any new capacity or unit check, so a receipt still replays after remaining reaches `0`;
3. requested-item read (`ReturnItemId`, **`OrderLineId`**, `Quantity`, `Disposition`);
4. coherent-evidence gate (`return.recovery.evidence_conflict`);
5. state gate for a non-`Requested` case (`return.receipt.state_invalid`);
6. every line must belong to the case (`return.receipt.line_invalid`);
7. **TB-277-R1 unit gate** — `ReadOwningUnitDecimalPlacesAsync` reads the case's own seller-order return context and maps
   each line's `OrderLineId` to the accepted line snapshot; then
   `OwningUnitQuantity.IsAlignedTo(line.Quantity, places)`; failure ⇒ `return.receipt.unit_quantity_invalid`
   (`400`, `Business`), **zero rows written, no money/stock call**;
8. only then the cumulative over-receipt ceiling and the inserts.

`ReadOwningUnitDecimalPlacesAsync` has **no fallback**: a null context, an empty `OrderLineId`, a line the context does not
answer, a null precision, or a precision outside `0..6` is the same typed refusal.

`OwningUnitQuantity.IsAlignedTo(decimal quantity, int decimalPlaces)` requires (a) `0 ≤ places ≤ 6`,
(b) `quantity > 0` and exactly representable at `numeric(18,6)`, and (c) `quantity * 10^places == decimal.Truncate(same)`.
Because it compares **decimal values**, `1` and `1.000000` are the same whole piece.

Focused facts — `Tooba.Returns.Tests/Behavior/ReturnsReceiptUnitPrecision277R1Tests.cs` (10 / 10):

| Fact | Assertion |
| --- | --- |
| whole pieces | `0.5` on a 0-decimal line ⇒ `return.receipt.unit_quantity_invalid`, zero receipt rows |
| whole pieces accept integers | `1` and `1.000000` accepted and stored as the same quantity |
| 3-decimal grams | `0.125` accepted |
| finer than the unit | `0.0001` on a 3-decimal line refused |
| unanswered metadata | no precision on the owning line ⇒ typed refusal, zero writes |
| foreign/missing line | an order line the context does not answer ⇒ typed refusal |
| exact replay | the same occurrence replays after the ceiling is exhausted |
| replay vs changed body | same key + different quantity ⇒ `return.receipt.conflict` |
| preserved gates | an unruly case still answers its pre-existing state/evidence code first |
| value equality | `1 == 1.000000` never becomes a precision refusal |

Contract: `OrderReturnLineSnapshot.QuantityDecimalPlacesSnapshot` is an additive trailing optional parameter; nothing was
renumbered and no stored data was touched. The bridge maps the same accepted-line snapshot the admin detail read already
publishes, so read and write cannot disagree about the unit.

## B — exact client quantities (no floating point anywhere in the receipt dialog)

`src/frontend/app/returns/return-quantity.ts`

- `QUANTITY_UNITS_PER_ONE = 1_000_000`; the largest accepted text (`999999999.999999`) yields
  `999999999999999`, well inside `Number.MAX_SAFE_INTEGER`, so the integer is exact with no `BigInt`.
- `parseExactUnits` reads the whole/fraction parts as **strings** and combines them as integers — the value never passes
  through `Number(text)` for its fractional part.
- `unitsFromWireNumber` reconstructs an authoritative wire `Number` through `toFixed(6)` (the decimal the server actually
  sent) so a 6-decimal quantity round-trips exactly.
- `normalizeQuantityInput` maps Persian `۰-۹`, Arabic `٠-٩`, `٫` → `.`, and strips `,`/`٬`/spaces.
- `EXACT_QUANTITY = /^\d{1,9}(?:\.\d{1,6})?$/` refuses a finer-than-persisted value (`1.0000001`) instead of rounding it.
- `isAlignedToUnit(units, places)` uses the unit's own quantum (`10^(6−places)`); `quantityStep` derives the field step
  from the same precision.

`src/frontend/app/returns/return-receipt-quantities.ts`

- `evaluateQuantityEntry(raw, decimalPlaces, ceilingUnits)` ⇒
  `empty | malformed | unit_unknown | ceiling_unknown | precision | over_ceiling | null`.
- `collectReceiptItems(lines)` sums `usable + damaged` with `addUnits` and compares with `compareUnits` against ONE
  `ceilingUnits`; both fields are individually validated first, so neither portion can fund the other.
- `describeQuantityProblem` produces the concise Persian message per problem (field-level), `formatCeilingFa` prints the
  authoritative remaining in Persian digits.

Workspace wiring (`admin-return-workspace.tsx`, 1386 → 1301 lines):

- `ReceiptDraftLine.ceilingUnits` is exact integer subtraction (`subtractUnits(unitsFromWireNumber(requested),
  unitsFromWireNumber(received))`) and `null` when the authoritative values cannot be reconstructed — the line is then
  blocked (`ceiling_unknown`), never given a guessed ceiling.
- `decimalPlaces === null` ⇒ `unit_unknown` and the field is blocked with
  «دقت واحد این قلم از خط سفارش تأیید نشد؛ تا آن زمان مقدار این قلم ثبت نمی‌شود.» — the old six-decimal default is gone.
- The confirm button's disabled state and the confirm handler both call `collectReceiptItems`, so an invalid amount is
  refused locally before any envelope is stored or any request is sent.
- Fields keep `min-h-11` (44px) and the draft starts empty on both conditions (nothing is preselected).

Focused facts — `app/returns/return-quantity.test.ts` (7 / 7): `0.1 + 0.2 == 0.3` **and** `0.1 + 0.2 !== 0.3` in plain JS
(the defect being removed); `1 − 0.8 == 0.2` exactly while `1 - 0.8 !== 0.2`; `1 == 1.000000`; Persian/Arabic
normalization; `1.0000001`/`0`/`""`/`1e3` refused; piece vs gram quantum; step derivation.

Mounted facts — `app/admin/admin-return-workspace-277.mount.test.ts` (8 / 8, unchanged intent): an over-ceiling amount is
refused inside the dialog before storage/request; the unit's precision bounds the field; usable + damaged share one
ceiling; no action form renders inline; approve sends only the decision; a short rejection reason cannot submit.

Queue-level facts — `app/admin/admin-returns-work-queue.test.ts` (26 / 26): the untouched dialog submits nothing, `0.5`
for a piece unit is `precision`, `2` against a 2-piece ceiling is accepted, `3` is `over_ceiling`, `usable 2 + damaged 1`
against a 2-piece ceiling is refused as a sum, an unknown precision is blocked, and the fields keep the 44px minimum.

## C — the retained-intent refusal set

`PROVEN_NO_EFFECT_REFUSAL_CODES` (final):

```
return.quantity_invalid, return.quantity_precision, return.duplicate_line, return.idempotency_required,
return.rejection_reason_required, return.receipt.occurrence_required, return.receipt.line_invalid,
return.receipt.unit_quantity_invalid, return.line_missing, return.receipt.origin_invalid
```

Removed in R1 (each of these can only describe the attempt that just failed, so it can never prove an earlier send did not
write): `admin.authorization.denied`, `admin.actor.missing`, `order.operation.denied`, `validation.failed`,
`return.not_owner`, `return.non_returnable`, `return.expired`, `return.not_delivered`, `return.not_paid`,
`return.fulfillment_missing`, `return.stale`, `return.already_approved`, `return.already_rejected`,
`return.idempotency_conflict`, `return.decision.reason_conflict`, `return.reject.invalid_state`,
`return.receipt.actor_required`, `return.receipt.state_invalid`, `return.receipt.over_received`,
`return.receipt.conflict`, `return.receiving.incomplete`, `refund.retry.invalid_state`, `refund.payment_missing`,
`refund.already_started`, `refund.already_completed`, `refund.destination.invalid`, `return.refund.first_not_lawful`,
`return.recovery.kind_invalid`, `return.recovery.attempt_mismatch`, `return.recovery.attempt_unbound`,
`return.recovery.state_mismatch`, `return.recovery.receiving_required`, `return.recovery.evidence_conflict`,
`return.recovery.transaction_owned`, `return.recovery.stock_binding_required`.

Consequences (asserted in `admin-returns-work-queue.test.ts`): a retained envelope is cleared only by an authoritative
answer or by a body-dictated refusal; `admin.authorization.denied`, `admin.actor.missing`, `order.operation.denied`,
`return.not_owner`, `return.receipt.state_invalid`, `return.receipt.conflict`, `return.stale`,
`return.already_approved`, `return.idempotency_conflict` are explicitly **not** treated as proof of no effect. Every
remaining token is still localized (`admin-error-map.test.ts` 8 / 8).
