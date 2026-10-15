# SHIPPING-FULL-OPERATIONS-072 — Bounded Existing Admin Weekly Schedule Editor

Task: `TB-SHIPPING-FULL-OPERATIONS-072` (task 2 of at most 10 in program `TOOBA-SHIPPING-FULL-OPERATIONS`)
Parent-Task: `TB-SHIPPING-FULL-OPERATIONS-071`
Mode: `BOUNDED_EXISTING_ADMIN_WEEKLY_SCHEDULE_EDITOR`
Expected-Base-SHA: `5ec3a37e1dbdfab723d093a3ec6e9c4edf18975c`

## What this slice delivers

A discoverable, actually usable Persian/RTL **weekly operating-schedule editor** inside the existing
`/admin/shipping-services` management screen. It is one focused dialog opened from the existing service
row (action «برنامه کاری ارسال», `Clock` icon, next to «نرخ و پوشش»). No new route, no new page, no
giant combined form.

The editor is bound to the 071 endpoints already shipped, with no fake data and no in-memory patching:

```text
GET   /v1/admin/shipping-services/{serviceCode}/operating-schedules
PUT   /v1/admin/shipping-services/{serviceCode}/operating-schedules
PATCH /v1/admin/shipping-services/{serviceCode}/operating-schedules/{scheduleId}/active
```

Every write is followed by a real re-read. Authorization reuses the existing admin conventions; a
`401`/`403` renders a visible denied state and never gets bypassed.

## Files

| Path | Role |
| --- | --- |
| `src/frontend/app/admin/shipping-services/shipping-operating-schedule-model.ts` | Pure model: ISO weekday identity, strict 24-hour time parsing, `HH:mm:ss` wire serialization, local validation, option identity, disclosure strings, stable-code → Persian error mapping. No React, no network. |
| `src/frontend/app/admin/shipping-services/shipping-operating-schedule-api.ts` | Admin client for the three 071 routes. Unwraps the `validation.failed` envelope so the actionable child code (`shipping.validation.schedule_window_overlap`, …) is surfaced instead of the envelope. |
| `src/frontend/app/admin/shipping-services/shipping-operating-schedule-dialog.tsx` | The Persian/RTL dialog: saved-schedule list by option + active status + timezone, identity selector, timezone input, weekday rows, add/remove, save, active toggle. |
| `src/frontend/app/admin/shipping-services-screen.tsx` | One added row action (`schedule`) that opens the dialog with the row's `code`/`name`. Nothing else changed. |
| `src/frontend/app/admin/shipping-services/shipping-operating-schedule-model.test.ts` | 15 pure model tests. |
| `src/frontend/app/admin/shipping-services/shipping-operating-schedule-api.test.ts` | 8 API round-trip tests against an in-memory fixture that implements the real 071 wire contract (no Development database write). |

No backend production source, migration, contract or permission was changed.

## Editor semantics

| Aspect | Decision |
| --- | --- |
| Weekday identity | ISO 1..7 (1 = دوشنبه … 7 = یکشنبه). Display order starts from شنبه (Iranian working week) but the numeric ISO identity is never remapped. |
| Time fields | Unambiguous 24-hour local `HH:mm`. `25:99` or `13:5` is refused, never normalized into a guessed value. |
| Wire time | `HH:mm:ss`, verified against the backend `TimeOnly` contract; `13:00` ⇄ `13:00:00` round-trips. |
| Range | `end > start`. Reversed, zero-length and overnight windows are all refused with the same range meaning; overnight is **not** supported (071 decision). |
| Overlap | Half-open `[start, end)`. Adjacent windows (`13:00–14:00`, `14:00–15:00`) are accepted; genuinely overlapping windows on the same day are refused locally before any request is sent. |
| Capacity | Optional positive integer ≤ 1,000,000. Labelled exactly `ظرفیت تنظیم‌شده (هنوز اعمال نمی‌شود)` — never «باقی‌مانده» and never presented as an enforced reservation. |
| Timezone | Explicit input defaulting to `Asia/Tehran` with a datalist of common ids. Never inferred silently from the browser: a bare `13:00` without a timezone is meaningless. |
| Option identity | Selectable options come from the existing Fulfillment service tree (`loadShippingMethodTree`). Service-level schedule is offered separately with an explicit fallback explanation. Unknown/foreign options are refused locally before the request. |
| Empty schedule | Shown as a real empty state with the explicit disclosure that it means *no explicit hours configured* and does **not** block the current Checkout. |
| Save safety | Save button disabled while a request is pending; failures re-read from the server instead of leaving the form on the failed desire; no fabricated success on network failure. |

## Deliberate non-goals

- No capacity enforcement, counting, reservation or locking. The number stays configuration-only.
- No customer delivery promise, slot, order or checkout change. Operating windows describe **carrier
  pickup/dispatch availability** and the dialog says so in-place.
- No new permission, no `AllowAnonymous`, no `Results.Json`, no invented service option.
- No backend rebuild, no migration, no second database, no parallel instance.

## Live proof (existing Development)

Verified in the running browser against the existing Development frontend `http://127.0.0.1:3000` and
host `http://127.0.0.1:5088` (tenant `store-alpha`, DB `tooba_alpha`):

- read/display of the saved 071 `tipax/standard` Sunday fixture (`13:00–14:00` and `16:30–18:00`, capacity 25);
- edit/update through a real `PUT` (capacity 25 → 30) with read-after-write, then restoration to 25;
- active toggle through the real `PATCH …/{scheduleId}/active` (off → `غیرفعال`, on → `فعال`), persisted and refreshed;
- refusals: same-day overlap, adjacent accepted, invalid time, invalid capacity — all with field-adjacent Persian messages and no request for the locally invalid shape;
- option isolation: `tipax/express` is empty and untouched while `standard` keeps its row; `post`, `snapp_courier`, `store_courier`, `in_person` all return `schedules: []`;
- authorization: without the actor header the dialog shows the denied message; `GET` returns `401 admin.actor.missing`.

Full interaction transcript, captured wire bodies and screenshots: `docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-072/`.

## Verification actually run

- `shipping-operating-schedule-model.test.ts` + `shipping-operating-schedule-api.test.ts`: **23/23 pass**.
- Existing shipping frontend tests (form model, roundtrip, rates/coverage model, destination picker): **36/36 pass**.
- `npx tsc --noEmit`: **0 errors**.
- Frontend architecture guards: 7/8 pass. The single failure is the pre-existing shrink-only size
  baseline (`category-admin-screen.tsx`, `admin-landing-page-composer.tsx`, `storefront-landing-blocks.tsx`,
  `storefront-shipping.tsx`, `shared-composition-renderer.tsx`) — none of those files is touched by this
  task and all five are unchanged on disk; the new files are well inside the limit.

## Remaining / not done here

- Capacity is still configuration-only; enforcement is a later task.
- No schedule consumer exists yet, so an empty or inactive schedule cannot block today's Checkout.
