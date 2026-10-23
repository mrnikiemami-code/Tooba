# TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R2 — Worker Result

Mode: BOUNDED_SAME_DRAFT_RELOAD_AND_UNCERTAIN_WORK_SELECTOR_REPAIR
Base SHA: `03271c75af1f25ca0faf598ee93b4570deb2dce6` (fast-forward only from `99d130210`)
Parent: TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R1
Claim: 2026-10-22T23:58Z · Result: 2026-10-23T00:03Z · Elapsed ≈ 5 min (within 10-min box)

Scope: ONLY `src/frontend/features/admin-shipping-authoring/session/editor-session.ts` plus one compact
session regression file and docs. No decoder/builder/API/`index.ts`/backend/UI/route/preview/schema
change. All four R1 repairs are untouched and were not replayed.

## Pre-fix executed regressions (unchanged R1 source)

New file `session/editor-session-200-r2.test.ts`, executed BEFORE repair: **1 passed / 3 failed**.

- `loadedA -> edited -> sameA loadStarted` did NOT return the same state: observed `epoch: 2`,
  `load: "loading"` versus expected `epoch: 1`, `load: "loaded"`. The pending success would then clear
  `edits`.
- `loadedA -> edited -> saveStarted(epoch 1, requestId 4) -> sameA loadStarted` produced `epoch: 2`,
  `load: "loading"`, `saveRequestId: 0` (save still `"saving"`), i.e. the reload invalidated the valid
  in-flight save completion.
- `hasUnsavedWork` after `saveFailed(outcomeUnknown) -> edited({})` returned `false` although
  `commitUncertain` was `true` and `closeRequested` demanded a discard decision.
- The explicit-discard-then-fresh-load and explicit unknown-reconciliation case passed before the fix
  (1 pass), confirming those paths were already correct and are retained.

## Repair

`reduceEditorSession` / `loadStarted`, same-draft branch only:

```ts
if (state.unsaved || state.save === "saving") {
  if (state.draftId !== action.draftId) {
    return { ...state, decision: "discardUnsaved", pendingSwitchTo: action.draftId };
  }
  // A same-draft refresh must not restart the live session.
  return state;
}
```

- Same draft while dirty or saving → the SAME state object: epoch, baseline, edits, `save`,
  `saveRequestId` all unchanged. A same-draft reload can therefore never discard work on success and
  can never make a legitimate in-flight save response stale.
- Different draft while dirty or saving → unchanged `discardUnsaved` + `pendingSwitchTo` decision.
- The latched-unknown guard (`commitUncertain && baseline !== null` → same state) and the explicit
  discard/reconcile exits are unchanged.

`hasUnsavedWork(state)` now returns `state.unsaved || state.commitUncertain || state.save === "saving"`.
Uncertainty no longer looks like "nothing at risk" when the editable values are empty; a clean loaded
session still returns `false`. No auto-retry, no fabricated success, no clearing of uncertainty.

No new actions, API surface or framework; no behaviour change outside these two branches.

## Post-fix

| Run | Result |
| --- | --- |
| `node --experimental-strip-types --test session/editor-session-200-r2.test.ts` | 4 passed / 0 failed |
| Final focused run: R2 file + existing `session/editor-session.test.ts` | 10 passed / 0 failed |

Assertions covered post-fix: same-draft dirty reload returns the identical object (epoch/baseline/edits
preserved) and an unsolicited `loadSucceeded` cannot clear work; same-draft reload during save returns
the identical saving state with `requestId: 4` and the current `saveSucceeded` is still accepted;
explicit discard → fresh load still works; explicit `reconcileStarted`/`reconcileSucceeded` still
clears the latch; `hasUnsavedWork` is `true` for in-flight save, `true` for latched unknown, still
`true` after `edited({})` with Apply blocked `uncertain` and `closeRequested` → `discardUnsaved`, and
`true` after discard until a fresh read; `false` for a clean loaded session.

## Files

- Modified: `src/frontend/features/admin-shipping-authoring/session/editor-session.ts`
- Added: `src/frontend/features/admin-shipping-authoring/session/editor-session-200-r2.test.ts`
- Docs: received task verbatim `docs/ai/tasks/TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R2.task.md`,
  this result, append-only correction to the R1 result, one recovery pointer.
- Public API unchanged (`EditorSessionState`, actions and selector signatures identical); only the
  selector's result broadened for unresolved work.

## Not run / limitations

No `tsc`, build, discovery, architecture guard, backend test, browser, server, DB, container or network
probe was run: the change is a local reducer branch and a boolean selector, and the task forbade a
broad validation loop. No shared app/port/DB/cache/bin/obj/.next/process/credential operation; no
install, download, reset, rebase, stash or force-push. One off-scope working-tree modification
(`.../TB-...-200-R1/post-result.js`) predates this task and was left untouched and uncommitted.
The historical R1 ≈27 min > 25 min deviation stays recorded history, not re-proved here.

Status: PENDING_ARCHITECT_REVIEW. Worker PASS != Architect ACCEPT. Production-Functional-Acceptance: PENDING.
