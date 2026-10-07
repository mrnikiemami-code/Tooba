PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PRICING-AMSC-001-W3-R4
Parent-Task: TB-TMAR-PRICING-AMSC-001-W3-R3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: FINAL_RECOVERY_LINEAGE_RECONCILIATION
Title: Close Pricing lineage after fresh INTERNAL_ONLY recertification

ARCHITECT VERDICT
Fresh Pricing recertification is architecturally accepted.

Independent repository verification:

W3-R2 Structure authority = 7159c8f773c1faa9b4b6d425b19067f50ca27572
W3-R3 certification implementation commit = a1ca9b5afab181da74fe6db0efbb38f43a3a9721
post-cert evidence-only follow-up = 127c596aba7abefe6bdd0a1edb7c69c064267762
current main/head = 127c596aba7abefe6bdd0a1edb7c69c064267762
127c596a parent = a1ca9b5a
127c596a changes only docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W3-R3/recovery-debt.md

Therefore certification authority is W3-R3 @ a1ca9b5a. 127c596a is evidence-only and MUST NOT be classified as certification authority.

Open recovery debt:

pricingAmsc001W0 commit/commitFull still PENDING_THIS_COMMIT
pricingAmsc001W1 lacks own final commit/commitFull
pricingAmsc001W2 lacks own final commit/commitFull
pricingAmsc001W3R1 lacks own final commit SHA
pricingAmsc001W3R2 lacks own final commit SHA if not already present
pricingAmsc001W3R3 lacks certification SHA
post-cert evidence hop 127c596a is not classified in SoT lineage

STARTING HEAD
127c596aba7abefe6bdd0a1edb7c69c064267762

CURRENT CERTIFICATION AUTHORITY
TB-TMAR-PRICING-AMSC-001-W3-R3
a1ca9b5afab181da74fe6db0efbb38f43a3a9721
COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002 / STRUCTURE_CERTIFIED

CURRENT STRUCTURE AUTHORITY
TB-TMAR-PRICING-AMSC-001-W3-R2
7159c8f773c1faa9b4b6d425b19067f50ca27572

HISTORICAL / CURRENT LINEAGE
W0 08d47b6a4af3e4e2102712b93f27185ac15a23ac
W1 069f77d2fa5b2c2cec3bb078147c3559a571bd64
W2 f7f6abfec455b771952852e8627df4c57b698caf
superseded W3 3c2cc61e7c61813ac72773ccdb8bb16317cafe70
historical W3-R1 2e664bb336f45b8304818b7e5754f9b0fc364f20
W3-R2 Structure repair 7159c8f773c1faa9b4b6d425b19067f50ca27572
W3-R3 fresh Certify a1ca9b5afab181da74fe6db0efbb38f43a3a9721
W3-R3 evidence-only follow-up 127c596aba7abefe6bdd0a1edb7c69c064267762

PRECHECK

Verify branch main and HEAD == origin/main == 127c596aba7abefe6bdd0a1edb7c69c064267762.
Verify exact parents:
069f77d2 <- 08d47b6a
f7f6abfe <- 069f77d2
3c2cc61e <- f7f6abfe
2e664bb3 <- 3c2cc61e
7159c8f7 <- 2e664bb3
a1ca9b5a <- 7159c8f7
127c596a <- a1ca9b5a
Verify a1ca9b5a is the commit that adds W3-R3 cert guard, promotes Pricing in manifest, updates SoT W3-R3 and Master Recovery.
Verify 127c596a changes only recovery-debt.md.
Reconfirm no Pricing Endpoints project/routes returned and W3-R3 certification truth is unchanged.
If mismatch: RECOVERY_CONFLICT + STOP.

GLOBAL RECOVERY LOCK
Preserve exactly:

lastAcceptedTask
lastAcceptedCommit
latestAcceptedImplementationWave
currentHostCheckpoint
nextHostFolder
repository-global workflowStop
repository-global automaticNextImplementationTask

ALLOWED FILES

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
existing Pricing recovery/cert guard only if a minimal lineage assertion is useful
docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W3-R4/*
docs/ai/tasks/TB-TMAR-PRICING-AMSC-001-W3-R4.task.md

FORBIDDEN

production files
project structure
manifest
Host production
schema/migrations
error/localization behavior
Contracts API
certification semantics
guard weakening
baseline widening
unrelated cleanup
next module

IMPLEMENTATION

Record historical wave self SHAs:
pricingAmsc001W0:
commit = 08d47b6a
commitFull = 08d47b6a4af3e4e2102712b93f27185ac15a23ac

pricingAmsc001W1:

commit = 069f77d2
commitFull = 069f77d2fa5b2c2cec3bb078147c3559a571bd64

pricingAmsc001W2:

commit = f7f6abfe
commitFull = f7f6abfec455b771952852e8627df4c57b698caf

Preserve all other historical fields.

Record historical W3-R1 own SHA:
pricingAmsc001W3R1:
commit = 2e664bb3
commitFull = 2e664bb336f45b8304818b7e5754f9b0fc364f20
Preserve certifiedCommit = 3c2cc61e...
Record W3-R2 own SHA if absent:
commit = 7159c8f7
commitFull = 7159c8f773c1faa9b4b6d425b19067f50ca27572
Record W3-R3 certification SHA:
pricingAmsc001W3R3:
commit = a1ca9b5a
commitFull = a1ca9b5afab181da74fe6db0efbb38f43a3a9721
certificationCommitState = RECORDED_A1CA9B5A
postCertificationEvidenceCommit = 127c596aba7abefe6bdd0a1edb7c69c064267762
postCertificationEvidenceCommitState = EVIDENCE_ONLY_NOT_CERTIFICATION_AUTHORITY

Replace only the previous bridge-only certificationCommitState. Do not alter architecture certification facts.

Add pricingAmsc001W3R4:
task = TB-TMAR-PRICING-AMSC-001-W3-R4
parentTask = TB-TMAR-PRICING-AMSC-001-W3-R3
mode = FINAL_RECOVERY_LINEAGE_RECONCILIATION
startingHead = 127c596a
state = PRICING_AMSC_001_RECOVERY_FINAL_CLOSED
productionCodeChanged = false
currentCertificationAuthority = TB-TMAR-PRICING-AMSC-001-W3-R3
currentCertifiedCommit = a1ca9b5afab181da74fe6db0efbb38f43a3a9721
currentStructureAuthority = TB-TMAR-PRICING-AMSC-001-W3-R2
currentStructureCommit = 7159c8f773c1faa9b4b6d425b19067f50ca27572
w0Commit = 08d47b6a4af3e4e2102712b93f27185ac15a23ac
w1Commit = 069f77d2fa5b2c2cec3bb078147c3559a571bd64
w2Commit = f7f6abfec455b771952852e8627df4c57b698caf
supersededW3Commit = 3c2cc61e7c61813ac72773ccdb8bb16317cafe70
historicalW3R1Commit = 2e664bb336f45b8304818b7e5754f9b0fc364f20
w3R2StructureCommit = 7159c8f773c1faa9b4b6d425b19067f50ca27572
w3R3CertificationCommit = a1ca9b5afab181da74fe6db0efbb38f43a3a9721
w3R3EvidenceOnlyCommit = 127c596aba7abefe6bdd0a1edb7c69c064267762
evidenceOnlyCommitClassification = EVIDENCE_ONLY_NOT_AUTHORITY
actualParentChainState = RECONCILED
certificationAuthorityState = W3_R3_A1CA9B5A
structureAuthorityState = W3_R2_7159C8F7
httpApplicability = NOT_HTTP_OWNING_INTERNAL_CAPABILITY_PROVIDER
endpointProjectState = ABSENT
structureState = CERTIFIED
manifestStructuralState = NOT_TOUCHED_THIS_WAVE
schemaMigrationState = UNCHANGED
globalHostCheckpointState = PRESERVED
guardsWeakened = NONE
baselinesWidened = NONE
workflowStop = USER_REVIEW_PRICING_AMSC_001_W3_R4
automaticNextImplementationTask = NONE

Do NOT add self-referential R4 commit placeholder.

Master Recovery final Pricing closure must distinguish:
Architecture sequence:
08d47b6a -> 069f77d2 -> f7f6abfe -> superseded 3c2cc61e -> historical 2e664bb3 -> Structure 7159c8f7 -> fresh Certify a1ca9b5a

Evidence-only hop:
a1ca9b5a -> 127c596a

State explicitly:

127c596a is NOT certification authority
current certification authority = W3-R3 @ a1ca9b5a
current structure authority = W3-R2 @ 7159c8f7
original W3 @ 3c2cc61e is historical/superseded
Pricing is certified as 5-project INTERNAL_ONLY with no Endpoints project
recovery debt CLOSED
manifest untouched by R4
Host checkpoint preserved
automatic next NONE
Optional minimal durable recovery assertion only if useful:
pin W0/W1/W2 full SHAs, historical R1 SHA, W3-R2 SHA, W3-R3 cert SHA, 127c596a evidence-only classification, current authority, Endpoints absent, Host checkpoint preserved.

EVIDENCE
Create docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W3-R4/:

recovery-reconciliation.md
lineage-reconciliation.md
authority-reconciliation.md
validation.md

Include exact parent chain, a1ca9b5a scope, 127c596a scope, authority classification, before/after SHA fields, zero production/manifest/schema change, Host checkpoint unchanged.

BOUNDED VALIDATION

JSON parse SoT
exact SHA assertions
exact parent chain
exact authority assertions
evidence-only classification
no Endpoints regression
focused recovery guard if modified
git diff scope proof

No broad suite.

PASS CRITERIA

W0/W1/W2 self SHAs recorded
historical W3-R1 own SHA recorded
W3-R2 own SHA recorded if absent
W3-R3 certification SHA = a1ca9b5a
127c596a classified evidence-only
current certification authority exact
current structure authority exact
recovery debt CLOSED
production/manifest/schema untouched
Host checkpoint preserved
automatic next NONE

COMMIT/PUSH
If PASS:

exactly one W3-R4 recovery commit
push main
verify HEAD == origin/main
STOP

RESULT CONTRACT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PRICING-AMSC-001-W3-R4
Parent-Task: TB-TMAR-PRICING-AMSC-001-W3-R3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Starting-HEAD-State: 127C596A | DIVERGED
Production-Code-Changed-State: ZERO | NONZERO
Current-Certification-Authority-State: W3_R3_A1CA9B5A | CONFLICT
Current-Structure-Authority-State: W3_R2_7159C8F7 | CONFLICT
W0-Commit-State: RECORDED_08D47B6A | MISSING | CONFLICT
W1-Commit-State: RECORDED_069F77D2 | MISSING | CONFLICT
W2-Commit-State: RECORDED_F7F6ABFE | MISSING | CONFLICT
Historical-W3-R1-Commit-State: RECORDED_2E664BB3 | MISSING | CONFLICT
W3-R2-Commit-State: RECORDED_7159C8F7 | MISSING | CONFLICT
W3-R3-Certification-Commit-State: RECORDED_A1CA9B5A | MISSING | CONFLICT
W3-R3-Evidence-Only-Commit-State: RECORDED_127C596A_NOT_AUTHORITY | MISSING | MISCLASSIFIED
Actual-Parent-Chain-State: RECONCILED | CONFLICT
Http-Applicability-State: INTERNAL_ONLY_NO_ENDPOINTS | REGRESSED
Pricing-Endpoints-Project-State: ABSENT | PRESENT
Structure-State: CERTIFIED | REGRESSED
Manifest-Structural-State: NOT_TOUCHED | REGRESSED
Schema-Migration-State: UNCHANGED | REGRESSED
Global-Host-Checkpoint-State: PRESERVED | REGRESSED
Guards-Weakened-State: NONE | NONZERO
Baselines-Widened-State: NONE | NONZERO
Recovery-SoT-State: FINAL_CLOSED | STALE | CONFLICT
Evidence-State: COMPLETE | INCOMPLETE
Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | CLEAN_EXCEPT_PREEXISTING_UNRELATED_ARTIFACTS | DIRTY_UNEXPECTED
User-Work-Preserved: YES | NO
Final-Certification-State: COMPLETE_REFERENCE_PATTERN_ARCH_COMPLETE_002_STRUCTURE_CERTIFIED | NOT_CERTIFIED
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_PRICING_AMSC_001_W3_R4
STOP
END_TOOBA_WORKER_RESULT

STOP RULE
STOP completely after result.
No R5.
No next module.
Wait for Architect review.

END_TOOBA_TASK