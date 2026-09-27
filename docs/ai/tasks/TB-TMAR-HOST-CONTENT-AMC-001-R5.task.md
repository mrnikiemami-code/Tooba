PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-CONTENT-AMC-001-R5
Parent-Task: TB-TMAR-HOST-CONTENT-AMC-001-R4
Parent-Commit: 224ec5a3c4741d104a70fd54f4f169024e4d9b74
Governance-Commit: d093ad25aa6bd998909c583af0096d3a11094115
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: RECOVERY_SOT_GOVERNANCE_SYNC
Title: Sync TMAR recovery documents with Content R4 and hardened semantic foldering/Contracts rules

ARCHITECT REVIEW OF R4

R4 is accepted for its bounded Content semantic-structure repair, subject only to recovery-document synchronization.

Verified from repository at commit:
224ec5a3c4741d104a70fd54f4f169024e4d9b74

Verified:

Content Application is capability-first and shallow.
No unjustified one-file Command/Query leaf folders.
Legacy Application *Contracts.cs bundles removed.
Duplicate CQRS command-shaped models removed.
Content.Contracts remains Errors-only after consumer audit.
No foreign module references Content.Application.
R1/R2/R3 boundaries remain preserved.
Content remains ARCH-COMPLETE-002 structureCertified.
tmar-current-state.json already contains hostContentAmcR4.

RECOVERY GAP TO CLOSE

The repository's durable recovery/governance documentation is not fully synchronized with the hardened rules and R4 final state.

At minimum:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md does not yet clearly record Content R4 as the latest accepted Content recovery checkpoint and does not clearly encode the new semantic Contracts + capability-first shallow foldering rule.
docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md still carries an older recovery checkpoint and only generic capability foldering language; it must reflect that destination certification includes semantic Contracts ownership and professional Command/Query folder granularity.
docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md must be checked for consistency with the hardened Skills. If it still permits/encourages one-folder-per-request by ambiguity, update it minimally so the architecture standard and Skills do not contradict each other.
docs/architecture/tmar-module-structure-manifests.json and docs/architecture/tmar-current-state.json must remain consistent with R4; do not rewrite them unless a synchronization correction is actually needed.

REQUIRED SKILLS / SOURCES

Read:

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

Then read:

AGENTS.md
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md
docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
docs/architecture/TMAR-architecture-locks.md
docs/architecture/tmar-current-state.json
docs/architecture/tmar-module-structure-manifests.json
Content R4 task/evidence
governance commit d093ad25aa6bd998909c583af0096d3a11094115

SCOPE

Documentation/governance only.

Allowed:

the recovery/master/protocol/standard documents above
minimal architecture guard/test only if one already exists for recovery-doc consistency and can be extended without production code changes
R5 task/evidence
SoT only if required to record this documentation synchronization

Forbidden:

production code changes
Content code refactor
next Host folder
frontend
schema/migrations
unrelated module recovery
broad historical rewrite

MANDATORY SYNCHRONIZATION RULES

The durable recovery documents must encode, without ambiguity:

Host evacuation = responsibility evacuation, not physical relocation

Host ZERO alone is insufficient.
touched destination must be canonical before PASS.
known certification violations cannot be residual debt.

Semantic Contracts ownership

Tooba.<Module>.Contracts contains stable module-boundary semantics:
cross-module DTOs/ports/events and module-owned stable error codes.
Application-internal CQRS requests/results/snapshots/models/ports stay Application-owned.
filename suffix does not determine ownership.
generic/mixed *Contracts.cs bundles inside Application are not acceptable certification targets.
one authoritative CQRS request type per use case.

Professional capability-first foldering

capability is the primary axis.
shallow default:
<Capability>/Commands,
<Capability>/Queries,
<Capability>/Models,
<Capability>/Ports,
<Capability>/Validators.
do not create a separate leaf folder for each Command/Query when it contains only one request file.
deeper per-use-case folder allowed only for genuine multi-file cohesion or meaningful complexity.
applies equally to Commands AND Queries.
do not flatten legitimate cohesive multi-file use cases.

Reference module is reference, not clone

Offer/other certified modules provide principles, not mandatory folder replicas.
current hardened semantic rules supersede copying an older over-granular physical pattern.

Content latest accepted state
Record Content lineage succinctly:

AMC physical evacuation
R1 destination CQRS/boundary repair
R2 typed-fault/message-classification repair
R3 original structure certification
R4 semantic Contracts + capability-first shallow realignment and re-certification
accepted commit 224ec5a3c4741d104a70fd54f4f169024e4d9b74
governance commit d093ad25aa6bd998909c583af0096d3a11094115
Host/Content ZERO
Content ARCH-COMPLETE-002 structureCertified
validator matrix 17/17 + 34 NO_VALIDATOR
Content.Contracts Errors-only because no foreign Content.Application consumer currently exists
no Endpoints→Infrastructure
Media/Localization Contracts-only
frontend unchanged
DB-gated Content behavior tests skipped without live Postgres is pre-existing/non-blocking

Current workflow stop

no next Host folder started
recovery checkpoint must be USER_REVIEW_HOST_CONTENT_R4_CHECKPOINT or an equivalent canonical post-R5 documentation-sync checkpoint that still requires user review before next Host folder.
do NOT imply next Host folder has started.

MASTER RECOVERY

Update docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md minimally:

add/refresh Content recovery status with R4 as latest accepted state;
include the hardened semantic Contracts/foldering rules in the current recovery method section if absent;
avoid duplicating long Skill text;
keep historical recovery entries intact unless they are factually contradicted by the current state;
make the current checkpoint unambiguous.

HOST EVACUATION PROTOCOL

Update docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md minimally:

preserve folder-by-folder bounded scope;
add the semantic destination-certification rule;
add capability-first shallow Command/Query foldering rule;
add semantic Contracts ownership rule;
update the stale current checkpoint to current Content R4 state;
do not turn the protocol into a copy of the Skills.

COMPLETE REFERENCE STRUCTURE STANDARD

Inspect docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md.

If necessary, minimally harden:

Application standard to capability-first shallow-by-default;
per-use-case subfolders only when justified by multi-file cohesion/complexity;
semantic Contracts/Application ownership;
no mixed Application *Contracts.cs dump;
no duplicate CQRS request shape.

Do not rewrite unrelated sections.

SOT / MANIFEST CONSISTENCY

Verify:

tmar-current-state.json.hostContentAmcR4 is present and honest;
structureLock.certifiedModules includes Content;
manifest Content entry reflects R4;
master/protocol/standard do not contradict SoT or manifest.

Only modify SoT/manifest if an actual inconsistency is found.
Do not create duplicate recovery state.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-CONTENT-AMC-001-R5/

Required:

recovery-sync.md
governance-consistency.md
validation.md
closure.md

VALIDATION

Focused documentation validation:

JSON parse for SoT and manifest
grep/search proving current Content R4 checkpoint appears in recovery docs
grep/search proving both Command and Query shallow-foldering rule is represented
grep/search proving semantic Contracts ownership rule is represented
no stale statement claiming R3 is latest Content checkpoint
no statement implying next Host folder started
existing architecture doc/SoT guards if available

No production build is required unless an existing doc/guard project requires compilation to validate the changes.

PASS CRITERIA

PASS only if:

Master Recovery reflects Content R4 as latest accepted Content state.
Host Evacuation Protocol reflects current R4 checkpoint.
Semantic Contracts ownership is durable in recovery/governance docs.
Capability-first shallow-by-default rule explicitly applies to BOTH Commands and Queries.
One-file-per-request folder explosion is explicitly disallowed unless justified.
Host evacuation responsibility-vs-relocation rule remains explicit.
ARCH-COMPLETE-002 standard is consistent with hardened Skills.
SoT + manifest + recovery docs agree.
Content remains certified; no production code touched.
next Host folder not started.
evidence + task persisted.
commit pushed.
HEAD == origin/main.
working tree clean.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-CONTENT-AMC-001-R5
Parent-Task: TB-TMAR-HOST-CONTENT-AMC-001-R4
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
R4-Acceptance-State:
Production-Code-State:
Master-Recovery-State:
Host-Evacuation-Protocol-State:
Complete-Reference-Standard-State:
Semantic-Contracts-Governance-State:
Capability-First-Foldering-Governance-State:
Command-Query-Rule-State:
Host-Evacuation-Meaning-State:
Content-Latest-Checkpoint-State:
SoT-Consistency-State:
Manifest-Consistency-State:
Next-Host-Folder-Started: false
Focused-Validation:
Remaining-Blockers:
Residual-Debt:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree:
User-Work-Preserved:
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After returning the canonical result:

STOP completely.
Do not inspect/start the next Host folder.
Do not self-issue another task.
Wait for Architect review.

END_TOOBA_TASK