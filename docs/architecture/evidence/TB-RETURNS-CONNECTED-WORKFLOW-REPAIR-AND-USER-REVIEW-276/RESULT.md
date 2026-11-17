# TB-RETURNS-CONNECTED-WORKFLOW-REPAIR-AND-USER-REVIEW-276 — Result

Status: **PASS within the issued A–F connected-repair + one-user-review-case scope** (worker result; NOT Architect
ACCEPT, NOT user visual/functional ACCEPT).
Base: `5fbdea3394d0636ae62e5ae06bab95e432563da2` (== issued `Expected-Base-SHA` == local `main` at claim).
Task: `543c1beb-3372-4e06-9e16-52d164046d20` on `tooba-main`. Single worker, one task, no delegation, no
historical agent resumed, no loop, no screenshots.

Decision:
`EXACT_DURABLE_REVIEW_WITH_VERIFIED_READ_BACK_EXACT_BODY_COMPARE_AND_CLEAR_EPOCH_FENCING_AND_A_TYPED_NO_EFFECT_RULE_AN_EXPLICITLY_BOUND_FAILED_REFUND_RETRY_THAT_REPLAYS_ITS_DETERMINISTIC_CHILD_BEFORE_ANY_GATE_MOUNTED_AND_DIRECTORY_PROOF_INSTEAD_OF_SOURCE_REGEX_ONE_LAWFUL_CUSTOMER_DELIVERED_REQUESTED_CASE_AND_A_SERVED_DESKTOP_MOBILE_OPERATOR_SURFACE_WITH_PER_LINE_PRODUCT_QUANTITY_AND_UNIT_ON_THE_EXISTING_RETURNS_DIRECTORY_WITH_ZERO_MIGRATION_AND_ONE_READ_ONLY_REVIEW_CASE`

## 1. What changed (source)

| Area | State | What it means |
| --- | --- | --- |
| A durable review | verified before first send | `app/returns/return-review-intent.ts` freezes `{version, target, returnRequestId, routeTargetId, action, occurrenceKey, body}` — `body` is the EXACT serialized transport string; `storeReturnReviewIntent` writes it and reads the SAME bytes back, and a storage get/set/read-back failure (or a second read mismatch) refuses to send at all. `sameReturnReviewIntent` compares the frozen bytes, and `clearReturnReviewIntentIfCurrent` clears only the exact reviewed occurrence+body. `isProvenNoEffectRefusal` is a small typed list (`PROVEN_NO_EFFECT_REFUSAL_CODES`), never all-4xx or message matching. The unconditional finish-clear and the blind «نادیده گرفتن» path are gone. |
| A epoch fencing | every mutation and read | `admin-return-workspace.tsx` keeps one `epoch` ref; the detail read, the dispatch, the answer handler, the toast/notice, the pending lock and the `finally` all run through `isCurrent(started)`, and unmount/retarget bump it. A late answer from a previous surface can neither clear a newer envelope nor release its pending lock. |
| A remaining Order menus | audited | the only Order-side entry is a navigation link (`admin-order-operational-delivery-section.tsx` L1648–1655, «مشاهدهٔ پروندهٔ مرجوعی» → `/fa/admin/returns/{id}`, 44px); the queue row action is the only other entry (`admin-returns-work-queue-screen.tsx` L77–79). Every approve/reject/retry/receipt control lives in `admin-return-workspace.tsx`; `admin-screens.tsx` keeps only the compatibility re-export. No duplicate blind approve/retry form survives anywhere in the app. |
| B failed-refund retry | explicit identity, child first | `ReturnDirectory.RetryRefundAsync` → `RequireReviewedFailedAttempt` refuses a null/empty/foreign/stale `ExpectedFailedAttemptId` with the existing typed codes and zero attempts. The deterministic child (`SHA256("tb275-refund-retry:{case}:{failed}")`, key `refund-retry-{failed}`) is located BEFORE any new-attempt gate: a stored child answers from storage even when the parent is now Pending/Completed/Failed, a stored `Pending` child reconciles that same attempt through the existing pending-recovery authority, and only a missing child may begin a new attempt after the present effectful gates. No new key, no grandchild, no gateway call for a stored terminal child. |
| C connected proof | mounted + directory | 10 mounted `AdminReturnWorkspace` cases on the real component (jsdom + real React lifecycle, controlled HTTP/storage), 15 backend cases through the real `ReturnDirectory`, 26 queue/mapping/label cases, 8 error-map cases. Helper/source-regex checks are NOT presented as mounted proof. |
| D one review case | created, left `Requested` | `01a1e6b0-f33a-7000-80ee-beffb6344490` (`RET-TB-20261114175151-01-7d5f59-1`), ordinary `CustomerDelivered`, 1 line × 1 `pcs`, 2,407,839 IRR, reason «پروندهٔ بررسی رابط کاربری مرجوعی — TB-276». Zero receipts, zero decisions, zero refund attempts, zero dispositions. |
| E served populated surface | desktop + mobile | queue and case detail read at 1440 and 390 with 0 document overflow, the case in its truthful `Receiving` stage, per-line product + Persian unit, Jalali dates, Persian digits, and a receipt form that opens with nothing preselected and closes on «انصراف» without any request. |
| F docs | this folder | `TASK.md` (verbatim envelope), `RESULT.md`, `RECEIPTS.md`, `result-contract.txt`, `connected-intent-and-retry-proof.md`, `user-review-case-proof.md`, `user-review.md`. |

Additive display contract introduced for E (no schema, no money/stock change):

- `ReturnItemSnapshot` gained trailing optional `ProductLabel` / `UnitLabel` (`null` when the owning read cannot answer).
- New Application port `IAdminReturnDetailQuery` + `AdminReturnDetailQueryEngine` (Infrastructure), registered beside
  `IAdminReturnGridQuery`; it reads the stored snapshot and attaches the Catalog variant title and the Order line's
  stored unit code through the SAME batched Contracts readers the queue already uses.
- `GetAdminReturnHandler` now reads through that port (still `return.missing` → 404 for an absent case).
- FE: `ReturnItem.productLabel` / `unitLabel` mapped; the detail table renders a «کالا / واحد» column and shows the unit
  only when the owning line answered it; receipt history lines name their product when known.
- FE display coherence on the reviewed surface: operator quantity cells use the existing
  `formatQuantityDisplayFa` (Persian digits + Persian decimal separator), and the queue's eligibility label is rendered
  through the new pure `formatEligibilityLabel` (Jalali day via `operationalDayLabel`, Persian digits, unchanged words).

## 2. Verification (each named suite run once after the final source change)

| Check | Result |
| --- | --- |
| `dotnet test … --filter FullyQualifiedName~ReturnsOperatorWorkflow275Tests` | **15/15** passed (14 retained + 1 new admin-detail display proof) |
| `dotnet test Host/Tooba.Host.Tests … --filter FullyQualifiedName~ReturnsModuleAmsc001` | **34/34** passed |
| `dotnet build src/backend/Tooba.slnx -c Release` | **0 errors** (pre-existing warnings only) |
| `dotnet publish Host/Tooba.Host` → `artifacts/publish-primary` | **0 errors** |
| `npx tsc --noEmit` (src/frontend) | **0** |
| `admin-returns-work-queue.test.ts` | **26/26** |
| `admin-return-workspace-276.mount.test.ts` | **10/10** |
| `admin-error-map.test.ts` | **8/8** |
| `npm run build` | **0**, `BUILD_ID gC-lsQo8rzo30IeWT6MwF` |

Served identity: Host `5088` PID `8572`, dll SHA256
`D998ECBC22459E127166C9C03747CA688475557D58937CBE516D1876BB32838E`, `/health` 200, `/health/ready`
`{"edition":"SingleStore","messaging":"Healthy"}`, `/__platform-commerce` → `store-alpha`. Next `3000` PID `21544`,
`BUILD_ID gC-lsQo8rzo30IeWT6MwF` served (`/_next/static/gC-lsQo8rzo30IeWT6MwF/_buildManifest.js` 200), `/fa` 200,
`/admin/returns` 200.

## 3. Served read-only observations (DOM/text, no screenshots)

Queue `/fa/admin/returns` at 1440: document overflow **0**; 7 work strips, each **44px**; the grid keeps its own
scroller (1071 visible / 1652 content); row `RET-TB-20261114175151-01-7d5f59-1` for order
`TB-20261114175151-01-7d5f59` in stage «دریافت کالا» with «در انتظار بررسی» / «بدون بازگشت وجه» and «۱ عدد»; after
scrolling the grid's own container the right-hand columns mount with «۵ روز باقی‌مانده · مهلت ۳۰ آبان ۱۴۰۵» and
«۱۴۰۵/۰۸/۲۶, ۰۲:۵۸». At 390: document overflow **0**, strips wrap locally, scroller 340/1652, no raw ISO and no Latin
unit token in the visible operator text.

Detail `/fa/admin/returns/01a1e6b0-f33a-7000-80ee-beffb6344490` at 1440 and 390: document overflow **0**; header
«بررسی مرجوعی» + stage «دریافت کالا» + Jalali instant; «وضعیت کار» explains that receiving must complete before any
decision/refund; money «۲٬۴۰۷٬۸۳۹ ریال» separate from physical state; item table «قلم ۱ · لنوو اولترابوک مدل 2 ·
واحد: عدد · ۱ ۰ ۰ ۰ · —»; honest empty history («هنوز دریافت فیزیکی ثبت نشده است.»); exactly ONE lawful action
«ثبت دریافت کالا». Receipt form open/close at both widths: 4 controls, each **44px**, quantity empty, condition
«انتخاب کنید» (unselected), no submit, «انصراف» closes it and re-enables the action.

## 4. Case facts (read-only, canonical `tooba_alpha` 127.0.0.1:5432)

| Fact | Value |
| --- | --- |
| case | `01a1e6b0-f33a-7000-80ee-beffb6344490` / `RET-TB-20261114175151-01-7d5f59-1` |
| origin / status | `CustomerDelivered` / `Requested` |
| seller order / checkout | `01a1db2f-e375-7000-8197-9657a380a505` (`Paid`) / `01a1db2f-e36e-7000-b7d3-afb2fe86e81a` |
| line | `01a1e6b0-f33f-7000-9d63-58fe9922572c`, qty `1.000000` `pcs`, unit price snapshot 2,407,839 IRR |
| refund amount / currency | `2407839.0000` / `IRR`, destination «پرداخت اصلی» |
| receipts / receipt lines | **0 / 0** |
| decisions | **0** |
| refund attempts | **0** |
| disposition / saleable restock | `None` / none |
| money fact on the case | `payment_id` is **not set** on this return (the source order owns the payment); so no refund/retry path exists to demonstrate on it, and none was fabricated |
| cases for this checkout | **1** — asserted by count over `returns.return_requests` filtered by `checkout_id`; the creation answered on the first attempt, so no replay was needed and none was exercised (declared limit) |
| Returns totals | `return_requests` 1, `Requested` 1, `return_items` 1 |
| normal Returns event | 1 `return.requested.v1` outbox row for THIS case, processed, `attempt_count=1` (disclosed; this is a real live case creation). The same table holds **2 pre-existing leftover rows** from 2026-11-14 whose request rows no longer exist (the 272 fixture audit); this task wrote no history row and deleted nothing |

## 5. Live read-only wire (`5088`, dev actor from `/v1/admin/dev-context`)

- `GET /v1/admin/returns/{case}` → 200, `status=Requested`, `items[0].productLabel=لنوو اولترابوک مدل 2`,
  `items[0].unitLabel=pcs`, `receipts=0`, `recordedDecision=null`, `receiving.received=0`, `work=Receiving`.
- `GET /v1/admin/returns/{case}` **without** an actor → 401 `admin.actor.missing`.
- `POST /v1/admin/returns/query` → 200 `totalCount=1`, `counts.allOpen=1`, `counts.receiving=1`, `counts.decision=0`.
- `POST /v1/admin/returns/{random-guid}/retry-refund` (with and without `expectedFailedAttemptId`) → 404
  `return.missing` — the reviewed retry is bound to a real case and identity; nothing is minted.
- No approve/reject/receipt/refund/recovery request was sent for the real case at any point.

## 6. Known limits (declared, not papered over)

1. **Not Architect ACCEPT, not user acceptance.** The worker cannot and does not claim the user's visual/functional
   verdict; the case is left in place for that review.
2. No committed two-writer race and no real PSP/Wallet/Inventory effect: money/stock are proven with controlled ports,
   the retry identity/replay with the real `ReturnDirectory` over in-memory EF, and the duplicate-answer rule by the
   deterministic child key rather than a concurrent harness (the accepted 273/275 limit is unchanged).
3. The mounted suite runs the real component under jsdom with controlled HTTP/storage; it is not a live browser trial.
   The browser checks above are read-only DOM/text measurements (no screenshots, per the task).
4. The queue's «مهلت / شرایط» and «آخرین تغییر» columns sit to the left inside the shared grid's own scroller at 1440;
   they were read after scrolling that container (pre-existing shared-grid behaviour, not introduced here).
5. The 36px shared grid toolbar controls and the pre-existing 16/32px header filter buttons were not restyled (the task
   scoped new controls to 44px; those pre-existing shared-grid controls are untouched).
6. The case detail shows the product and unit only because the owning Order/Catalog reads answered; a case whose owning
   read cannot answer renders no product label and no unit (never a fabricated one).
7. Only `Tooba.Returns.Tests` filtered to the named suite and the Returns AMSC guard were run; no unfiltered
   Returns/Host/Shipping/Order/parity/baseline/stress suite was executed.
8. Two earlier FE builds of this task existed while the display coherence edits were made; the final served build is
   `gC-lsQo8rzo30IeWT6MwF`. The backend was published once after the final source change.
9. Untracked `scripts/tb266r1-*.mjs` / `scripts/tb270r2-*.mjs` / `scripts/tb276-read-bridge-db.mjs` scratch files from
   earlier rounds remain in the worktree and are NOT part of this change set; the CDP measurement scripts live outside
   the repository (`D:\temp`).

STOP_FOR_ARCHITECT; `automaticNextImplementationTask: NONE`.
