# TB-279-R1 — customer intent, modal and template feedback proof (F6, S1–S5)

## Durable, viewer-scoped customer act

`app/returns/return-submission.ts` (`RETURN_SUBMISSION_VERSION = 2`,
prefix `tb279:customer-return-submission:`):

* **Viewer scope is required.** `buildReturnSubmission`, `readReturnSubmission`, `storeReturnSubmission`,
  `recordChildOutcome` and `clearReturnSubmissionIfCurrent` all take the resolved authenticated `viewerId`; there is no
  viewerless fallback key, so a shared-browser act can never be stored, adopted, updated or cleared by another
  customer. Legacy unscoped evidence is not silently adopted.
* **Exact bytes are frozen and re-verified.** One child per `(sellerOrderId, orderLineId, reason)` with its own stable
  key and its body; parsing re-validates seller/line/quantity/reason/destination/idempotency key against the envelope
  and refuses tampered bytes (never replayed). Only the exact act can be cleared, and a newer act survives a late
  answer.
* **Storage is probed, not assumed.** `acquireSubmissionStorage()` checks the `sessionStorage` PROPERTY and its
  methods (write/read/remove probe); a denied, missing or throwing storage is an explicit reviewable state — the modal
  refuses to send and says so, rather than treating "unreadable" as "nothing pending".
* **The retained act is never overwritten.** Before a new write, the exact current scoped evidence is compared: a
  pending/unknown/unreadable act keeps its bytes, and reopening or changing the selection cannot replace them until the
  operator/customer resolves the exact stored occurrence. Read-back verifies the exact bytes before the first send.
* **Epoch fencing** covers open/close/unmount/retarget/viewer change, every next child send, callbacks/toasts/load/
  `finally` and the busy lock: a late answer may reconcile only its own originating stored child; it never paints or
  releases newer UI state, replaces newer evidence or clears a different act.
* **Only body/identity-dictated refusals are no-effect.** A later refusal never proves an earlier send was unwritten, so
  the uncertainty card survives.

## One shared design-system dialog

`app/returns/return-request-modal.tsx` now renders the existing shared `Dialog` primitive (not a private
`position: fixed` `role="dialog"` overlay), with focus entering the first field and returning on close, Escape/cancel
that writes nothing, local scrolling and ≥44px new controls. Operator surfaces keep their shared dialogs; no action
form is expanded inline below a page.

## Template feedback (actual template)

Command outcomes use the application's **existing react-toastify** — `toast.success` / `toast.error` through the ONE
`ToastContainer` in `app/providers.tsx`. No `ToastRegion`, no second container and no custom floating black box; a GET
or a render emits no toast, and field-level errors stay inline.

## Unknown unit precision (consistent read/write policy)

`ReturnLineEligibility` / `ReturnItemSnapshot.UnitDecimalPlaces` are the single owning metadata. Fractional entry is
blocked when the owning unit did not answer (`unitUnavailable`) with an explicit explanation, while a lawful whole
quantity stays acceptable and is never hidden; no six-decimal default, no epsilon, no rounding. Exact millionths
(`return-quantity.ts`) remain the arithmetic authority and Persian/Arabic digits plus `٫` normalize exactly.

## Operator clarity (S3/S4)

The workspace shows the human case locator, each line's own product + unit, **requested** vs **approved** money
(`admin-return-requested-amount` / `admin-return-approved-amount`), accepted vs returned-to-stock, and the outstanding
custody remainder per rejected line (`admin-return-custody-remaining`: declined / handed back / still in company
custody). A rejected or zero-owed case is never labelled "all refunded", and a case with an owed portion never reads as
settled.

## Fixture update (S5)

`admin-return-workspace-276.mount.test.ts` now carries the accepted current wire shape (`productLabel`, `unitLabel`,
`unitDecimalPlaces`) so the receipt line is not blocked by a stale fixture, asserts the template toast container text
and uses a current body-dictated refusal token. No assertion was weakened; superseded expectations were replaced by the
equivalent current ones.

## Facts (all run after the final FE change)

```
npx tsc --noEmit            → 0 errors
return-submission.test.ts + return-submission-279.test.ts
  + return-request-modal-278.mount.test.ts
  + admin-return-workspace-276.mount.test.ts
  + admin-return-workspace-277.mount.test.ts
  + admin-return-workspace-279.mount.test.ts → 44/44 pass
npm run build               → success, BUILD_ID 1qgSFICgCehoh6FzH3a0f
```

Limits: the mounted suites are jsdom (behaviour, not geometry); no live customer session or checkout was exercised and
no live POST was made.
