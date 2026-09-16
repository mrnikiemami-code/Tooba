# Recovery Reconciliation — TB-P11-T001

R13 evidence (`docs/evidence/TB-P10-T022-R13/recovery-sot.md`) is left immutable. It still records Last Architect-accepted before review as TB-P10-T022-R12C because that was worker Result time.

Architect later ACCEPTED TB-P10-T022-R13. Canonical current-state docs now reflect:

| Field | Value |
| --- | --- |
| Phase | P11 — Admin Completion |
| Last Architect-accepted | TB-P10-T022-R13 |
| Last Implementation | TB-P11-T001 |
| Current Issued/Repair | none |
| Appearance USER_VISUAL_ACCEPTED | YES |
| Builder USER_VISUAL_ACCEPTED | NO |
| Locks | LOCK-SF-001…366 |
| Worker | tooba-worker-01 |
| Channel | tooba-main |
| Protocol | BRIDGE-WAKE-V1 |
| TB-P10-T023 | does not exist; must not be invented |
| TB-P11-T002 | not started |

Updated files:

- `docs/PROJECT-STATE.md`
- `docs/ai/TOOBA-RECOVERY-CONTEXT.md`
- `docs/ai/recovery-staleness.guard.test.mjs`
- `docs/architecture/TOOBA-LOCKS.md` (LOCK-SF-363…366)
