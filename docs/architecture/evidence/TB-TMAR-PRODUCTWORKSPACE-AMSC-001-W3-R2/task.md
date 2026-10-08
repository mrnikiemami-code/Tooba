PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R2
Parent-Task: TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Skill: tooba-architecture-certify
Mode: FRESH_CERTIFY_AFTER_BLOCKER_REPAIR

STARTING HEAD
e5575d68e72171f8bd09458e2fc91a4432de5cb5

AUTHORITIES
Structure: W2 @ 592c346e91d22cb5c31fb50f560a049a4883f19e
Repair: W3-R1 @ f0500c29d9d97e8761f70ce0036452d50bdeefe8
Old W3 c0b86fea is historical/superseded.

GOAL
Fresh independent Certify only. No production repair unless certification truth differs.

VERIFY FROM DISK

HTTP_OWNING, exactly 17 routes.
exactly 17 endpoint-reachable IRequest types = 3 queries + 14 commands.
explicit validator matrix covers all 17 exactly once.
current expected split: 0 VALIDATOR_REQUIRED / 17 NO_VALIDATOR_REQUIRED, but re-verify reasons independently.
zero raw Results.Json/BadRequest/Problem where ApiResponseFactory applies.
exactly two canonical api.Created 201 paths.
W2 structure still PROFESSIONAL_SHALLOW / EXACT / CLEAN / ENFORCED / 5 projects.
Contracts-only foreign boundaries, zero foreign App/Infra/Domain edge, no join/persistence leak.
schema/migrations unchanged; Host business/persistence authority ZERO.
unique error descriptor ownership.
global Host checkpoint unchanged.

IF ANY CHECK FAILS
CERTIFICATION_BLOCKED + STOP. Do not repair in this task.

IF ALL PASS

Add fresh SoT block productWorkspaceAmsc001W3R2 with:
state PRODUCTWORKSPACE_AMSC_001_RECERTIFIED
verdict COMPLETE_REFERENCE_PATTERN
lockVersion ARCH-COMPLETE-002
structureCertified true
structureAuthority W2 @ 592c346e...
repairAuthority W3-R1 @ f0500c29...
supersededCertification W3 @ c0b86fea...
httpApplicability HTTP_OWNING
moduleOwnedRoutes 17
endpointReachableRequests 17
endpointReachableQueries 3
endpointReachableCommands 14
validatorCoverageState exact verified matrix result
apiResultPatternState CANONICAL
rawResultsState ZERO
microserviceExtractable true
productionCodeChanged false
guardsWeakened NONE
baselinesWidened NONE
automaticNextImplementationTask NONE
workflowStop USER_REVIEW_PRODUCTWORKSPACE_AMSC_001_W3_R2
Do not fabricate own commit SHA.

Refresh only ProductWorkspace manifest certificationNote to current truth:
17 routes / 3 queries + 14 commands / exhaustive 17-request matrix / canonical ApiResponseFactory.Created.
No project/allowlist changes.

Add fresh R2 cert guard pinning current truth and authorities.

Append short Master Recovery R2 checkpoint.

Create concise R2 certification evidence.

ALLOWED

SoT
ProductWorkspace manifest certificationNote only
ProductWorkspace cert guards
Master Recovery
R2 evidence/task

FORBIDDEN

production code
structure/project changes
schema/migrations
unrelated modules
guard weakening
baseline widening
next module

VALIDATE
Focused only: ProductWorkspace AMSC guards + ErrorCatalogUniqueCodeGuard + Host.Tests build if needed. No full suite required.

RESULT CONTRACT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R2
Parent-Task: TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R1
Status: PASS | CERTIFICATION_BLOCKED
Summary: <short>
Certification-State: COMPLETE_REFERENCE_PATTERN | BLOCKED
Route-State: EXACT_17 | CONFLICT
Request-State: EXACT_17_3Q_14C | CONFLICT
Validator-Matrix-State: EXHAUSTIVE_17 | CONFLICT
Api-Result-State: CANONICAL_ZERO_RAW_RESULTS | CONFLICT
Structure-State: CERTIFIED_W2_592C346E | REGRESSED
Boundary-State: CONTRACTS_ONLY_ZERO_FOREIGN_APP_INFRA_DOMAIN | REGRESSED
Schema-Migration-State: UNCHANGED | REGRESSED
Host-Checkpoint-State: PRESERVED | REGRESSED
Production-Code-Changed-State: ZERO | NONZERO
Guards-Weakened-State: NONE | NONZERO
Baselines-Widened-State: NONE | NONZERO
Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_PRODUCTWORKSPACE_AMSC_001_W3_R2
STOP
END_TOOBA_WORKER_RESULT

STOP RULE
STOP. No recovery follow-up and no next module. Architect will reconcile final SHA.
END_TOOBA_TASK