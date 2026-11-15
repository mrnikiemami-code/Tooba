# Date display proof — TB-SHIPPING-QUEUE-PERSIAN-DATES-AND-CONSISTENT-TIME-DISPLAY-269

All runs are read-only. No fixture, no write, no mutation, no provider call, no migration.

## A Focused formatter suite (once)

```text
cd src/frontend
node --experimental-strip-types --test app/fulfillment/fulfillment-work-queue-persian-dates-269.test.ts app/fulfillment/fulfillment-work-queue-260.test.ts
ℹ tests 13
ℹ pass 13
ℹ fail 0
```

New suite composition (7 cases):

| Case | Pinned fact |
| --- | --- |
| Persian calendar, not Gregorian | `2026-11-15` → `۲۴ آبان ۱۴۰۵ · به وقت تهران`; customer `2026-11-15` + `09:00-10:00` → `۲۴ آبان ۱۴۰۵ · ⁦۰۹:۰۰-۱۰:۰۰⁩` |
| date-only stable under opposite client zones | the same cell renders byte-identical under `TZ=Pacific/Kiritimati` and `TZ=Pacific/Midway` (separate Node processes, `TZ` read at start) |
| instant crossing UTC midnight | `2026-11-15T21:30:00Z` in `Asia/Tehran` → `۲۵ آبان ۱۴۰۵، ۰۱:۰۰` (carrier day, not the UTC day) |
| range order, isolated LTR | cell contains `\u2066۰۸:۰۰–۲۰:۰۰\u2069`; `۰۸:۰۰` precedes `۲۰:۰۰` |
| three instants, ONE zone label | cell starts `آماده‌سازی تا ۲۳ آبان ۱۴۰۵، ۱۳:۰۰`, ends `تحویل به حامل تا ۲۳ آبان ۱۴۰۵، ۱۶:۳۰ · به وقت تهران`, and `به وقت تهران` occurs exactly once |
| missing promise | `مشخص نشده` / `—` / `—` — nothing invented |
| invalid input or refused zone | `تاریخ نامعتبر`, `منطقهٔ زمانی نامعتبر`, `منطقهٔ زمانی مشخص نشده`; asserted with `/\d{4}-\d{2}-\d{2}/`, `/\d{2}:\d{2}:\d{2}/` and `/[A-Za-z]+\/[A-Za-z_]+/` **absent** from the operator string |

The retained queue contract suite pins the same repair from the other direction: the expectation that used to read
`"2026-11-10 · 10:00–13:00 (Asia/Tehran)"` now reads `"۱۹ آبان ۱۴۰۵ · ⁦۱۰:۰۰–۱۳:۰۰⁩ · به وقت تهران"`.

## B Existing affected suite + toolchain (once each)

```text
npm run test:fulfillment     ℹ tests 6  ℹ pass 6  ℹ fail 0
npx tsc --noEmit             exit 0
npm run build                exit 0  →  BUILD_ID UmAnnlHYHCf-Sa5-dTMkm
```

## C Served build

```text
GET http://127.0.0.1:3000/fa                                    200
GET http://127.0.0.1:3000/_next/static/UmAnnlHYHCf-Sa5-dTMkm/_buildManifest.js   200
GET http://127.0.0.1:3000/_next/static/chunks/app/admin/fulfillments/page-d1ac35abce16c2c2.js   200
GET http://127.0.0.1:3000/_next/static/chunks/967-076c4bd18e2dcc76.js            200   carries «منطقهٔ زمانی نامعتبر»
GET http://127.0.0.1:5088/health          200      (Host left untouched)
GET http://127.0.0.1:5088/health/ready    200
```

Next production server PID 38800 on port 3000 (previous process stopped and replaced so the new build is the served one).

## D Live read-only DOM measurement (`/fa/admin/fulfillments` and `/admin/fulfillments`)

GET only. The grid was scrolled horizontally to bring each virtualized cell into the DOM — no filter, no sort request, no
edit, no order action.

```text
1440px : documentElement.scrollWidth 1425 == clientWidth 1425   → document overflow 0
         handoff  cell == title == «۲۴ آبان ۱۴۰۵ · ⁦۰۸:۰۰–۲۰:۰۰⁩ · به وقت تهران»
         deadlines cell == title == «آماده‌سازی تا ۲۴ آبان ۱۴۰۵، ۱۳:۰۰ · خروج تا ۲۴ آبان ۱۴۰۵، ۱۸:۴۰ ·
                                     تحویل به حامل تا ۲۴ آبان ۱۴۰۵، ۲۰:۰۰ · به وقت تهران»
         customer cell == title == «۲۶ آبان ۱۴۰۵ · ⁦۰۹:۰۰-۱۰:۰۰⁩»
         rows without an accepted date: «مشخص نشده»  (no raw value, no invented date)

 390px : documentElement.scrollWidth 390 == clientWidth 390     → document overflow 0
         same three Persian strings; grid viewport 2882 > 340 so the queue keeps its own scoped horizontal scroll

/admin/fulfillments (user route): handoff cell «۲۴ آبان ۱۴۰۵ · ⁦۰۸:۰۰–۲۰:۰۰⁩ · به وقت تهران»
```

Wire values are untouched: the column accessors still read `plannedHandoffDate`, `customerReceivingDate` and
`preparationStartUtc` as ISO facts, so server sorting/filtering and raw exports keep their existing behaviour.
