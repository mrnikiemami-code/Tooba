# TB-TMAR-PARTY-AMSC-001-W3-R3 — Bounded validation

Scope: W3 Master Recovery descriptor-count truth reconciliation only. No builds, no full tests, no production edits (per task).

## Exact text assertions

1. W3 checkpoint no longer says "10 registered descriptors":
   `rg "10 registered descriptors" docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` → **0 matches**.
2. W3 checkpoint states 11 declared / 11 registered:
   `rg "11 declared Party-owned stable codes .* 11 registered Party-owned descriptors" docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` → 1 match (the corrected W3 canonical-mechanisms bullet).
3. No other stale count variants: `rg "→ 10|-> 10" docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` → 0 matches.

## Source counts (direct)

- `PartyErrorCodes.cs` `public const string` = **11**.
- `PartyErrorCatalogContributor.cs` `D(PartyErrorCodes.` = **11**.

## SoT JSON parse + R2 lineage assertions unchanged

- `node -e "JSON.parse(...tmar-current-state.json)"` → parse OK after the additive R3 block.
- `partyAmsc001W3R2.state = PARTY_AMSC_001_RECOVERY_CLOSED_RECONCILED` unchanged; `partyAmsc001W3R2.w3R1Commit = a75e3bf4…` unchanged; `partyAmsc001W3R1.certifiedCommit = d1cc2f48…` unchanged; `partyAmsc001W0.stableErrorCodeState = CATALOGUED_10_OF_10_MISSING_ISKNOWN_DECLARED_CODE_GUARD` unchanged verbatim.
- New `partyAmsc001W3R3` block present with state `PARTY_AMSC_001_RECOVERY_FINAL_CLOSED`, `w3MasterRecoveryDescriptorState = RECONCILED_11_OF_11`, `historicalW0ErrorCountState = PRESERVED_AS_STALE_HISTORICAL_ANALYSIS_METADATA`, `workflowStop = USER_REVIEW_PARTY_AMSC_001_W3_R3`, `automaticNextImplementationTask = NONE`; no self-referential R3 commit placeholder.

## git diff scope proof

This R3 commit touches exactly:
- `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` (single W3 bullet corrected)
- `docs/architecture/tmar-current-state.json` (additive `partyAmsc001W3R3` closure block only)
- `docs/architecture/evidence/TB-TMAR-PARTY-AMSC-001-W3-R3/*` (this evidence)
- `docs/ai/tasks/TB-TMAR-PARTY-AMSC-001-W3-R3.task.md` (persisted task artifact)

Zero changes under `src/`, zero csproj, zero manifest (`tmar-module-structure-manifests.json` untouched), zero resx/resources, zero migrations, zero guards, zero Host/frontend files. All other dirty/untracked files are pre-existing unrelated artifacts preserved untouched.

## Commit/push

Exactly one R3 reconciliation commit; pushed to `origin/main`; `HEAD == origin/main` verified post-push.
