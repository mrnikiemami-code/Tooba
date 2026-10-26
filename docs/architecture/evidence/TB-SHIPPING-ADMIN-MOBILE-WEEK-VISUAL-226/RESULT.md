# 226 — the mobile week is one Closed badge, one coherent hours line, and native-viewport proof

Task receipt: `TASK.md`. Reviewed source: `afbdadc7beb363393eba8aa6c988beacf827db24`; merged
`94ea7ed2e` (docs-only Architect commit) before starting. Parent: 225-R2 (money integrity accepted — NOT
reopened here). Scope: week PRESENTATION only.

## The defect in the source was exactly what the screenshots showed

`ui/authoring-week-editor.tsx` rendered, for a closed day in VIEW mode, the status column text
(`weekClosed`) **and** a second span across the bound columns with the same word. Independent of the PNG
canvas problem, that is a real duplicate. The working day in the narrow view put its two bounds in a
two-column block whose visible labels sat above the values, so the hours read as two far-apart numbers.

## What changed (presentation only)

- Narrow **VIEW**, closed day: no second element at all — the row is `weekday` + ONE Closed badge (the
  status column). One line, no dash, no bounds, row height 28px (measured).
- Narrow **VIEW**, working day: first line `weekday` + ONE working status; second line ONE inline range
  `از 09:00 – تا 17:00` / `From 09:00 – To 17:00`, each label in the same inline flow immediately next to
  its own value (measured RTL: `از` right edge 337 with `09:00` at 292–323; `تا` 266–274 with `17:00` at
  230–260). One extra line only, 47px, no giant whitespace.
- Narrow **EDIT**: unchanged controls, made compact — `weekday` + the working checkbox on one line, the two
  equal accessible `type="time"` inputs on the second line for a working day (138px each at 390, 123px at
  360), and a closed day is a single compact line with its checkbox and NO extra Closed label.
  `onDayWorking` / `onDayTime` / the model guards and every existing test id are untouched.
- Desktop **VIEW**: the accepted four-column contract is unchanged (header `روز | وضعیت | از | تا`,
  measured day cell x=481 w=607 matching its own header cell, `وضعیت` x=337 w=136, `از` x=209 w=120,
  `تا` x=81 w=120; all seven rows identical). The one duplicate that also existed there is gone: a closed
  row now shows `تعطیل` ONLY in the status column and an honest `—` in the bound columns, which is what
  210-R1 already described as the truthful per-column view.
- Row padding tightened on narrow only (`py-1.5`→`py-1`, `gap-y-1`→`gap-y-0.5`); desktop keeps `sm:py-1`.
  No time model, validation, dirty/cancel, area/rate/destination modal or money policy touched.

## Proof (only what changed)

One `tsc --noEmit` clean (0 errors). No 188/76/42/full frontend/backend/solution sweep, no new CSS/source
regex tests (none were invented).

Mounted in a NEW task-owned Chrome: installed Chrome 155.0.8059.85, `--headless=new`,
`--remote-debugging-port=9334`, `--user-data-dir=%TEMP%\tb226-chrome-profile` (created fresh, port verified
free first), every page in a target created by this task (`Target.createTarget`). Real FE :3000 with real
`/v1` traffic, no interception, no restart, no write, no Apply/publication/catalog call. Real draft
`01a16eeb-70b3-7000-87c2-564c82835496`, stored week read as-is (Saturday…Friday, only Monday 09:00–17:00;
no invented working days).

Native viewport PNGs (device metrics 390×844 / 360×780, `Page.captureScreenshot` with
`captureBeyondViewport:false`; dimensions read from the PNG header and inspected by eye):

| file | bytes | size | receipt |
| --- | --- | --- | --- |
| `screens/226-390-view.png` | 29 195 | 390×844 | RTL/fa, `scrollWidth 390`, one `تعطیل` per closed row, range `از 09:00 – تا 17:00` |
| `screens/226-390-edit.png` | 30 007 | 390×844 | RTL/fa, 7 checkboxes, exactly 2 time inputs (day 1) |
| `screens/226-390-edit-day1-closed.png` | 29 781 | 390×844 | local toggle: day 1 closed → compact one line, 0 time inputs, 0 Closed labels |
| `screens/226-360-view.png` | 24 862 | 360×780 | narrowest, same content, `overflowX false` |
| `screens/226-360-edit.png` | 26 886 | 360×780 | inputs 123px each, no clipping |
| `screens/226-390-view-en.png` | 31 801 | 390×844 | EN/LTR via the real `/en` prefix, `lang=en dir=ltr` |
| `screens/226-390-edit-en.png` | 33 699 | 390×844 | EN/LTR edit, same compact rows |
| `screens/226-1440-view.png` | 53 491 | 1440×900 | desktop columns unchanged, one `تعطیل` + `—` |

DOM receipts for every row (height, exact text, closed-count, time-input count, checkbox presence) are in
`mount-receipts.json`. Toggle round-trip on the REAL working day: closed → 0 time inputs, height 28, and
reopened → `09:00`/`17:00` restored with Apply still disabled; no Apply was ever pressed.

## Honest limits

- The `?locale=en` query on the admin route does NOT switch the locale (that switch is the `/en` URL prefix
  or the `tooba_locale` cookie); the first EN attempt therefore had to be redone through `/en`. While doing
  that the same `tooba_locale` cookie was written and the 390 FA files were briefly overwritten; they were
  cleared and re-captured, and the shipped FA files are the re-captured RTL bytes listed above.
- The edit-mode row label next to the checkbox is the pre-existing control label (`روز کاری` / `Working
  day`); an unchecked box means the day is closed. It is intentionally not replaced with a second state
  word, per the task's "no redundant Closed label".
- The dashed `—` on desktop closed rows is new presentation text (no localization key), chosen to match the
  truthful per-column view 210-R1 described; the narrow view has no dash.
- NOT claimed: user Visual ACCEPT, the desktop/mobile form as a whole, Storefront wiring, seller-admin entry,
  or the Tehran service-area gap (unchanged, out of scope).
- A pre-existing typecheck break in the ACCEPTED 225-R2 evidence base was repaired as a bare typing
  annotation with no behaviour change: `destination-rate-modals-225-r2.test.ts` used `this` in a default
  parameter (`key(character, caret = this.text.length)`), which `tsc --noEmit` rejected (TS2683) — the
  `npx tsc` run in that task happened BEFORE the test file was added, so the acceptance did not include a
  clean typecheck with it. It is now `caret?: number` + `caret ?? this.text.length`; the 8 facts are
  unchanged and no money/model code was touched. Reported here rather than silently fixed.
- Actual elapsed ≈ 10 min (20:33 → 20:43 local) against the HARD15 box — recorded; the box was respected in
  wall-clock terms but this is still NOT a process PASS.
- Active task: NONE. Next: Architect review. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
