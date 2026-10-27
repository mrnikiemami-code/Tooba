# TB-227 — destination modal: in-popup search + stable geometry

Base `2d744fc67df34dc9427b2f0db56e2946c1344784` (fast-forward from `9734e77ac`, unrelated backend
line-ending drifts left unstaged). Scope: the destination modal of `/admin/shipping-services/authoring/{id}`.
No money/rate/range/week/publication/backend/geography change; no Apply, no PUT, no migration, no restart.

## A — the defect was measured, not guessed

RED on the unchanged source (`before/measure-before.json`, real API traffic, task-owned Chrome 155):

| branch step | dialog `top` frames | `top` travel | height swing |
| --- | --- | --- | --- |
| open (1 country) | 299 | — | h = 303 |
| pick country → 12 provinces | 299 → 271 → 257 | **42 px** | 303 → 387 → 359 (**84 px**) |
| pick province → 5 cities | 271 → 257 | 14 px | 28 px |

Cause (measured, not inferred): the modal body CONDITIONALLY inserted `path`, `branchNote`, deepest-level
hint, added confirmation, loading and failure rows. A native `<dialog>` is vertically centred by the user
agent, so every height change moved `top` by half the delta, and the height itself swung while a read was
in flight (`loading` row + hint). Page scroll stayed 0 and the internal scroller stayed at 0, so neither of
those was the cause.

## B — one stable reservation + a popup that never resizes the dialog

- The four fixed fields are always rendered with their existing identities
  (`shipping-authoring-destination-country|area|city|service-area`); nothing is inserted or removed per level.
- Every transient line (path, loading, branch note, deepest hint, added confirmation, read failure) now
  shares ONE reserved status slot (`min-height: 3.25rem`), filled by a deterministic priority.
- Each field is a searchable combobox whose search input is the FIRST element INSIDE the opened dropdown
  (no external search box). The popup is portalled into the open `<dialog>` with `position: fixed`
  coordinates measured from the trigger: a modal dialog makes everything outside it inert, so a
  `document.body` popup could be seen but neither focused nor clicked — verified by a failed run before the
  correction. Because it is out of flow it cannot change the dialog's height, it is not clipped by the
  dialog's `overflow: hidden`, it flips above the field when there is no room below, and it always scrolls
  inside the viewport.
- Search covers ONLY the rows the current verified branch returned. The Geography routes return the whole
  branch array (no pagination), and the popup prints `{shown} / {total}` so a filtered view is never
  presented as a complete search.

Slot height was sized from the real localized strings at each viewport (`measure-slot.mjs`): all nine
status texts keep the slot at 52 px at 1440, 390 and 360 — with one measured residual: `path + «برای این
کشور استان ثبت نشده است…»` needs 66 px at ≤390, i.e. one +14 px shift when a branch settles EMPTY. That is
recorded, not hidden, and it is not the loading shake.

## C — mounted proof (real traffic, task-owned Chrome, no user tab touched)

`measure-after.json`, 2230 sampled frames at 1440, 125 at 390, 124 at 360:

| viewport | dialog `top` | dialog height | travel | page scroll | inner scroll |
| --- | --- | --- | --- | --- | --- |
| 1440×900 | 273 (single value) | 355 (single value) | 0 | 0 | 0 |
| 390×844 | 171 | 503 | 0 | 0 | 0 |
| 360×780 | 139 | 503 | 0 | 0 | 0 |

Exercised with real input: country popup open (search inside, focused, `1 از 1`), query `zz` →
`0 از 1` + honest no-result text, ArrowDown+Enter pick, province popup with the 12 real provinces,
Persian filter `ته` → `1 از 12` → Enter picks `تهران`, city popup (6 real cities), city pick, service-area
popup for `مرند` → `0 از 0` + «برای این شاخه مقداری ثبت نشده است.», Escape inside an open dropdown closed
ONLY the dropdown (panel 128 px → gone, dialog still open), Escape with no dropdown closed the dialog,
country clear emptied AND disabled all three descendants immediately, page scroll 0 and
`scrollWidth == clientWidth` at 390/360. Screenshots: `227-1440-country-popup.png`,
`227-1440-city-popup.png`, `227-1440-service-area-empty.png`, `227-1440-modal-final.png`,
`227-390-popup.png`, `227-360-popup.png`.

## D — tests

ONE clean `tsc --noEmit` after the final source. New `destination-search-227.test.ts` 7/7 (branch-only
filtering, coverage honesty, inactive-not-selectable, wrap/Home/End bounds, Enter clamping, key set) and
62/62 in the affected destination/rate classes (`204-r1`, `204-r2`, `225`, `225-r1`, `225-r2`) — 69/69
total. No whole-suite, backend or solution replay.

## Limits (honest)

- This installation's only country has no fourth stage under its cities, so **service-area options are not
  live-provable**: the branch was proven honest-empty, and the selectable path is test-proven only.
- The +14 px shift for the longest no-provinces note at ≤390 remains (recorded above); all other measured
  states are pixel-stable.
- The unrendered `authoring-destination-picker.tsx` (dead since TB-225) was left untouched.
- No user Visual ACCEPT is claimed. No Storefront/consumer wiring: the published/selected revision is still
  not connected to checkout. Compact per-order-group evidence decision remains mandatory and unimplemented.

Actual elapsed ≈15 min (HARD25 / implementation 15 respected). Deviations recorded: three implementation
corrections during the mounted gate (portal container had to move inside the `<dialog>`, the search focus
had to move to the post-placement commit, and the reserved height could not use a Tailwind arbitrary
utility because this build did not emit it — inline `min-height` used instead).
