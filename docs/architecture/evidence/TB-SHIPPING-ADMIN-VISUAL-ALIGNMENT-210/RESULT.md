# TB210 — bounded visual alignment repair (user screenshot rejection)

Task: TB-SHIPPING-ADMIN-VISUAL-ALIGNMENT-210, base `3f14b3beebddc5265fdd23b078bfd6ba38ff92b7`, channel `tooba-main`, worker `tooba-worker-01`.
Mode: bounded user-screenshot grid/tabs/week alignment repair. No backend, DTO, API, session/save policy, migration, seed or dependency change.

## User-reported defects being repaired

- Main tabs share an outer width, but General inner `max-w-4xl` formed a second content axis.
- Type sat in a floating `max-w-sm` box, so type/name/settings/week edges disagreed.
- Language chips changed geometry between selected/unselected (selected had no border).
- Week was `md:grid-cols-2` day cards with inner `justify-between`, so labels sat on one side and
  toggles/inputs on the other and no column lined up across the seven days.

## Source changed (existing feature only)

- `ui/authoring-general-tab.tsx`: removed the General-only `max-w-4xl`; the panel now uses the page's
  single content width. The type control moved into a card with the same padding/edges as the
  name/description card, so the select's inline edge matches the name input exactly. Language chips
  now share one box model (`border` + `min-h-8` in BOTH states, only colour changes). Settings cards
  are `items-stretch` + `h-full` so legends and options share baselines.
- `ui/authoring-week-editor.tsx`: replaced the two-column day cards with ONE seven-row grid. A header
  row and every day row use the same column template `minmax(6rem,1fr) 8.5rem 7.5rem 7.5rem`
  (`[day][status][from][to]`), applied as an explicit inline template so it is independent of any
  CSS-bundle regeneration. Below 640px the template becomes `minmax(0,1fr) auto`, i.e. day+status on
  line one and full-width from/to on line two. Weekday ids/order, remembered closed-day hours,
  validation, `TimeText` handling, timezone value and one-root-week authority are untouched.
- `messages/authoring-messages.ts`: added the four column headers (`weekDayColumn`, `weekStatusColumn`,
  `weekFromColumn`, `weekToColumn`) in `fa` and `en`. Database-driven content languages were not
  hardcoded.

`data-testid`s, session/model/save policy and view-mode read-only behaviour from 209/209-R1/R2 are unchanged.

## Before → after alignment decision

| element | before | after |
| --- | --- | --- |
| General panel width | page width minus `max-w-4xl` (narrower than Regions) | full page content width `1057px`, identical to Regions |
| type vs name inline edge | type in a floating `max-w-sm` box | same card padding: select `x=73 w=1023`, name input `x=73 w=1023` |
| language chips | selected 30/32px without border | both states `32px` tall with `1px` border, same top |
| week layout | 2 independent day cards | 7 rows + header on one shared column template |
| week columns across 7 days | labels/toggles/inputs drifted | identical for all seven days (0px spread) |

## Actual DOM bounding boxes — 1440×900, EDIT, same sidebar state

Main panel: screen `x=56 w=1057`; General tab `w=1057`; Regions tab `w=1057`.
Main tab bar: «اطلاعات روش» height `40px`, «مناطق و هزینه‌ها» height `40px`, both `border-bottom: 2px`.

General:
- type card `x=56 w=1057`; name card `x=56 w=1057`
- type select `x=73 w=1023 h=44`; name input `x=73 w=1023 h=44`
- language chips: `h=32`, `border-top=1px`, `top=454` for all three configured languages
- settings cards: `x=56 w=523` and `x=591 w=523` (equal halves of the same axis)

Week (all seven rows identical, therefore 0px difference):
- day label `x=481 w=607`; status checkbox `x=457 w=16`; start input `x=209 w=120`; end input `x=81 w=120`
- header cells: day `x=481 w=607`, status `x=337 w=136`, from `x=209 w=120`, to `x=81 w=120`

## Mobile — 390×844

- `document.scrollWidth = 390 = innerWidth` → no horizontal overflow on General or Regions.
- week rows resolve to `minmax(0px,1fr) auto`: day + working control on line one, from/to full-width
  on line two, consistent across all seven days (`week-edit-390x844.png`).

## Screenshots actually inspected

- `general-edit-1440x900.png`
- `regions-edit-1440x900.png`
- `week-edit-1440x900.png` (all seven rows, working controls and from/to columns)
- `week-edit-390x844.png`

## Data / revision integrity

No Apply, no HTTP write, no POST/PUT, no seed/migration, no backend build or restart, no domain mutation.
Inspection used the real populated draft `01a16eeb-70b3-7000-87c2-564c82835496` on FE3000/Host5088.
After exiting edit without saving, view readback still shows freights `120000.00` / packaging `5000.00`,
so stored values and revision are unchanged. The user's own browser and unsaved work were not reloaded;
task inspection used an isolated tab.

## Validation and timing (honest)

- `npm run typecheck` clean (frontend).
- No full test suite, no source-regex CSS tests, no new logic tests (no behaviour was changed; the week
  edit only restructured markup/columns).
- Claimed ≈21:40Z, implementation stop point was minute 13 but the alignment/measurement loop ran to
  ≈minute 25 and the result was finished at ≈minute 32 — i.e. the hard 20-minute timebox was exceeded.
  Recorded as a real process deviation, not a PASS.

## Claim limits

Presentation/alignment only. Functional 209 behaviour and the accepted 209-R1/R2 corrections are
preserved, not re-proved here. No save, activation, readiness, quote, deployment or production
acceptance. **User Visual ACCEPT remains PENDING** — the worker does not grade its own UI. Await
Architect review.
