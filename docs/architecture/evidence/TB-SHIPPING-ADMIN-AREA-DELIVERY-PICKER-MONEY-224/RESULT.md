# TB-SHIPPING-ADMIN-AREA-DELIVERY-PICKER-MONEY-224 — RESULT

Parent: `TB-SHIPPING-ADMIN-PUBLICATION-OWNER-PAGES-223`
Base: `920b51a7f2dc193afb47366daf9a531415d21373` (fast-forwarded from `origin/main`, no reset/stash/rebase)
Mode: BOUNDED_USER_REPORTED_AREA_FORM_REPAIR (frontend only)

## A — ONE delivery range per DELIVERY AREA

`model/editor-area-range.ts` (new) is the AREA-LEVEL view over the row values:

- `areaRangeOf(rows)` → `empty` (no rows) / `uniform(min,max)` (every row identical) / `conflict(pairs)`
  (distinct stored pairs listed, never resolved to first/min/max).
- `applyAreaRange(rows,min,max)` writes the chosen pair to exactly THAT area's rows and returns the SAME
  array when nothing changes, when the pair is partial or when it is invalid — so an unrelated edit can
  never normalize a legacy difference.
- `inheritedAreaRange(rows,staged)` gives a new row the area's own shared range only (uniform, or a lawful
  explicitly staged pair for a row-less area); a conflicting area inherits NOTHING.
- `areaRangeIssue` mirrors the real domain bounds (0..365 working days, `max >= min`, integers).

UI: `AreaDeliveryRange` in `ui/authoring-rates-editor.tsx` sits in the area card ABOVE the rows and owns
its own typed text (`seedKey` = derived range identity, so a new baseline/derived range re-seeds it).
`RateRowEditor` / read rows no longer render any per-row duration field. `ui/use-area-range-edits.ts` keeps
the last staged pair per area (so a first row can inherit it) and applies a lawful pair through the screen's
ONE commit path; the staged pairs are forgotten when the confirmed baseline object changes.
New rows go through `withAddedRowInheriting` in `model/editor-rates.ts`.

Wire/schema boundary (NOT changed, documented honestly): the existing DTO/snapshot still repeats
`minDeliveryDays`/`maxDeliveryDays` per rate row, so a "shared" range is serialized by writing the same pair
to every row of that area. A row-less area persists nothing (`no empty range auto-serialized as 0/0`); its
staged range is local until its first row exists. A true group-level persisted duration would be a schema
change and was deliberately NOT attempted inside this box.

Legacy conflict: rows with unequal durations keep their stored values; the area control starts empty, states
the conflicting pairs, and only a deliberate lawful entry unifies them. `rowIssue` still blocks Apply on a
row that has no duration (e.g. a new area where no range was ever chosen), and that reason is now reported
ONCE on the area range instead of on a price row.

## B — duplicate check at ADD, navigation never blocked

`ui/authoring-destination-picker.tsx` no longer computes a duplicate during render. The old
`duplicate = terminal !== null && isSelected(terminal)` gate is gone, so selecting an already stored country
shows NO error and keeps the branch explorable. `canAdd` is `destinationAddAvailable(choice, readState,
disabled)` (new model helper, deliberately independent of "already stored"); a submitted Add re-checks
`choiceChainActive` + `destinationAddEligibility` against the coordinator's CURRENT evidence, then asks
`isDuplicateSelection(settled, isSelected)`. A refusal shows the localized message inline, KEEPS the staged
path, and is cleared as soon as the level or any path stage changes. The stored chips are never removed.

The honest overlap wording was added (`destinationOverlapHint`): the example screenshot's stored **Iran
(country)** covers Iran descendants, so browsing a city under it is allowed but ADDING overlapping coverage
is still rejected by the authoritative no-overlap rule on Apply. No bypass, no fabricated success.

## C — relevant-level picker

The four terminal-level buttons were replaced by ONE localized level `select` (`Country` /
`AdministrativeArea` / `City` / `ServiceArea`) and the path renders only what that terminal needs: country;
country+province; country+optional province+city; country+optional province+city+service area. The optional
province filter is never fabricated, never substituted for a city's real parent, and country-wide
`null`-admin cities stay lawful (unchanged `choiceAncestry`/`destinationAddEligibility` semantics).

## D — exact money display

`model/money-text.ts` (new) is the ONE exact-decimal text layer: `toAsciiDigits` (Persian/Arabic-Indic),
`sanitizeMoneyText` (drops group separators/Arabic decimal separator/spaces, keeps digits and one dot, `0.`
for a leading dot), `groupMoneyText` (string grouping, no `Number`), `digitIndexAt`/`caretForDigitIndex`
(a DIGIT-anchored caret), `interpretMoneyEdit` (canonical token + caret anchor).
`MoneyField` renders `groupMoneyText(raw)` while handing the model the RAW token; grouping never changes the
value, the scale, or the dirty flag. Applied to freight, packaging and both merchandise bounds; read
summaries use the same string grouping (`formatDecimalText` now uses `groupMoneyText` instead of `Number`).
Currency stays server-owned; no forced `.00`, no guessed scale, no conversion.

## Proof executed

- `tsc --noEmit`: clean (one run after the final source).
- Feature tests (recursive, all 16 files incl. the new `area-range-money-224.test.ts`): 155/155 pass.
  New facts (11) execute the real models: area range writes all+only that area, new-row inheritance
  (uniform / staged / conflict), legacy conflict preserved until an explicit lawful pair, unchanged uniform
  baseline clean, cancel reseeds from the same baseline, domain-bound validation, stored-country staging
  available (`destinationAddAvailable`) while submitting THAT level is a duplicate, verified/active/ancestry
  guard retained, and money `120000.00→120,000.00→120000.00`, `0.50`, paste/local digits, blank, caret.
- Guards: feature-boundary, flat-folder, import-boundary, test-discovery, seo — pass. `frontend-source-size`
  fails ONLY on the four pre-existing drifts (`category-admin-screen` 2230>2215,
  `landing-page-composer` 1442>1440, `storefront-landing-blocks` 1033>1026,
  `shared-composition-renderer` 918>907); this task's file was kept at 798 LOC (≤800 threshold) and the four
  untouched files were NOT repaired.
- Two source assertions were corrected to the truthful new presentation, no assertion weakened:
  `204-05` now asserts the level choice exists (`destination-level`, no longer per-level buttons) and
  `204-R2` asserts `destinationAddAvailable(choice, readState, disabled)`.
- Live isolated stack reused with no restart: FE `127.0.0.1:3000` (PID 28288), Host `127.0.0.1:5088`
  (PID 26600, health 200). Real traffic, no `/v1` interception.
  - Edit mode, real draft `01a16eeb-70b3-7000-87c2-564c82835496`: freight `120,000.00`, packaging `5,000.00`
    (grouped), ONE range `1..3` at area level, ZERO per-row duration inputs.
  - Staging the already stored Iran: no duplicate message, Add enabled → clicking Add shows the localized
    duplicate/overlap message and KEEPS the staged country. Switching the level to City clears the message
    and renders country+province+city (province options 13, city options 48) — deeper exploration allowed.
  - Area range edited to `4..9` with a second row added earlier (that row inherited `1..3`); money typing
    `1850000.00` displayed `1,850,000.00`; pasted Persian grouped `۵٬۰۰۰` displayed `5,000`.
  - Cancel → «خروج بدون ذخیره» → re-entering edit restores the SAME baseline: 1 row, range `1..3`,
    `120,000.00` / `5,000.00`. No residue.
  - Screenshots: `edit-area-range-1440.png` (1440×900) and `edit-area-range-390.png` (390×844,
    `scrollWidth 390 == clientWidth 390`, area/picker width 300). Native viewport bytes.
- Not exercised / NOT claimed: no Apply, no PUT, no publication or catalog write, no migration/seed/reset.
  The user draft was NOT written by this task (local edits only, then discarded). Legacy-conflict rendering
  and the row-less staged-area inheritance are proven by the focused tests, not by live user data.
  No user Visual ACCEPT.

## Remaining boundary (unchanged by 224)

The compact order evidence invariant in
`docs/architecture/shipping/SHIPPING-PUBLICATION-AND-COMPACT-ORDER-EVIDENCE-DECISION-2026-10-26.md` still
governs the future Storefront connection: one shared immutable snapshot per owner+draft+revision, compact
per-order-group reference/final money/receiving facts, no full authored JSON per order, no historical
repricing. Publication remains selection INTENT; there is still no Storefront/checkout consumer, no
projection/selection/commit, and no seller-admin entry work (planned separately).

Elapsed: ≈55 min against the HARD40 box / 25-min implementation stop — recorded, NOT a process PASS.
STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
