# TB210-R1 — mobile paired labelled week bounds + truthful view columns

Task: TB-SHIPPING-ADMIN-VISUAL-ALIGNMENT-210-R1, parent TB-SHIPPING-ADMIN-VISUAL-ALIGNMENT-210,
base `705921b85a9675dd6af994213ef908f54a4f237d`, channel `tooba-main`, worker `tooba-worker-01`.

## What the parent review left unfinished (and is now fixed)

1. **Mobile week was three lines per day** — day/status, then start full-width, then end full-width.
2. **No visible From/To label** on mobile because the desktop header is hidden; inputs also had no
   weekday-specific accessible name.
3. **View mode put the whole formatted range in the Status cell** while From/To headers stayed empty.

## Source changed (existing feature only)

`ui/authoring-week-editor.tsx`:
- Each day row now renders ONE paired bounds block. Desktop: `display: contents`, so the two bounds are
  ordinary grid items in the existing columns 3 and 4 — the accepted 210 geometry is untouched.
  Narrow: `grid-column: 1 / -1` with `minmax(0,1fr) minmax(0,1fr)` and an 8px gap, i.e. exactly one
  second line with two equal halves.
- Every bound is a `<label>` holding a `sr-only` weekday name plus the localized From/To word, then the
  input (edit) or the stored time text (view). On narrow screens the From/To word is visible; on desktop
  it is `sr-only` and the header supplies the visible label — so the accessible name is
  "‹weekday› From" / "‹weekday› To" in both modes and the desktop does not repeat it twice visually.
- View mode is truthful by column: Status shows «روز کاری»/«تعطیل» (working/closed) as text, From shows
  the stored start, To shows the stored end, and a closed day shows an honest «—» in both bounds.
  The formatted range is no longer duplicated into the Status cell.
- Narrow view keeps the same two-line rhythm (day+status, then paired From/To), so no third line.
- Unchanged: weekday ids/order, remembered closed-day hours, `TimeText`/fraction handling,
  `onDayWorking`/`onDayTime` callbacks, timezone value, one-root-week authority, all `data-testid`s.
  `authoringFormat` import removed (no longer used in this file). No message/DTO/model change.

## Measured (actual DOM boxes, same tab, actual page)

Desktop 1440×900 (edit), FA — retained 210 columns exactly, header == rows:

| cell | x | w |
| --- | --- | --- |
| day label | 481 | 607 |
| status control | 457 | 16 |
| start input | 209 | 120 |
| end input | 81 | 120 |

All seven rows identical (`h=44`), header cells `481/607`, `337/136`, `209/120`, `81/120`.

Mobile 390×844, FA, edit — two lines per day, all seven rows identical:

| row | height | start (x,w,top) | end (x,w,top) |
| --- | --- | --- | --- |
| 1..7 | 86 | 199,138, same top | 53,138, same top |

Start and end share the same `top` on every row and have equal widths (138 each) — one paired line.
`document.scrollWidth == innerWidth == 390` → no horizontal overflow.

Mobile 390×844, view (FA): `week inputs = 0`, row texts
`شنبه / روز کاری / از <from> / تا <to>` and closed rows show `تعطیل` + `—` in both bound cells.
Header row present only on desktop (`hidden` below 640px) and reads Day/Status/From/To.

Accessible names (EN, edit): `"Monday From"`, `"Monday To"`, `"Tuesday From"`, `"Tuesday To"`.

EN/LTR bounded check via the canonical locale cookie `tooba_locale=en` (dir `ltr`, lang `en`):
desktop 1440×900 and mobile 390×844 week captured; header `Day/Status/From/To`, rows
`Monday / Working day / From 09:00 / To 17:00`, `Tuesday / Closed / From — / To —`.
No content-language switch was used; the cookie was restored to `fa` afterwards.

## Screenshots actually inspected (real bytes, cropped to the emulated viewport)

- `week-view-390x844.png` — FA view, week scrolled into view inside a true 390×844 viewport.
- `week-edit-390x844.png` — FA edit, all seven rows with paired From/To labels.
- `week-edit-1440x900.png` — desktop edit, retained seven-column geometry.
- `week-en-view-1440x900.png` — EN/LTR desktop view.
- `week-en-view-390x844.png` — EN/LTR mobile view.

The 210 evidence defect (`week-edit-390x844.png` showed only the top of General) is not repeated: every
390×844 capture above is a 390×844 PNG cropped from the rendered viewport region after explicitly
scrolling the week element into view, and the week text is visible in the image.

## Data / revision integrity

No Apply, no POST/PUT/PATCH/DELETE, no seed/migration, no backend build or restart. Edit was opened and
left through "Leave edit mode" without a save; afterwards the page is back in view, the week panel has
0 inputs, the Regions panel has 0 inputs and readback still shows freight `120000.00` and packaging
`5000.00`. The user's own browser tab and unsaved work were not reloaded (isolated inspection tab).

## Validation and timing (honest)

- `npm run typecheck` clean (run once after the source change, plus once after removing an unused import).
- No unit/source-regex tests, no full suite, no save replay.
- Claimed 21:52:55Z; source edit + typecheck done by 21:58Z; documentation/commit began 21:59Z, i.e.
  inside the 12-minute box with the minute-7 implementation stop point respected. Actual elapsed for the
  whole result was about 8–10 minutes (finish/commit/push), reported from the real clock rather than
  estimated.

## Claim limits

Presentation only. No save/activation/readiness/quote/deployment/production acceptance.
**User Visual ACCEPT remains PENDING** — the worker does not grade its own UI. STOP_FOR_ARCHITECT.
