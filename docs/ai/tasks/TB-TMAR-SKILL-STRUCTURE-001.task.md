PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-TMAR-SKILL-STRUCTURE-001
Parent-Task: NONE
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Architecture Skills
Mode: SKILL_DEFINITION_ONLY
Track: ARCHITECTURE_STRUCTURE_SKILL
Title: Create dedicated fourth Tooba architecture skill for physical structure/foldering
Estimated-Time-Minutes: 12
Hard-Timebox-Minutes: 15
ARCHITECT DECISION
Create a fourth independent Cursor skill:
.cursor/skills/tooba-architecture-structure/SKILL.md
The architecture workflow must become four distinct skills:
1. tooba-architecture-analyze
2. tooba-architecture-migrate
3. tooba-architecture-structure
4. tooba-architecture-certify
This task creates ONLY the new Structure skill.
Do not redesign or rewrite the other three skills in this task.
Do not modify production code.
Do not modify module structure.
Do not re-certify any module.
Do not touch frontend.
Do not reopen Host.
PRIMARY PURPOSE
The new skill owns PHYSICAL / VISUAL / FOLDER STRUCTURE quality as a first-class architecture concern.
It exists specifically to prevent false structural PASS states such as:
Application/
  Commands/
    CreateRole/
      CreateRoleCommand.cs
    UpdateRole/
      UpdateRoleCommand.cs
when each leaf folder exists only to wrap one source file.
The skill must enforce professional Visual Studio / filesystem organization, not merely compiling code, namespace correctness, or manifest membership.
MANDATORY PRINCIPLE
CAPABILITY-FIRST, SHALLOW-BY-DEFAULT.
For multi-capability modules, capability is the primary organizational axis.
Technical axes such as Commands / Queries / Validators / Models / Ports are secondary.
Preferred example:
Application/
  Roles/
    Commands/
      CreateRoleCommand.cs
      UpdateRoleCommand.cs
      ArchiveRoleCommand.cs
    Queries/
      GetRoleQuery.cs
      ListRolesQuery.cs
    Validators/
      CreateRoleCommandValidator.cs
      UpdateRoleCommandValidator.cs
  Assignments/
    Commands/
      AssignRoleCommand.cs
      RemoveAssignmentCommand.cs
    Queries/
      ListAssignmentsQuery.cs
    Validators/
      AssignRoleCommandValidator.cs
Non-canonical by default:
Application/
  Commands/
    CreateRole/
      CreateRoleCommand.cs
    UpdateRole/
      UpdateRoleCommand.cs
Application/
  Queries/
    GetRole/
      GetRoleQuery.cs
    ListRoles/
      ListRolesQuery.cs
A per-use-case subfolder is allowed ONLY when that use case genuinely owns multiple cohesive production source files with distinct responsibilities, or its complexity materially benefits from isolation.
Count SOURCE FILES, not declared types.
A folder containing one .cs file is still a single-file leaf even if that file contains:
- request
- handler
- record
- helper type
A single-file leaf folder named after one Command/Query/UseCase is over-foldering unless explicitly justified.
SKILL SCOPE
The Structure skill must inspect and, when the invoking task explicitly authorizes migration/repair, normalize:
- Contracts physical structure
- Domain physical structure
- Application physical structure
- Infrastructure physical structure
- Endpoints physical structure
- Solution Explorer / .slnx grouping
- project membership where relevant
- physical file placement
- path ↔ namespace exactness
- root allowlists
- forbidden root files
- forbidden top-level folders
- stale/duplicate physical copies
- folder depth
- folder explosion
- technical-axis-first trees
- capability-first grouping
- file cohesion as it affects structure
- god-file vs over-splitting balance
- manifest physical structure entries
DO NOT OWN
This skill must NOT become a duplicate of Analyze/Migrate/Certify.
It does NOT own:
- business ownership analysis beyond what is needed to determine capability grouping
- cross-module contract redesign
- CQRS semantics beyond structural placement
- validator semantic coverage
- API result semantics
- localization semantics
- logging/telemetry semantics
- persistence behavior
- schema/migrations
- business behavior redesign
- certification verdict for the whole module
It may REPORT blockers found in those areas but must not absorb those concerns.
MANDATORY REPOSITORY SOURCES
Read before defining the skill:
- AGENTS.md
- docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
- docs/architecture/TMAR-architecture-locks.md
- docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md
- docs/architecture/tmar-current-state.json
- docs/architecture/tmar-module-structure-manifests.json
- current three skills:
  - .cursor/skills/tooba-architecture-analyze/SKILL.md
  - .cursor/skills/tooba-architecture-migrate/SKILL.md
  - .cursor/skills/tooba-architecture-certify/SKILL.md
Reuse existing terminology.
Do not invent a conflicting structure standard.
NEW SKILL REQUIRED FILE
Create exactly:
.cursor/skills/tooba-architecture-structure/SKILL.md
Required frontmatter:
name: tooba-architecture-structure
description: <concise description focused on physical/capability-first structure, Solution Explorer, path↔namespace, root allowlists, over-foldering, god-file balance, manifest structure, and preparing a touched module surface for certification>
REQUIRED SKILL SECTIONS
At minimum include:
1. Mission
2. When To Use This Skill
3. Required Repository Recovery
4. Structure Classification Model
5. Capability Discovery
6. Layer-by-Layer Structure Rules
7. Application Capability-First Rules
8. Single-File Leaf Folder Rule
9. Per-Use-Case Folder Exception Rule
10. Technical-Axis-First Detection
11. Folder Depth / Folder Explosion Rules
12. File Cohesion / God-File Balance
13. Contracts Structure Rules
14. Domain Structure Rules
15. Infrastructure Structure Rules
16. Endpoints Structure Rules
17. Visual Studio / Tooba.slnx Grouping Rules
18. Physical Path ↔ Namespace Exactness
19. Root Allowlist / Forbidden Root Rules
20. Stale / Duplicate Physical Copy Detection
21. Certified Module Preservation
22. Host Final Closure Preservation
23. Manifest Interaction
24. Durable Structure Guard Requirements
25. Focused Validation
26. Required Evidence
27. Completion States
28. Hard Rules
STRUCTURE CLASSIFICATION STATES
The skill must define at least these explicit states:
Folder-Granularity-State:
- PROFESSIONAL_SHALLOW
- OVER_FOLDERED
- TECHNICAL_AXIS_FIRST
- ROOT_DUMP
- OVER_NESTED
- MIXED
Solution-Explorer-State:
- CANONICAL
- MISSING_GROUPING
- WRONG_GROUPING
- STALE_PROJECT_ENTRY
Path-Namespace-State:
- EXACT
- MISMATCH
Physical-Copy-State:
- CLEAN
- STALE_COPY
- DUPLICATE_COPY
Root-Allowlist-State:
- ENFORCED
- VIOLATION
File-Cohesion-State:
- COHESIVE
- OVERSIZED_ONLY
- MULTI_RESPONSIBILITY_COHESION_VIOLATION
- OVER_SPLIT
Structure-State:
- READY_FOR_CERTIFY
- REPAIR_REQUIRED
- BLOCKED
- RECOVERY_CONFLICT
CAPABILITY-FIRST RULE — HARD REQUIREMENT
For multi-capability modules, reject technical-axis-first Application trees by default.
Examples to explicitly treat as suspicious/non-canonical:
Application/Commands/<UseCase>/
Application/Queries/<UseCase>/
Application/Validators/<UseCase>/
Preferred:
Application/<Capability>/Commands/
Application/<Capability>/Queries/
Application/<Capability>/Validators/
Application/<Capability>/Models/
Application/<Capability>/Ports/
Do not mechanically invent capability names.
Capability names must come from actual business responsibility and repository semantics.
SINGLE-FILE LEAF RULE — HARD REQUIREMENT
A directory created only to wrap one production source file must be treated as OVER_FOLDERED unless an explicit, concrete structural reason exists.
Examples:
Commands/CreateRole/CreateRoleCommand.cs
Queries/GetRole/GetRoleQuery.cs
=> OVER_FOLDERED by default.
The following does NOT automatically justify the folder:
- file declares multiple types
- request and handler are in same file
- name matches use case
- existing code happened to use that style
- Visual Studio collapses namespaces acceptably
- tests pass
- manifest accepts the path
PER-USE-CASE EXCEPTION
A deeper leaf may be allowed when it contains multiple cohesive files, for example:
CreateRole/
  CreateRoleCommand.cs
  CreateRoleHandler.cs
  CreateRolePolicy.cs
  CreateRoleMapper.cs
  CreateRoleValidator.cs
Even then, skill must ask whether the deeper grouping is materially clearer than the shallow capability folder.
Do not force flattening when complexity genuinely benefits from isolation.
GOD-FILE VS FOLDER-EXPLOSION BALANCE
The skill must explicitly prevent both extremes:
BAD:
- StoryContracts.cs / AccessControlContracts.cs giant mixed dumps
- StoryDirectory.cs god-file
- one folder for every request containing one file
GOOD:
- cohesive files
- shallow capability folders
- meaningful separation
- no cosmetic splitting to game size guards
- no flattening that recreates god-files
VISUAL STUDIO / SOLUTION EXPLORER
The skill must verify filesystem structure AND Solution Explorer grouping separately.
For module projects, preserve or create canonical .slnx grouping such as:
/Modules/<Module>/
Do not:
- change assembly names merely for visual grouping
- move project files unnecessarily
- create decorative Solution Folders disconnected from disk/project reality
- treat namespace-only organization as sufficient
HOST FINAL CLOSURE GUARD
Preserve:
HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED
HOST_ROOT_FINAL_CERTIFIED
The Structure skill must never use Host as a destination to solve module foldering.
New Host production folders/files are prohibited unless an explicit architecture decision proves genuine:
- HOST_COMPOSITION_ROOT
- GLOBAL_HOST_PLATFORM_BOUNDARY
- canonical generic platform/runtime seam
Any business/module responsibility moved into Host:
HOST_FINAL_CLOSURE_REGRESSION
=> BLOCKED.
CERTIFIED MODULE PRESERVATION
A structure-certified module is not automatically correct forever if the current touched surface demonstrates a concrete structure regression.
However:
- do not broad-rewrite certified modules
- inspect only the touched/current scope
- if a concrete contradiction exists between certification and physical structure, report it explicitly as certification drift
- do not hide the drift by changing the rule or weakening a guard
DURABLE GUARD REQUIREMENTS
The new skill must require structure guards that can machine-detect, as applicable:
- single-file request leaf folders
- technical-axis-first request trees
- forbidden root dumps
- exact path↔namespace
- stale/duplicate copies
- root allowlists
- solution grouping
- forbidden Host growth after final closure
- manifest/project physical consistency
Important:
Do NOT demand one generic regex that blindly rejects all single-file folders in the repository.
Detection must be scoped to semantic request/use-case trees and explicit touched/certified surfaces so legitimate framework/resource/generated/special-purpose folders are not falsely rejected.
FOCUSED VALIDATION
No broad solution-wide tests.
Use only:
- structure-specific guards
- affected project build(s) if paths/namespaces changed
- solution parsing/check if .slnx changed
- module-specific architecture tests
- manifest/recovery guards when modified
No open-ended repair loop.
REQUIRED EVIDENCE
Produce, when invoked on a module/task:
- physical-tree-before.md
- physical-tree-after.md if changed
- folder-granularity.md
- capability-map.md
- solution-explorer.md
- path-namespace.md
- root-allowlist.md
- stale-duplicate-copy.md
- cohesion-balance.md
- manifest-structure.md if applicable
- validation.md
Evidence must list exact offending paths, not generic statements.
COMPLETION SEMANTICS
READY_FOR_CERTIFY only when:
- Folder-Granularity-State = PROFESSIONAL_SHALLOW
- Solution-Explorer-State = CANONICAL or NOT_APPLICABLE
- Path-Namespace-State = EXACT
- Physical-Copy-State = CLEAN
- Root-Allowlist-State = ENFORCED
- no unjustified single-file request leaf folders
- no unjustified technical-axis-first request tree
- no root dump
- no unresolved god-file/over-split structure blocker
- Host final closure preserved
- focused structure guards pass
Otherwise:
REPAIR_REQUIRED or BLOCKED.
IMPORTANT INTEGRATION NOTE
This task creates the fourth skill only.
Do NOT yet remove foldering rules from Analyze/Migrate/Certify.
Do NOT rewrite those three skills to delegate to Structure in this task.
That integration should be a separate bounded follow-up after the fourth skill exists and can be reviewed.
This prevents accidental loss of existing protections.
SUCCESS CRITERIA
PASS only if:
- .cursor/skills/tooba-architecture-structure/SKILL.md exists
- valid Cursor skill frontmatter exists
- skill is independent and clearly scoped
- capability-first / shallow-by-default is explicit
- single-file request-folder rule is explicit
- technical-axis-first detection is explicit
- source-file-count clarification is explicit
- god-file vs folder-explosion balance is explicit
- filesystem + Solution Explorer are both covered
- path↔namespace exactness is covered
- root allowlists are covered
- stale/duplicate copy detection is covered
- Host final closure guard is preserved
- certified-module drift handling is covered
- durable machine-guard requirements are covered
- no production code changed
- existing Analyze/Migrate/Certify files remain byte-for-byte unchanged
- no automatic next task is started
VALIDATION
Verify:
- new SKILL.md parses as Markdown/frontmatter
- grep/read exact new skill
- git diff confirms only the new skill file plus required task/evidence/SoT bookkeeping if repository protocol requires it
- existing three skill hashes unchanged from task start
- no production source touched
RECOVERY / SOT
If this repository records skill work in Recovery/SoT, add only the minimum honest entry for:
TB-TMAR-SKILL-STRUCTURE-001
Do not change module certification state.
Do not change Host checkpoint.
Do not change Story/AccessControl certification state.
EVIDENCE
Create:
docs/evidence/TB-TMAR-SKILL-STRUCTURE-001/
At minimum:
- skill-summary.md
- scope-integrity.md
- validation.md
Persist this exact task as:
docs/ai/tasks/TB-TMAR-SKILL-STRUCTURE-001.task.md
CANONICAL RESULT CONTRACT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-SKILL-STRUCTURE-001
Parent-Task: NONE
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Structure-Skill-State: CREATED | INCOMPLETE
Structure-Skill-Path: .cursor/skills/tooba-architecture-structure/SKILL.md
Four-Skill-State: ANALYZE_MIGRATE_STRUCTURE_CERTIFY_PRESENT
Capability-First-State: EXPLICIT
Shallow-By-Default-State: EXPLICIT
Single-File-Leaf-Rule-State: EXPLICIT
Technical-Axis-First-Rule-State: EXPLICIT
Source-File-Count-Rule-State: EXPLICIT
GodFile-FolderExplosion-Balance-State: EXPLICIT
Solution-Explorer-State: COVERED
Path-Namespace-State: COVERED
Root-Allowlist-State: COVERED
Stale-Duplicate-Copy-State: COVERED
Host-Final-Closure-Guard-State: PRESERVED
Certified-Module-Drift-State: COVERED
Durable-Structure-Guard-State: COVERED
Existing-Analyze-Skill-State: BYTE_FOR_BYTE_PRESERVED
Existing-Migrate-Skill-State: BYTE_FOR_BYTE_PRESERVED
Existing-Certify-Skill-State: BYTE_FOR_BYTE_PRESERVED
Production-Code-Change-State: ZERO
Module-Certification-Change-State: ZERO
Host-Checkpoint-State: PRESERVED
Focused-Validation-State: PASS | FAIL
Evidence-Path: docs/evidence/TB-TMAR-SKILL-STRUCTURE-001/
Commit-SHA: <sha>
Push-State: PUSHED_ORIGIN_MAIN | NOT_PUSHED
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: <state>
User-Work-Preserved: YES | NO
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_SKILL_STRUCTURE_001
STOP
END_TOOBA_WORKER_RESULT
STOP RULE
After PASS/INCOMPLETE:
STOP completely.
Do not integrate the other three skills yet.
Do not repair AccessControl foldering yet.
Do not start another module/task.
Wait for Architect review.
END_TOOBA_TASK
