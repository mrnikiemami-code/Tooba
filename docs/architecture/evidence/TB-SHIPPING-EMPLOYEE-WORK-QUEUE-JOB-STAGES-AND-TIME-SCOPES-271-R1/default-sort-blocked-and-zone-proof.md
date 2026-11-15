# TB-271-R1 — default sort, blocked work, refused selections and the zone boundary

## 1. Real default order (Repair C)

Before: `AdminFulfillmentGridQueryPolicy.Normalize` injected `updatedAt desc` whenever the request had no sort, so
`AdminFulfillmentWorkQueueQueryEngine.ApplySort`'s job-priority branch never ran on a clean entry.

After: `Normalize` keeps an absent sort absent (`sorts` is filtered, never defaulted), and the engine's branch runs on
`sort is null || blank`:

`PriorityRank(JobTimeState)` (past_due 0 → due_today 1 → undated 2 → future 3) → `JobTimeUtc` → dispatch-bucket rank →
`DispatchDeadlineUtc` → `PlannedHandoffDate` → `WorkUnitKey` (ordinal tie-break, so two parcels of one order never swap).

An explicit operator/saved-view sort is preserved verbatim and takes the explicit column branch (`updatedAt asc` in the
tests; live `updatedAt asc` returned the ordered set). Selection tokens are not sortable: `SortableFields` is a separate
whitelist from `FilterFields`, so `jobStage`/`timeScope` sorts are dropped instead of silently ordering by nothing.

Evidence: `An_absent_sort_stays_absent_so_the_job_priority_order_can_run`,
`An_explicit_user_sort_is_preserved_verbatim_and_a_selection_token_is_never_sortable`,
`The_default_order_is_overdue_then_future_with_no_client_sort` (UpdatedAt deliberately conflicting), and the mounted FE
case asserting `sort: []` on clean entry.

## 2. Blocked active work is not history (Repair A3)

`ResolveStage` now accepts `hasOpenObligation` — a POSITIVE projection fact:

- group row: remaining `ordered > processing` or `processing > packed`, or packed quantity no open parcel consumed;
- parcel row: any live parcel that is not a fully settled `Returned` source;
- package row: `Created`/`Dispatched` package;
- history row (kind `Fulfillment`): the projection produced no open unit at all.

`AvailableActionCodes == []` is never terminality. A blocked unit keeps its honest one-of-five stage and its existing
``اقدام مجاز فعلی برای این ردیف وجود ندارد…`` blocker, and gains no permission. A `Returned` source with no resendable
capacity and no outstanding damaged quantity becomes archive, while one that still owes a decision stays `returns`.

Evidence: `A_blocked_active_unit_keeps_its_honest_stage_because_terminality_is_a_positive_fact`,
`A_created_parcel_without_tracking_is_ready_for_send_and_never_archive`.

## 3. Unsupported selections refuse before any read (Repair D)

- Advanced expression: `AdvancedFilterRefusedFields = { jobStage, timeScope, queueFilter }` ⇒
  `grid.advancedFilter.field.invalid` inside `Normalize`, before the DB is touched. Ordinary conditions (`cityName`, …)
  still normalize and evaluate.
- Top-level selection: `SelectionFields = { jobStage, timeScope, queueFilter }` must use `equals`/`in` with exactly ONE
  non-blank value; `notEqual`/`notIn`/`blank`/multi-value ⇒ `grid.filter.operator.invalid`. A `notEqual` can never be
  read as "the first token".

Evidence: `A_selection_token_inside_an_advanced_expression_is_refused_before_any_read`,
`A_stage_or_scope_selection_must_be_exactly_one_value`, `A_refused_selection_returns_the_stable_field_error_and_reads_nothing`
(zero combined/page/count reads). Live: HTTP 400 `grid.advancedFilter.field.invalid` and `grid.filter.operator.invalid`.

## 4. Unknown accepted zone is unknown (Repair E)

`AdminFulfillmentJobStageAuthority.AcceptedZone` uses `TimeZoneInfo.FindSystemTimeZoneById(id)` and returns `null` for a
missing or refused id — there is NO UTC/Tehran/browser fallback at the new classifier boundary (the older scheduling
helper is left untouched).

| Accepted evidence | Result |
| --- | --- |
| no instant | `jobTimeMeaning=missing_plan`, «موعد ارسال مشخص نشده», `undated` |
| instant + missing zone | `jobTimeMeaning=zone_unavailable`, «منطقهٔ زمانی مشخص نشده؛ زمان کار قابل تعیین نیست», `undated`, instant retained |
| instant + invalid zone | `jobTimeMeaning=zone_unavailable`, «منطقهٔ زمانی نامعتبر؛ زمان کار قابل تعیین نیست», `undated`, instant retained |
| instant + valid zone | the stage's own meaning/label and `StateOf` (half-open `<= now` ⇒ past_due; same local day ⇒ due_today; later day ⇒ future) |

An undated active row stays in «امروز» as the honest exception (never claimed as future) and is counted in the explicit
«بدون موعد مشخص» summary. Valid-zone semantics are unchanged: the same instant is `future` in `Asia/Tehran` and
`due_today` in `UTC`.

Evidence: `A_missing_accepted_zone_is_undated_with_an_explicit_reason_instead_of_a_guess`,
`An_invalid_accepted_zone_is_named_as_invalid_and_never_replaced_by_utc_or_tehran`,
`A_valid_accepted_zone_still_classifies_the_day_in_that_zone`, plus the mounted FE case that prints only the missing-zone
sentence with `data-job-time-state="undated"` and no raw ISO / IANA id.
