PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-TMAR-PROMOTION-AMSC-001-W3-R3
Parent-Task: TB-TMAR-PROMOTION-AMSC-001-W3-R2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Skill: tooba-architecture-certify
Mode: FRESH_INDEPENDENT_CERTIFY_AFTER_BOUNDED_REPAIR

STARTING HEAD
b002d4596dbec4851bc77c6810c7c4b3e3a1b08c

AUTHORITIES
W0 Analyze: d00cf666b58ede6c950bdfdd12f95bfd8a0335b3
W1 Migrate: 068589331283aac32c25da62b24b372ac1e61331
W2 Structure: 597147ea8cc3fa4a0f9d6e301133a7237fc68e43
Historical W3 Certify: 6bf747455b0ab0d8468595ecf42fb73a4d15a52b (SUPERSEDED, not an accepted current verdict)
W3-R1 Independent Review: 85d9818d79fa0b1b906c52af16b1a33f1c2149ce (CERTIFICATION_REVIEW_BLOCKED; locale gap)
W3-R2 Accepted Repair: b002d4596dbec4851bc77c6810c7c4b3e3a1b08c (READY_FOR_FRESH_CERTIFY)

MISSION
Fresh independent ARCH-COMPLETE-002 certification of Modules/Promotion from actual disk state. Do not inherit a PASS from historical W3 or infer proof solely from SoT, manifests, old test counts, or prose. Confirm the repaired locale validator, re-derive the entire 21-request matrix, and validate all applicable Certify gates. Preserve all accepted Host closure and other-module locks. One active task only. No automatic next module.

PRECHECK

Verify branch main, git fetch origin, HEAD == origin/main == STARTING HEAD, safe/known working tree. If mismatched or unsafe: RECOVERY_CONFLICT + STOP; do not reset/stash/rebase/force-push.
Read AGENTS.md; four architecture Skills; docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md; tmar-current-state.json; tmar-module-structure-manifests.json; TMAR-architecture-locks.md; TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md; TMAR-HOST-EVACUATION-PROTOCOL.md; W3-R1 review and W3-R2 repair evidence.
Verify the authority commits exist and are in the stated ancestry. Do not silently substitute a SHA.

INDEPENDENT DISK CERTIFICATION MATRIX
A. Applicability: HTTP_OWNING; exactly 21 distinct module-owned routes, Host Promotion routes ZERO; 21 endpoint-reachable distinct MediatR requests and real matching handlers; all endpoints use ISender; no direct persistence/handler invocation or duplicate registration. Count HTTP verbs and literal routes; prove one-to-one mapping rather than only counting Send tokens.
B. Validators: derive an explicit 21-row request-to-route-to-validator matrix from current endpoint request construction, request types and validator registrations. Exactly 19 VALIDATOR_REQUIRED and 2 NO_VALIDATOR_REQUIRED, with concrete reasons. ListMerchandisingCampaignTypesQuery receives user-controlled locale and MUST have discoverable ListMerchandisingCampaignTypesQueryValidator using PromotionValidationCodes.LocaleInvalid and existing BeWellFormedLocale semantics. Both other locale reads remain covered. The ONLY unvalidated requests are ListSellerPromotionsQuery (authorizer-derived scope) and ListAdminPromotionsQuery (no malformed client transport shape); re-check actual endpoint signatures rather than relying on historical claims. Verify DI discovery and validation pipeline and malformed vs valid/absent locale behavior. A mere validator count without 1:1 classification is insufficient.
C. Canonical results: Result/SemanticError and ApiResponseFactory.From/Created; no raw Results.Json, Results.BadRequest, Results.Problem or ad hoc failure mapping; 201 Location behavior preserved. No failure classification by exception/message text.
D. Stable codes/localization: 40 unique declared Promotion codes; 13 HTTP descriptor registrations with unique canonical ownership; 27 domain-invariant codes as evidenced; 40 EN + 40 FA resources, registration exactly once; no unregistered/duplicate descriptors or hard-coded user-facing text. Verify composed uniqueness, not only string counts.
E. Structure: W2 capability-first PROFESSIONAL_SHALLOW, exact physical paths/namespaces, no unjustified single-file use-case leaves, no stale/duplicate copies, project root allowlists ENFORCED, physical Tooba.slnx grouping CANONICAL with six projects, file cohesion/size guards, no ceremonial project.
F. Boundaries: inventory project edges AND production source dependencies in all five non-test project layers, including Endpoints. No foreign Application/Infrastructure/Domain dependencies, cross-module joins, foreign DbContext/DbSet, foreign schema/table reach-through, shared mutable aggregate, or Contracts signature leak. Legitimate foreign Contracts ports remain allowed. Treat microserviceExtractable as ARCHITECTURAL readiness; do not assert runtime deployment proof.
G. Persistence/Host: own PromotionDbContext/schema/outbox and the two existing migration files unchanged in identity and semantics; no schema change. Host composition-root and accepted global security seams only, Host business/persistence authority ZERO. Preserve HOST_ROOT_FINAL_CERTIFIED and global lastAcceptedTask/lastAcceptedCommit/workflowStop, and all closed-folder/manifest locks.
H. Validation baseline: run focused Promotion behavior/architecture tests, new locale tests, ErrorCatalogUniqueCodeGuard and relevant structure/SoT guards. Known unrelated failures in broader global tests must be identified by exact names and compared with clean STARTING HEAD evidence if encountered. Do not weaken tests/guards or widen baselines. No open-ended full-suite retry loop.

IF ANY APPLICABLE GATE FAILS
Report CERTIFICATION_BLOCKED with exact file/line, test evidence and smallest repair proposal. STOP. No production repair, no certification promotion, no unrelated fixes, no next task.

IF ALL PASS

Create new, honest SoT block promotionAmsc001W3R3: task/parent, state PROMOTION_AMSC_001_RECERTIFIED, verdict COMPLETE_REFERENCE_PATTERN, lockVersion ARCH-COMPLETE-002, httpApplicability HTTP_OWNING, structureCertified true, 21 routes/21 endpoint requests, exhaustive 19-required/2-not-required validator matrix, canonical API-result/error/localization state, contracts-only boundary, structural readiness for microservice extraction (runtime unproven), preserved Host checkpoint and schema, guardsWeakened NONE, baselinesWidened NONE, productionCodeChanged false, workflowStop USER_REVIEW_PROMOTION_AMSC_001_W3_R3, automaticNextImplementationTask NONE, and exact W2/W3-R1/W3-R2 authority ancestry. Do not fabricate own cert commit SHA; leave an explicit pending Architect reconciliation marker.
Reconcile ONLY Promotion-specific historical W3 superseded verdict/current certification state in SoT. Preserve historic W3/W3-R1/W3-R2 evidence and global Host accepted pointers. Do not rewrite unrelated program state.
Refresh ONLY Promotion manifest certificationNote/status metadata to verified 19+2 current truth. Preserve its certified module membership, projects and allowlists exactly; preCertModules remains clear of Promotion.
Add a fresh W3-R3 durable cert guard that proves the request/validator set relation, current authorities, schema/Host/manifest lock and canonical outcome; do not weaken or replace existing guard assertions just to get green.
Add concise W3-R3 evidence (certification.md, validation.md), append a short module-local checkpoint to Master Recovery and save this received task under docs/ai/tasks.
Focused build/tests and evidence verification only; commit and push permitted files, verify HEAD == origin/main, report actual SHA in RESULT only, STOP.

ALLOWED FILES

docs/architecture/tmar-current-state.json (Promotion W3-R3 + narrowly scoped Promotion metadata only)
docs/architecture/tmar-module-structure-manifests.json (Promotion certification metadata only)
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md (short Promotion W3-R3 checkpoint only)
src/backend/Host/Tooba.Host.Tests/Architecture/PromotionModuleAmsc001W3R3CertGuardTests.cs (new durable cert guard)
docs/architecture/evidence/TB-TMAR-PROMOTION-AMSC-001-W3-R3/**
docs/ai/tasks/TB-TMAR-PROMOTION-AMSC-001-W3-R3.task.md

FORBIDDEN

Production code, endpoint/DTO/validation/business changes, project/physical structure changes
Schema/migrations, other modules, Host runtime, frontend, accepted global Host checkpoint
Guard weakening, baseline widening, concealment of failing tests
Automatic next task/module, broad refactor, unrelated recovery repairs

RESULT CONTRACT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PROMOTION-AMSC-001-W3-R3
Parent-Task: TB-TMAR-PROMOTION-AMSC-001-W3-R2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | CERTIFICATION_BLOCKED | RECOVERY_CONFLICT
Summary: <brief evidence-grounded outcome>
Certification-State: COMPLETE_REFERENCE_PATTERN | BLOCKED
Route-State: EXACT_21 | CONFLICT
Request-State: EXACT_21_ISENDER | CONFLICT
Validator-Matrix-State: EXHAUSTIVE_19_REQUIRED_2_NO_VALIDATOR_REQUIRED | CONFLICT
Locale-Validator-State: VERIFIED_CANONICAL | CONFLICT
Api-Result-State: CANONICAL | CONFLICT
Error-Catalog-State: UNIQUE_40_CODES_13_DESCRIPTORS_40_BILINGUAL | CONFLICT
Structure-State: CERTIFIED_ARCH_COMPLETE_002 | REGRESSED
Boundary-State: CONTRACTS_ONLY_ZERO_FOREIGN_APP_INFRA_DOMAIN | REGRESSED
Extractability-State: ARCHITECTURAL_READY_RUNTIME_UNPROVEN | BLOCKED
Schema-Migration-State: UNCHANGED | REGRESSED
Host-Checkpoint-State: PRESERVED | REGRESSED
Focused-Validation-State: <exact counts and named failures>
Production-Code-Changed-State: ZERO | NONZERO
Guards-Weakened-State: NONE | NONZERO
Baselines-Widened-State: NONE | NONZERO
Evidence-Path: docs/architecture/evidence/TB-TMAR-PROMOTION-AMSC-001-W3-R3/
Commit-SHA: <actual SHA or NONE if blocked>
HEAD-Equals-Origin-Main: YES | NO
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_PROMOTION_AMSC_001_W3_R3
STOP
END_TOOBA_WORKER_RESULT

STOP RULE
STOP after reporting. Worker PASS != Architect ACCEPT. No next task, no new module; Architect reconciles final SHA separately.
END_TOOBA_TASK
