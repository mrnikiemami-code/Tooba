# TASK — TB-SELLER-OFFER-STAGE2-FINAL-PROOF-286

Protocol: TOOBA-OPENCODE-DELIVERY-V1@1.3.0
Parent: TB-SELLER-OFFER-STAGE2-DUAL-ROLE-CREATION-285
Repository: mrnikiemami-code/Tooba
Mode: **VERIFY-ONLY, NO PRODUCT CHANGE**
Authority: Owner explicitly authorized one narrow final proof check 2026-11-23 to unblock Stage 2; this is a standalone **exception**, not an automatic second implementation repair for 285.
Stop: STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.
Reference code: `507d7933a54fe067c240beb6aeb62b63f4b110c0`; 285 evidence `5dd4d7b47aa64e821b19567d73d8bd39ade8c4a5`; review: `docs/opencode/tasks/TB-SELLER-OFFER-STAGE2-DUAL-ROLE-CREATION-285/ARCHITECT-FINAL-REVIEW.md`.

## One bounded objective

Resolve **only** the final proof/provenance blocker for 285 and clarify *late replay after legitimate Offer edit*; make an evidence-based recommendation for architect ACCEPT or BLOCK. No redundant full 8-REQ matrix rerun; its 23 connected results are preserved as historical evidence. **Do not claim any rerun if not executed.**

Read prior final review and source; compile tiny fixture/harness against real Offer Host and isolated PostgreSQL (use existing test infrastructure safely, not live canonical DB). Only run the minimal discriminating test cases below, recording exact request/response/independent GET/SQL, code SHA and environment.

## Fixed atomic acceptance checks (all mandatory; one focused suite)

- **V1 provenance:** the committed test script itself writes the observed JSON directly; rerunning it reproduces all material fields. No manual JSON embellishments. The verifier parses each observed field/value (not just labels/hashes), validates genuine producing-script provenance and rejects a mutated missing/wrong critical fact. Save full raw logs and SHA256 once.
- **V2 conflict:** seller and admin each POST identical requestId+intent twice → 201/201 same OfferId, one Offer row; then same id with changed SKU → exactly 409 `offer.create.intent_conflict`, unchanged original Offer and no second Offer. SQL before/after must also show **no new Pricing/Inventory writes** caused by the conflict (compare actual owning persisted rows/events; no inference from Offer count).
- **V3 concurrency:** two concurrent identical POSTs → same OfferId, one row, no 500, no duplicate Pricing/Inventory effects; use actual concurrent transport and independent read.
- **V4 late replay semantics:** create Offer with requestId+intent A; perform an authorized legitimate Offer-owned edit to SKU or status; replay original requestId+intent A and then same requestId+intent B. Record exact statuses and current/initial persisted values. Decide against original requirement 'resumes saved intent' whether a mutable-state comparison can falsely reject original intent or falsely accept a changed intent. **Do not automatically waive a demonstrated integrity defect.** If durable fingerprint needs migration/contract change, report BLOCKED_ARCHITECT_DECISION rather than edit product code.

All checks run on **one disposable test DB**, with actual Host and read-side; error substitution only if explicitly labeled and necessary. Preserve initial 285 evidence unchanged. No migration, provider/payment, product code, Frontend/Backend, AGENTS, Cursor, roadmap, OpenSpec spec or live data changes. Allowed modifications: new task-owned scripts/checker/evidence and test harness configuration **only** under this task path. Do not rewrite 285 TASK/REQUIREMENTS/RESULT.

## Efficiency + truth gate

- No code implementation, design iterations, speculative extras, repeated builds, screenshots or second full suite. One fixture setup, one focused suite, one checker with a single mutation self-test, one teardown.
- If setup unsafe/unavailable, stop BLOCKED with exact cause and no faux PASS. If any V fails, report BLOCKED with observed facts; do not self-issue a repair task.
- Report **real wall-clock timing** from timestamps of execution: startedAt (ISO-8601), endedAt, elapsedMinutes (e.g. `Task 286 — 45 minutes` **only if actually measured**), plus setup/test/review time if available. Never claim a predetermined time or round it misleadingly.
- Run source diff scope guard; verify canonical services untouched and DB dropped/test ports stopped.

## Deliverables and exit

`docs/opencode/tasks/TB-SELLER-OFFER-STAGE2-FINAL-PROOF-286/`: `TASK.md`, `VERIFY.mjs`, `evidence/*.json|log`, `RESULT.md`, `RECEIPTS.md`, and relevant hashes. A single terminal result: `PASS/READY_FOR_ARCHITECT_REVIEW` **only if V1–V4 succeed and the late-replay contract has no unresolved integrity issue**; otherwise `BLOCKED/NOT_READY` and precise reason. In the worker final chat use at most 8 short lines, include actual elapsed time and commit SHAs.

**The architect alone accepts Stage 2 and may then issue Stage 3. No automatic Stage-3 implementation.**
