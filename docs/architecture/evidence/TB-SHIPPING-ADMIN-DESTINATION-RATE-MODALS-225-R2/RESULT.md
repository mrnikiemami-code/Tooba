# 225-R2 — money edit provenance is FIELD STATE, and the mounted proof runs in an isolated task Chrome

Task receipt: `TASK.md` (verbatim). Reviewed main: `1d06252188a3278127401e804f53b263725c47e1`.
Base for this task: that same commit (clean fast-forward, no reset/stash/rebase/force-push).
Parent: `TB-SHIPPING-ADMIN-DESTINATION-RATE-MODALS-225-R1`. 225 and R1 remain **NOT accepted**; no user
trial was called and none may be called before Architect review.

## A — the one defect: provenance was re-inferred from the text instead of owned by the field

R1 decided trust with `readMoneyText(previousRaw).valid`. That is enough for `12,34` but not for the state ONE
keystroke later: `12,345` is structurally well-grouped, so the very next key took the TRUSTED branch, dropped
the operator's comma as "display grouping" and accepted `123456` — a different amount, silently.

Executed repair:

- `model/money-text.ts` now has a real provenance record —
  `MoneyInputProvenance { raw, untrusted }` — and `interpretMoneyTyping(previous, nextText, caret)` takes it.
  `trusted = !provenance.untrusted && readMoneyText(provenance.raw).valid`, so the operator's OWN unsupported
  text stays untrusted through any number of appended keys until he himself removes/replaces the offending
  grouping. `readMoneyProvenance(raw)` is the only fallback and is used for an EXTERNAL token (reseed): there
  the crime is what the text is.
- `MoneyEdit` carries `untrusted`, so every entry point propagates it: `interpretMoneyEdit` (paste/drop/
  replacement), `interpretMoneyBackspace`, `interpretMoneyDelete` and `interpretMoneyTyping`.
- `ui/authoring-money-field.tsx` owns the state: one `provenance` ref keyed by the committed token, a single
  `commit(edit)` used by the change, Backspace and Delete paths, and `currentProvenance()` that drops the
  record the moment the value prop no longer matches it (external reseed, another row, a reopened modal).
  Blur/re-render/another field changing cannot launder it because the record lives with the value, not with
  the rendered text.
- No second sanitizer, no copy of model authority: the model still receives the raw text verbatim and the
  parent validation still blocks on it. Rate shape, self-exclusion, legacy-fraction and MessageBox semantics
  are untouched.

## B — RED measured, then GREEN on the same sequence

RED on the unchanged R1 module (`git show 1d0625218:.../money-text.ts`, probe executed, not remembered):

```
paste 12,34 -> "12,34"
append 5    -> "12,345"      (invalid, still truthful)
append 6    -> "123456"      (ACCEPTED — different money)
append 7    -> "1234567"
```

GREEN — `destination-rate-modals-225-r2.test.ts`, 8 facts, all driving the same state transitions the
component owns (`field()` models the committed token + its provenance, `aria-invalid`, and paste/typing/
backspace/delete/reseed entries):

1. paste `12,34` → `5` → `6` → `7`: every step stays `aria-invalid`, raw `12,34567`, and a re-render/blur
   does not change it.
2. the operator's own deletion of the separator makes the text lawful again.
3. replacing the whole value is also a deliberate correction.
4. provenance never leaks to a different row / external reseed.
5. valid grouped value keeps a middle insertion and the caret after the typed digit.
6. sequential `5000000` → `5,000,000`, `0` valid, scaled decimal side retained.
7. malformed paste, `-100`, `1e3`, `abc100`, `12.3.4` stay invalid; a stored `100.50` stays a lawful token.
8. comma-adjacent Backspace and Delete stay symmetric, and deleting the operator's comma is a correction.

Affected classes: `destination-rate-modals-225-r2` 8/8, `-225-r1` 12/12, `-225` 22/22 (42/42 together),
plus `area-range-money-224-r1` and `user-trial-209-r2` 21/21. One `tsc --noEmit` clean (0 errors).
No 188/76/full-feature/backend/solution/guard sweep was run — as instructed.

## C — mounted gate in a FRESH task-owned Chrome (not the user's webview)

Identity recorded: `C:\Program Files\Google\Chrome\Application\chrome.exe` 155.0.8059.85, launched with
`--headless=new --remote-debugging-port=9333 --user-data-dir=%TEMP%\tb225r2-chrome-profile` (fresh, deleted
before launch), port verified free before launch. The page target `75081F7DB2547954825D0574BD0D1869` was
**created by this task** (`Target.createTarget`) — no "first matching localhost tab", no enumeration or
selection of any user tab/session. Only processes whose command line contained that profile were stopped
afterwards; the port has no listener (only TIME_WAIT sockets owned by nobody). All `/v1` traffic is real; no
interception, no request fulfil.

Real input only (`Input.dispatchKeyEvent` char events, `Input.insertText`, `Input.dispatchMouseEvent`), with
`document.activeElement` verified as `shipping-authoring-rate-freight` before every sequence, and the DOM
value/selection read after each:

| step | DOM value | aria-invalid | caret |
| --- | --- | --- | --- |
| typed `1234` | `1,234` | false | 5 |
| insert `9` at offset 3 | `12,934` | false | **4 (after the `9`)** |
| typed `0` | `0` | false | 1 |
| typed `5000000` | `5,000,000` | false | 9 |
| pasted `12,34` | `12,34` | **true** | 5 |
| + `5` | `12,345` | **true** | 6 |
| + `6` | `12,3456` | **true** | 7 |
| + `7` | `12,34567` | **true** | 8 |
| after clicking another field | `12,34567` | **true** | 8 |
| + `8` | `12,345678` | **true** | 9 |

Confirm on that invalid token: the modal stayed open and the MessageBox showed a non-empty localized reason
(«مبلغ باید عددی نامنفی باشد؛ محدودیت دو رقم اعشار فقط برای ارزهای اعشاری اعمال می‌شود.»), with the typed
text kept — `screens/225-R2-refusal.png`. Cancel then restored the baseline exactly: one row, `120,000` /
`5,000`, delivery `1–3`, Apply disabled — no false dirty, no residue.

HONEST MECHANISM NOTE: the pasted token was produced by dispatching a real `paste` event on the element (so
the field's own paste path runs) followed by CDP `Input.insertText` — the text is inserted by browser input,
never by a value setter, but the clipboard itself is not used (clipboard commands are denied in this tool
set). The `12,34` paste is therefore the browser's paste CODE PATH, not an OS clipboard round-trip. The
appended digits after it are ordinary real key events.

HONEST NEGATIVE: typing a comma into an already-valid value still does NOT create a comma in the model — the
comma keystroke is taken as display grouping and dropped, so `12,34` can only be reached by paste/drop. That
is pre-existing accepted behaviour of the grouped field (not a R2 regression) and is recorded here rather
than silently fixed outside the task's scope.

## D — user tab incident (recorded, NOT recovered)

R1's mounted detour reloaded an existing USER editor tab and discarded that tab's unsaved LOCAL edits. The
stored draft was untouched, but an unsaved local edit is not recoverable from the server, so this is a real
loss and must not be described as "user tabs untouched". No attempt was made to read, enumerate or restore
that tab; nothing in this task touched the user's Electron webview, and no claim of recovery is made.

## E — untouched, and limits

- No Apply/PUT/publication/catalog/migration/seed; the draft `01a16eeb-70b3-7000-87c2-564c82835496`, its
  revision and the publication pointer were not written by this task (the mounted run only opened the modal
  and cancelled).
- Shared FE :3000 and Host :5088 were used as-is, no restart; the four-stage destination cascade, one-path
  chips, shared area range, MessageBox semantics, mobile week and the corrected `rateConfirmIssue` are
  unchanged.
- The Tehran/service-area live gap is unchanged and outside this correction.
- NOT claimed: user Visual ACCEPT, Architect acceptance, any Storefront/checkout consumer, or that the
  whole editor is defect-free.
- Actual elapsed ≈ 45 min against the HARD20 box (implementation ~15, mounted gate with two focus/layout
  corrections ~20, evidence/notes rest) — recorded, NOT a process PASS.
- Active task: NONE. Next: Architect review. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
