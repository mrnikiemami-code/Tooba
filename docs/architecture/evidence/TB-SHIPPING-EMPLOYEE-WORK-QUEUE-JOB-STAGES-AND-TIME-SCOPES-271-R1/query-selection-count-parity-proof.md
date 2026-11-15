# TB-271-R1 — query selection / count parity proof

All facts below come from the REAL owning pipeline
`AdminFulfillmentGridQueryPolicy.Normalize → QueryAdminFulfillmentWorkQueueHandler → AdminFulfillmentWorkQueueQueryEngine.QueryWithCountsAsync
→ AdminFulfillmentWorkUnitProjection → AdminFulfillmentWorkQueueCountsBuilder`, not from hand-built row lists.

## 1. ONE predicate behind page, total, undated and badges

`AdminFulfillmentWorkQueueCountsBuilder.InSelection(row, stage, scope)`

- `stage == history` ⇒ `!IsActive(row)` — the archive is exactly the terminal subset and ignores the echoed time scope;
- otherwise the row must be `IsActive` (five stages only), must match the selected stage (`all` matches every active
  stage and never `history`), and must fall inside the selected time scope;
- `SelectedTotal = rows.Count(InSelection)` — the same predicate the page is cut from;
- `Select(...)` is the sequence the engine slices, so `TotalCount == SelectedTotal` by construction.

Badge dimensions (deliberately different, all from the same materialized set):

| Badge | Set |
| --- | --- |
| stage badges | ACTIVE rows inside the SELECTED time scope, before the selected stage |
| time badges | the SELECTED active stage, before the selected scope (0 on the archive) |
| archive badge | the whole terminal subset after the common search/column constraints, independent of the active tab |
| undated | the «موعد مشخص نشده» subset of the current selection |

## 2. No stage-dependent SQL narrowing

`ClassifyAsync` applies only the common search/column filters and the advanced expression, then loads and classifies
the complete physical row set. There is no `Cancelled/Delivered` prefilter keyed on the selected stage, so the archive
badge is a constant function of the search/column constraints. Terminal-vs-active is decided once, in the projection,
through `hasOpenObligation` (remaining process/pack/allocation quantity, a live parcel that must move, or a received
source that still owes a decision).

## 3. One classification, one clock, one response

`QueryWithCountsAsync` reads `_clock.UtcNow` ONCE and calls `ClassifyAsync` ONCE; the page, the total and every badge are
derived from that single materialized set. `QueryAsync` and `CountAsync` both delegate to it, so no caller can produce a
second classification inside one response. The live handler calls only `QueryWithCountsAsync`.

Evidence (`AdminFulfillmentWorkQueue271R1Tests`):

- `The_live_handler_uses_the_one_combined_read_and_never_the_split_page_count_path` — 1 combined read, 0 page reads,
  0 count reads, `Assert.Same` on the returned items/counts, forwarded sort empty;
- `One_combined_read_reads_the_server_clock_once_and_counts_the_same_classified_set_as_the_page` — `clock.Reads == 1`,
  `TotalCount == Counts.SelectedTotal == 2`, `Item s` = 1 row, `stageAll == 2`, `stageHistory == 1`;
- `Every_page_is_cut_from_the_same_total_beyond_page_size_and_never_repeats_a_row` — pages 1 and 2 of a 2-row set both
  total 2 with identical badges and disjoint keys; page 3 is empty while `SelectedTotal` stays 2.

## 4. Active/archive parity (`The_all_view_and_its_totals_never_carry_a_terminal_row`,
`The_archive_badge_is_identical_under_every_active_tab`,
`The_archive_selection_is_time_independent_and_its_time_badges_are_honestly_zero`)

- «همه» never contains a history row; `stageAll == preparation + ready_to_send + handoff + delivery + returns`;
- `stageHistory` is identical for `all`, `preparation` and `handoff` selections;
- an empty tab reports `SelectedTotal 0` instead of "all rows";
- the archive returns terminal rows only and its rows/ total do not change between an echoed `today` and `all`;
- the archive's time badges are 0 (it has no time dimension) instead of borrowing the active counts.

## 5. Live parity on the canonical database (read-only)

```
no sort/no filters : totalCount 4  selectedTotal 4  stageAll 4  stageHistory 1
                     items: preparation, preparation, ready_to_send, delivery (no history row)
                     time: today 4 / future 0 / all 4, undated 4
history + today    : totalCount 1  selectedTotal 1  rows=1 (stage history)  time 0/0/0
history + all      : totalCount 1  same single row (time-independent)
all + all          : totalCount 4  stageAll 4  stageHistory 1
preparation + all  : totalCount 2  selectedTotal 2  time 2/0/2
handoff + future   : totalCount 0  stageAll 0  stageHistory 1 (unchanged)
```

Served DOM: stage badges همه ۴ · آماده‌سازی ۲ · آمادهٔ ارسال ۱ · تحویل به حامل ۰ · پیگیری ۱ · برگشتی ۰ and the archive
badge stayed «۱» across «همه» → «تحویل به حامل» → «همه»; the archive tab rendered exactly one row with a disabled time
control, 0 time badges and «۱ ردیف در این نمایش»; the grid footer matched the server total, and the live request body
carried `sort: []` plus only `jobStage`/`timeScope`.
