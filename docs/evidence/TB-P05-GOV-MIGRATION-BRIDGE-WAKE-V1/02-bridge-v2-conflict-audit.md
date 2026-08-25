# 02 — BRIDGE-V2 Conflict Audit

Task: `TB-P05-GOV-MIGRATION-BRIDGE-WAKE-V1`

| Conflict | Location | Resolution |
| --- | --- | --- |
| `PIPELINE-PROTOCOL: BRIDGE-V2` as current ops | `AGENTS.md`, protocol/controller docs, SoT, prompts, templates, `SETUP.md` | **fixed** → `BRIDGE-WAKE-V1` |
| Continuous `GET /api/tasks/next` while idle | `TOOBA-PIPELINE-CONTROLLER.md`, `SETUP.md`, `pipeline-runtime-policy.json` | **fixed** → idle Worker; poll only on wake for claim |
| Permanent online Worker requirement | protocol/controller/AGENTS | **fixed** → offline between Tasks is normal |
| Idle `Waiting` heartbeat | `pipeline-runtime-policy.json`, controller startup section | **fixed** → heartbeat only while `Working` |
| Resume polling after Result | templates, controller, AGENTS | **fixed** → return to IDLE; no polling loop |
| Alert on Worker offline between Tasks | protocol, controller, AGENTS, START-HERE | **fixed** → alert only for real transport failures |
| Missing Watchdog / BRIDGE-WAKE semantics | all operational docs | **fixed** → Watchdog authority documented |
| `pollEndpoint` / `heartbeatIntervalSeconds` as active idle policy | `pipeline-runtime-policy.json` | **fixed** → replaced with wake/watchdog/idleSemantics |
| `TB-P05-GOV-MIGRATION-BRIDGE-V2` evidence describing V2 as current | `docs/evidence/TB-P05-GOV-MIGRATION-BRIDGE-V2/*` | **historical-only** |
| Prior task files with `PIPELINE-PROTOCOL: BRIDGE-V2` | `docs/ai/tasks/TB-P05-T010` … `T014`, gov V2 task | **historical-only** |
| UX/domain "polling" in architecture docs | payment/fulfillment/shopeiva study | **unrelated** — not Bridge transport |

All current operational governance conflicts with BRIDGE-V2 online Worker semantics are resolved or explicitly marked historical-only.
