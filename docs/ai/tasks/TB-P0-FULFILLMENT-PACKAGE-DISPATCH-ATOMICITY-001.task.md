PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-P0-FULFILLMENT-PACKAGE-DISPATCH-ATOMICITY-001
Parent-Task: TB-FE-TYPECHECK-155-ROOT-CAUSE-AUDIT-001
Program: TOOBA-FULFILLMENT-DISPATCH-INTEGRITY-RECOVERY
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: IMPLEMENT_BOUNDED_P0_BACKEND_REPAIR_WITH_STOP_GATES
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Starting-HEAD: 4b4754455cb7053a5dcfb4a1fa7636f4641c1f7f
Priority: P0

MISSION
Repair a deterministic collision in generated consolidated-package member shipment tracking references AND prevent partial persisted dispatch of a consolidated package. Investigate the already-partially-dispatched package; do NOT mutate production data. Keep scope narrow. Report honest PASS/BLOCKED per independent gate.

SAFETY / PREFLIGHT

Verify HEAD == origin/main == expected SHA, clean conflicting tracked worktree. STOP if divergent.
Read AGENTS.md, relevant backend skill/architecture standards and locks, and locate actual files via repository search (do not assume paths). Read FulfillmentDirectory.Packages.cs, AssignTrackingCoreAsync, FulfillmentDbContext, ConsolidatedPackage, orchestrator, persistence/UoW and relevant tests.
NO calls that dispatch/cancel/modify the live order 01a12e97-ee9e-7000-826c-a9eca536b223, its consolidated package 01a12f79-6ef1-7000-8578-dd7cc841195d, or member shipments. NO live writes, direct SQL data repairs, or production migrations.
No weakening tracking uniqueness enforcement; no arbitrary tenant scoping of uniqueness. Do not touch F3/F4, UI, auth, or unrelated findings.

GATE A — DETERMINISTIC TRACKING COLLISION (F1)

Confirm UUIDv7 N[..8] prefix collisions for the given two member IDs and identify every generation/use of member tracking codes, existing codes, length/format constraints and backwards compatibility.
Implement smallest deterministic collision-resistant member reference using enough shipment identity to guarantee uniqueness within valid domain constraints; prefer full UUID N (or mathematically guaranteed stable unique representation) if length permits. Last eight chars alone is NOT a proof of uniqueness. Honor unique index and carrier formatting constraints. NEVER rewrite existing tracking codes on an already-dispatched member.
Add focused tests for two different UUIDv7 IDs sharing first eight hex digits, retries/idempotence and existing reference preservation. No assertions reliant on chance/randomness.
If guarantees cannot be proven without changing external carrier/DB contract: BLOCK GATE A and explain choices.

GATE B — ATOMIC PACKAGE DISPATCH (F2)

Trace actual SaveChanges, EF tracking, ambient transaction, domain-event/outbox and external carrier calls. Show where first member is persisted before second fails.
Implement one atomic DB commit for ALL member status/tracking updates and package transition, or use a proven existing UoW/transaction pattern in-repo. Ensure rollback on failure preserves original member+package states and no interim committed partial dispatch. Avoid wrapping irreversible external side effects in a DB transaction: if present, BLOCK for architecture decision instead of claiming atomicity.
Add focused failure injection test (second-member duplicate/error) verifying DB rollback and unchanged package/member states; successful two-member path and idempotent retry. Do not silently skip previously dispatched members or invent state transitions.
If change cannot be bounded or safe with existing repository transaction semantics, STOP Gate B with precise implementation plan, no unsafe transaction hack.

GATE C — EXISTING PARTIAL PACKAGE RECOVERY (DESIGN / NO LIVE MUTATION)

Analyze whether existing package status Created + one member Dispatched is representable and recoverable through existing public commands. Evaluate cancellation invariants and shipment/package reconciliation. Provide a concrete safe runbook requiring explicit Architect/user approval before ANY mutation.
NO automatic data repair, cancellation, reset of Dispatched status, tracking-code rewrite, or database cleanup. If no safe public command exists, explicitly mark BLOCKED_RECOVERY_REQUIRES_ARCHITECT_DECISION.

VERIFICATION

Run only relevant focused backend tests, compile/build only the affected backend project if feasible, and relevant invariant/architecture guards, with raw outputs. NO AMSC, no broad test sweeps, no repeat loops. If gates blocked, still report diagnostic evidence.
Document precise changed files, before/after behavior, pending deployment/migration, limitations and rollback. If implementing, commit and push to main ONLY after all implemented gates pass focused tests; do not call live Host write operations. Do not claim the current stuck package has been fixed by a code deployment.
Task receipt: docs/ai/tasks/TB-P0-FULFILLMENT-PACKAGE-DISPATCH-ATOMICITY-001.task.md
Evidence: docs/architecture/evidence/TB-P0-FULFILLMENT-PACKAGE-DISPATCH-ATOMICITY-001/
Final result must distinguish Gate A, B and C, include HEAD, tests, outstanding risks, and STOP for independent Architect acceptance. NO automatic next task.
END_TOOBA_TASK
