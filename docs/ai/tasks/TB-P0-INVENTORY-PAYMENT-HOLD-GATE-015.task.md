TB-P0-INVENTORY-PAYMENT-HOLD-GATE-015

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-CHECKOUT-PAYABILITY-ATOMIC-014
Mode: BOUNDED_IMPLEMENTATION_OR_BLOCKED — STOP_FOR_ARCHITECT
Expected-Base-SHA: ba424e68a9f9eca11ede109da2b26b5c7279fed6

Goal

Introduce the smallest Inventory-owned Contracts-only primitive to verify, under PostgreSQL row locks and in a caller-owned ambient transaction, that all supplied reservation IDs are still Held and unexpired. This is a building block, NOT authorization to charge and NOT an end-to-end payability guarantee. No endpoint and no Payment initiation wiring in this task.

Safety / authority
Host root, Host.Tests, Program.cs, the running Development Host on :5088, frontend, database schemas/data, migrations, settings, reservation TTLs, workers, and checkout commit behavior MUST NOT change. No stop/build/restart of live Host. No SQL writes or external provider calls.
Do not weaken guards or invent a typed error. Return a structured boolean/proof result or a fail-closed typed result through an Inventory.Contracts interface; preserve existing Inventory error conventions. No Payment-owned code or new dependency.
Maximum ~20 minutes; do not loop builds/tests. One focused module test run; if blocked by the live Host DLL, STOP rather than kill it.
No changing reservation states, extending TTL, or fabricating journals.
Implementation requirements
Validate repository SHA/clean content diff; inspect Inventory's ReleaseExpiredHoldsAsync implementation and current Contracts style.
Add one narrowly scoped IInventoryPaymentHoldGate (name may follow existing conventions) in Tooba.Inventory.Contracts, with a method accepting nonempty DISTINCT reservation IDs and a timestamp/clock policy; never trust caller-supplied time for validity. Document required ambient transaction. Return false (or a typed unavailable result) if ANY reservation is missing, Released, Consumed, or expired.
Inventory implementation must lock all reservation rows in deterministic order via PostgreSQL FOR UPDATE or an equivalent provider-safe locking primitive, compatible with the reclaimer's FOR UPDATE SKIP LOCKED, and ensure status = Held AND expires_at > database-now (handle NULL TTL according to existing policy). Do NOT silently fall back to an unlocked query. Use same connection/transaction as the caller where possible. If Transaction.Current is absent or expected enlistment cannot be guaranteed, FAIL CLOSED; do not create a misleading independent transaction.
Register the contract in Inventory module DI only. No Host edits.
Focused tests for all-held, one-released, one-expired, one-missing, and multi-line all-or-none; test actual PostgreSQL locking semantics ONLY if an isolated test harness is available without modifying or stopping the running Host. Otherwise mark locking behavior UNVERIFIED, describe a later isolated harness, and STOP_FOR_ARCHITECT if safe implementation cannot be defended.
Clarify explicitly that row locks are released at caller transaction end, and no external PSP call must be made inside that transaction. Do not claim this primitive alone prevents hold expiry after commit.
Completion gate
If safe and guard-compliant: scoped code/tests commit + evidence, exact SHA, files, command/output, remaining race and what Order/Payment must do next. STOP_FOR_ARCHITECT.
If implementation would require changing Host/Host.Tests, a new migration, state-machine changes, or an unprovable lock/enlistment model: NO partial unsafe implementation; report BLOCKED with smallest viable alternative and STOP_FOR_ARCHITECT.
automaticNextImplementationTask=NONE. Never start the next task automatically.
