# RESULT — TB-SHIPPING-QUEUE-PERSIAN-DATES-AND-CONSISTENT-TIME-DISPLAY-269

Status **PASS within the issued presentation-only scope**.
Decision `QUEUE_HANDOFF_CUSTOMER_AND_OPERATIONAL_DEADLINE_CELLS_RENDER_ONE_PERSIAN_JALALI_CALENDAR_WITH_DATE_ONLY_STABILITY_ACCEPTED_ZONE_INSTANTS_ISOLATED_LTR_RANGES_AND_TRUTHFUL_INVALID_MARKERS_WITH_ZERO_DATA_CHANGE`.

NOT Architect ACCEPT, NOT user VISUAL ACCEPT, no shipping-complete claim.

## 1 The reported defect, source-confirmed

The user's 2026-11-15 screenshot of `/admin/fulfillments` printed the handoff commitment as a raw `2026-11-15` while the
deadline column beside it was already Jalali. `fulfillment-work-queue-dispatch.ts` was returning the wire values verbatim:

| Formatter | Before | What the operator saw |
| --- | --- | --- |
| `formatPlannedHandoff` | `row.plannedHandoffDate` + raw window + `(Asia/Tehran)` | `2026-11-15 · 08:00–20:00 (Asia/Tehran)` |
| `formatCustomerReceiving` | `row.customerReceivingDate` + raw window | `2026-11-17 · 09:00-10:00` |
| `formatOperationalDeadline` | `fa-IR` `2-digit` fields, `utc`/raw zone on failure | `۱۴۰۵/۰۸/۲۴ ۱۳:۰۰` and a raw ISO/zone on any refusal |

TB-267 had standardized the order-operations surface only; the queue kept its own three shapes. Cell **and** `title`
both called these formatters, so the tooltip repeated the same raw value.

## 2 The repair — display only, one shared authority

`formatPlannedHandoff`, `formatCustomerReceiving`, `formatOperationalDeadline` and `formatPreparationDeadlines` now render
through the **existing** shared helpers in `src/frontend/lib/operational-format.ts` (`operationalDayLabel`,
`operationalInstantLabel`, `operationalTimeZoneLabel`, `operationalTimeWindowLabel`). No new formatting framework, no
dictionary, no locale registry change.

| Fact | Rendered as | Why |
| --- | --- | --- |
| date-only acceptance (`2026-11-15`) | `۲۴ آبان ۱۴۰۵` | the promise is that **calendar day**; `operationalDayLabel` anchors it at local noon, so it is never converted through UTC/local midnight and can never slide a day |
| accepted plan instant | `۲۳ آبان ۱۴۰۵، ۱۳:۰۰` in the accepted carrier zone | `operationalInstantLabel` formats **in** the plan's own zone id, so an instant that crosses UTC midnight reads the carrier day, not the UTC day |
| clock range | `\u2066۰۸:۰۰–۲۰:۰۰\u2069` | Persian digits, stored start/end order preserved, isolated LTR inside the RTL line |
| zone | `به وقت تهران`, printed **once** per cell | derived from the existing accepted zone id; the raw `Asia/Tehran` is no longer repeated beside each instant |
| missing | `مشخص نشده` (handoff) / `—` (customer, deadlines) | unchanged existing markers; nothing invented |
| invalid date / invalid zone / missing plan zone | `تاریخ نامعتبر` / `منطقهٔ زمانی نامعتبر` / `منطقهٔ زمانی مشخص نشده` | a truthful concise marker instead of a raw ISO spill or a browser-zone fallback |

A zone is validated before use (`Intl.DateTimeFormat` construction), so a refused id yields the named marker rather than
silently rendering in the machine's zone. No `Date` is constructed from a date-only string without the noon anchor.

## 3 What deliberately did **not** change

- `plannedHandoffDate`, `customerReceivingDate`, `dispatchDeadlineUtc` and the `dispatchBucket` accessors still sort and
  filter on the **ISO wire values**; only the visible cell/title text changed.
- No server, contract, endpoint, query, filter, calculation, "today" logic or authority was touched. No new date filter.
- Paragraph/copy additions were not made: unrelated labels and the TB-268 tab counts are untouched.
- Grid height (64px rows), readability rules, and the existing scoped horizontal scroll are unchanged.

## 4 Proof once (see `date-display-proof.md`)

- New focused suite `fulfillment-work-queue-persian-dates-269.test.ts` **7/7**: date-only Jalali, date-only stable under
  `Pacific/Kiritimati` versus `Pacific/Midway` child processes, instant crossing UTC midnight
  (`2026-11-15T21:30Z` → `۲۵ آبان ۱۴۰۵، ۰۱:۰۰`), range order + isolation, three instants with ONE zone label,
  missing values, invalid date / refused zone / missing zone — the last three assert that no raw ISO date, ISO time or
  IANA id survives.
- Existing affected queue suite `fulfillment-work-queue-260.test.ts` **6/6** (its one expectation that pinned the old raw
  string was updated to the new Persian shape).
- `npm run test:fulfillment` 6/6, `npx tsc --noEmit` 0, `npm run build` 0 → `BUILD_ID UmAnnlHYHCf-Sa5-dTMkm`.

## 5 Live read-only verification (served build)

Next production server restarted on port 3000 (PID 38800, `/fa` 200, `_buildManifest.js` 200, page chunk
`967-076c4bd18e2dcc76.js` 200 and carrying the new markers). Host 5088 untouched (`/health` 200, `/health/ready` 200).

Measured on the real queue, GET only, no fixture and no mutation:

| Viewport | Document overflow | Handoff cell | Operational deadlines cell | Customer cell |
| --- | --- | --- | --- | --- |
| 1440 | 0 (`sw 1425` = `cw 1425`) | `۲۴ آبان ۱۴۰۵ · ⁦۰۸:۰۰–۲۰:۰۰⁩ · به وقت تهران` | `آماده‌سازی تا ۲۴ آبان ۱۴۰۵، ۱۳:۰۰ · خروج تا ۲۴ آبان ۱۴۰۵، ۱۸:۴۰ · تحویل به حامل تا ۲۴ آبان ۱۴۰۵، ۲۰:۰۰ · به وقت تهران` | `۲۶ آبان ۱۴۰۵ · ⁦۰۹:۰۰-۱۰:۰۰⁩` |
| 390 | 0 (`sw 390` = `cw 390`) | same Persian string, grid keeps its own scoped scroll (`2882 > 340`) | same Persian string | — |

Rows whose accepted plan carries no date still read `مشخص نشده`; no row leaked a raw `YYYY-MM-DD` or a raw IANA id.
The user's own route `/admin/fulfillments` renders the same Persian handoff string. Cell text equals its `title`.

## 6 Known limits

- No user visual acceptance is claimed; the review URL is in `user-review.md`.
- The design-system grid may drop a column out of the DOM when it is scrolled outside the virtualized viewport, so the
  live check scrolled the grid to read each cell; this is pre-existing grid behaviour, not a change of this task.
- The expected base SHA `d4fb3b8e3505a445b123efc8d63badea52df1172` from the task envelope is **not present** in this
  clone (`git cat-file` fails); work started from the actual local `main` head `f62bd7564d5394dc8e36df4193b065d17be38682`,
  which already contains TB-268. Declared, not worked around.
- The queue's `createdAt`/`updatedAt` columns keep using `formatFulfillmentDate` (already `fa-IR`); they were not part of
  the reported defect and are unchanged.

STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.
