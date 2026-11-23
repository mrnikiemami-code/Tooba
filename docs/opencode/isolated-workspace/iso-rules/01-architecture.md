# ISO-RULES — Architecture (always-on summary)
Source of truth (read when planning/editing): `docs/architecture/TOOBA-DELIVERY-ASSURANCE.md`

MUST:
- One authorized task scope; do not drop issued REQ IDs.
- Before production edits: source-grounded ARCHITECTURE-PLAN + DELIVERY.json with real path UI→transport→auth→CQRS/domain→persistence→independent read.
- Small connected waves inside the same task; UI button without owning command is incomplete.
- Worker PASS ≠ Architect ACCEPT ≠ owner acceptance.
- No automatic next task; STOP_FOR_ARCHITECT when required.
- Do not invent new domain abstractions that replace existing Contracts/CQRS/Design System.

When analyzing only (no edits): still name the module boundary and owning contract/path you observed.
