# TB-279 — Customer retention proof (278 durability carried forward, not regressed)

279 had to retain the 278 customer improvements while adding the admin decision/physical/financial authority. Nothing in
278's customer surface was rewritten or relaxed; the frontend was verified again.

## 1. Durable, viewer-scoped submission intent

| 278 property | State in 279 | How it is proven |
| --- | --- | --- |
| one frozen child per `(sellerOrderId, orderLineId, reason)` with a stable key and verbatim bytes | retained unchanged | `app/returns/return-submission.ts` not modified; `return-submission.test.ts` + `return-submission-279.test.ts` 25/25 |
| recording is verified before the first send (write → read back → compare) | retained | same suite (set/read/compare refusal paths) |
| only body-dictated refusals prove "no effect"; a possibly-sent child stays unresolved | retained | same suite; the classifier set is unchanged in 279 |
| exact-current-act clear (only the exact envelope is cleared) | retained | mount suite (`admin-return-workspace-279.mount.test.ts`) |
| unresolved children retried, resolved children never re-sent | retained | submission suite: partial success retains only unresolved children |

No new `ToastContainer`, no `ToastRegion` on Returns surfaces: outcomes still use the application's real
`react-toastify` `toast.success`/`toast.error` through the single container in `app/providers.tsx` (the 278 convention).

## 2. Customer selection stays deliberate

- nothing is preselected, no "return the whole order" default;
- per line the modal shows the real product, the owning unit, the authoritative remaining ceiling and the unit's own
  step/precision;
- per-line or shared reason; exact integer-millionth arithmetic shared by the confirm-disabled state and the confirm
  handler;
- correct/unselected goods are untouched.

Verified by `return-request-modal-278.mount.test.ts` and `admin-return-workspace-279.mount.test.ts` (`17/17` combined).

## 3. Admin surface is additive to the customer surface

The new `reopen_for_review` / `publish_resolution` / `record_custody` actions live in the existing
`AdminReturnWorkspace`; the customer modal and submission layer were untouched. The served chunk proof
(`RECEIPTS.md` §3) shows the admin chunk carrying the three new verbs and «بازگردانی» while the customer chunk remains
served and unchanged.

## 4. Retained evaluation exclusions

`ReturnEligibilityEvaluator` now accepts `excludeReturnRequestId`, which is used **only** by the correction paths
(re-acquire after reopen, re-check after rejection). The customer eligibility read and the creation gate pass no
exclusion, so a customer can never use the exclusion to over-claim capacity. This was re-verified by the whole
`Tooba.Returns.Tests` project (`172/172`), which includes the 278 creation-quantum and eligibility facts.

## 5. Known customer-side limits (unchanged, disclosed)

- the mounted suite is jsdom: behaviour, not browser geometry;
- an unanswered owning unit precision still conservatively blocks a fractional read while whole quantities remain
  lawful (278 limitation, not fixed here);
- no live customer session/checkout was exercised in 279; the customer wire proof remains the typed refusal and the
  served chunks.
