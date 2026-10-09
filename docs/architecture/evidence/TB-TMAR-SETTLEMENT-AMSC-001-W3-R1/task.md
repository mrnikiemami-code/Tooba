PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-TMAR-SETTLEMENT-AMSC-001-W3-R1
Parent-Task: TB-TMAR-SETTLEMENT-AMSC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Skill: tooba-architecture-certify
Mode: BOUNDED_CERTIFICATION_PROOF_AND_SOT_LINEAGE_REPAIR

REPO
mrnikiemami-code/Tooba

STARTING HEAD (MUST VERIFY)
529b8054ec8cdfe43c0fadf7c2c6436ac8a4a4c2

AUTHORITIES
W0 Analyze: bac4dbe38a46788c8fe27f42820c087294584d6d
W1 Migrate: 4ca4aafc0acc3b5bc62c6cc366a9e50d07b11d05
W2 Structure: 86ebb4ddb0ba85da480f44a7e797cf1791233f2a
Historical W3 Certify: 529b8054ec8cdfe43c0fadf7c2c6436ac8a4a4c2
Use the currently committed certify skill including section 6a. No next module.

PURPOSE
Independently close two verifiable W3 proof defects without re-running AMSC or changing Settlement production:

Existing SettlementModuleAmsc001W3CertGuardTests counts 10 routes and 10 Send sites and checks 10 static request/handler names but does not derive actual route -> handler -> dispatched request mapping. A route can dispatch another known request while counts remain unchanged.
W3 SoT startingHead / W3 guard incorrectly refer to W1 commit 4ca4aafc; actual W3 starting HEAD is W2 commit 86ebb4dd. Correct this as current W3 provenance while preserving immutable historical evidence and real W0/W1/W2 authority.

PRECHECK / STOP

Fetch origin, verify clean safe worktree, exact expected HEAD and authority ancestry. If origin/main has advanced, STOP for Architect unless there is an unambiguous safe fast-forward of documentation-only commits and report it explicitly.
Inspect current W3 SoT, manifest, Master Recovery, W3 certification/validation/matrix, all actual Settlement route handlers, validators, policies and new/old guards before editing.
If any actual production defect or other gate failure appears, report CERTIFICATION_BLOCKED with file/line and STOP. Do not repair production or broaden scope.

REQUIRED VERIFICATION
A. Derive all actual mappings from SettlementEndpointModule audience group prefixes plus EVERY MapGet/MapPost/MapPut/MapDelete route, resolve its handler and actual ISender.Send request construction. Trace direct new T(...) and named local variables; FAIL CLOSED for unknown/ambiguous syntax. Require exact route + verb + handler + dispatched request equality for the currently shipped 10 routes. Count equality alone is NOT sufficient.
B. Derive concrete AbstractValidator<T> target set and prove exact match to the four VALIDATOR_REQUIRED requests; verify six NO_VALIDATOR_REQUIRED exemptions individually by source of bound data and absence of malformable client transport values. Confirm runtime DI discovery and canonical validation behavior. Verify QueryAdminPayoutGridQuery combines its validator with the actually executed AdminPayoutGridQueryPolicy and typed error mapping.
C. Verify 10 request types have 10 real corresponding IRequestHandler implementations and real module-only ISender dispatch; module-owned 10 routes, Host-owned 0. No cross-module App/Infra/Domain edge; no cross-module persistence/join; canonical result, localization, stable descriptors, own schema and unchanged migration.
D. Add a durable guard (prefer new SettlementModuleAmsc001W3R1SetEqualityGuardTests; avoid weakening historical W3 guard) proving exact route -> request mappings from real endpoint source and entire classified set. Include a pure in-memory negative mutation test swapping one dispatch to another known request while keeping send counts unchanged, and demonstrate the new guard detects it. Do not modify production files even temporarily if a pure in-memory mutation suffices.
E. Reconcile the current W3 SoT startingHead to full W2 SHA 86ebb4ddb0ba85da480f44a7e797cf1791233f2a, and repoint the historical W3 guard's corresponding assertion. Preserve historical evidence as an immutable record; write a W3-R1 correction note rather than silently rewriting historical provenance. Ensure no self-referential current commit SHA claim in an as-yet-uncommitted file.
F. Independently verify the three-project manifest entry is intentional (Application, Endpoints, Infrastructure root-rule coverage) while six projects exist in the solution; DO NOT widen manifest solely to force numeric equality.

ALLOWED CHANGES

Add/update Settlement certification guard tests only, including the erroneous W3 startingHead assertion.
Minimal Settlement-specific SoT correction and new settlementAmsc001W3R1 record; optionally Settlement manifest certificationNote only if needed to reflect authority without changing project lists or allowlists.
Append short Settlement W3-R1 correction to Master Recovery.
New task receipt and W3-R1 evidence under docs/architecture/evidence/TB-TMAR-SETTLEMENT-AMSC-001-W3-R1/.

FORBIDDEN

Any production code, route, DTO, CQRS handler, validator, DbContext, schema or migration modification.
Changes to unrelated modules, Host production, frontend, solution grouping, project graph, root allowlists, certified-module membership or global Host checkpoint.
Weakening guards, broadening baselines, concealing known failures, auto-starting another module, rewriting W0-W3 historical evidence.

VALIDATION

Focused SettlementModuleAmsc001 W1/W2/W3/W3-R1 guards; Settlement module tests; ErrorCatalogUniqueCodeGuardTests.
Verify the new negative mutation catches a within-set dispatch swap while legacy count-only checks do not.
Compare any broader global-structure/recovery failures by exact test-name set against the known pre-existing Catalog.Contracts.Cart namespace and two stale TmarDurableGuardTests pins; do not assume unrelated without evidence.
JSON parse; diff scope, production absence, SHA ancestry and HEAD == origin/main post-push.

SUCCESS
REVIEW_PROOF_REPAIRED_READY_FOR_ARCHITECT_ACCEPT (not automatic next module). If any required check cannot be substantiated, CERTIFICATION_BLOCKED + STOP.

RESULT CONTRACT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-SETTLEMENT-AMSC-001-W3-R1
Parent-Task: TB-TMAR-SETTLEMENT-AMSC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | CERTIFICATION_BLOCKED
Summary: ...
Route-Request-Set-State: ...
Validator-Matrix-State: ...
Mutation-Negative-Guard-State: ...
W3-StartingHead-Reconciliation-State: ...
Production-Code-Changed-State: ZERO
Guards-Weakened-State: NONE
Baselines-Widened-State: NONE
Focused-Validation-State: ...
Evidence-Path: ...
Commit-SHA: ...
HEAD-Equals-Origin-Main: YES | NO
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_SETTLEMENT_AMSC_001_W3_R1
STOP
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
