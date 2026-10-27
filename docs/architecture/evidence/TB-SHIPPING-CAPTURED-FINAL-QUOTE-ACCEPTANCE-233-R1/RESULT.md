# TB-SHIPPING-CAPTURED-FINAL-QUOTE-ACCEPTANCE-233-R1 — RESULT

Base: `388ca996f` (issue commit, fast-forward). Worker: `tooba-worker-01`. Status: **EXECUTED — PARTIAL**,
NOT accepted, no user Visual ACCEPT, no live/HTTP/browser run, no frontend/StartPayment/payment-provider
activation, no shared migration apply, no schema change.

## What actually landed (verified: builds 0 errors, focused 36/36)

### A — the direct-caller bypass is closed (source + proof)
`CheckoutDirectory.AcceptCapturedShippingAsync` no longer short-circuits on caller evidence. It now always makes
**one own-Order DB probe** (`ShippingDrafts.AsNoTracking()` + groups + lines) and treats
`stored captured evidence OR caller evidence OR nonblank ExpectedSelectionRevision` as the captured route:

- captured route + `CheckoutEnabled == false` ⇒ `storefront.captured_preview.not_ready` (before any reservation);
- captured route + **no stored captured draft** ⇒ the same `not_ready` (a forged/omitted caller can never be
  treated as legacy);
- genuine legacy (no stored capture, no caller evidence, no revision intent) ⇒ the exact caller plans are
  returned, with **zero foreign module calls** (only the own-Order probe).
- nonblank coupon ⇒ `storefront.captured_selection.coupon_unavailable` before reservation;
- blank/missing/spaced/signed/fractional/exponential `ExpectedSelectionRevision` ⇒ `intent_invalid`;
  a parsed revision that differs from the persisted root `SelectionRevision` ⇒ `conflict`.

`CheckoutDirectory.TryParseSelectionRevision` (ASCII digits only, `"0"` lawful, `4294967296` preserved) is no
longer test-only: it now controls acceptance.

### B — facts come from THIS submit's already-quoted SellerOrder lines, no requote
The 217 membership/authoritative-lines/group-facts logic was extracted once into
`ShippingGroupGoodsFactsBuilder` (one `FactsLine` projection, one code path) and is now used by **both**
`ReadAsync` (preview) and the final acceptance over `IReadOnlyList<OrderLine>` from the actual
`QuoteSellerOrdersAsync` result, using `_orderLineIdByCartLineId` only (never OfferId/quantity matching).
`final amount = Σ TaxInclusiveSnapshot`, shipping excluded, weight/count null.

`StorefrontCapturedGroupProjectionComposer.ComposeFromFactsAsync(request, facts, ct)` is the new typed
facts-input entry: it shares `ValidatePlan`/`ValidateFacts`/candidate/calendar/quote code with `ComposeAsync`
through one private `ComposeValidatedAsync`, and **never** calls `ICheckoutShippingGroupGoodsFactsReader`,
Preview/Submit, Pricing, Promotion or Tax. `ComposeAsync` still reads 217 exactly once.

### C — saved-choice revalidation through existing authorities (source)
Only the stored partition is revalidated: exact stored groups/lines as a complete cart partition, real cart
version, active SingleStore **Store** owner equal to the current trusted tenant, one batch Offer assignment
read (explicit `Platform` responsibility + assigned type equal to the stored evidence type), batched active
Fulfillment types, **one 219 read per distinct full owner+binding** (cached, no candidate enumeration, no
"latest" fallback), then the facts-input composition (one 216 quote per saved group + the shared 213 calendar).
Every stored scalar (snapshot/draft/source revision/format/selection version/area/row/type/currency/
freight/packaging/payable/collect/receiving date/100 ns open+close ticks/timezone/min+max days) is compared
with the current quote and the customer's own selected date; any difference ⇒ `conflict` /
`unavailable` (new selection required, never a silent rewrite or a fresh price).
Destination: the command's province/city is resolved with the existing `IGeographyLookup.ResolveLegacyAsync`
and compared with the persisted priced destination scalars on the draft root; ambiguous/unresolved/service-area
⇒ refuse. No `StorefrontCheckoutService` call from `CheckoutDirectory` (no DI recursion).
Manual charge = `PayableAtCheckoutAmount`; carrier collect stays separate.

### D — real boundary (source)
`CheckoutProcessManager.SubmitAsync` still calls the acceptance **after** `QuoteSellerOrdersAsync` and
**before** the existing `TransactionScope`/reserve/persist/convert, and `PersistCheckoutAsync` materializes
`acceptedShippingGroups ?? command.ShippingGroups`. The accepted list is the only captured list that can reach
persistence; the stored 231 evidence instance is carried unchanged (no authored JSON, no schema/history table).

## What was NOT delivered (prominent)

1. **The REQUIRED POSITIVE real-submit fact was NOT executed.** No `CheckoutProcessManager.SubmitAsync`
   end-to-end run exists for this slice. Inside the source window the acceptance seam could not be driven
   through the real process manager: the acceptance reads `_orderLineIdByCartLineId`, which is populated only by
   the real `QuoteSellerOrdersAsync`, and the available Order test harnesses either stop before the submit
   boundary or throw on the first reservation/cart-conversion call. Building that end-to-end fixture (real
   reserve/convert/persist seams) is exactly the work that HARD40 did not allow. **This is the delivery-gate
   blocker**: the positive proof (two sellers, same Store binding/shared snapshot, independent area/row/date/
   ticks/fees, goods 99/198 reaching their own 216 calls, persisted order groups keeping their own evidence,
   one whole-cart goods quote, zero 217 recursion) is therefore **unproven**.
2. Not executed: stale/missing token, changed cart/partition/address/pointer/type/row/money/date/hours matrix,
   nonblank-coupon captured-route refusal, preview-vs-final goods-change refusal, order-write/cart-convert
   failure rollback of compact evidence, idempotent replay after pointer change. Source paths exist and fail
   closed, but no relational fact was run for them.
3. `AcceptCapturedShippingAsync` requires the real quote-derived cart-line→order-line map; a direct call without
   it refuses `checkout.shipping_groups.invalid`. This is intended (no OfferId inference) but means the seam is
   only exercisable through the real submit path.
4. Public Storefront `CommitAsync` / `ProjectPaymentDraftAsync` remain `not_ready`; no StartPayment payload
   activation; `CheckoutEnabled` stays checked-in **false**; no runtime gate change.
5. 231/232 migrations remain WRITTEN/shared NOT APPLIED; stale EF snapshot retained; no migration/baseline edit.
6. Point-in-time Shipping pointer observation only — no distributed lock and no serializability across
   Shipping/Offer is claimed. The Order-side revision check is exact-match, inside the existing Order
   transaction sequence.

## Proof actually executed

- Disposable task-owned `postgres:17-alpine` at `127.0.0.1:15433/order_tb231/tb231`, identity asserted by the
  existing `OrderEvidenceRelationalTestTarget` parser, container removed and port `15433` verified closed.
- `CapturedCheckoutGateTests` **16/16** (relational own-Order probe): legacy pass-through with the exact same
  plan instance and no refusal; caller evidence refused with the gate OFF; forged caller evidence with the gate
  ON refused (`not_ready`); revision intent alone never falls back to legacy; 12 strict revision-parser facts.
- `ShippingGroupGoodsFactsTests` + `StorefrontCapturedGroupProjectionComposerTests` + the gate class run **once**
  together: **36/36**.
- `Order.Infrastructure` and `Order.Tests` build 0 errors.

Honest limits: 216/219/Offer/Fulfillment/Geography are **controlled Contracts fixtures** in every executed fact
(labelled, not live provider proof); the DB facts exercise the probe/refusal path only, not the positive
acceptance. Elapsed ≈75 min vs HARD40 — recorded **NONCOMPLIANT**, not a process PASS. One correction round was
used (217/218 fact-builder extraction + the composer's facts validation call).

## Bounds respected

No frontend/HMR/browser, no Host/FE restart or rebuild, no live buyer/cart/address/assignment/draft/pointer/
payment write, no shared migration apply, no schema/migration/baseline edit. The USER VISIBLE review window was
never enumerated/read/automated/reloaded/closed. Local preview ON / count 7 unchanged. Known gaps retained:
230 empty-key/in-flight wait, cross-owner label/decoder limits, missing Offer API proof, 227 mobile empty-note
residual, unrecovered 225-R1 unsaved LOCAL user edits. TB185 stays paused/default-OFF/PendingAllocation.

NEXT (Architect): decide whether to issue a bounded fixture-building task whose ONLY goal is the end-to-end
`SubmitAsync` positive with real reserve/convert seams (so the acceptance receipt is actually exercised), or to
accept this slice as wiring-only. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.

## CORRECTION (2026-10-27, appended by 233-R2 — original text preserved above)

Root's 233-R1/ARCHITECT-REVIEW.md finding is confirmed against actual source ce7356217: this slice could
not pass a positive submit. Recorded source blockers: (1) CheckoutDirectory took a captured composer whose
ICheckoutShippingGroupGoodsFactsReader resolved the same CheckoutDirectory — a constructor CYCLE, so no real
DI scope could resolve the directory; (2) facts keyed groups by GUID while plans keyed them by MethodCode, so two
sellers on one binding collided; (3) ComposeFromFactsAsync validated the facts against the facts' OWN
CartId/CartVersion instead of the command; (4) a sellerByOffer.ToDictionary broke lawful carts where two lines
share one OfferId. The acceptance gate itself was also outside the CheckoutProcessManager transaction, so the
root revision was compared without a lock and before reservation.

All four are repaired in 233-R2 (StorefrontCapturedGroupCompositionCore reader-free core, persisted
ShippingGroupId keys, command-bound facts validation, per-line assignment checks) and the acceptance now runs inside
the existing transaction with a SELECT … FOR UPDATE root lock. The positive and the refusal matrix were executed
through real CheckoutDirectory.SubmitAsync — see ../TB-SHIPPING-CAPTURED-SUBMIT-INTEGRATION-233-R2/RESULT.md.
Still open there: AddressBook saved-address authority (country code passed as 
ull), a true concurrent advance, and
the coupon parity. No acceptance/Visual ACCEPT is implied by this correction.
