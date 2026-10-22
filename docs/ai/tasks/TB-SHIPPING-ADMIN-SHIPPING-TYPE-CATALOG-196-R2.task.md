PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R2
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: d937e32a7d6162fc186b71292efd0a1456272b7a
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_TYPE_DOMAIN_SORT_ZERO_AND_ATOMIC_INVARIANT_REPAIR
Hard-Timebox: 20 minutes HARD from claim through commit/push/result. Stop new coding/testing at minute 15; reserve final 5 minutes. Deadline is a STOP condition, not permission to finish later. PARTIAL if incomplete.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Repair ONLY two concrete R1 regressions in ShippingType domain boundaries: valid sortOrder=0 rejected by Create, and lost translation/identity invariant guards. Add minimal meaningful targeted regressions. Do not absorb missing API/auth/registry/listener/PostgreSQL proof or redesign the capability. TB-196 remains NOT ACCEPTED after this slice.

FIRST CHECK
Fetch and verify HEAD == origin/main == Expected-Base-SHA and inspect dirty/untracked files before edits. Mismatch/unsafe recovery => STOP without edits. Read AGENTS.md, current recovery, TB-196-R1 ARCHITECT-REVIEW.md, .cursor/skills/tooba-backend-feature/SKILL.md and the existing Fulfillment Domain dependency guard. Scope is the existing Domain/Aggregates/ShippingType.cs + directly relevant focused tests/evidence. No physical restructuring or new root needed.
Architect correction: DO NOT add Domain -> Fulfillment.Contracts. Previous request to reuse external typed constants cannot override this module's existing boundary guard. Keep canonical user-input errors in Application/Contracts; Domain retains local invariant enforcement through lawful existing guard/argument-exception conventions. Do not catch or parse argument-exception message text into a business response. No new shared-error abstraction or package.

REPAIR A — INCLUSIVE SORT BOUNDARY
ShippingType.Create currently executes:
ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sortOrder > SortOrderMaxValue ? -1 : sortOrder)
This rejects the declared valid minimum 0. Reuse the same local inclusive 0..1000000 check as Update. Do not change min/max, database CHECK or Application contract. Test Create/Update at 0, 1, 1000000 accepted and -1/1000001 rejected BEFORE mutation. A rejected Update leaves SortOrder, IsActive, Revision and UpdatedAt byte/value-identical.

REPAIR B — ATOMIC LOCAL DOMAIN INVARIANTS
Public ShippingTypeTranslation.Create/Replace must not permit malformed local state merely because callers normally preflight it:

server translation id and LanguageId nonempty at creation;
name non-null, nonblank and <=128; optional description <=512;
validate the COMPLETE proposed pair before assigning either name or description;
AddTranslation must not modify the collection on invalid input or duplicate LanguageId/translation-row identity;
ShippingType.Create must reject null/invalid/duplicate supplied translation entries before constructing an exposed invalid aggregate. Supplied valid entries remain valid; do not add registry/default-language I/O here.
Use existing canonical text preparation semantics and inspect Application ShippingTypePreparation first. Preserve successful Application normalization; do not introduce a competing business policy, silently broaden accepted values, guess languages or move foreign reads into Domain. Local guards may check prepared-value invariants without repeating registry/business orchestration. Reuse existing helpers where lawful; no cross-layer upward reference.
Preserve code immutability/rejection of surrounding whitespace, inactive rev1 creation and existing Application no-op/preflight behavior. Do not introduce another revision change for translation operations or mutate transport/error/resource/schema contracts. Do not make an invalid pair safe by swapping in a new aggregate while the original remains partially changed.

REQUIRED BOUNDED TESTS

Add the sort-zero regression first and run it ONCE against the base: capture failure due to this actual guard. Then implement the bounded repair.
Direct Domain factory/mutator boundary tests for the valid/invalid sort cases; valid Unicode names at128/description512; invalid name/description/id; bad description with a valid new name leaves the SAME translation object and both original values unchanged; invalid/duplicate add leaves SAME aggregate collection unchanged; invalid supplied entry/duplicate in Create rejects. Cover meaningful boundaries with a small theory/table, not one copied assertion per line.
ONE controlled EF InMemory own store or real handler regression creates a type with sortOrder=0 and a lawful default-language fixture, succeeds inactive revision1 and reads it back. This proves the caller path no longer trips the Domain guard; explicitly label it not HTTP/PostgreSQL proof. Reuse the existing test harness/clock/id fixtures; no external service.
Build only Fulfillment.Tests and required project references, using isolated task-owned output if locked. Run the new focused tests and existing ShippingTypeCatalogRulesTests, plus ONLY the already-existing specific Domain->Contracts boundary guard if available by exact test name. No broad --filter Architecture, full module/Host/solution suite, repeated full build or guard-driven layout rework. Commands bounded <=120s. If another unrelated failure appears, record it and STOP/PARTIAL; no exploratory repair loop.

HARD SCOPE / SAFETY
Production changes limited to ShippingType.cs; a minimal directly necessary Application helper reuse is allowed ONLY if it preserves API/error behavior and is explained (no workflow redesign). Otherwise report the exact blocker.
No Endpoint/transport/auth/tracing/DI/schema/EF mapping/migration/designer/snapshot/Contracts/resource/manifest/skill/Host/frontend/Shipping/Storefront edits. No namespace/folder moves. No database/container/server/browser/HTTP listener/network probe/seed/package install/cache purge. Do not build Host or touch shared bin/obj/.next/listeners. No reset/stash/rebase/force-push; preserve unrelated dirty/untracked/stashes.
No assignment/UI, pricing/quote/promise/grouping, carrier/funder, activation or deployment. TB-185/default-OFF/PendingAllocation unchanged. No Bridge dummy/probe payload. Do not re-run R1 or repair its remaining proof by source assertions.

EVIDENCE
Persist this received artifact verbatim at docs/ai/tasks/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R2.task.md. Evidence docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R2/RESULT.md must identify baseline failure, exact fixed guards, tests/commands/counts, changed files, no architecture guard weakening, UTC claim/result and real elapsed time. Append a short correction to R1 RESULT.md: Domain boundary regression and still-missing API/listener/set-equality/PostgreSQL gates; do not rewrite history. Update recovery status pointer, not Architect ACCEPT.
PASS only for these two repaired Domain boundaries with executed required focused proof inside20minutes. PARTIAL if unfinished/time exceeded. R1/TB196 remain PARTIAL_PENDING_API_TRACE_AND_RELATIONAL_PROOF; no certification or product readiness. Commit scoped changes, fast-forward push, fetch verify parity, stop. Do not create next task.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R2
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_TYPE_DOMAIN_SORT_ZERO_AND_ATOMIC_INVARIANT_REPAIR
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: d937e32a7d6162fc186b71292efd0a1456272b7a
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Baseline-SortZero-Regression: ...
Inclusive-Sort-Boundaries-And-Unchanged-RejectedUpdate: ...
Atomic-Translation-Identity-Text-And-Collection-Guards: ...
Domain-Contracts-Boundary-Preserved: ...
Controlled-SortZero-StoreOrHandler-Readback: ...
Focused-Commands-And-Test-Counts: ...
Changed-Files: ...
API-Registry-Listener-Classification-Proof: PENDING_NOT_RUN
Postgres-Snapshot-And-Relational-Races: PENDING_NOT_RUN
TB196-Acceptance: PARTIAL_PENDING_API_TRACE_AND_RELATIONAL_PROOF
Schema-Data-Flag-Changes: NONE
Isolation-And-Cleanup: ...
Claim-UTC: ...
Result-UTC: ...
Elapsed-Minutes: ...
Hard-Deadline-Compliance: ...
Out-Of-Scope-Commands: ...
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK