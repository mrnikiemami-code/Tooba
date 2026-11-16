# TB-RETURNS-OPERATOR-WORKFLOW-QUEUE-DETAIL-AND-MOBILE-275 — worker result

Status: **PASS within the issued operator-workflow scope (A–F)**.
NOT Architect ACCEPT. NOT user visual/functional acceptance. NOT any provider, financial, stock or
"Returns is complete" claim.

Decision (one clause):
`ONE_RETURNS_OWNED_WORK_DESCRIPTION_SHARED_BY_A_SERVERSIDE_CLASSIFIED_QUEUE_AND_THE_SINGLE_CASE_WORKSPACE_WITH_PRE_PAGING_COUNTS_ONE_ACTION_OWNER_A_DURABLE_COMPARED_REVIEW_INTENT_EXACTLY_REPRESENTABLE_RECEIPT_QUANTITIES_AND_A_DETERMINISTIC_REVIEWED_FAILURE_RETRY_ON_THE_EXISTING_RETURNS_DIRECTORY_WITH_ZERO_MIGRATION_AND_ZERO_LIVE_BUSINESS_WRITE`

Expected-Base-SHA: `79258250c87c313cf778a73ab94e8eb4be45d19e` (== actual base == `origin/main` at claim).
Parent task: `TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274-R1` (its acceptance and evidence are
untouched by this task). `276` stays **NOT ISSUED**.

## What changed, by issued scope

| Area | State | What it means |
| --- | --- | --- |
| A one work description | Returns-owned, reused by queue + detail | `ReturnWorkDescription` / `ReturnWorkPolicy.Describe` classifies one case into `receiving` / `decision` / `refund` / `refund_failed` / `disposition` / `settled` / `closed` from STORED facts only, and delegates every money/physical completion question to the EXISTING `ReturnRecoveryPolicy` instead of re-deriving it. Each description carries a typed blocker code and positive capability flags (`CanRecordReceipt`, `CanDecide`, `CanResumeRefund`, `CanRetryRefund`, `CanSettlePhysical`) plus the exact published action codes — no invented verb |
| A queue | one classified set, pre-paging counts | `AdminReturnGridQueryEngine.QueryWithCountsAsync` returns `items` + `page` + `pageSize` + `totalCount` + `counts{allOpen,receiving,decision,refund,refundFailed,disposition,history}` from ONE classification pass under common filters; the page is cut from the same constrained set, so a badge cannot disagree with its rows. Legacy `queueFilter` tokens translate ONCE (`pending_review`→decision, `approved`/`refund_needed`/`refund_pending`→refund, `refund_failed`→refund_failed, `completed`/`rejected`→history) and an unknown token lands on the honest active set — never on "everything". A work selection is refused inside the advanced-filter expression (`grid.advancedFilter.field.invalid`) and a malformed selection operator is `grid.filter.operator.invalid`, both before any DB read |
| A display facts | honest, per line | rows carry a stable human case locator (`RET-<order>-<seq>` derived from immutable order/number facts, never a raw GUID), the own product label/quantity/unit per line (`AdminReturnWorkQueueLine`), origin, receiving progress, money vs physical phase, next step and blocker. Lines are never summed into the first item's unit |
| B action owner | detail only | `admin-returns-work-queue-screen.tsx` is read-only: ONE `بررسی مرجوعی` row action plus order navigation; the financial kebab is gone. `/admin/returns/{returnRequestId}` renders the new `AdminReturnWorkspace`, which is the single action owner; `admin-screens.tsx` keeps a compatibility re-export (`AdminReturnDetailScreen` → workspace) instead of the legacy card + unguarded retry button |
| B detail content | distinct facts | per line: requested / received / usable / damaged / `SaleableRestockedQuantity` / disposition, so a mixed item never reads as wholly saleable; receipt history lists real occurrences with recorded instant and per-(line,condition) portions; refund attempts are quiet (`amount/currency/status/createdAt/failureCode`, no invented PSP reference); the customer's request reason and the winning operator decision reason are separate |
| B actions | server-described only | record receipt (deliberate, explicit quantities/conditions), approve (Order operations authority, deliberate confirm), reject (meaningful reason ≥3 chars, confirmed), resume stored pending refund (exact `expectedRefundAttemptId`), reviewed definite-failure retry (exact `expectedFailedAttemptId`), physical-only settlement (`settle_physical_disposition`). Every button is rendered ONLY when the server description says it is executable, and each sends through its owning route |
| C1 partial receiving continuation | lawful, bounded | a NEW occurrence stays lawful on a financially progressed ordinary case only while nothing has a disposition AND the cumulative coverage is still incomplete and coherent; a decided-away (rejected/cancelled) case, an already-disposed item or complete coverage refuses with `return.receipt.state_invalid`; over-receiving stays `return.receipt.over_received`, the exact-key replay stays exact, incoherent stored evidence refuses with `return.recovery.evidence_conflict` and 0 rows. Receipt recording still makes **zero** money/stock/provider calls |
| C2 reviewed failure retry | deterministic identity, no schema | `RetryReturnRefundCommand.ExpectedFailedAttemptId` is the reviewed failure; the child attempt id is derived as `SHA-256("tb275-refund-retry:{case}:{failedAttempt}")` and its provider key as `refund-retry-{failedAttempt}` (`ReturnRetryIdentity`), so a lost response or duplicate POST answers the SAME child (primary-key arbitration, no counter, no new table). A non-bound/foreign/stale/ambiguous failure is a typed refusal; a failure that already produced its child is history and the next review binds to THAT child. An older caller without the reviewed identity is answered from the stored description's current reviewable failure or fails closed (`return.recovery.attempt_unbound`) — there is no bypass and no count-based key |
| D durable review intent | stored before the first possible send | `return-review-intent.ts` (Returns-owned helper) persists `{action, occurrenceKey, body, createdAt}` in `sessionStorage` BEFORE the request, refuses to send when storage is unavailable, serializes a same-tick double click through one `inFlight` ref and `epoch` fencing, and clears with compare-and-clear on the exact occurrence + body only. Unknown/unmount/reload keeps the unresolved intent visible with an exact-body retry; a 4xx/conflict keeps the uncertainty instead of silently resolving it |
| E mobile / readability | 44px controls, scoped scroll | queue strip and detail actions are ≥44px; at 390 the strip wraps locally and the grid keeps its own horizontal scroller (340 visible / 1652 content) with document overflow 0; receipt quantity/condition controls are 44px with localized decimal input; Persian/Jalali dates and Persian digits via the existing helpers |
| F proof | focused only | backend `ReturnsOperatorWorkflow275Tests` **12/12**; frontend `admin-returns-work-queue.test.ts` **21/21**; `tsc --noEmit` 0; production Next build 0 errors; `dotnet build src/backend/Tooba.slnx -c Release` 0 errors; Host publish 0 errors |
| schema / data | untouched | **zero migration**, no DDL/backfill/seed/reset/cleanup/restore, one canonical `tooba_alpha`, no live case/order/draft/hold/stock/decision/settings write, no provider/PSP/Wallet/Inventory/waybill/notification call |

## Live served artifacts (read-only)

- Host `5088` PID `16532`, loaded assembly `D:\Users\User\source\repos\SarvNewVer\artifacts\publish-primary\Tooba.Host.dll`
  SHA256 `02AECDC72FA4506C889C743713CD8A93A1983F25C489268B4E0E3F55BF48DD73`; `/health` 200,
  `/health/ready` 200 (`SingleStore`, messaging `Healthy`).
- Next `3000` PID `50724`, `BUILD_ID TaMAKtGjoy8sg6NHdnsO9`; the served page references
  `/_next/static/chunks/692-4941a8a633b47e39.js` (200, 169,248 B) carrying the workspace copy, the not-found state and
  the storage-failure guard.
- `GET /fa/admin/returns` and `GET /fa/admin/returns/{unknown-guid}` serve 200 at 1440 and 390 with document
  overflow `0`.

## Known limits (declared, not papered over)

- **No live return case exists** in the canonical database: the queue renders `۰` for every strip and the grid says
  «ردیفی برای نمایش نیست». There is therefore **no served operator walkthrough** of a real receiving/approval/refund.
  All actionable proof is mounted/controlled (in-memory EF + controlled ports).
- No demo order/case/receipt/hold/stock row was created to make the screens look populated, per the task.
- True two-writer concurrency is not stress-tested; the C2 duplicate-POST and stale-source cases are proven with
  sequential/repeated calls through the owning directory, and the retry identity's safety rests on the derived primary
  key plus the existing unique constraints.
- The pre-existing 36px shared grid toolbar buttons (`فیلتر پیشرفته`, `ستونها`, …) were deliberately not restyled
  (the task forbids a global toolbar restyle to repair historical controls); the buttons introduced by this task are 44px.
- `return.recovery.transaction_owned` and other typed refusals catalogued earlier are unchanged; no new error code was
  introduced by 275.
- The `ReceiptDraftLine` quantity input accepts Persian digits and requires ≤6 decimals client-side; a value that would
  need rounding is refused before any intent is stored (the server independently enforces precision).
- Worker PASS ≠ Architect ACCEPT ≠ user acceptance.

## Review URL

`http://127.0.0.1:3000/fa/admin/returns` (queue) and, for the honest empty-detail state,
`http://127.0.0.1:3000/fa/admin/returns/00000000-0000-0000-0000-000000000000`.

STOP_FOR_ARCHITECT. `automaticNextImplementationTask: NONE`.
