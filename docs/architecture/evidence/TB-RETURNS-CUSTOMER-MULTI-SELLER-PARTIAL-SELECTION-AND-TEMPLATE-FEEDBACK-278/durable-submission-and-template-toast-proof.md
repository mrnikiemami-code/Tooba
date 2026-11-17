# Durable submission, partial recovery and the actual template toast — proof

## 1. One frozen act, many stable children

`app/returns/return-submission.ts` (`RETURN_SUBMISSION_VERSION = 1`, storage prefix
`tb278:customer-return-submission:`):

- `buildReturnSubmission` freezes **one child per selected `(sellerOrderId, orderLineId, reason)`**, each with its own
  `idempotencyKey` and its own `body` string produced once (`submissionChildKey`);
- `storeReturnSubmission` writes the act and **reads the same bytes back**, refusing to send when storage is missing,
  throws, or disagrees (a lying/unavailable storage never dispatches);
- `readReturnSubmission` distinguishes *absent* from *unreadable*: unreadable is an explicit state, never "nothing
  pending";
- `recordChildOutcome` makes `recorded` terminal (`caseId`/`caseNumber` kept, never re-sent) and keeps `unknown` with its
  bytes;
- `SUBMISSION_NO_EFFECT_REFUSALS` holds only six body-dictated tokens (`return.quantity_invalid`,
  `return.quantity_precision`, `return.quantity_unit_invalid`, `return.duplicate_line`, `return.idempotency_required`,
  `return.line_missing`); every other refusal (authorization, state, window, capacity, conflict, expired) leaves the
  child `unknown`, because an earlier send of the same bytes may already have written;
- `clearReturnSubmissionIfCurrent` clears only the exact current act, and only once every child is resolved;
- `summarizeReturnSubmission` / `unresolvedChildren` drive the retained card and the «ادامه» action.

## 2. Mounted partial-success walkthrough

`return-request-modal-278.mount.test.ts` fact 3: responses are `ok(socks)`, `transport`, `ok(lamp)`.

1. Two children are sent; the socks child is recorded, the lamp child never answered.
2. The retained card shows «۱ ثبت‌شده» and «نتیجه نامشخص», the resume action appears, and an "uncertain" toast is shown —
   nothing is cleared by an uncertain outcome.
3. The customer resumes: exactly **one** new POST, byte-identical to the first lamp send (`raw === raw`), and the
   recorded socks child is **not** re-sent (exactly one socks POST total).
4. The authoritative answer for those same bytes resolves the act: storage empties, the retained card disappears, and
   the template success toast appears.

## 3. Actual template toast (react-toastify)

- `app/providers.tsx` already installs the application's single `react-toastify` `ToastContainer` (top-right, RTL,
  colored, 3000 ms). No new provider/container/style was added.
- `admin-return-workspace.tsx` now imports `toast` from `react-toastify` and emits `toast.success(successLabel(...))` /
  `toast.error(mapAdminErrorMessage(...))` for command outcomes; the passive design-system `ToastRegion` is gone from
  every Returns surface (the only remaining `ToastRegion` usage is the design-system showcase page).
- The customer modal uses the same `toast.success` / `toast.error`, with concise **inline** messages for field-level
  validation (fraction/ceiling/unknown precision/reason) instead of toasting validation noise.
- Served proof: `/_next/static/chunks/9627-0b0f29163bba673b.js` (200, the react-toastify runtime used by the served
  route chunk) and the mounted tests assert on `.Toastify__toast` text, not on a custom element.

No seller-facing Returns mutation surface exists in this application, so no seller toast was converted (declared).

## 4. What this does NOT prove

No live customer POST, no real cross-device replay, no browser-level toast position/pixel check (mounted jsdom with a
real `ToastContainer`), and no server-side proof that a retried body is deduplicated beyond the existing
`ix_return_requests_idempotency_key` exact-replay path (277/272 evidence), which this task relies on rather than
re-proves.
