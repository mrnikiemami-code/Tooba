# Architect review — TB200-R1 / 2026-10-23
Disposition: FOUR ORIGINAL REPAIRS RETAINED; ONE SMALL SESSION REPAIR REQUIRED BEFORE CLOSURE.
Reviewed main99d13021087353e3a226f860b2abb63f8d714706 against29cf0117eeaa087c7edc5e4cf1a0240d1a9f5e8e. Source/patch review only, not Architect tests.
R200-01 baseline-aware builder now preserves omitted groups/rows and distinguishes explicit[]; active-language writes and terminal ancestry identity are checked. R200-02 epochs increase across switching/discarding; completions carry epoch. R200-03 unknown latch and explicit reconcile/discard paths are present. R200-04 requested decoder coherence checks are implemented. Retain these, do not replay them or rewrite wire parser.
Reported pre-fix0/12, post-fix12/12 and total25/25+typecheck are worker evidence. Approx27min vs25 is a recorded process deviation, not grounds for a proof-only loop.

NEW CONCRETE REGRESSION:
In reduceEditorSession/loadStarted, if unsaved or saving AND the target is the SAME draft, execution now falls through, increases epoch, sets loading and preserves edits temporarily. loadSucceeded then unconditionally sets edits:{} and unsaved:false. This silently discards dirty work on refresh; during in-flight save it also invalidates the save completion. Original TB200 branch returned the same state for this case. Restore that minimal protection; explicit discard/reconcile remain the only intentional recovery paths.

Public hasUnsavedWork returns state.unsaved only. After unknown PUT then edited({}), unsaved=false but commitUncertain=true; closeRequested correctly requests confirmation yet public selector falsely says no work is at risk. Align selector with unresolved/in-flight operation state so later UI cannot bypass the warning. This is local session safety, not a new policy/framework.

Only next task TB-SHIPPING-ADMIN-EDITOR-FRONTEND-ADAPTER-200-R2: session file + focused regressions/docs only,10min/stop6. No decoder/builder/API/backend/UI edits; no replay of accepted fixes. Then review and proceed UI. No task201 while200 unfinished.
