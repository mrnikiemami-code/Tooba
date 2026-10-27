# RESULT — TB-SHIPPING-CAPTURED-ORDER-COMMIT-PARITY-233

Status: **PARTIAL / EXECUTED**, NOT accepted, NO user Visual ACCEPT.
Base: `1bcc3c41a` (fast-forward of the issue commit; nothing else staged).
Elapsed ≈ 70 min vs HARD45 — recorded **NONCOMPLIANT**, not a process PASS.

## What actually landed (verified)

### A — one explicit final gate, default OFF
`StorefrontCapturedPreviewOptions.CheckoutEnabled` (default `false`) is the ONLY key for captured final
submit. It is coherent with `Enabled`/`SelectionEnabled`: preview reading and selection writing can be ON
while the final order stays closed. Checked-in configuration stays OFF; nothing is enabled at runtime.

### B — the selection revision is a real request token, end to end
`StorefrontStartPaymentBody.ExpectedSelectionRevision` (string) → `StorefrontStartPaymentRequest` →
`StorefrontShippingService.CommitAsync` → `StorefrontCheckoutService.SubmitAsync` → `BuildCommand` →
`SubmitCheckoutCommand.ExpectedSelectionRevision`. The value is carried as **text** (no numeric coercion at
the boundary), so `"4294967296"` survives; `CheckoutDirectory.TryParseSelectionRevision` is the only parser
and accepts AscI digits only: `""`, `" "`, `" 1"`, `"1 "`, `"-1"`, `"+1"`, `"1.0"`, `"1e3"`, `"abc"` are all
**invalid** (never silently `0`), while `"0"` remains lawful absence.

### C — acceptance is inside the real submit path, before the first reservation
`ICheckoutSubmitHost.AcceptCapturedShippingAsync` is invoked by `CheckoutProcessManager.SubmitAsync`
**after** `QuoteSellerOrdersAsync(requireReservation:false)` and **before** the existing `TransactionScope`,
inventory reservation, `PersistCheckoutAsync`, cart conversion and completion. Its result — not the caller's
raw `command.ShippingGroups` — is what `PersistCheckoutAsync` materializes
(`acceptedShippingGroups ?? command.ShippingGroups`), so a caller cannot hand plans straight to persistence
without passing the acceptance seam.

### D — fail-closed, caller evidence is never authority
`CheckoutDirectory` (new partial `CheckoutDirectory.CapturedAcceptance.cs`):

| situation | behaviour |
| --- | --- |
| gate OFF, no caller evidence | returns the exact same plan list, **zero** extra read |
| gate OFF, caller supplies `AcceptedEvidence` | `storefront.captured_preview.not_ready` **before any read** |
| gate ON, fresh draft read finds no captured root and no caller evidence | legacy plans unchanged |
| gate ON (or a stored captured root / caller evidence is present) | `storefront.captured_preview.not_ready` |

A stored captured draft is read fresh and untracked (root + groups + lines, `AsNoTracking`) so stale
`Include` truth is never the evidence source. Because full server revalidation is **not** implemented, the
gate never opens a captured write: both OFF and ON keep the stable `not_ready` refusal.

## What did NOT land (honest gaps — the OUTCOME is not delivered)

1. **Trusted revalidation** of the accepted selection against current server data (prepared saved-address
   owner + unique Geography identity vs persisted priced destination scalars, cart version/whole-line
   membership, explicit 228 responsibility/type/seller, active type/binding, selected 219 pointer
   snapshot/draft/source revision/selection version, same 216 quote and same 213 calendar, all accepted
   money/date/interval/min-max scalars) — **not implemented**.
2. **Final-quote financial truth** (build group goods facts from THIS submit's already-quoted
   `SellerOrder` lines via a typed facts input into 217/218, one 216 evaluation per candidate, no recursive
   Preview/Submit, foreign-call counting) — **not implemented**.
3. **Compact evidence persistence** into `order.order_shipping_groups` inside the successful checkout atomic
   boundary, checkout-vs-collect totals, idempotent replay returning the original stored order — **not
   implemented** (the storage value 231/232 already exists and stays unreached by a production writer).
4. **ProjectPaymentDraftAsync** captured readiness under the gate — **not implemented**.
5. No positive two-seller-group submit, no changed-amount/address/pointer/type refusal matrix, no
   order-write/cart-convert failure rollback proof, no direct-caller forged-evidence proof beyond the gate-OFF
   fact, no relational `15433/order_tb231` run at all.

Consequently captured checkout remains explicitly unsupported: `CommitAsync`/`PaymentDraft` still refuse
`storefront.captured_preview.not_ready`, and only an explicit caller-supplied evidence plan is refused at the
submit seam today.

## Proof actually run

- `Tooba.Order.Infrastructure` build: 0 errors (new partial compiles; `ICheckoutSubmitHost` and
  `PersistCheckoutAsync` extended with an additive optional parameter).
- `Tooba.Order.Tests` build: 0 errors.
- `Tooba.Host` build: 0 errors.
- Focused run ONCE, `--no-build`:
  `CapturedCheckoutGateTests` + `StorefrontCapturedPreviewWriteGuardTests` +
  `OrderShippingGroupMaterializerTests` = **28/28 passed** — includes 12 strict-revision parses
  (`0`/`1`/`4294967296` lawful; blank/space/`-1`/`+1`/`1.0`/`1e3`/`abc` refused), legacy plan identity with
  the gate OFF, and caller-evidence refusal with the gate OFF, plus the unchanged 232 write-guard facts and
  the unchanged materializer invariants.
- One in-module correction: the first draft of the gate-ON fact required the relational target (fresh draft
  read) and was replaced by the strict-parse facts; the gate-ON branch is therefore **unproven** and stated
  as such above.

## Bounds honoured

No frontend/HMR, no browser, no Host/FE restart/rebuild, no live assignment/cart/address/draft/publication/
payment write, no shared migration apply, no schema change, no EF snapshot regeneration, no rewrite of 231/232
migrations. `231`/`232` remain shared NOT APPLIED and the stale EF snapshot gap is retained. The user's
visible review window was never enumerated/read/automated/reloaded/closed; local previewON/count7 left
unchanged; every new gate checked-in OFF.

Known retained and NOT fixed: 230-R1 empty-key destination retry/in-flight wait, cross-owner label map/decoder
limits, Offer API proof gaps, 227 mobile empty-note residual, 225-R1 lost unsaved user LOCAL edits.
TB185 paused / default-OFF / PendingAllocation preserved.

STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
