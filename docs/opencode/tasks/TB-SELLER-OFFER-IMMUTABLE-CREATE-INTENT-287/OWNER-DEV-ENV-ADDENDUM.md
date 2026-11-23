# Owner decision / execution addendum for TASK 287 — reuse development environment

Date: 2026-11-23. Decision given after repeated 287 disposable DB/Host provisioning failures. Original TASK.md and ACCEPTANCE-MATRIX.json remain immutable. Do not restart task or discard its existing real proof.

**Owner's preference:** On Tooba DEV (no real business data according to owner), prefer using the ALREADY RUNNING development Host and SAME development DB, and clean up ONLY records created by this test. No per-run DB/Host provisioning. This is a workflow choice, NOT a waiver of authorization, data ownership, required schema, or evidence correctness. Refer to `docs/opencode/DEV-HOST-TEST-POLICY.md`.

## For the CURRENT in-flight 287
- If the existing 5094 disposable Host+DB already contains the candidate code and new fingerprint column, **finish the current verified final test + teardown once**. Do not tear down and switch environments merely because this addendum arrived.
- If a fresh run is unavoidable, first inspect whether the common DEV Host/DB contains the exact candidate Offer build and fingerprint schema. If not, it cannot validate this new behavior. Do not restart or redeploy the shared development Host, add migrations to it, reset shared demo catalog, drop DB, or delete any preexisting records merely for 287.
- If a shared dev run is actually compatible and safe, use unique test IDs and precise owned-row cleanup; leave common fixtures intact. For T13 migration/restart and any destructive proof, keep a narrowly isolated already-authorized test fixture; otherwise mark NOT_RUN/BLOCKED, not PASS.
- No new re-provision loop. Record actual results and mismatch, if any. Finish T15 checker, T16 cleanup and source/evidence review; report STOP_FOR_ARCHITECT. This addendum does not alter original 16 test IDs or permit a fake PASS.
