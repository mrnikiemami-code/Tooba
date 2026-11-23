# ISO-RULES — OpenSpec (always-on summary)
Sources: `openspec/config.yaml`, task-local change under `openspec/changes/**`, OpenCode delivery method.

MUST:
- Use versioned OpenSpec workflow for OpenCode tasks; do not invent a parallel requirements system.
- REQ IDs in TASK and machine-readable requirements/matrix MUST match.
- A test not run is NOT_RUN, never PASS.
- Missing proof blocks acceptance; it is not proof of product correctness.

When analyzing: map observations to named REQs/invariants if a change/spec is in scope; otherwise state “no OpenSpec change loaded”.
