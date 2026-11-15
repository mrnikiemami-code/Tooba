# TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271-R1 — RESULT

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271-R1
Parent-Task: TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271
Mode: REPAIR_QUEUE_ACTIVE_ARCHIVE_PARITY_SINGLE_READ_AND_REAL_DEFAULT_ORDER
Status: **PASS within the issued repair scope** (worker PASS — NOT Architect ACCEPT, NOT user visual/functional
acceptance, NOT any shipping/carrier/financial claim).
Decision:
`ONE_ACTIVE_ARCHIVE_SELECTION_WITH_A_CONSTANT_ARCHIVE_BADGE_ONE_COMBINED_CLASSIFIED_SET_UNDER_ONE_SERVER_NOW_A_REAL_JOB_PRIORITY_DEFAULT_ORDER_TYPED_REFUSAL_OF_UNEVALUABLE_SELECTIONS_AND_AN_EXPLICIT_ZONE_UNAVAILABLE_CLASSIFICATION_ON_THE_EXISTING_QUEUE_WITH_ZERO_LIFECYCLE_OR_DATA_CHANGE`

Base-SHA (verified ancestor, actual work base): `09cbaa436a03310f0cb46e6e622824f938d6c728` (== `HEAD` before the change
set; the issued `Expected-Base-SHA` is exactly this commit and `git merge-base --is-ancestor` returned 0).
Code/Evidence/HEAD SHAs: see `result-contract.txt`.

## What was actually wrong (source-confirmed) and what changed

| # | Architect defect | Repair |
| --- | --- | --- |
| A1 | `Select`/`MatchesStage` returned history rows on the active «همه» page while `Build` counted active only | ONE predicate `InSelection`: an active selection (including «همه») returns active rows only inside the selected scope, `history` returns only terminal rows and is time-independent; `SelectedTotal`, the undated count and the page all use this same predicate |
| A2 | stage-dependent `Cancelled/Delivered` SQL prefilter made the archive badge a function of the selected tab | the prefilter is gone: the whole physical row set for the common search/column constraints is classified on EVERY request, so `stageHistory` cannot move with the active tab; the terminal decision lives in the projection |
| A3 | blocked active work (`no available codes`) was pushed into `History` | terminality is a POSITIVE fact: `hasOpenObligation` (remaining process/pack/allocation quantity, a live parcel that must move, or a received source that still owes a decision) is projected from physical facts; a blocked unit keeps one of the five honest stages with its existing exact blocker and gains no permission |
| B | handler called `QueryAsync` + `CountAsync`, each with its own classification and clock read | new `QueryWithCountsAsync` returns `AdminFulfillmentWorkQueueSnapshot` (items + page + total + counts) from ONE `ClassifyAsync` under ONE injected `_clock.UtcNow`; the live handler calls it once. The legacy `QueryAsync`/`CountAsync` methods delegate to the same combined read, so no second classification can occur |
| C | `Normalize` injected `updatedAt desc`, so the engine's job-priority branch never ran | an absent sort stays absent; the engine's overdue → due-today → undated → future ordering (with `JobTimeUtc`, dispatch bucket, handoff date and `WorkUnitKey` tie-breaks) now executes on a clean entry; an explicit operator/saved-view sort is preserved verbatim; selection tokens are not sortable |
| D | `jobStage`/`timeScope` were accepted inside advanced expressions and then ignored | `NormalizeAdvanced` refuses them (plus the legacy `queueFilter`) with `grid.advancedFilter.field.invalid` before any read; a `jobStage`/`timeScope` selection must be exactly one value with `equals`/`in` (`grid.filter.operator.invalid` otherwise) |
| E | `ResolveZone` fell back to UTC, so unknown-zone facts looked due today/future | `AcceptedZone` validates the accepted id (`TimeZoneInfo.FindSystemTimeZoneById`) with NO fallback; missing/invalid ⇒ `jobTimeMeaning=zone_unavailable`, `jobTimeState=undated` and an explicit Persian label, while the stored instant is kept for auditing. Half-open cutoff and accepted-zone day comparison are unchanged for valid zones |

Untouched by design: five job stages, the Today/Future/All control, the quiet archive tab, 64px rows, the exact
`WorkUnitKey`, the row/plan binding, own-plan vs first-dispatch timing, one operator link, legacy URL normalization.

## Connected proof (actual handler/query wiring, not helpers)

`AdminFulfillmentWorkQueue271R1Tests` **17/17** on the real pipeline
`AdminFulfillmentGridQueryPolicy.Normalize → QueryAdminFulfillmentWorkQueueHandler → AdminFulfillmentWorkQueueQueryEngine
→ AdminFulfillmentWorkUnitProjection → AdminFulfillmentWorkQueueCountsBuilder`:

- absent sort stays absent; explicit `updatedAt asc` + `dispatchBucket desc` preserved; `jobStage`/`timeScope` sorts dropped;
- advanced `jobStage`/`timeScope`/`queueFilter` conditions refused with `grid.advancedFilter.field.invalid`; an ordinary
  `cityName` condition still normalizes;
- `notIn`/`notEqual`/blank/multi-value selections refused with `grid.filter.operator.invalid`; single `in` value accepted;
- the handler performs exactly ONE combined read and ZERO page/count reads, returns the SAME snapshot objects and
  forwards an empty sort;
- a refused selection returns the stable error with zero reads (no DB access);
- one combined read reads the clock exactly ONCE and the page is cut from the same classified set the badges describe;
- default order is overdue → future with no client sort;
- «همه» contains no history row and `stageAll` equals the sum of the five stage badges; the archive badge is identical
  under «همه»/preparation/handoff, and an empty tab is honestly 0 (never "all rows");
- the archive selection is time-independent (same rows/total for an echoed `today` and `all`) and its time badges are 0;
- a blocked unit with an open obligation keeps `ready_to_send`/`preparation`/`delivery`; the same shapes with
  `hasOpenObligation=false` are history; a Created parcel with no tracking stays ready-to-send;
- pages 1 and 2 of a two-row set both total 2 with identical badges and no repeated row; a page past the end is empty
  while the total stays 2;
- missing accepted zone ⇒ `zone_unavailable` + «منطقهٔ زمانی مشخص نشده؛ زمان کار قابل تعیین نیست» + `undated` (instant
  retained); invalid zone ⇒ «منطقهٔ زمانی نامعتبر…»; a valid zone still answers the local day in that zone
  (`Asia/Tehran` future vs `UTC` due-today for the same instant).

Frontend `admin-fulfillment-work-queue-271.mount.test.ts` **12/12** (2 new): a clean entry sends `sort: []` and the two
visible tokens; the grid footer total is the server's `totalCount`/`selectedTotal` (not the fetched row) and the archive
badge never moves across stage/time clicks; a `zone_unavailable` row prints the missing-zone sentence with
`data-job-time-state="undated"` and no raw ISO/IANA id; retained plan attempt, stale-response fence, legacy-URL
translation, back/forward restore and read-only filtering stay green.

## Live read-only proof (one canonical `tooba_alpha`, no fixture, no write)

`POST /v1/admin/fulfillments/work-queue/query` through the Development dev-actor (`01a036c2-…`), read-only:

| Request | Result |
| --- | --- |
| no sort, no filters | `totalCount 4`, `selectedTotal 4`, `stageAll 4`, `stageHistory 1`, four active items (`preparation×2`, `ready_to_send×1`, `delivery×1`), time badges today 4 / future 0 / all 4, undated 4 |
| `history` + `today` | 1 row (stage `history`), `totalCount 1 = selectedTotal 1`, time badges 0/0/0 |
| `history` + `all` | same single row (time-independent archive) |
| `all` + `all` | `totalCount 4`, `stageAll 4`, `stageHistory 1` |
| `preparation` + `all` | `totalCount 2 = selectedTotal 2`, time badges 2/0/2 |
| `handoff` + `future` | 0 rows, `stageAll 0`, `stageHistory 1` (unchanged) |
| advanced `jobStage` condition | HTTP 400 `grid.advancedFilter.field.invalid` |
| multi-value `jobStage` filter | HTTP 400 `grid.filter.operator.invalid` |
| explicit `updatedAt asc` | 200, ordered rows (explicit sort preserved) |
| supported advanced `cityName` | 200, accepted and evaluated |

Served UI (`/fa/admin/fulfillments`, `/admin/fulfillments`, DOM/text only, no screenshots): stage badges همه ۴ ·
آماده‌سازی ۲ · آمادهٔ ارسال ۱ · تحویل به حامل ۰ · پیگیری ۱ · برگشتی ۰; time امروز ۴ / آینده ۰ / همه ۴; archive
«سابقهٔ ارسال ۱» constant while switching «همه»→«تحویل به حامل»→«همه»; summary «۴ ردیف در این نمایش · ۴ ردیف بدون
موعد مشخص»; the archive tab shows exactly 1 row, its time control is `disabled`, its time badges are 0 and the summary
is «۱ ردیف در این نمایش»; the live queue request body was captured as `sort: []` with only the two visible selection
filters; document overflow 0 at 1440 and 390; all seven stage tabs and three scope tabs measure 44px.

## Checks (run once after the final code change)

- Fulfillment `AdminFulfillmentWorkQueue271R1Tests` **17/17**; `AdminFulfillmentJobStage271Tests` **15/15**; combined
  with `FulfillmentErrorAndGridTests` **37/37**.
- FE mount `admin-fulfillment-work-queue-271.mount.test.ts` **12/12**; node suites
  `admin-fulfillment-work-queue.test.ts` **9/9**, `test:fulfillment` **6/6**, `test:grid` 6 pass with ONE pre-existing
  failure (see limits).
- `tsc --noEmit` 0; `npm run build` 0 (`BUILD_ID mry3IgeR--YNQDX_gvEMt`); `dotnet build Tooba.slnx -c Release`
  0 errors; Host publish 0 errors.
- Whole-project Fulfillment test run disclosed: 489 passed / 20 failed, where the 20 are the pre-existing
  disposable-Postgres-gated classes (`ShippingTypePostgresStorageTests`, `*MigrationTests`,
  `HistoricalDefaultInsertParityTests`, `HistoricalNameAlignmentResidualDiffTests`,
  `TypeCatalogModelMetadataParityTests`) plus the two pre-existing module architecture guards
  (`Fulfillment_directory_stays_under_the_arch_size_ceiling`: `FulfillmentDirectory.cs` 1110 LOC > 800;
  `Fulfillment_golden_boundaries_and_physical_layout_remain_clean`: a `Returns` folder is not in the guard allowlist).
  None of those files is in this change set.

## Deploy

ONE canonical `tooba_alpha` 5432 SingleStore `store-alpha`; Host 5088 (PID 24008, `/health` 200,
`/health/ready` 200 → `{"edition":"SingleStore",…}`); Next 3000 (PID 41084, `/fa` 200, served
`_buildManifest.js` 200 on BUILD_ID `mry3IgeR--YNQDX_gvEMt`). No migration/DDL/reset/reseed/backfill/business SQL/
cleanup/restore, no new fixture, no settings/key/CapturedPreview/edition/connection/port change, no provider/waybill/
money/notification action. User records and the completed trial data stayed read-only.

## Limits (declared)

- The live set is **entirely undated** (4 active rows, all `undated`: three legacy groups with no accepted plan and one
  dispatched parcel whose delivery follow-up stores no SLA), so live dated ordering, cutoff equality, independent zones
  and the future scope rest on the focused tests. No demo row was manufactured to fake coverage.
- The archive holds one row; the archive badge invariance is proven by the live responses and the focused tests, not by
  a large terminal set.
- `npm run test:grid` has one PRE-EXISTING failure in `column-manager.test.ts`
  (`PAGE_SIZES = [10, 25, 50, 100, 1000]` expected while the unchanged `AppDataGrid.tsx` at `HEAD` already contains
  `[10, 20, 25, 50, 100, 1000]`); not introduced or touched here.
- 25 pre-existing 36px grid toolbar controls remain under 44px at 1440; the stage/scope tabs and row actions are 44px.
- Read-only DOM/text verification only (no screenshots, no clicks on business actions); no user visual/functional
  acceptance is claimed.
- Untracked `scripts/tb266r1-*.mjs` / `scripts/tb270r2-*.mjs` scratch files are not part of this change set.

Review URL: see `user-review.md`.
`STOP_FOR_ARCHITECT` · `automaticNextImplementationTask: NONE`
