# Governance files reviewed

Task: `TB-P05-GOV-MIGRATION-BRIDGE-V2`

| Current operational file | Inspected | Changed | Outcome |
| --- | --- | --- | --- |
| `AGENTS.md` | yes | yes | Roles and execution authority are Bridge-V2 and agent-neutral |
| `README.md` | yes | yes | Project summary identifies Bridge-V2 flow |
| `SETUP.md` | yes | yes | Manual chat/prompt paste replaced by Bridge Worker setup |
| `docs/PROJECT-STATE.md` | yes | yes | P05, accepted T009/repair, migration review state, and held T010 synchronized |
| `docs/ROADMAP.md` | yes | yes | Accepted history and held/reissue sequencing synchronized |
| `docs/ai/TOOBA-PIPELINE-PROTOCOL.md` | yes | yes | Replaced by canonical Bridge-V2 protocol |
| `docs/ai/TOOBA-PIPELINE-CONTROLLER.md` | yes | yes | Replaced chat controller with Bridge lifecycle |
| `docs/ai/TOOBA-RECOVERY-CONTEXT.md` | yes | yes | Recovery now uses Bridge channel and current P05 state |
| `docs/ai/pipeline-runtime-policy.json` | yes | yes | Runtime policy now models Bridge-V2, Worker mutex, heartbeat, polling, review, and alerts |
| `docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md` | yes | yes | Recovery start is repository + Bridge based |
| `docs/prompts/TOOBA-CURSOR-PIPELINE-START.md` | yes | yes | Legacy prompt reduced to a retired notice and canonical pointers |
| `docs/prompts/TOOBA-ARCHITECT-NEW-CHAT.md` | yes | yes | Architect bootstrap is Bridge-V2 and agent-neutral |
| `docs/pipeline/TASK-TEMPLATE.md` | yes | yes | Bridge-V2 downloadable Task template |
| `docs/pipeline/GATE-TEMPLATE.md` | yes | yes | Bridge-V2 Gate template |
| `docs/architecture/00-technical-inventory.md` | yes | no | Historical P00 inventory; no current controller instruction |

No governance/doc validation script existed in the repository. Validation is
therefore the explicit retired-rule search, canonical-marker search, JSON parse,
Markdown evidence review, `git diff --check`, and Git synchronization recorded
by this Task.

Historical preservation:

- `docs/ai/tasks/**` was not mass-edited;
- `docs/evidence/**` was not mass-edited;
- accepted architecture, ADRs, product requirements, Shopeiva decisions, and
  phase history were not rewritten;
- the received Bridge task was persisted as
  `docs/ai/tasks/TB-P05-GOV-MIGRATION-BRIDGE-V2.task.md`.
