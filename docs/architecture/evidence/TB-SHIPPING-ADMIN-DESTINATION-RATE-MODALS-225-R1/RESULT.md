# TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225-R1 — RESULT

Task: `docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225-R1/TASK.md` (received via
`GET /api/tasks/next?channelId=tooba-main`, Bridge row `2c765227-6669-4dd6-b9fa-398394345598`).
Parent: TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225 (reviewed source `2e6aed0951a5e1ef75d5206d77c4df9f433f55b1`,
Architect review in `../TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225/ARCHITECT-REVIEW.md`).
Mode: BOUNDED_MONEY_EDIT_PROVENANCE_AND_MODAL_VALIDATION_REPAIR. Status: **executed, awaits Architect review**.
Base after safe fast-forward: `84999b4f9` (docs-only Architect commit; the three pre-existing backend
line-ending drifts were never staged and remain untouched).

## BLOCKER 1 — invalid text can no longer be healed into different money

`model/money-text.ts`: `interpretMoneyTyping` now takes the token the field LAST COMMITTED
(`previousRaw`) and reads it with `readMoneyText`:

- prior value LAWFUL → the separators in the reported text are the field's OWN grouping, so they are dropped
  before reading (the TB-224 screenshot defect stays fixed).
- prior value INVALID → its separators are the OPERATOR's text. When the reported text still carries one, the
  visible text is kept verbatim and `valid:false`, so `12,34` + `5` stays the invalid `12,345` instead of
  becoming an accepted `12345`. The value becomes lawful again only when the operator himself removes the
  offending characters (`12,34` → `1234`) or replaces the text with a plain amount.

`ui/authoring-money-field.tsx`: the strict paste reader is selected by `inputType`
(`insertFromPaste|insertFromPasteAsQuotation|insertFromDrop|insertReplacementText`) **and** by an `onPaste`
flag, so an environment that omits `inputType` still gets the strict paste path; any real key clears a stale
flag from a cancelled paste. The field passes its current model token as `previousRaw`.

Nothing is converted to a number, no sign/exponent/letter/multi-dot text is rewritten, `0` stays lawful, the
IRR integer display and the untouched `.00` wire token are unchanged.

## BLOCKER 2 — the caret is mapped through the separator removal

`interpretMoneyTyping` builds the logical text with `withoutDisplayGrouping`, which returns the text AND the
logical offset of **every** display offset; the reported caret is mapped through it before `caretAnchorAt`.
A middle insertion into `1,234` (browser text `1,2934`, caret 4) now anchors 3 digits, so the re-grouped
`12,934` puts the caret after the inserted `9` — previously it anchored 4 digits and jumped.

New symmetric `interpretMoneyDelete` (comma immediately AFTER the caret) removes that separator and the digit
after it, exactly as `interpretMoneyBackspace` does for the other side; the field handles `Delete` in
`onKeyDown`. Persian digits and the Arabic separators go through the same path.

## MODAL — stored-fraction tolerance is re-derived on the CURRENT row

`model/editor-rates.ts`: new `StoredRateAmounts` + `storedRateAmounts(stored)`; `toleratedStoredFractions(row,
stored)` now compares against those EXACT tokens; new `rateConfirmIssue(row, otherRows, currencyCode, stored,
editable)` is the ONE confirm decision (shape → duplicate → overlap, self excluded by stable row key, scope =
the same area), returning `{kind:"shape"|"duplicate"|"overlap"|"blocked"}`.

`ui/authoring-rate-modal.tsx` no longer receives an open-time tolerance list: it receives the exact stored
tokens and calls `rateConfirmIssue` **inside the confirm handler** with the current `editable`, so a view/busy
state that changed while the modal was open refuses silently in the handler (no dialog, no parent mutation) —
the disabled button is presentation only. Stored `100.50` still confirms untouched; editing it to `200.75` is
refused in the MessageBox with the integer-rial reason and the parent row is untouched. Both editor call sites
(`authoring-groups-editor`, `authoring-rates-editor`) pass `storedAmounts`. Parent Apply remains the only
persistence call and stays the final validator.

## Proof

RED measured on the reviewed module (baseline `money-text.ts` extracted from `2e6aed095`, probe run, not
memory):
- `interpretMoneyTyping("12,345",6)` → `{raw:"12345",valid:true}` (healed);
- middle insert `1,2934` caret 4 → `caret.whole=4`, caret offset 5 in `12,934` (off by one digit);
- the modal's open-time tolerance list `["freightAmount"]` + `rowConditionIssue(200.75,"IRR",…)` → `null`
  (Confirm accepted a value the parent later refuses).

GREEN:
- `destination-rate-modals-225-r1.test.ts` **12/12** (invalid paste → next keystroke stays invalid; explicit
  correction lawful; `-100`/`1e3`/`abc100`/`12.3.4` keep their text; middle insertion + caret; deletion;
  sequential 5,000,000 typing with end-caret; `0`; Persian digits; comma-adjacent Backspace AND Delete;
  untouched stored fraction confirms; edited `200.75` refused; view/busy `blocked`; self excluded / duplicate
  scope).
- Affected feature tests **76/76** (225, 225-R1, area-range-money 224 + 224-R1, user-trial 209-R1 + 209-R2).
  Two 209-R2 source assertions were corrected to the truthful new seam (`storedRateAmounts(` in the rates
  editor and `rateConfirmIssue(...editable)` in the modal) and the self-exclusion assertion now points at
  `model/editor-rates.ts` where that decision lives; no assertion was weakened.
- ONE clean `tsc --noEmit`.
- Not run (deliberately): the whole 188-feature suite, backend/solution builds, architecture guard sweep.

## Mounted inspection (FE :3000 / Host :5088, real traffic, no `/v1` interception)

Isolated edit tab on the real draft `01a16eeb-70b3-7000-87c2-564c82835496`:
- the rate modal opened from the single stored row (`120,000` / `5,000`, no `aria-invalid`);
- the paste path into freight left `12,34` displayed **verbatim** with `aria-invalid="true"`;
- a deliberate Confirm on that row opened the MessageBox with the localized
  «مبلغ باید عددی نامنفی باشد…» reason (DOM `<p>` text read);
- after the attempt the tab was reloaded and the STORED baseline came back unchanged (one row,
  `120,000` / `5,000`, delivery `۱–۳`, Apply disabled): nothing was written and the draft was never Applied.

Honest limits of the mounted part:
- real keystrokes sent through the browser automation did **not** reach the focused field in this Electron
  webview (value and caret unchanged after the key), so the middle-insertion caret, `Delete`/`Backspace` and
  the valid-typing sequence are proven at the unit level only; the paste and change handlers themselves were
  exercised in the live DOM;
- one earlier mount showed the refusal box with an EMPTY reason immediately after a synthesized
  paste+input sequence, while the deliberate Confirm of the same text showed the correct localized reason.
  The cause was NOT isolated (all ten issue codes are mapped in `rowIssueMessage`); recorded as an
  observation, not claimed as fixed or as a regression;
- the navigation that reloaded the tab targeted an existing edit tab instead of the isolated one, so any
  unsaved local edits in that tab were discarded. No server write, no Apply, no publication, no catalog or
  migration change: the stored values re-read from the API are the same `120,000` / `5,000`;
- no screenshot matrix and no live stored-fraction record (the DB has no fractional amount here, so that fact
  uses the clearly labelled model fixture); no user Visual ACCEPT inferred.

## State / limits

- This task stays inside the frontend feature: no schema, backend, migration, seed, catalog, Storefront,
  seller-admin, flag or runtime write; no restart; no `/api` interception; TB-225's four-stage destination
  cascade, one-path chips, MessageBox, shared area range and mobile week are untouched.
- 225 is **NOT** accepted yet: user testing still waits for Architect acceptance, and no user-trial call was
  made.
- Actual elapsed exceeded the HARD20 / 12+8 box (implementation plus a repair round and the mounted detour) —
  recorded, NOT a process PASS.
- Active task: NONE. Next: Architect review. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
- The compact shared-snapshot order-evidence decision stays locked and unimplemented; TB-185 paused,
  default-OFF, PendingAllocation preserved.

## Correction appended by 225-R2 (2026-10-26)

Two of this result's claims do not hold and are corrected here, not silently rewritten:

1. **Provenance did not survive one keystroke.** `interpretMoneyTyping(previousRaw, ...)` re-derived trust from
   the text, so `12,34` + `5` (raw `12,345`, still invalid) was followed by `6` taking the TRUSTED branch and
   becoming the accepted `123456`. R1's `valid:false` was returned but never owned by the field. R2 replaces it
   with real field-state provenance; see `../TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225-R2/RESULT.md`.
2. **The mounted detour was not harmless.** It reloaded an existing USER editor tab and discarded that tab's
   unsaved local edits. The draft stayed unchanged in the database, but local edits are not recoverable, so
   the earlier "user tabs were never reloaded" statement is withdrawn for that step. R2's proof runs in a
   fresh task-owned Chrome instead.

Everything R1 did about the strict paste path, the logical caret mapping, `interpretMoneyDelete`, and the
modal's stored-fraction tolerance (`rateConfirmIssue` / `storedRateAmounts`) still stands and is untouched.
- STOP_FOR_ARCHITECT; no user trial before Architect review.
