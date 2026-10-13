TB-P0-INVENTORY-ATOMIC-HOLD-EXTENSION-017

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-CHECKOUT-TRANSACTION-BOUNDARY-016
Mode: BOUNDED_IMPLEMENTATION_OR_BLOCKED
Timebox: 20 minutes maximum; one bounded module test pass, no loops.
Expected-Base-SHA: 08338e8647eca4b0cac3b5d43486fc6d436c489d (verify HEAD == origin/main before work; if drift, STOP)
automaticNextImplementationTask: NONE

Mission

Implement ONLY an Inventory-owned, atomic, all-or-none extension of existing Held reservation expiry for a future, separately authorized payment-initiation orchestration. This is a building block, not payment authorization. Preserve the existing 015 verification primitive unchanged unless a defect directly blocks this task; report such a defect rather than widening the task.

Invariants and ownership
Inventory owns reservation truth, expiry changes and row locks. Order must later provide authoritative, actor-checked, checkout-bound reservation IDs and the policy-derived desired deadline. This primitive must NOT claim that supplied IDs belong to a checkout.
Exactly one local Inventory-owned PostgreSQL transaction and connection for the operation; no ambient multi-module TransactionScope. No remote PSP call, wallet debit, payment initiation, Order or Cart mutation.
For every distinct requested ID, require an existing row with status Held and currently unexpired as measured by PostgreSQL database time; only if ALL pass can ANY expiry update be committed. Missing, released, consumed, expired, Guid.Empty and empty input fail closed with a structured non-success outcome. Deterministic locking to avoid deadlocks.
Deadline is caller-supplied from EFFECTIVE CONFIGURED policy, in absolute UTC form or a rigorously validated equivalent. Never hardcode a 1-minute, 2-hour or arbitrary duration; reject absent, invalid or already-past finite deadlines. No change to Store/Offer/Category/provider settings or limits.
Existing NULL expires_at denotes a durable reservation: preserve NULL, never shorten existing finite expiry; use max(existing, requested). Never revive a released/expired hold. Replays must be monotonic and safe.
Scope of atomicity must be credible: commit/rollback in one Inventory DB transaction, including rollback if verification or update fails. Do NOT expose a lock held after method return. Avoid hidden DbContext connection ownership changes.
Do not claim this by itself eliminates expiry during PSP processing or makes initiation race-free; later Order/Payment must ensure the deadline window is meaningful and handle late callbacks/compensation.
Permitted scope

Inventory.Contracts/Orders, Inventory.Infrastructure/Directories and module wiring if essential, Inventory.Tests. No Host, Host.Tests, Program.cs, Frontend, Order, Payment, database migrations, schema changes, running DB writes, or changes to runtime settings. No restart, rebuild or stop of the running Host on 5088. No deploying or launching payment activity. Do not alter 015 semantics or any locked architecture guard.

Verification

Use the smallest focused module tests for all-or-none failure, monotonic replay, NULL-TTL preservation, missing/invalid IDs, and deadline validation. A production code implementation dependent on PostgreSQL FOR UPDATE or transactions MUST have a behaviorally credible PostgreSQL verification that does not touch or block the running Development Host. Prefer an isolated disposable DB/container; do not seed the live tooba_alpha DB. If isolation/tooling is unavailable, return BLOCKED with precise proof gap instead of claiming a proven race invariant. One test run for the changed slice; no large solution test and no test retry loops.

Stop conditions

If the task cannot be implemented and verified within the timebox; if migration, error-code expansion, Host/Host.Tests changes, a different module owner or distributed transaction becomes necessary; if the caller deadline cannot be validated without inventing policy; or if any test would overwrite binaries locked by the running Host: STOP_FOR_ARCHITECT, leave no partial unsafe production API. No next task automatically.

Output receipt

BEGIN_TOOBA_WORKER_RESULT with Task-ID, before/after HEAD and commit(s), exact changed-file list, tests run with numbers, transaction and locking behavior evidence, known limitations, and verdict PASS/BLOCKED. STOP_FOR_ARCHITECT. Explicitly state that no payment endpoint/PSP call was added and the Host was untouched.
