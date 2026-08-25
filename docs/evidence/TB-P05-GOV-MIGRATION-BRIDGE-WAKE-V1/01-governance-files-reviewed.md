# 01 — Governance Files Reviewed

Task: `TB-P05-GOV-MIGRATION-BRIDGE-WAKE-V1`

| File | Reviewed | Updated | Notes |
| --- | --- | --- | --- |
| `AGENTS.md` | yes | yes | BRIDGE-WAKE-V1 core rules, idle semantics, retired BRIDGE-V2 polling |
| `docs/ai/TOOBA-PIPELINE-PROTOCOL.md` | yes | yes | Canonical protocol rewritten to BRIDGE-WAKE-V1 |
| `docs/ai/TOOBA-PIPELINE-CONTROLLER.md` | yes | yes | IDLE state, Watchdog wake, no idle polling |
| `docs/ai/pipeline-runtime-policy.json` | yes | yes | Wake/watchdog/idle semantics; retired V2 fields |
| `docs/ai/TOOBA-RECOVERY-CONTEXT.md` | yes | yes | Pipeline mode, resume rule, T014 accepted |
| `docs/PROJECT-STATE.md` | yes | yes | Pipeline, issued task, resume block |
| `docs/ROADMAP.md` | yes | yes | P05-14 accepted; P05-GOV-WAKE awaiting accept |
| `docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md` | yes | yes | Recovery start for BRIDGE-WAKE-V1 |
| `docs/prompts/TOOBA-ARCHITECT-NEW-CHAT.md` | yes | yes | Architect bootstrap updated |
| `docs/prompts/TOOBA-CURSOR-PIPELINE-START.md` | yes | yes | Legacy bootstrap notes updated |
| `docs/pipeline/TASK-TEMPLATE.md` | yes | yes | Protocol marker and IDLE return |
| `docs/pipeline/GATE-TEMPLATE.md` | yes | yes | Protocol marker and IDLE return |
| `SETUP.md` | yes | yes | Setup instructions for BRIDGE-WAKE-V1 |
| `docs/ai/tasks/TB-P05-GOV-MIGRATION-BRIDGE-WAKE-V1.task.md` | yes | yes | Persisted from Bridge dispatch |

Historical-only (reviewed, not rewritten as current ops):

| File | Classification |
| --- | --- |
| `docs/ai/tasks/TB-P05-GOV-MIGRATION-BRIDGE-V2.task.md` | historical task artifact |
| `docs/evidence/TB-P05-GOV-MIGRATION-BRIDGE-V2/*` | historical evidence |
| `docs/ai/tasks/TB-P05-T*.task.md` (prior tasks) | historical task artifacts |
| Architecture/product docs mentioning UX polling | unrelated domain polling; not Bridge transport |

No product code, API, schema, or Shopeiva UI files were modified.
