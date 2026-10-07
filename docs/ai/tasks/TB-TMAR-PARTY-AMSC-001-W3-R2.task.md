PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PARTY-AMSC-001-W3-R2
Parent-Task: TB-TMAR-PARTY-AMSC-001-W3-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: RECOVERY_LINEAGE_AND_HISTORICAL_TRUTH_RECONCILIATION_ONLY
Title: Close Party recovery chain, metadata hops, R1 SHA, and historical W0 error-count truth

ARCHITECT VERDICT
Party current production architecture is accepted.

Independent repository verification confirms:
- W2 structure is genuinely PROFESSIONAL_SHALLOW with zero per-use-case request leaf folders.
- Promotion.Infrastructure no longer references Tooba.Party.Application.
- current PartyErrorCodes declares exactly 11 Party-owned stable codes.
- current catalog contributor registers exactly 11 Party descriptors.
- current manifest says IsKnown covers all 11 codes and Contracts-only boundary is current truth.
- W3 remains current certification authority.

Recovery is NOT yet closed because:
- partyAmsc001W0.commit is still PENDING_THIS_COMMIT.
- partyAmsc001W3R1 does not record its own final SHA a75e3bf4390c7950ce8fa0e9462b786aecefe9c1.
- metadata reconciliation commits ee9ba997, f0621ca6, 548a7829 are real handoff commits but not explicit in final SoT lineage.
- historical W0 says CATALOGUED_10_OF_10 although actual catalog already contains 11 stable codes; W1/W3 correctly use 11. Reconcile this additively, do not rewrite history.

STARTING HEAD
a75e3bf4390c7950ce8fa0e9462b786aecefe9c1

CURRENT CERTIFICATION AUTHORITY
TB-TMAR-PARTY-AMSC-001-W3
d1cc2f480619ce1ec68cd730650018883684e6c7
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002
STRUCTURE_CERTIFIED

SEMANTIC WAVES
W0 2477bbb30224c0976b7b02a3d12be9e1b72248db
W1 29012df08590550d3171722895d988bd59e3c2fa
W2 ffff7100bc119a76c472ba77e27a658b1f18f129
W3 d1cc2f480619ce1ec68cd730650018883684e6c7

METADATA/RECOVERY HANDOFFS
W1 metadata ee9ba997ccc2c70009a71dc242a983335d098a19
W2 metadata f0621ca6dd2ba7abb4e2feaa1fbdf5ce2c8eb711
W3 metadata 548a7829e3c40b9158589118e521add67eaec6a7
W3-R1 a75e3bf4390c7950ce8fa0e9462b786aecefe9c1

PRECHECK
- HEAD == origin/main == a75e3bf4390c7950ce8fa0e9462b786aecefe9c1.
- Verify parents: 29012df0<-2477bbb3; ee9ba997<-29012df0; ffff7100<-ee9ba997; f0621ca6<-ffff7100; d1cc2f48<-f0621ca6; 548a7829<-d1cc2f48; a75e3bf4<-548a7829.
- Reconfirm W3 certification facts, shallow structure, 4 requests, validator matrix 2 required + 2 no-validator, Contracts-only both directions, zero foreign App/Infra/Domain coupling, microserviceExtractable true.
- Reconfirm PartyErrorCodes = 11 constants and PartyErrorCatalogContributor = 11 descriptors.
- Reconfirm Promotion.Infrastructure has Party.Contracts reference and no Party.Application reference.
- If conflict: RECOVERY_CONFLICT + STOP.

GLOBAL RECOVERY LOCK
Preserve lastAcceptedTask, lastAcceptedCommit, latestAcceptedImplementationWave, currentHostCheckpoint, nextHostFolder, global workflowStop, global automaticNextImplementationTask exactly.

ALLOWED FILES
- docs/architecture/tmar-current-state.json
- docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
- existing Party W3 cert/recovery guard only if minimal metadata assertion is useful
- docs/architecture/evidence/TB-TMAR-PARTY-AMSC-001-W3-R2/*
- docs/ai/tasks/TB-TMAR-PARTY-AMSC-001-W3-R2.task.md

FORBIDDEN
- production files; Promotion production files; csproj; Host production; frontend; tmar-module-structure-manifests.json; global structure gates; routes/DTO/error behavior; validators/resources; schema/migrations; changing W3 certification truth; guard weakening; baseline widening; unrelated cleanup; next module.

IMPLEMENTATION
- In partyAmsc001W0 replace only unresolved commit placeholder with: commit=2477bbb3, commitFull=2477bbb30224c0976b7b02a3d12be9e1b72248db.
- In partyAmsc001W3R1 add: commit=a75e3bf4, commitFull=a75e3bf4390c7950ce8fa0e9462b786aecefe9c1. Preserve certifiedCommit d1cc2f48 and all certification fields.
- Add partyAmsc001W3R2 block (state=PARTY_AMSC_001_RECOVERY_CLOSED_RECONCILED; full lineage fields per task; workflowStop=USER_REVIEW_PARTY_AMSC_001_W3_R2; automaticNextImplementationTask=NONE). Do not add self-referential PENDING commit for R2.
- Master Recovery must distinguish semantic waves (2477bbb3 -> 29012df0 -> ffff7100 -> d1cc2f48) from actual handoff (2477bbb3 -> 29012df0 -> ee9ba997 -> ffff7100 -> f0621ca6 -> d1cc2f48 -> 548a7829 -> a75e3bf4); state metadata commits are metadata-only, W3 d1cc2f48 remains authority, historical AMC baseline preserved, Host checkpoint preserved.
- Historical W0 error-count reconciliation: preserve original W0 field/history; explicitly record W0 10/10 was stale analysis metadata; actual catalog at W0/W1 already had 11 constants/descriptors; W1 did not add/remove error constants (it added IsKnown/typed seam); current authoritative count = 11/11.

EVIDENCE
Create docs/architecture/evidence/TB-TMAR-PARTY-AMSC-001-W3-R2/: recovery-reconciliation.md, lineage-reconciliation.md, historical-error-count-reconciliation.md, validation.md.
Evidence must prove exact parent chain, W0/R1 SHA before-after, 11 constants, 11 descriptors, W1 did not add/remove constants, Promotion has zero Party.Application reference, zero production/manifest/schema change, W3 authority unchanged, Host checkpoint unchanged.

BOUNDED VALIDATION
- JSON parse SoT; exact SHA assertions; exact parent-chain assertions; exact 11-code/11-descriptor count; Promotion->Party.Application absence; shallow structure spot checks; focused Party guard if modified; git diff scope proof.

PASS CRITERIA
All recovery facts reconciled, W3 authority unchanged, current catalog 11/11, structure still certified/shallow, validator matrix 2+2, Promotion Party.Application coupling zero, production/manifest/schema untouched, Host checkpoint preserved, automatic next NONE.

COMMIT/PUSH
Exactly one R2 reconciliation commit, push main, verify HEAD==origin/main, then STOP.

STOP RULE
STOP completely after result. No R3. No W4. No next module. Wait for Architect review.

END_TOOBA_TASK
