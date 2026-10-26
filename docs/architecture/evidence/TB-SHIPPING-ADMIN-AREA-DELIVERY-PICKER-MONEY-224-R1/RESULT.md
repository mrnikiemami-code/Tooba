# TB-SHIPPING-ADMIN-AREA-DELIVERY-PICKER-MONEY-224-R1 — RESULT

Task: `TASK.md` in this folder (verbatim, issued by the Architect).
Parent: TB-SHIPPING-ADMIN-AREA-DELIVERY-PICKER-MONEY-224 (`../TB-SHIPPING-ADMIN-AREA-DELIVERY-PICKER-MONEY-224`).
Mode: BOUNDED_INVALID_RANGE_AND_MONEY_INTEGRITY_REPAIR. Reviewed source: `d7e3f4e3d72acb588c2d9d0de7f24495d8b90053`;
this work starts from `63b0a3c3c3b588769e37b31d0e9200b8a3dbcba6` (the Architect's 224-R1 issue commit) with a
clean fast-forward, no reset/stash/rebase/force-push. The four pre-existing backend line-ending drifts stayed
untracked/unstaged (verified: `git diff --ignore-cr-at-eol --stat -- src/backend` is empty).

## A — the TYPED delivery range is the form state that blocks Apply

`ui/use-area-range-edits.ts` (ref-based text with an early return) is DELETED. The operator's shared text is now
first-class form state:

- `model/editor-area-range.ts`: `AreaRangeDrafts` (keyed by the SAME stable group key as the rows),
  `areaRangeEdit` (keeps the typed TEXT verbatim; writes the pair to the rows of exactly that area only when it
  is lawful), `areaRangeDraftIssues` (per-area blocking reason, ignoring keys that no longer exist),
  `areaRangeDraftsDirty`, `createAreaRangeDrafts` (seeds every area from the CONFIRMED baseline rows).
- `model/authoring-intent.ts`: `buildAuthoringIntent(..., areaRanges)` folds the typed ranges into issues AND
  `localDirty`, so blank/partial/invalid text is real local work that blocks Apply.
- `ui/authoring-screen.tsx`: `areaRanges` is component state; ONE shared `applyLocalDrafts(...)` reseeds the
  form, groups, rates, area ranges and week from the same baseline on a fresh load and on a confirmed cancel
  (a rejected cancel keeps every draft, including the ranges).
- `ui/authoring-rates-editor.tsx`: `AreaDeliveryRange` is fully controlled (`value` + `issue`); a row-less area
  with staged text states `areaRangeRowsRequired` instead of silently persisting nothing.
- `ui/authoring-problem-view.tsx`: the issue summary lists the area-range blockers (`areaRangeIssueMessage`).
- `messages/authoring-messages.ts`: `areaRangeRowsRequired` in fa-IR and en-US.

HONEST BOUNDARY (unchanged by this repair): the DTO/capture format is untouched, so a lawful shared pair is
still serialized as the same days on every row of that area, and a row-less area persists nothing. A true
group-level persisted duration remains a schema change, NOT done here.

## B — an invalid money text can never become another amount

`model/money-text.ts` no longer "cleans" unsupported input:

- `readMoneyText(text)` returns `{ raw, valid }`: Persian/Arabic-Indic digits, the Arabic decimal separator,
  correctly formed 3-digit comma grouping and boundary whitespace are normalized; anything else (signs,
  letters, exponent, multiple/malformed separators, `12,34`) keeps its raw text with `valid: false`.
- `MoneyCaret { whole, fraction, inFraction }` + `caretAnchorAt`/`caretOffsetFor` replace the digit-count-only
  anchor, so the caret knows which side of the separator it is on and local digits are counted as digits.
- `interpretMoneyBackspace(text, caret)` (found DEFECTIVE during this task's mounted gate, see below): display
  grouping re-inserts the separator, so a browser Backspace that deleted only the comma left the same text and
  the same caret forever. A Backspace directly after a separator now removes the separator AND the digit it
  follows (the digit the operator is deleting), leaves the remaining digit run verbatim and anchors the caret
  at the deletion point; any other caret position stays a plain browser edit.
- `MoneyField` renders `aria-invalid` from `isCanonicalMoneyText` and hands the RAW token to the model; the
  row/amount validation (`amountInvalid`) blocks Apply. No Number/parseFloat round-trip, no scale forcing.

`area-range-money-224.test.ts` assertions that described the OLD lossy behaviour were corrected to the truthful
new contract (no assertion loosened: `sanitizeMoneyText("1,2,3")` now returns `"1,2,3"` and
`readMoneyText("1,2,3").valid === false`), `authoring-rates-week-203-r1.test.ts` now asserts the shared
`applyLocalDrafts` reseed (all five draft setters, including the area ranges), and `shipping-groups-204.test.ts`
follows the new `buildAuthoringIntent` signature.

## RED first (measured on the reviewed baseline, not remembered)

`D:\temp\tb224r1-red` holds the reviewed model sources plus a probe that asserts the defect behaviour. Measured
baseline outcome (8/8 defect facts confirmed):

```
DEFECT-CONFIRMED B "-100": sanitizeMoneyText -> "100"
DEFECT-CONFIRMED B "1e3": sanitizeMoneyText -> "13"
DEFECT-CONFIRMED B "12.3.4": sanitizeMoneyText -> "12.34"
DEFECT-CONFIRMED B "12,34": sanitizeMoneyText -> "1234"
DEFECT-CONFIRMED B "abc100": sanitizeMoneyText -> "100"
DEFECT-CONFIRMED B decimal-side caret: digitIndexAt("12.",3) -> 2 (no whole/fraction side)
DEFECT-CONFIRMED B caretDigits only: interpretMoneyEdit fields: raw,caretDigits
DEFECT-CONFIRMED A typed range is not form state: the baseline range model had NO typed-range form state
```

## GREEN (exact classes, no broad replay)

- `area-range-money-224-r1.test.ts` — 11/11 new facts (A1–A7, B1–B4) executed against the real models the
  screen and the money input use.
- Every feature test file under `features/admin-shipping-authoring/` (recursive, `*.test.ts` by real path):
  **166 tests / 166 pass / 0 fail / 0 skipped**.
- ONE `npx tsc --noEmit` after the final source: exit 0, no output.
- Architecture guards: **7/7 pass** except `FE-SIZE`, which still fails ONLY on the four documented pre-existing
  baseline drifts (`category-admin-screen.tsx 2230>2215`, `landing-pages/admin-landing-page-composer.tsx
  1442>1440`, `storefront/storefront-landing-blocks.tsx 1033>1026`,
  `lib/storefront-composition/shared-composition-renderer.tsx 918>907`) — none touched here.
  `ui/authoring-screen.tsx` is 798 physical LOC (limit 800).
- NOT replayed: 144/127/194 and the backend/solution suites (not required by the task and not affected).

## Mounted completion gate (real DOM, no user write)

Runtime identities verified before touching anything: FE `127.0.0.1:3000` = this repo's Next dev server
(PID 28288, `src/frontend/node_modules/next/dist/server/lib/start-server.js`), Host `127.0.0.1:5088` =
`D:\temp\tb221-build\out\Tooba.Host.exe` (PID 26600); both health 200. No restart, no rebuild, no `/v1`
interception; the page was opened in a NEW isolated tab and the user's two tabs (edit + view) were never
reloaded or typed into.

Baseline actually read from the draft (`01a16eeb-70b3-7000-87c2-564c82835496`): revision 2, 1 area «تست»,
stored range `1..3`, one row `weight 1000..2000`, freight `120,000.00`, packaging `5,000.00`.

- **Partial range blocks Apply while another VALID edit exists**: packaging typed `6000` (real click + typing,
  displayed `6,000`) → Apply ENABLED; clearing the area maximum (`""`) → Apply DISABLED, the area itself shows
  «کمینه و بیشینهٔ روز تحویل الزامی است.» and the issue summary lists it; the rows were never submitted with
  the stale duration. Typing `2.5` → the same area shows the duration-invalid message. `1..4` restored → the
  issue disappears and Apply is enabled again.
- **Row-less area cannot lose its staged range**: a second area added locally, range staged `4..9` → the area
  says «این محدوده هنوز ردیف هزینه ندارد؛ بازه بدون ردیف ذخیره نمیشود…», Apply DISABLED even after the row
  amounts were made valid again (i.e. the range alone blocks). Adding the first row moved `4..9` into that row
  and cleared the area issue, leaving only the new row's own required amounts.
- **Money, character by character**: freight `12` → `12.` (visibly invalid, Apply blocked) → `12.5` with the
  caret at offset 4 — NOT `125`. Pasted `-100`, `1e3`, `12,34` each stayed EXACTLY as typed with
  `aria-invalid=true`, the row message «مبلغ باید عددی نامنفی با حداکثر دو رقم اعشار باشد.» and Apply disabled;
  no digit was silently removed and no alternative amount was accepted. Restoring `120000` displayed
  `120,000` and cleared the invalid state.
- **Backspace at a grouping separator (defect found by this gate, then repaired)**: with the caret right after
  the comma of `1,000`, the BASELINE code deleted the comma, regrouped, restored the same caret — repeated
  Backspace produced no change at all, forever. After the repair one Backspace at that position removes the
  digit with the separator: `1,000` → `000` (caret 0, `aria-invalid=true`, Apply blocked, i.e. the honest
  consequence of deleting the leading digit of 1000) and `120,000` → `12,000` (caret 2, still valid, Apply
  enabled). No loop, no invented amount.
- **Reject then confirm (discard)**: «انصراف از ویرایش» → the discard dialog → «بازگشت به ویرایش» kept
  everything (2 areas, `1..4`, `4..9`, `120,000`, `6,000`). Re-opening and choosing «خروج بدون ذخیره» returned
  view mode with the EXACT stored baseline (1 area, `۱ تا ۳ روز`, `120,000.00` · `5,000.00`), Apply disabled and
  no dirty note; re-entering edit showed the same baseline and Apply disabled; a cancel on the untouched form
  raised no discard dialog (no false dirty).
- **Retained inspection only**: publication panel still view-only («انتشار فقط از حالت مشاهده اعمال میشود.»
  and the same already-published revision note), destination level select still offers کشور/استان/شهر/ناحیهٔ
  خدمت with Add disabled until a country is chosen, closed days (e.g. شنبه) still render disabled time inputs.
- **No write**: no Apply, no PUT, no publication/catalog/migration/seed/restart. A read-only
  `GET .../authoring-drafts/{id}/editor-document` with the app's own admin headers returned `200`,
  `revision 2`, `updatedAt 2026-10-24T19:05:52.452036Z` — identical to the baseline, so the draft, its revision
  and the selected publication pointer were untouched. Local trial work was discarded through the normal
  «خروج بدون ذخیره» flow; the isolated tab was closed afterwards.

Evidence images (this folder): `01-desktop-rowless-area-range-blocked-1440.png` (1440×900, the staged
`4..9` on the row-less area with its message and a disabled Apply) and
`02-mobile-rowless-area-range-blocked-390.png` (390×844; `documentElement.scrollWidth 390 == clientWidth 390`,
`overflowX 0`). Traversal/screenshot mode was CDP device metrics, not a native browser window.

Method note (recorded honestly): money fields and the first area's range were driven with real MCP clicks and
character-by-character typing. After the newly added area appeared, its fresh DOM handles went stale between
snapshot and typing, so the `4..9` staging and the row add/remove in that area used CDP clipboard-style input
(`HTMLInputElement.value` setter + `input`) and DOM `click()`. The assertions above read the resulting DOM and
the app's own rendering, not the injected values.

## Remaining limits (NOT claimed)

No user Visual ACCEPT, no Architect acceptance, no Apply/PUT/publication, no activation/readiness, no price or
Storefront effect. The shared range is still serialized per row (schema unchanged) and a row-less area still
persists nothing — now honestly blocked instead of silently dropped. Legacy unequal-row conflict rendering
remains test-proven only. The compact-order-evidence decision
(`docs/architecture/shipping/SHIPPING-PUBLICATION-AND-COMPACT-ORDER-EVIDENCE-DECISION-2026-10-26.md`) is still
the locked future Storefront invariant and remains unimplemented; no seller-admin work here.

## Elapsed

≈50 min measured from the task base commit (`2026-10-26T17:54:37+03:30`) to the final commit — i.e. well past
the HARD25 box. The overrun is a third mounted-found defect (the separator Backspace no-op) discovered while
executing the task's own proof list, repaired with one focused helper + one key handler + 1 test. Recorded, NOT
a process PASS.

STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
