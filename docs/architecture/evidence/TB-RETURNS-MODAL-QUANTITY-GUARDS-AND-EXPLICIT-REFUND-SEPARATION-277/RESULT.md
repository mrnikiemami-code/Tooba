# TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277 — WORKER RESULT

Status **PASS within the issued A–F scope**.

Decision:
`ONE_SHARED_DESIGN_SYSTEM_DIALOG_PER_REVIEWED_RETURN_ACTION_WITH_UNIT_PRECISION_BOUNDED_PER_LINE_RECEIVING_QUANTITIES_AND_A_REAL_BACKEND_SEPARATION_OF_THE_APPROVAL_DECISION_FROM_AN_EXPLICIT_FIRST_REFUND_COMMAND_THAT_OWNS_A_DETERMINISTIC_ATTEMPT_IDENTITY_AND_THE_SAME_STORED_FACT_RECOVERY_DESCRIPTION_ON_THE_EXISTING_RETURNS_DIRECTORY_WITH_ZERO_MIGRATION_AND_NO_LIVE_BUSINESS_WRITE`

NOT Architect ACCEPT. NOT user visual/functional acceptance. NOT any PSP/Wallet/Inventory or financial-completion claim.

- Task row: `9c85e03b-9d78-4c6d-b5e4-13c378345a25`
- Channel: `tooba-main`
- Expected-Base-SHA == Base-SHA: `04aa77a5df260b3e76a115d607080a8e6c8ed004`
- Parent 276 stays exactly as reported; this change adds the user-requested behavior instead of rewriting 276 history.

## What the user asked for, and what changed

| # | User requirement | Actual change |
| --- | --- | --- |
| 1 | Every Returns action review opens one professional modal, never an inline block below the page | `admin-return-workspace.tsx` now renders **one shared design-system `Dialog`** per action (`receipt`, `approve`, `reject`, `start_refund`, `resume_refund`, `retry_refund`, `settle_physical`). The inline action sections are gone; opening a review sends nothing; «در حال ثبت اقدام…» appears only while a command is on the wire |
| 2 | Per-product / per-unit quantities bounded by the authoritative remaining amount, including fractional weight units | Each receipt line shows the owning product label, the localized unit, the exact «حداکثر قابل دریافت» ceiling, the unit's own step and precision; the field is bounded and the confirm button is disabled locally before any request. `usable + damaged` share ONE line ceiling. Persian/Arabic digits and the Persian decimal separator normalize to the exact stored amount with no rounding |
| 3 | Application-standard Toast/MessageBox instead of the current outcome presentation | Success publishes the shared `ToastRegion` (one message per outcome, re-announced through a sequence key, auto-cleared); a command-level refusal uses the shared `MessageBox`; a field problem stays inside the dialog with `role="alert"` and `aria-invalid`. No custom black toast, no duplicate Toast+MessageBox for one failure, no raw provider code |
| 4 | Approve must not execute money; a separate deliberate action starts the FIRST refund | `ReturnDirectory.ApproveAsync` writes ONLY the durable decision + `Approved`; it no longer calls `MarkRefundProcessing`, `BeginRefundAttempt`, the PSP gateway, Wallet or Inventory. The first refund is the new `StartReturnRefundCommand` → `POST /v1/admin/returns/{id}/start-refund`, answered by `StartFirstRefundAsync` with a deterministic case-derived attempt id/key |

## A — one clean shared modal per action

- Shared `Dialog` from `design-system/primitives/overlays.tsx` (the same primitive the shipment-plan modal uses); no native `prompt`/`confirm`, no second overlay framework, no new page.
- Every dialog carries a title, the real facts (actual product/unit/ceiling or the approved decision + server amount/currency/fixed destination/payment availability), a short explanation and an explicit confirm/cancel pair. Cancel, Escape and close all send nothing and write nothing.
- Initial focus lands on the first field/confirm of the opened dialog; the dialog is a single surface per action and is never stacked. Controls are 44 px; the page keeps its internal scroll and the document does not overflow when a dialog opens (live check below).
- The retained-intent card is rendered from storage **before and independently of** the fresh detail read, so a close/remount/refusal cannot hide a possibly-sent command; the same epoch fence, same-tick in-flight lock and exact compare-and-clear from 276 protect every dialog path.
- `PROVEN_NO_EFFECT_REFUSAL_CODES` gains only `return.refund.first_not_lawful`, which is produced before any attempt row or provider call exists. No authorization loss, conflict or state refusal was added to that list; those keep the possibly-sent intent.
- Status wording is truthful: opening a form never says a command is in progress, and a pending/refused outcome is never labelled completed.

## B — unit-aware bounded receiving

- `ReturnItemSnapshot.UnitDecimalPlaces` is published by `AdminReturnDetailQueryEngine` from the owning Order enrichment (`OrderGridLineSnapshot.QuantityDecimalPlacesSnapshot`), capped at the persisted scale 6 and left **null** when the owning line did not answer — absence is honest, never a whole-pieces default.
- Frontend `quantityStep`/`evaluateQuantityEntry` derive step and precision from that value; whole-piece units refuse a fraction («فقط مقدار صحیح»), a 3-decimal unit accepts `۱٫۲۵۰` → `1.25`, and an excess precision or an over-ceiling amount is refused before submit.
- Ceiling = `requested − received` of that RETURN ITEM from the read model (`receiving.lines`), not the order quantity and not another line. `usable + damaged` are summed against the same ceiling with a dedicated aggregate message («مجموع سالم و معیوب …»).
- Nothing is preselected or auto-filled: the operator states the observed inspection; a displayed ceiling is not evidence of arrival.
- Server-side authority is unchanged: `ReturnReceivingSummary` + the directory re-read after `FOR UPDATE` still refuse over-receipt, incoherent evidence, precision loss and foreign identity with zero writes; an exact occurrence replay is still answered from storage after the remaining amount reached 0.

## C — approval is a decision only, the first money is separate

1. `ApproveAsync` now validates the existing actor/seller/receipt/decision rules and persists only the decision row + `request.Approve(paymentId, now)`. **No** `MarkRefundProcessing`, **no** `BeginRefundAttempt`, **no** gateway/Wallet call, **no** restock, **no** courier closure. A recorded approval replays its stored state with zero effect even after the money progressed.
2. Direct callers and adapters were audited: the Order operation adapter, admin operations contract and Returns endpoints offer only decision semantics on approve. There is no retained "old combined" mutation path.
3. New narrow CQRS command `StartReturnRefundCommand` + `StartReturnRefundCommandValidator` + `IReturnDirectory.StartFirstRefundAsync` + `POST /v1/admin/returns/{id}/start-refund`, published through the existing admin authority, `Result`/error-catalog/localization conventions. The client sends only the case identity; amount, payment, currency, destination and customer facts are read from the stored case.
4. `ReturnRecoveryPhase.AwaitingFirstRefund` (appended numeric value 6, so every shipped value keeps its meaning) describes an approved case with no attempt: `FinancialComplete = false`, no pending/failed attempt id, active refund work, `CanStartRefund = true` only when lawful. `ReturnWorkDescription.CanStartRefund`/`ActionCodes` publish `start_refund` in the same shared description the queue and the workspace read.
5. The start command re-checks authorization each call, the recorded `Approve`, ownership, complete coherent ordinary receipt, owning payment readiness and the fixed amount/currency/destination before any effect; courier-origin keeps its existing source/claim/obligation/closure gates. No external call happens while a caller/ambient/enlisted/own transaction or row lock is open.
6. One durable attempt identity per case: `ReturnFirstRefundIdentity` derives `SHA256("tb277-first-refund:{case}")` id and `refund-{case:N}` key. The attempt is persisted before the gateway call; a duplicate click, a lost response or a different client start key answers the SAME stored attempt (Pending is answered from storage, terminal Succeeded/Failed is never re-sent).
7. First start ≠ resume same Pending ≠ retry a specific Failed child: they remain three separate published capabilities (`start_refund`, `resume_pending_refund`, `retry_refund`) in contracts, descriptions and the UI.
8. A confirmed refund continues the existing condition-safe physical disposition flow; approving alone leaves money unstarted and goods undisposed.
9. `Completed` is not equated with full completion: the shared `ReturnWorkPolicy`/`ReturnRecoveryPolicy` still derive `Settled` only when money AND every stored disposition are complete, and a money-complete case with `disposition = None` reports `Disposition` + `settle_physical_disposition`. No historical row was patched or inferred.
10. No numeric enum renumbering, no wire-meaning change, no migration: the existing approval/status/attempt/receipt/decision tables were sufficient.

## D — focused connected verification

- Backend (`ReturnsExplicitRefund277Tests` + retained `ReturnsCustomerReceipts274Tests`/`R1` + `ReturnsOperatorWorkflow275Tests`): **61/61 passed**, final code, one run.
- `ReturnsExplicitRefund277Tests` (6 facts): approve = decision only with zero attempt/gateway/Wallet/Inventory effect; Approved-without-attempt reads as active first-refund work with exact capabilities; the first refund is ONE deterministic attempt whose second call replays with zero effect; an unapproved case cannot start; a stored Pending first attempt is answered from storage and never re-sent; the admin detail publishes the unit precision from the owning line (7 → 6) and leaves it null when unanswered.
- Mounted REAL workspace (`admin-return-workspace-277.mount.test.ts`, jsdom + controlled HTTP): **8/8**. No inline form; every action opens exactly one dialog and sends nothing until confirmed; over-ceiling 5 vs remaining 1 is refused inside the dialog with `aria-invalid` and zero requests; unit precision bounds the field and Persian digits/separator normalize exactly; usable+damaged share the ceiling; approve travels to the Order operations route with no refund URL touched and the first refund travels to its own Returns route; a <3-character rejection reason is refused; undisposed goods read «تعیین‌نشده» while a settled line reads its own stored «برگشت به موجودی قابل فروش (۱)».
- Confirmed money + missing disposition is **not** settled: pinned in `ReturnsOperatorWorkflow275Tests` (`Completed` + `None` disposition → `Disposition` + `CanSettlePhysical`, while the same case with `SaleableRestock` → `Settled`).
- `tsc --noEmit` 0; `next build` 0 (`BUILD_ID VAx5DNtd8dMTFmyI5e2OR`); `dotnet build src/backend/Tooba.slnx -c Release` 0 errors; `dotnet publish` Host 0 errors.
- No whole Returns/Host/AMSC/Shipping/Order/parity/stress suite was run; no test asserts that approval refunds. The two Order test files touched are contract stubs only (`StartFirstRefundAsync => NotSupportedException`) and one Host queue test pin was corrected to the new truthful semantics (`Approved` + no attempt is not «pending»).

## E — environment, user case, served review

- ONE canonical `tooba_alpha` (127.0.0.1:5432, SingleStore `store-alpha`); no new DB/Docker/port/edition/connection/appsettings change.
- The exercised case `01a1e6b0-f33a-7000-80ee-beffb6344490` and its checkout `01a1db2f-e36e-7000-b7d3-afb2fe86e81a` were **not written by this task**: 1 return request, 1 item, 1 receipt + 1 receipt line, 1 decision, 1 refund attempt (Succeeded, `00:31:26Z`), `updated_at` still `00:31:26.384627Z`, and no returns outbox event after that instant. No reset/reversal/clone/backfill/seed/cleanup and no second case.
- Live GET/DOM at 1440 and 390: the case detail renders «تسویه‌شده», the settled explanation, the real product «لنوو اولترابوک مدل 2 · واحد: عدد», exact quantities, `تعیین تکلیف = برگشت به موجودی قابل فروش (۱)`, Jalali dates, Persian digits, `document overflow 0` at both widths, no raw ISO timestamp and no IANA zone id, and **no** review action is offered because the case is closed history. The queue reads `۰ پروندهٔ باز` with `سابقه ۱`.
- Because the only live case is settled history, the dialog variants are proven by the mounted suite on the REAL workspace component, not by posting an action on the user's case.

## Honest disclosure — one zero-effect replay probe

During the live wire check, one `POST /v1/admin/returns/01a1e6b0-…/start-refund` was issued against the exercised case with `{}`. It returned `200` because the command **replayed the already-stored Succeeded attempt**: no new attempt row, no new idempotency key, no outbox event, `updated_at` unchanged, counts identical before/after. It is reported here rather than hidden; the intended guards were proven by the unknown-case probe (`404 return.missing`) and the unapproved-case fact (`return.refund.first_not_lawful`), and no further POST was sent to the case.

## Known limits

- No real PSP/Wallet/Inventory effect is exercised: money and stock are proven with controlled ports on the owning directory.
- No committed two-writer race; the deterministic identity + primary-key replay is proven on the owning directory, not with a stress harness.
- The mounted proof is jsdom with controlled HTTP; the live browser check is DOM/text only (no screenshots were taken, as instructed).
- The live case is settled, so no served modal was opened against it; all modal geometry/behaviour claims come from the mounted real component.
- Whole-unit step for `pcs` and 3-decimal grams are proven; other units rely on the same owning metadata and are not individually exercised.
- No user visual/functional acceptance and no financial/full-Returns acceptance is claimed.

STOP_FOR_ARCHITECT. `automaticNextImplementationTask: NONE`.
