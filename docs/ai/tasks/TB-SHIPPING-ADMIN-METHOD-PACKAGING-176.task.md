PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-METHOD-PACKAGING-176
Parent-Task: TB-SHIPPING-ADMIN-METHOD-SCHEDULE-175
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: cf0438651ddba756847ba21c04cc3bd788927a65
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_METHOD_SCOPED_PACKAGING_TAB_PERSISTENCE_AND_READONLY_REPAIR
Hard-Timebox: 45 minutes; stop and report PARTIAL rather than exceed the limit.

MISSION
Finish the actual flat method detail tab «بسته‌بندی و کرایه» for a single canonical shipping method. Repair the known TB-175 gap: View mode currently lacks readOnly wiring for Packaging. Ensure method-specific packaging/freight policy can be read, safely edited when authorized, saved, re-read and reopened without modifying sibling/provider records or implying unsupported customer quote effects. This is implementation/verification, NOT another wireframe or report-only task.

MANDATORY FIRST CHECK

Verify HEAD == origin/main == Expected-Base-SHA, inspect relevant dirty/stashed files without touching them. If divergence, STOP_FOR_ARCHITECT.
Read docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md (locked addendum), docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md, TB-173/174/175 outcomes; inspect existing Shipping Packaging owner Contracts/queries/commands/endpoints/EF and frontend flat-method-detail-dialog + packaging tab API/model.
Map true canonical identity via authoritative server binding (serviceCode+optionCode); NEVER infer by splitting methodCode in browser. Distinguish provider/shared legacy rows from exact-method owned rows. Check whether an owner write seam already exists; reuse it rather than duplicating routes.

IMPLEMENTATION SCOPE
A. Pass readOnly from the View/Edit mode in flat-method-detail-dialog into Packaging tab. All inputs and mutate buttons disabled in View; guard EVERY client write handler early, including save, add, remove, activate/deactivate, and all fallback handlers. Display compact Persian read-only status using existing design tokens, RTL, four non-nested tabs, three list-row actions preserved.
B. On Edit, ensure existing packaging policy is loaded from owner read response and bound ONLY to exact (serviceCode,optionCode). Save through existing authorized Shipping-owned writer, then authoritative re-read and reopen. Fail closed on unresolved binding, denied/unavailable/mismatched response, stale/ambiguous identity, without optimistic success. If owner API is actually service-wide only, do NOT fake method isolation: document precise limitation and implement smallest owner-correct additive seam only if safely achievable in this slice; otherwise PARTIAL.
C. Clearly separate carrier packaging/operational data from customer shipping charge and from the unresolved shipping fee allocation. No invented carrier cost, 0/FREE, settlement share or customer promise; explain if policy has no actual storefront consumer today. Do not change quotes, payment, payouts or carrier calendar.
D. Preserve existing working fields/behaviour and responsive layout; no global CSS redesign, no extra nested tabs, no legacy parent/child surface reinstatement. Prefer small modules and focused changes; no source-size baseline rebaseline.

ACCEPTANCE / REQUIRED PROOF

Focused frontend tests for View-mode readOnly (no write API invocation for every mutation route); Edit mode can use existing writer; exact method mapping and malformed-binding fail closed; response re-read; sibling/provider isolation; npm run typecheck.
Focused Shipping backend owner tests only where relevant; prove no sibling policy borrowed and exact option validation. Avoid broad redundant suites.
In disposable loopback scratch only, using TB-157/166/173/174 recipes as needed: sanctioned Development Admin authorization; existing method tipax:standard edit and persisted GET/readback + tipax:express unchanged; ideally real private browser Edit -> save -> full reload -> View disabled proof with genuine screenshot. Preserve shared instances and data unchanged. If live environment cannot be assembled in time, report PARTIAL clearly, not PASS.
Inspect real owner-to-storefront/operational consumer paths and state truthfully what changes customer-facing amounts or scheduling; no speculative assertions.
Record exact source files, test counts, command outputs, authorized HTTP statuses, isolation guard, cleanup and screenshots only if genuine. Ensure latest HEAD/origin parity and scoped worktree hygiene.

SAFETY / STOP
ONE task only. Keep :3000/:5088/:5432, tooba_alpha/store-alpha, shared Redis/RabbitMQ, running Host, unrelated dirty Admin file, TB-075/079 logs and existing stashes untouched. No restarts, shared DDL, auth bypass, real PSP/SMS/carrier/payout, force push, stash/reset/rebase, Results.Json, Host business logic or cross-module DbContext dependency. Existing architecture: MediatR/ISender, Result, Contracts-only crossings. No automatic next task. Stop within HARD 45 minutes. Verdict PASS only if safely persisted method-specific edit/read/reopen + genuine read-only guard are verified; else PARTIAL/BLOCKED with precise blocker.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-METHOD-PACKAGING-176
Parent-Task: TB-SHIPPING-ADMIN-METHOD-SCHEDULE-175
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Status: PASS|PARTIAL|BLOCKED
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Scope-Delivered: ...
Focused-Tests: ...
Real-Admin-Read-Write-Proof: ...
Sibling-Isolation: ...
View-Mode-ReadOnly: ...
Customer-Effect-Provenance: ...
Open-Gaps: ...
Isolation-And-Cleanup: ...
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK