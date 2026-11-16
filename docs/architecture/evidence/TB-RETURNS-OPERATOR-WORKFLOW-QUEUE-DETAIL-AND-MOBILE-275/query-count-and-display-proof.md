# TB-275 — query, count and display proof

## One constrained set, one classification, one count block

`AdminReturnGridQueryEngine.QueryAsync` (the only queue read):

1. builds `q` from `returns.return_requests` with the common search + every ordinary column filter,
2. skips the `workCategory` filter in SQL (the selector is answered by the classification itself, never by a SQL guess),
3. applies the advanced-filter expression unless it names a work selector,
4. materialises the constrained `CaseFact` set,
5. classifies that WHOLE set (`ClassifyAsync`) into `ReturnWorkDescription` rows,
6. computes `counts{allOpen,receiving,decision,refund,refundFailed,disposition,history}` from it,
7. filters the SAME classified list by the canonical selection, sorts it, and only then cuts the page,
8. returns `AdminReturnWorkQueuePage(items, page, pageSize, totalCount, counts)`.

Therefore:
- `totalCount` is the size of the set the page was cut from — never a page-derived number;
- each badge is counted over the same constrained set, so a badge cannot disagree with its rows;
- `history = !IsOpen` is a constant function of the common constraints, so it does not move when an active strip is
  selected;
- nothing is tallied from a fetched page and there is no arbitrary row cap that would make a count look plausible.

## Batch reads (no per-row Directory / provider call)

`ClassifyAsync` performs 5 set-based reads over the constrained ids (`return_items`, `refund_attempts`,
`return_decisions`, `customer_return_receipts`, `customer_return_receipt_lines`) and groups them in memory.
`MapAsync` performs 3 batched Contracts enrichments for the PAGE rows only (`GetOrderNumbersAsync`,
`GetRecipientNamesAsync`, `GetPartyDisplayNamesAsync`) and the case-ordinal batch. There is no per-row `GetAsync`,
payment read or provider call, and the engine depends on Contracts (`IOrderGridEnrichmentReader`, `IPartyLookup`,
`ICatalogVariantLookup`, `IFulfillmentReturnReader`) — never on the Order orchestrator.

## Selection tokens (server)

`AdminReturnWorkQueueSelection.Canonicalize`:

| Input | Canonical selection |
| --- | --- |
| `""`, `all`, unknown | `open` (the honest active set) |
| `pending_review` | `decision` |
| `approved`, `refund_needed`, `refund_pending` | `refund` |
| `refund_failed` | `refund_failed` |
| `completed`, `rejected` | `history` |
| `receiving`, `decision`, `refund`, `disposition`, `settled`, `history` | itself |

A selector placed inside the advanced expression is refused with `grid.advancedFilter.field.invalid`; a selection
without a single value / a supported operator is `grid.filter.operator.invalid`. Both are raised in `Normalize` before
the database is touched.

## Stable human locator

`returnReference = "RET-{orderNumber}-{ordinal}"`, where `ordinal` is the case's position in its seller order by
`(CreatedAt, ReturnRequestId)` computed from the sibling cases of that order — an immutable fact, so the same case keeps
the same label across page, sort and filter, and two cases of one order can never share it. A missing order number
degrades to «مرجوعی» rather than printing a GUID.

## Multi-product / mixed-unit honesty

`AdminReturnWorkQueueLine` carries each line's OWN product label, quantity and unit. Aggregates are never produced by
summing incompatible units; a row with `n > 1` lines publishes «N قلم» and the detail table lists every line with its
own unit. `AdminReturnWorkQueueRow.Quantity` remains the raw requested total for sorting/export only.

## Frontend mapping proof (`admin-returns-work-queue.test.ts`, 21/21)

| Fact | Test |
| --- | --- |
| The queue is a read-only work list with ONE reviewed entry per row (no kebab, no hidden mutation) | `work queue screen is a read-only work list with one reviewed entry per row` |
| The strips are the issued primary set plus a quiet history tab, in one `role=tablist` | `work strips are the issued primary set plus a quiet history tab` |
| The screen barrel delegates to the work queue and the detail barrel keeps the workspace | `AdminReturnsScreen delegates to work queue screen and the detail barrel keeps the workspace` |
| The workspace is the single action owner and never sends before the intent is persisted | `case workspace is the single action owner and never sends before persisting the intent` |
| The DTO mapper keeps refund lifecycle separate from return status | `mapReturnWorkQueueList maps work-queue DTO without mixing refund into return status` |
| Per-line units are preserved, never summed | `work-queue rows keep per-line units instead of summing incompatible ones` |
| An absent work stage is never turned into an invented open permission | `an absent work stage is never read as an invented open permission` |
| An absent counts block is UNKNOWN, not zero | `work counts are read from the server payload and an absent block is unknown, not zero` |
| Legacy tokens normalize once and the strip round-trips through the URL | `legacy queue tokens normalize once, and the strip round-trips through the URL` |
| Mixed inspected/restocked facts are mapped without invention | `a case snapshot maps the mixed inspected/restocked facts without inventing them` |
| The receipt review never preselects arrival/condition and refuses inexact quantities | `receipt review never preselects arrival/condition and refuses inexact quantities` |

## Live read-only wire (Host 5088)

```text
POST /v1/admin/returns/query           no actor                → 401 admin.actor.missing
POST /v1/admin/returns/query           actor, {}               → 200 totalCount 0 + counts{allOpen:0,receiving:0,
                                                                     decision:0,refund:0,refundFailed:0,
                                                                     disposition:0,history:0}
POST /v1/admin/returns/query           workCategory in [refund_failed]  → 200, same empty page + counts
POST /v1/admin/returns/query           workCategory in [pending_review]  → 200 (legacy → decision, no error)
POST /v1/admin/returns/query           workCategory in [refund_needed]   → 200 (legacy → refund, no error)
POST /v1/admin/returns/query           workCategory in [zzz_unknown]     → 200 (active set, never everything)
POST /v1/admin/returns/query           workCategory in [history]         → 200
POST /v1/admin/returns/query           workCategory inside advancedFilter → 400 grid.advancedFilter.field.invalid
POST /v1/admin/returns/query           workCategory without operator     → 400 grid.filter.operator.invalid
GET  /v1/admin/returns/{unknown}       → 404 return.missing
```

## Live DOM (served build `oyZqjXe6auOIgTkU-Gm6R`)

| Viewport | Result |
| --- | --- |
| 1440 | document overflow `0`; 7 strips, each 44px, counts in Persian digits («همهٔ کارهای باز ۰» … «سابقه ۰»); grid «ردیفی برای نمایش نیست» + «نمایش ۰ تا ۰ از ۰ ردیف» |
| 390 | document overflow `0`; strip overflow `0`; min tab height 44px; grid text viewport 340 with 1652 content (scoped scroll inside the grid) |
| 390 with `?queueFilter=refund_needed` | «پیگیری بازگشت وجه» selected; the legacy token was translated once, no old+new double filter |
| click «سابقه» | `location.search` → `?workCategory=history`; selection moved; honest empty page |

The live queue is empty because **no return case exists** in the canonical database; the counts are honest zeros, not
an invented dataset.
