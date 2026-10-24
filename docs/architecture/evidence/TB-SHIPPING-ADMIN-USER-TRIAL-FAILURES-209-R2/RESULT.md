# TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209-R2 — RESULT

Parent: TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209-R1
Mode: BOUNDED_GROUP_AND_ROW_SCOPED_ERROR_FOCUS_CORRECTION
Base: `2de29283a9d32739607d7d8be26666a19bb82a2a`
Verdict: **READY_FOR_ARCHITECT_REVIEW** — frontend-only, zero domain writes.

## 1. Timing (actual, not invented)

- Claim: 2026-10-24T21:19:37Z (row `fd1d9932-7f72-4fff-9e8b-73a5ac1d4696`).
- Implementation end: ≈21:27Z; evidence/commit/push ≈21:31Z. **≈12 min vs the 10-min hardbox
  (≈2 min over; recorded, one correction round, no finishing loop).**

## 2. The defect and the fix

The review found that `parseProperty` kept only `rows[j]`, `ServerFieldIssue` had no group identity, the
screen built `Map<number, string>` from `rowIndex` alone and shared it with every group, and focus used a
document-global `querySelectorAll(testId)[rowIndex]`. So `regionGroups[1].rows[0].freightAmount` landed on
group0 row0, equal row indices collapsed across groups, and a closed optional-conditions disclosure blocked
focus.

- `ui/server-field-error.ts`
  - `ServerFieldIssue` now carries `groupIndex` **and** `rowIndex` (both parsed from the actual submitted
    path; `parseProperty` reads `regionGroups[i]` via the same `segmentOf` splitter).
  - De-duplication key is `target|groupIndex|rowIndex|testId|reason`, so two groups' row0 are two issues.
  - Localized label is scoped: `fieldRatesLabel — محدودهٔ ارسال {g+1} · ردیف {r+1}` (group title/row label
    reuse existing messages; no new wording authority).
  - New pure helpers: `isRowScoped`, `rateRowKey(groupIndex, rowIndex)` = `"g:r"`, and
    `serverIssueSelector(issue)` which composes an explicit scoped selector
    (`group[data-group-index] row[data-row-index] input[data-testid]`) and `null` for `unknown`.
  - Exact-code reason mapping and unknown-code honesty unchanged.
- `ui/authoring-groups-editor.tsx`: group section carries `data-group-index={position}`;
  `serverRateErrors` is now `ReadonlyMap<string, string>` keyed `rateRowKey(position, rowIndex)` — the
  submitted index is resolved against the CURRENT ordered groups/rows, so a reason reaches only its own
  row, and an absent/out-of-range index simply matches nothing (no fallback to row0). No second grouping store.
- `ui/authoring-rates-editor.tsx`: editable row carries `data-row-index={index}` (presentation metadata).
- `ui/authoring-screen.tsx`: `rateServerErrors` builds composite keys; `revealServerIssue` resolves
  `serverIssueSelector`, then walks up from the target and sets `open = true` on ONLY the enclosing
  `<details>` elements before scrolling/focusing. That walk opens nothing else, edits no value, marks no
  dirty flag and never touches the revision.

## 3. Focus guarantees

1. intended GROUP element → intended ROW element (row-scoped only) → the actual input `data-testid`;
2. no document-wide Nth-match anywhere (`querySelectorAll(\`[data-testid=…\`)` is gone);
3. a rejection whose index does not exist in the current tree highlights nothing (honest silence) instead
   of another row;
4. a rejected optional-condition input inside a closed disclosure is opened by its own ancestor only.

## 4. Validation (focused only)

`features/admin-shipping-authoring/user-trial-209-r2.test.ts` — **10/10 pass**:
group1 row0 keeps `(1,0)`; selector is exactly group1→row0→freight; group0 row1 delivery + group1 row0
freight stay separate with correct reasons; equal code/row0 in two groups yields TWO issues and distinct
labels; composite keys never collide with bare row keys; out-of-range `(9,7)` is preserved and never
redirected to row0; group-level path scopes to the group container; non-region path stays global; the screen
opens only enclosing disclosures and its reveal block contains no value/commit path; group/row ownership
reaches the rendered attributes and the map lookup.

Earlier focused suites updated/kept green: `user-trial-209.test.ts` (label now group+row scoped),
`user-trial-209-r1.test.ts` — combined **26/26 pass**. `npm run typecheck` clean (run once after the fix).

## 5. Not done / limits

- No browser/runtime startup, no screenshots, no HTTP probe, no save replay, no seed/migration/rebuild/Host
  restart; the real rev2 draft was not touched.
- The mounted behaviour of the disclosure-opening walk is source/test-level evidence here; it was not
  exercised in a live browser in this task.
- Group-level (`regions`) rejections are still shown in the banner summary only, not inline in the group
  card — unchanged from R1, unchanged scope.
- Picker relevant-level rendering, General view disabled-vs-text, and user Visual ACCEPT remain outside
  this task (R1 review already recorded them); no production/functional acceptance is claimed.
