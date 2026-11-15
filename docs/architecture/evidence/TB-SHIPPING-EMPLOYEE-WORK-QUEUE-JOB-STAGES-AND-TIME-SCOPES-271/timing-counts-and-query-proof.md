# TB-271 — time, counts and query proof

Every fact below is either read from the existing source seams named here or was measured read-only on the deployed
canonical runtime. No business row, plan, hold, setting or draft was written.

## 1. Query composition (one set, one pass)

`AdminFulfillmentWorkQueueQueryEngine.QueryAsync`:

1. loads the filtered **physical** units exactly as before (`AdminFulfillmentWorkUnitProjection`; legacy timing
   `queueFilter` is NOT used as a SQL shortcut any more for the new dimension),
2. resolves the accepted plan / timing facts per unit (existing readers, unchanged),
3. classifies each row with `AdminFulfillmentJobStageAuthority` (stage) and the injected server `now` + the row's own
   zone (bucket),
4. hands the classified set to `AdminFulfillmentWorkQueueCountsBuilder`, which produces **both** badge dimensions and
   `Select`s the page from the same in-memory set,
5. maps rows once (`JobStage`, `JobStageLabelFa`, `JobTimeUtc`, `JobTimeMeaningFa`, `JobTimeState`, `TimeBucket`,
   `IsUndated`, counters).

`CountAsync` is not a second authority: the response carries `totalCount`, the five stage counts, the «همه» count, the
time counts, the `undatedCount` and the `historyCount` computed from the same classified set (so page and badges cannot
disagree by construction). No per-row API/DB fetch, no page-local tally, no client-side date arithmetic.

Why not filter by seller status first: sibling physical rows of one seller differ in stage (a terminal sibling next to
an active parcel, a package next to its locked child), which the task forbids collapsing.

## 2. Badge dimensions (explicit)

| Badge | Definition |
| --- | --- |
| stage `x` | active rows of stage `x` **within the selected time scope**, after search/column/advanced filters, before paging |
| «همه» | all active rows within the selected time scope, same constraints |
| `سابقهٔ ارسال` | terminal rows, time-independent, never part of an active total |
| time `t` | rows of the **selected stage** at scope `t`, same constraints |
| page total | rows matching the selected stage **and** scope — the exact set the page is cut from |
| «بدون موعد مشخص» | the `undated` subset of the current selection |

Live confirmation of the independence (measured, `/fa/admin/fulfillments`):

| Step | Stage badges | Time badges | Rows |
| --- | --- | --- | --- |
| clean entry | همه ۴ · آماده‌سازی ۲ · آمادهٔ ارسال ۱ · تحویل ۰ · پیگیری ۱ · برگشتی ۰ | امروز ۴ · آینده ۰ · همه ۴ (سابقهٔ ۰) | «۴ ردیف در این نمایش · ۴ ردیف بدون موعد مشخص» |
| select `آمادهٔ ارسال` | unchanged (whole set within scope) | **امروز ۱ · آینده ۰ · همه ۱** | 1 row |
| then select `آینده` | unchanged | امروز ۱ · آینده ۰ · همه ۱ | «۰ ردیف در این نمایش» (honest empty, no fake zero) |
| URL after both | `?stage=ready_to_send&scope=future` | | |

So the stage dimension is counted over the whole active set while the time dimension is counted inside the selected
stage — the definitions above, observed live, not asserted.

## 3. Time semantics (one meaning per stage)

| Stage | Fact | Meaning printed | Bucket rule |
| --- | --- | --- | --- |
| preparation | `PreparationStartUtc` | آخرین مهلت شروع آماده‌سازی | `<= now` → `past_due`; same local day → `due_today`; later day → `future` |
| ready_to_send | `TransferDepartureUtc` | آخرین مهلت خروج به سمت حامل | same |
| handoff | `HandoffDeadlineUtc` | آخرین مهلت تحویل به حامل | same, half-open (equal to the cutoff is already late) |
| delivery / returns | none | پیگیری جاری؛ مهلت مشخصی ثبت نشده | always `undated` |
| history | none | — | always `undated`, excluded from active totals |

- `PreparationStartUtc` is a **start** deadline; no pick/pack duration and no "packing finished" instant is invented.
- Departure deadline and carrier acceptance close keep separate names; the close is a cutoff, not an appointment.
- The customer receiving promise is never used as the warehouse due time.
- A missing/invalid zone yields the truthful unavailable marker; the browser zone is never substituted.
- `today` includes `undated`; `future` contains only `future` relevant facts, so tomorrow's handoff does not hide
  today's preparation start.

Live example of the exact distinction (dispatched row, 1440, RTL grid scrolled right):

```
stage            پیگیری تحویل
job time         پیگیری جاری؛ مهلت مشخصی ثبت نشده      <- delivery has no stored SLA: NOT borrowed from the plan
bucket           در مسیر
handoff window   ۲۴ آبان ۱۴۰۵ · ⁦۰۸:۰۰–۲۰:۰۰⁩ · به وقت تهران
deadlines        آماده‌سازی تا ۲۴ آبان ۱۴۰۵، ۱۳:۰۰ · خروج تا ۲۴ آبان ۱۴۰۵، ۱۸:۴۰ · تحویل به حامل تا ۲۴ آبان ۱۴۰۵، ۲۰:۰۰ · به وقت تهران
customer         ۲۶ آبان ۱۴۰۵ · ⁦۰۹:۰۰-۱۰:۰۰⁩
next step        ثبت تحویل
```

Undated active rows stay visible and labelled (`موعد ارسال مشخص نشده`) with `ایجاد مرسوله` / `بسته‌بندی` as their
real next verb, instead of being treated as overdue or completed.

## 4. Legacy URL normalization (measured)

`/fa/admin/fulfillments?queueFilter=needs_action` → renders `همه` + `امروز` (badges همه ۴ · امروز ۴) and no hidden
second filter is applied. The token is translated once at entry (mapping table in `work-stage-mapping.md` §5) and
unknown/absent tokens fall back to the same clean selection. Back/forward restores the same `(stage, scope)` pair —
covered by the mounted suite, and the mount test asserts the legacy token is removed rather than combined.

## 5. Response fence (no stale publication)

The screen keys every request by a monotonically increasing sequence and drops a response whose sequence is not the
latest, so an older stage/time answer can never republish rows or badges over a newer selection (mounted case
`an older response can never publish its badges over a newer selection`). Changing stage/time issues reads only: no
mutation request, no cleared planning attempt, no timer and no background poll (mounted case
`changing stage/time is a read: no mutation request and no cleared planning attempt`).
