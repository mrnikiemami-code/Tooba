TB-P0-ORDER-PAYMENT-HOLD-COORDINATION-018

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-INVENTORY-ATOMIC-HOLD-EXTENSION-017
Mode: BOUNDED_IMPLEMENTATION_OR_BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: f9d87199d61c39655ea2873f2d4c7e620c62dd50
Timebox: 20 minutes HARD. No automatic next task.

Goal

Add an Order-owned, checkout-bound preparation seam for safe payment initiation that uses the already implemented IInventoryPaymentHoldExtensionPort (017). This is a narrowly bounded building block; do NOT publish a final-click or payment-triggering endpoint in this task.

Mandatory preflight
Verify HEAD == origin/main == expected base; if drift, STOP and report.
Read 014–017 evidence and current implementations, relevant AGENTS.md / architecture guards.
Confirm read-only ownership of checkout, active reservation cycle, exact order-line reservation IDs, current effective policy and supported payment methods. NEVER trust reservation IDs, amount, expiry or checkout identity supplied by browser.
Behavioral contract
Order must authorize actor + cart/checkout ownership and obtain actual persisted reservation IDs for all affected lines; fail closed on missing, duplicated/ambiguous, foreign, incomplete, already consumed or released reservation association. Do not permit a caller-provided list.
Resolve the effective policy for selected payment method, respecting existing Store / Category / Offer / method overrides, manual, wallet and gateway semantics. No fixed TTL, no new floor linking independent settings. If safe deadline cannot be determined, BLOCKED (do not invent it).
Check the Order-owned cycle payability (including wall-clock expiration) and apply 017 Inventory atomic all-or-none extension with an absolute UTC deadline outside any ambient multi-module TransactionScope. If any hold unavailable, stop before payment operation; no fake success.
Payability race: clearly distinguish durable Inventory extension from the Order reservation-cycle status. Re-evaluate or correlate the Order cycle as needed; do not claim a cross-module ACID guarantee. Document what the seam guarantees, what remains a saga/compensation or deadline race.
Must be idempotent and retry-safe, not extend unrelated or foreign checkout reservations. Do not call PSP, debit wallet, create payment attempts or convert Cart in this task.
Hard boundaries
NO Host, Program.cs, Host.Tests, Host config, Host build, Host stop/restart, or :5088 mutation.
NO Frontend, schema/migration, DB live writes, changes to Settings, quota/expiry numbers, or loosening locked guards.
Changes only within Order and existing Contracts seams, and focused tests. If a new cross-module contract/error code or >3 essential production files is required, STOP with exact proposed file set and architectural decision rather than shipping partial code.
Avoid Host.Tests build (locked DLL). One focused Order module test run; do not loop or run broad suites.
No new endpoint or money-moving action until separate Architect acceptance.
Verification
Focused tests: successful checkout-bound selection; foreign/missing reservation rejected before extension; mixed reservations rejected all-or-none; expired cycle rejected; retry does not shorten deadline; method-specific policy not hard-coded. Use fakes for seam correctness; never claim PostgreSQL end-to-end proof from fakes.
Evidence: exact diff and SHA; PASS/BLOCKED; file:line; test commands/results, runtime vs source proof, residual races, no Host edits, automaticNextImplementationTask=NONE.
STOP_FOR_ARCHITECT. Do not start another task.
