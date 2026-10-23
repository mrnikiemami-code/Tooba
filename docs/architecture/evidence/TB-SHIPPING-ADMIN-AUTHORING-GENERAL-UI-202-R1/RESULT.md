# TB-SHIPPING-ADMIN-AUTHORING-GENERAL-UI-202-R1 — RESULT

Mode: `BOUNDED_GENERAL_FORM_DIRTY_DISCARD_AND_REACT_LIFECYCLE_REPAIR`
Parent: `TB-SHIPPING-ADMIN-AUTHORING-GENERAL-UI-202`
Base (verified `HEAD == origin/main`): `a66573615d902df3ceeefc43adf571a2db59179f`
Channel: `tooba-main` — Worker: `tooba-worker-01`

## 1. Regression-before / after (the exact 202 defects)

| # | 202 behaviour (before) | R1 behaviour (after) |
| --- | --- | --- |
| 1 | Configured but untouched settings emitted `methodSettings`; `unsaved` was set from serializable edits only, so the screen enabled Apply on a session the controller refused. | Settings are compared to the confirmed baseline; an untouched configured pair is omitted. Changing ONE axis emits BOTH (the untouched axis is preserved) and reverting both becomes `{}` again. |
| 2 | Blanking a stored name produced `edits={}` + `nameRequired`; `controller.edit({})` made the session clean, and the screen's clean-form effect could rebuild/erase typed input. | Raw form work is a first-class `localDirty` session fact: blank name, description-only, one-sided settings and an explicit description edit stay dirty even when no wire patch exists. `edited({})` no longer clears it, and no render re-seeds the form. |
| 3 | A description typed for an untranslated active language was silently ignored (`edits={}`, `issues=[]`). | It is a real intent: reported as `nameRequired` for that language and kept dirty. |
| 4 | One independent setting on an unconfigured baseline produced `edits={}` + `settingsIncomplete` and looked clean. | Dirty + invalid (`settingsIncomplete`); Apply is blocked and discard/browser warning still fires. |
| 5 | Cancel Edit reset `form/controller.edit({})/mode` with no confirmation and was enabled during save. | Cancel Edit and Back share ONE explicit exit plan: clean → direct, real work (including invalid raw work) → existing discard Dialog, in-flight save/reconcile → blocked/inert. Reject keeps the exact form/mode/intent/uncertainty/revision. Unknown commit is never cleared by Cancel Edit (only the existing fresh read recovers it). |
| 6 | `useEditorController` captured the first draft id in a ref with empty-dependency callbacks, no cleanup and no route key. | The hook owns a per-draft controller slot: a different draft replaces and disposes the previous controller, unmount retires it, StrictMode setup→cleanup→setup re-creates a live controller instead of reviving a disposed one, and the thin route keys the screen by `draftId`. |
| 7 | A completion rejected by epoch/request identity still updated `savedRevision`. | Apply/reconcile only adopt metadata when the reducer actually accepted the completion; a disposed or stale completion returns `ignored` and cannot show success. |

## 2. Exact wiring now in the rendered slice

- `model/editor-form.ts`
  - `buildEditorEdits` compares settings and both language axes to the confirmed baseline; a
    description typed before its name is no longer dropped; a select/clear type intent keeps the
    accepted-202 semantics (`preserve` is the only omitted selection).
  - New `formHasLocalChanges(document, form)` is the RAW-dirty oracle (names, descriptions, settings,
    type). It is intentionally wider than the wire patch so invalid work can never look clean.
- `session/editor-session.ts`
  - Minimal typed extension: `localDirty` (state) + optional `localDirty` on `edited` (default false,
    so all accepted-200/202 callers keep their semantics). `unsaved = isDirty(edits) || localDirty`.
  - `saveStarted` now requires a real serializable patch, so raw-only work can never become an empty
    PUT; load/save/discard reset `localDirty`; reconcile keeps it; `commitUncertain`, epoch,
    request-id and explicit reconciliation are untouched.
- `session/editor-controller.ts`
  - `formSeed` advances only on an ACCEPTED fresh load and an ACCEPTED save (never on reconcile/render).
  - `edit(edits, localDirty?)`, adopted-only `savedRevision`/`reconciled`, `ignored` apply outcome for
    disposed/stale completions.
- `ui/authoring-exit.ts` — pure `planAuthoringExit` (`blocked` | `direct` | `confirm`).
- `ui/authoring-screen.tsx`
  - The form is seeded from the confirmed baseline ONLY on `formSeed` changes (fresh load, accepted
    save, discard + fresh read). A plain render, an invalid `edited({})` or a reconcile never re-seeds.
  - Editing dispatches `controller.edit(edits, formHasLocalChanges(...) || issues.length > 0)`.
  - Apply requires session dirt + a real patch + no local issues + resolved facts.
  - Cancel Edit / Back are one exit flow with a tracked local destination, the existing Dialog, and
    inert controls while a save or reconciliation is actually pending.
- `ui/use-editor-controller.ts` — per-draft slot, disposal on unmount/draft change, StrictMode-safe
  revival (no permanently disposed controller, no lost initial load).
- `app/admin/shipping-services/authoring/[draftId]/page.tsx` — `key={draftId}` identity boundary only;
  no business/transport logic added.

## 3. Commands actually run

| Command | Result |
| --- | --- |
| `node --experimental-strip-types --test features/admin-shipping-authoring/authoring-lifecycle-202-r1.test.ts` | PASS 10/10 (new focused suite) |
| all five `features/admin-shipping-authoring/**/*.test.ts` | PASS 48/48 (38 accepted regressions preserved) |
| `npx tsc --noEmit` | exit 0 |
| guards: `frontend-import-boundary`, `frontend-feature-boundary`, `frontend-test-discovery` | PASS 5/5 |

New suite facts: R202-01 configured untouched / one-axis / revert; R202-02 blank stored name;
R202-03 description-only untranslated language; R202-04 one-sided settings on unconfigured baseline;
R202-05 untouched type vs explicit clear; R202-06 invalid work stays unsaved and sends no PUT;
R202-07 exit plan matrix; R202-08 disposed controller ignores a late save (no `savedRevision`, no seed);
R202-09 accepted save advances the seed, reconcile keeps pending work/seed; R202-10 type-then-clear is
not dirty.

Not run: whole frontend discovery/build, backend/Host/solution suites, Next server, browser/visual or
mounted React harness (none is installed by this task), live/network/merchant probe.

## 4. Wiring limitation (stated precisely)

The hook/route lifecycle is repaired in source but there is NO installed React mount harness in this
repository, so the tests prove the controller/session/form/exit-plan functions the screen and hook
actually call, plus inspect the real hook/route code. No mounted or browser test is claimed.
Visual: `NOTRUN` (no authorized running app/session); no Visual ACCEPT.

## 5. Deviations / limits

- Two bounded corrections were used: the accepted-202 explicit type clear had to be preserved after
  an initial over-suppression, and its focused assertion was aligned with that preserved contract.
- Reconciliation intentionally does not re-seed the form, so pending local edits stay visible for
  review against the new confirmed revision (no automatic discard, no second store).
- Region/rate/week editing, create/activation and cutoff/carrier/funding controls remain out of scope
  and read-only; no backend/schema/DB/shared design-system/package change.

Production-Functional-Acceptance: PENDING
Schema-Data-Flag-Changes: NONE
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
