TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209-R2

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
Worker-Id: tooba-worker-01
Repository: mrnikiemami-code/Tooba
Branch: main
Parent-Task: TB-SHIPPING-ADMIN-USER-TRIAL-FAILURES-209-R1
Expected-Base-SHA: 2de29283a9d32739607d7d8be26666a19bb82a2a
Mode: BOUNDED_GROUP_AND_ROW_SCOPED_ERROR_FOCUS_CORRECTION
Hard-Timebox:10 minutes
Stop-New-Implementation:minute6; final4 check/evidence/commit/push
Automatic-Next-Task:NONE
Stop-State:STOP_FOR_ARCHITECT

Exact task; no expanded scope

R1 visual/view/cost-layout improvements accepted boundedly; live save already accepted209.
Architect viewed all4PNG and inspected actual source; remaining genuine defect:

server-field-error.parseProperty discards regionGroups[i] and carries only rows[j].
ServerFieldIssue has rowIndex but no group identity.
screen serverRateErrors uses Map<number,string> rowIndex-only and all groups receive it.
focus queries document-global repeated testId by rowIndex.
dedup key lacks group.
Thus regionGroups[1].rows[0].freightAmount targets group0/row0 and duplicates can collapse across groups.
Optional-condition focus also fails when its details disclosure is closed.
Fix ONLY this nested ownership/placement/focus wiring. No visual redesign/picker work, no new framework.

Read AGENTS, TOP recovery,209-R1/ARCHITECT-REVIEW.md and frontend feature skill/standard.
Verify safe main/origin, preserve understood docs-only descendants/unrelated dirt; persist task verbatim.
Local-dev authorization persists; this task is frontend-only ZERO domain writes.

Required correction
Preserve BOTH groupIndex and rowIndex from actual submitted property path into ServerFieldIssue,
label/dedup/focus metadata. Do not assume all errors belong to first group or flatten local row indices.
Keep exact-code reason mapping and unknown-code honesty unchanged.
Screen/groups-editor use composite group+row ownership to place a reason ONLY on intended row.
Resolve submitted index against current ordered groups/rows, keeping ids/local keys where appropriate;
no another grouping store. Guard absent/out-of-range indices honestly; do not redirect to row0.
Focus scope is intended group element -> intended row -> actual input testId. No globalNth shortcut.
If field is within closed details, open its containing disclosure before focus; presentation only,
never edits values/dirty/revision. Do not open other groups' conditions.
Dedup includes group+row and actual input/field, so two groups' row0 errors are both retained.
Two-group regression: group0 has>=2rows, group1 has>=1row.
group1.row0.freight points ONLY group1.row0; group0.row1 delivery remains correct;
equal code/row0 across group0+1 yields TWO issues; out-of-range safe; optional-condition target opens
its own disclosure via the same focus helper the screen calls. Use minimal existing/pureDOM helper
mechanism; no React/browser framework or package install.
Validation / no replay

Run only new/changed focused error regressions and npm run typecheck (once after correction).
No full feature suite, backend/solution/architecture sweep, browser/runtime startup, screenshots,
HTTP mutation/probe, save replay, seed/migration/rebuild/Host restart. Do not touch realrev2 draft.
One correction maximum; stop at timebox with exactPARTIAL if blocked, no finishing loop.
Keep general/cost layout, view guards, session/unknown/Apply semantics, type/catalog and runtime untouched.

Result / recovery

209-R2/RESULT.md with actual changed paths, focused results, exact ownership/focus guarantees and limits,
actual claim/stop/finish (do not invent timing). Append209-R1RESULT correction for the prior “exact row” overclaim.
TOP recovery+AGENTS current worker pointer. Commit scoped files, push/fetch parity.
No userVisual/fullproductionacceptance; no newtask emitted. Stop for Architect; then user retest,
not another visual/proof loop if this defect is fixed.
