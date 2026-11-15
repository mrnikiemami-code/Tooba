# RESULT — TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Status: **PASS within the issued read-model/query + existing-queue-UI scope**,
decision
`FIVE_SERVER_CLASSIFIED_JOB_STAGES_PLUS_AN_INDEPENDENT_TODAY_FUTURE_ALL_SCOPE_ONE_COUNT_SET_BEFORE_PAGING_AND_A_QUIET_HISTORY_VIEW_ON_THE_EXISTING_QUEUE_WITH_STAGE_RELEVANT_TIME_MEANINGS_AND_ZERO_BUSINESS_AUTHORIZATION_OR_DATA_CHANGE`,
NOT Architect ACCEPT, NOT user visual/functional acceptance, NOT any shipping/carrier/provider/financial claim.

| Field | Value |
| --- | --- |
| Parent-Task | TB-SHIPPING-RESEND-REPLACEMENT-OWN-DISPATCH-PLAN-270-R4 (Architect ACCEPT within its four-defect repair scope) |
| Channel / Worker / Agent-Type / Program | `tooba-main` / `tooba-worker-01` / `cursor` / TOOBA |
| Bridge-Task-Row | `a4ddd1a9-8d79-44da-95c5-b8a8e778452f` |
| Expected-Base-SHA | `c501fe9bcdb2c42a26d05e23d44af288931612ed` |
| Base-SHA (verified) | `c501fe9bcdb2c42a26d05e23d44af288931612ed` (`HEAD` == `origin/main`, `--is-ancestor` exit 0) |
| Code-SHA / Evidence-SHA | see `result-contract.txt` |
| Stop-State | STOP_FOR_ARCHITECT |
| automaticNextImplementationTask | NONE |

## 1. Root of the complaint and the repair

The screen rendered **two competing primary quick-filter rows** (`FULFILLMENT_DISPATCH_FILTERS` status buckets and
`FULFILLMENT_QUEUE_FILTERS` time buckets) over one `queueFilter` state. The employee could not tell which list was
"what to do next", the two rows could combine invisibly, and a row's date was the legacy `dispatchBucket` rather than
the time of the work the employee has to do.

Repair: ONE server read-model classifier produces **one job stage per active physical work unit** and ONE independent
time scope, the counts for both dimensions come from ONE pre-paged set, and the frontend only renders supplied
fields/counters. No lifecycle, no new status, no new permission, no shipping calculation, no write path.

## 2. Five job stages (+ one quiet archive), decided by existing server facts

| Token | Label | Decided by (existing owning facts) |
| --- | --- | --- |
| `preparation` | آماده‌سازی و بسته‌بندی | a `Group` row whose available codes contain `mark_processing` / `mark_packed` |
| `ready_to_send` | آمادهٔ ارسال | `create_shipment` · `plan_shipment_dispatch` · `assign_tracking`/`correct_tracking` on a `Created` parcel · `assign_consolidated_package_tracking` |
| `handoff` | تحویل به حامل | `dispatch_shipment`, or `dispatch_consolidated_package` when the package prerequisite is already satisfied |
| `delivery` | پیگیری تحویل | `Dispatched`/`InTransit`, `deliver_shipment`, `deliver_consolidated_package`, `record_non_delivery` |
| `returns` | برگشتی‌ها | `NonDelivered`/`Returning`/`Returned`, or `start_return`/`record_return_receipt`/`resend_shipment`/`resolve_damaged_goods` |
| `history` | سابقهٔ ارسال | terminal rule: `Kind == Fulfillment`, state ∈ {`Delivered`,`Cancelled`,`Failed`}, or `!PendingDispatch && no available code` |

Precedence: `history` → `returns` → `preparation` → `handoff` → `delivery` → `ready_to_send` → `history`.
A **settled returned source** (no resendable capacity, no damaged decision) is `history`, never active return work.
`correct_tracking` sits **below** `dispatch_shipment` and below `plan_shipment_dispatch`, so a correction can never
masquerade as the employee's next job. A **blocked active** row keeps its honest stage and carries the existing server
blocker; it is never hidden or re-labelled. Full mapping/precedence: `work-stage-mapping.md` (written before the code).

## 3. Independent time scope and stage-relevant time

`today` (default) · `future` · `all`, using the injected server `now` and the accepted zone of the row's own plan.

| Stage | Time fact shown | Persian label | Absent |
| --- | --- | --- | --- |
| preparation | `PreparationStartUtc` | آخرین مهلت شروع آماده‌سازی | undated |
| ready_to_send | `TransferDepartureUtc` | آخرین مهلت خروج به سمت حامل | undated |
| handoff | `HandoffDeadlineUtc` | آخرین مهلت تحویل به حامل (half-open close) | undated |
| delivery / returns | none stored | پیگیری جاری؛ مهلت مشخصی ثبت نشده | always undated |
| history | — | — | always undated |

`PreparationStartUtc` is never printed as a packing-finish time; the departure deadline and the carrier acceptance
close keep distinct names; the customer receiving promise stays its own column. `today` = overdue + due-today +
**undated** (unknown/ongoing work stays visible, labelled, with its own count); `future` = only future-day relevant
facts (tomorrow's handoff does not remove today's preparation). Sort: overdue first, then due-today, then undated, then
future, `WorkUnitKey` tie-break; an explicit user sort still wins. No browser-zone fallback.

## 4. Counts, paging and legacy URLs

One classification pass of one set (after search/column/advanced filters, before paging) produces:
stage badge = active rows of that stage **within the selected time scope**; «همه» = all active within scope;
time badge = rows of the selected stage at that scope; page `totalCount` = the rows the page is cut from;
history is time-independent and excluded from active totals; the undated subset is always explicit.
Nothing is tallied from a fetched page, and a package row still owns its locked members (no count inflation).

Legacy `?filter=` / `?queueFilter=` tokens are translated **once** at entry into a `(stage, scope)` pair and the token
is dropped, so two authorities can never silently combine (table in `work-stage-mapping.md` §5). Unknown/absent tokens
become the safe clean entry `همه` + `امروز`. `jobStage`/`timeScope` are rejected inside the advanced-filter expression
because they cannot be evaluated in SQL.

## 5. UI (existing screen and grid only)

- ONE job-stage strip (همه + five) as a real tablist with Persian count badges, plus a clearly secondary
  امروز/آینده/همه control; `سابقهٔ ارسال` is a quiet, separate, time-independent archive tab.
- Row shows the human parcel locator (`مرسولهٔ ۱ از ۱`), the exact server next verb (`ایجاد مرسوله`, `بسته‌بندی`,
  `ثبت تحویل`, …), the job-time with its exact meaning, and the separate customer promise.
- ONE existing «عملیات ارسال» link per row targeting the exact group/parcel/package, carrying the current queue
  selection for return; no mutation button was added to the queue.
- Row height 64px, stage/time tabs measured 44px, scoped horizontal grid scroll, no document overflow.

## 6. Focused proof (each once after the final code)

| Check | Result |
| --- | --- |
| Fulfillment `AdminFulfillmentJobStage271Tests` | **15/15** (stages, precedence, terminal vs active, package ownership, plan-before-correction, preparation-today/handoff-tomorrow, overdue + cutoff equality, independent zones, no-SLA undated, unknown capture visible in Today, counts beyond page 1, legacy tokens, explicit sort) |
| FE mount `admin-fulfillment-work-queue-271.mount.test.ts` | **10/10** (clean entry default, independent serialization, legacy rewrite, back/forward, counts vs page, unknown counts, stale-response fence, row verb/meaning/locator, archive, read-only) |
| FE node suites (`admin-fulfillment-work-queue`, `fulfillment-work-queue-260`, `fulfillment-work-queue-persian-dates-269`, `fulfillment-api`) | **28/28** |
| `npm run build` | 0 errors, `BUILD_ID 3jjJwEQe78ttzvxLwFBNg` |
| `dotnet publish` Host | 0 errors |

Architecture guards measured, **pre-existing failures only**: `FE-FOLDER-001`/`FE-SIZE` fail with the same
already-oversized/novel path set as at HEAD (`fulfillment-api.ts` was already 845 LOC > 800 at HEAD; this task adds 29
lines to that already-flagged path and adds no new violating path, and the extracted columns file keeps
`admin-fulfillment-work-queue-screen.tsx` at 537 lines). No baseline entry was rewritten.

## 7. Runtime (one canonical environment)

`tooba_alpha` @ `127.0.0.1:5432` (SingleStore, tenant `store-alpha`) — Host `5088` PID `40692`
(`/health` 200, `/health/ready` 200), Next `3000` PID `24868` (`BUILD_ID 3jjJwEQe78ttzvxLwFBNg`, manifest 200,
`/fa` 200, served chunk `967-fdda0cb21881b957.js` 200 with the stage copy). No migration, no DDL, no business SQL, no
fixture, no provider call, no settings/draft change.

Read-only live observation (`/fa/admin/fulfillments` and `/admin/fulfillments`): stage badges همه ۴ · آماده‌سازی ۲ ·
آمادهٔ ارسال ۱ · تحویل به حامل ۰ · پیگیری ۱ · برگشتی ۰; time badges امروز ۴ · آینده ۰ · همه ۴; `سابقهٔ ارسال ۰`;
summary «۴ ردیف در این نمایش · ۴ ردیف بدون موعد مشخص»; 390 and 1440 document overflow 0.

## 8. Limits

- The live set is small and **entirely undated**: the single dispatched row is `delivery` (undated by design) and the
  other three are legacy/uncaptured rows whose own job time does not exist yet, so no live *dated* preparation /
  ready-to-send / handoff / returns row was available. Dated ordering, cutoff equality, independent zones and
  future-scope separation rest on the focused behaviour tests, not on live rows.
- `سابقهٔ ارسال` is 0 on this data set; the archive is proven by the classifier + frontend tests, not by a live row.
- The two pre-existing architecture-guard failures above are reported, not repaired (no unrelated baseline work).
- No user visual/functional acceptance is claimed; a worker PASS is not an Architect ACCEPT.
