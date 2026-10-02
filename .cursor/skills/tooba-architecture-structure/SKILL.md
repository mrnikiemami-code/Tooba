---
name: tooba-architecture-structure
description: Normalize Tooba module physical/capability-first structure—Solution Explorer/.slnx grouping, path↔namespace exactness, root allowlists, over-foldering and single-file request leaves, technical-axis-first trees, god-file vs folder-explosion balance, stale/duplicate copies, and manifest structure—preparing a touched surface for certification without owning business, CQRS semantics, or module cert verdicts.
---

# Tooba Architecture Structure

Use this skill when the Architect or task requires **physical / visual / folder structure** quality as a first-class architecture concern for a touched Tooba module surface.

This skill is the fourth architecture skill. The workflow is four distinct skills:

1. `tooba-architecture-analyze`
2. `tooba-architecture-migrate`
3. `tooba-architecture-structure` (this skill)
4. `tooba-architecture-certify`

This skill owns **filesystem + Solution Explorer organization**. It does not absorb Analyze/Migrate/Certify concerns.

## 1. Mission

Make the touched module surface **professionally foldable in Visual Studio and on disk** under ARCH-COMPLETE-002 / `TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD`:

- **Capability-first, shallow-by-default**
- No unjustified single-file request/use-case leaf folders
- No technical-axis-first request trees for multi-capability modules
- Exact path ↔ namespace
- Enforced root allowlists
- Clean physical copies
- Canonical `/Modules/<Module>/` solution grouping when applicable

Compiling, green tests, and manifest membership alone are **not** a structural PASS.

## 2. When To Use This Skill

Use when:

- a Bridge/Architect task authorizes structure definition, inspection, or structure repair/migration;
- Analyze/Migrate left ownership correct but folder layout is cosmetic, over-nested, or technical-axis-first;
- Certify would falsely PASS because path/namespace/compile are fine while Solution Explorer / leaf folders are unprofessional;
- a touched certified module shows concrete structure drift that must be reported or repaired within scope.

Do **not** use this skill to:

- invent business ownership;
- redesign Contracts APIs;
- rewrite validator/result/localization/telemetry semantics;
- issue a whole-module certification verdict (that remains Certify).

## 3. Required Repository Recovery

Before acting, read (when present):

- `AGENTS.md`
- `docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md`
- `docs/architecture/TMAR-architecture-locks.md`
- `docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md`
- `docs/architecture/tmar-current-state.json`
- `docs/architecture/tmar-module-structure-manifests.json`
- sibling skills (for boundary awareness only; do not rewrite them here):
  - `.cursor/skills/tooba-architecture-analyze/SKILL.md`
  - `.cursor/skills/tooba-architecture-migrate/SKILL.md`
  - `.cursor/skills/tooba-architecture-certify/SKILL.md`

Reuse existing terminology (`APPLICATION_CAPABILITY_FOLDERS`, `ENDPOINTS_CAPABILITY_FOLDERS`, `INFRASTRUCTURE_CAPABILITY_INTEGRATION_FOLDERS`, `PATH_NAMESPACE_ALIGNMENT`, `ROOT_ALLOWLIST`, Host final closure keys). Do not invent a conflicting structural standard.

Also establish:

```text
branch = main (or task-authorized branch)
HEAD == origin/main when protocol requires
known/safe working tree
Host checkpoint preserved
```

Unsafe divergence ⇒ `Structure-State: RECOVERY_CONFLICT`.

## 4. Structure Classification Model

Record every applicable state explicitly.

### Folder-Granularity-State

| State | Meaning |
| --- | --- |
| `PROFESSIONAL_SHALLOW` | Capability-first shallow trees; justified depth only |
| `OVER_FOLDERED` | Unjustified single-file request/use-case leaves |
| `TECHNICAL_AXIS_FIRST` | Commands/Queries/Validators primary axis for multi-capability module |
| `ROOT_DUMP` | Capability/implementation files at project root illegally |
| `OVER_NESTED` | Depth without cohesion benefit |
| `MIXED` | Multiple of the above |

### Solution-Explorer-State

| State | Meaning |
| --- | --- |
| `CANONICAL` | `/Modules/<Module>/` (or authorized equivalent) matches disk/projects |
| `MISSING_GROUPING` | Module projects under flat/wrong solution folders |
| `WRONG_GROUPING` | Decorative or incorrect Solution Folders |
| `STALE_PROJECT_ENTRY` | .slnx entry missing/stale vs disk |
| `NOT_APPLICABLE` | Explicitly out of scope for this invocation |

### Path-Namespace-State

| State | Meaning |
| --- | --- |
| `EXACT` | Path-derived namespace equality for production `.cs` |
| `MISMATCH` | Any production mismatch or alias workaround for folder debt |

### Physical-Copy-State

| State | Meaning |
| --- | --- |
| `CLEAN` | One authoritative physical home |
| `STALE_COPY` | Leftover path after move |
| `DUPLICATE_COPY` | Same responsibility in two live paths |

### Root-Allowlist-State

| State | Meaning |
| --- | --- |
| `ENFORCED` | Root matches allowlist/forbidden lists |
| `VIOLATION` | Forbidden root file/folder present |

### File-Cohesion-State

| State | Meaning |
| --- | --- |
| `COHESIVE` | Balanced file responsibilities |
| `OVERSIZED_ONLY` | Large but single-responsibility (WATCH / optional split) |
| `MULTI_RESPONSIBILITY_COHESION_VIOLATION` | God-file / mixed dump |
| `OVER_SPLIT` | Cosmetic split to game size/structure guards |

### Structure-State (overall)

| State | Meaning |
| --- | --- |
| `READY_FOR_CERTIFY` | All READY gates below met |
| `REPAIR_REQUIRED` | Locally repairable structure defects |
| `BLOCKED` | Needs Architect decision / Host closure risk / out-of-scope |
| `RECOVERY_CONFLICT` | Unsafe git/SoT divergence |

## 5. Capability Discovery

Capabilities are **business responsibility axes**, not invented folder names.

Discover from:

- existing capability folders in the module;
- Endpoints audience/capability maps (Admin/Seller/Storefront/…);
- Contracts/Domain aggregates and ports;
- repository maps/SoT (`TOOBA-CAPABILITY-MAP`, module manifests) when present;
- Analyze findings for the same task surface.

Rules:

- Do **not** mechanically invent capability names from Command type names alone.
- Prefer names already used by the module (e.g. `Roles`, `Assignments`, `Articles`).
- Cross-capability shared rules may live under shared `Validators/` / `Validation/` / `Composition/` when repository pattern already uses them.
- Certified modules are **principle references**, not mandatory physical clones.

## 6. Layer-by-Layer Structure Rules

Inspect each project independently:

| Layer | Primary rule |
| --- | --- |
| Contracts | Boundary contracts only; capability folders (`Errors/`, `Enums/`, ports); no Application dump |
| Domain | Aggregates/Enums/Rules/… as existing module pattern; no root god dump |
| Application | Capability-first shallow CQRS placement |
| Infrastructure | Capability/integration folders; Persistence + Persistence/Migrations |
| Endpoints | Audience/capability folders; composition entry at root only |

Solution Explorer grouping is verified **separately** from disk (section 17).

## 7. Application Capability-First Rules

**Hard requirement for multi-capability modules.**

Preferred:

```text
Application/
  <Capability>/
    Commands/
      CreateXCommand.cs
      UpdateXCommand.cs
    Queries/
      GetXQuery.cs
      ListXQuery.cs
    Validators/
      CreateXCommandValidator.cs
    Models/
    Ports/
```

Technical axes (`Commands`, `Queries`, `Validators`, `Models`, `Ports`) are **secondary** and live **under** capability.

Reject by default:

```text
Application/Commands/<UseCase>/...
Application/Queries/<UseCase>/...
Application/Validators/<UseCase>/...
```

when those use-case leaves are unjustified single-file folders or the primary axis is technical.

Single-capability modules may keep a shallower tree if capability is implicit and still professional; do not force empty ceremony folders.

## 8. Single-File Leaf Folder Rule

**Hard requirement.**

Count **production SOURCE FILES** (`.cs` and other production sources), **not** declared types.

A folder that exists only to wrap **one** production source file is `OVER_FOLDERED` when named after one Command/Query/UseCase, even if that file declares request + handler + helpers.

Examples (non-canonical by default):

```text
Commands/CreateRole/CreateRoleCommand.cs
Queries/GetRole/GetRoleQuery.cs
```

These do **not** automatically justify the folder:

- multiple types in one file;
- request and handler co-located;
- use-case-named folder;
- historical style;
- Visual Studio namespace collapse;
- tests pass;
- manifest path accepted.

Legitimate single-file folders **outside** semantic request/use-case trees (framework, Resources, generated, Persistence/Migrations, explicit shared Errors, etc.) must **not** be blindly rejected by a repo-wide regex.

## 9. Per-Use-Case Folder Exception Rule

A deeper leaf **may** be allowed when it contains **multiple cohesive production files** with distinct responsibilities, for example:

```text
CreateRole/
  CreateRoleCommand.cs
  CreateRoleHandler.cs
  CreateRolePolicy.cs
  CreateRoleMapper.cs
  CreateRoleValidator.cs
```

Even then, prefer the shallow capability folder unless isolation materially improves clarity/complexity management. Do not force flattening that recreates god-files.

## 10. Technical-Axis-First Detection

Flag `TECHNICAL_AXIS_FIRST` when Application (or similar) organizes primarily as:

```text
Commands/ | Queries/ | Validators/
```

with per-use-case children, **and** the module has multiple real capabilities.

Detection scope:

- touched Application trees;
- certified surfaces under the invoking task;
- semantic request/use-case trees only.

Do not flag every folder named `Commands` under a capability (`Roles/Commands/` is capability-first).

## 11. Folder Depth / Folder Explosion Rules

- Prefer shallow: capability → technical axis → files.
- Avoid one-folder-per-request explosion.
- Avoid unnecessary extra nesting (`Commands/Roles/Create/CreateRole/...`).
- Empty ceremonial folders are forbidden.
- Depth requires cohesion or complexity justification recorded in evidence.

## 12. File Cohesion / God-File Balance

Prevent both extremes:

| BAD | GOOD |
| --- | --- |
| Mixed `*Contracts.cs` Application dumps | Models/Ports/Exceptions split by responsibility |
| Unrelated multi-responsibility god-files | Cohesive files with clear ownership |
| One folder per request with one file | Shallow capability folders |
| Cosmetic splits to game size guards | Meaningful separation only |

`OVERSIZED_ONLY` (large cohesive adapter) may be WATCH without forcing a structure FAIL if not multi-responsibility. Structure skill may recommend a later split; do not invent parallel architecture.

## 13. Contracts Structure Rules

- Stable boundary types only (`Errors/`, `Enums/`, cross-module ports/DTOs).
- No CQRS request dump in Contracts.
- No root `*Contracts.cs` mixed dump when folders exist for the same content.
- Path ↔ namespace exact.
- Root allowlist empty unless explicitly justified.

## 14. Domain Structure Rules

- Prefer `Aggregates/`, `Enums/`, `Rules/`, `Tenant/` (or module-established equivalents).
- No root god file bundling enums+entities+rules when foldering is required.
- Domain may reference same-module Contracts for shared enums/error codes when repository pattern already does.
- Path ↔ namespace exact.

## 15. Infrastructure Structure Rules

Satisfy `INFRASTRUCTURE_CAPABILITY_INTEGRATION_FOLDERS`:

- Persistence under `Persistence/`; migrations under `Persistence/Migrations/`.
- Directories/Adapters/Authorization/Messaging/Observability/Development as module pattern requires.
- Foreign adapters coherent; no fragmented competing top-level homes.
- Root allowlist typically composition/`*Module.cs` only (+ explicit GlobalUsings if locked).

## 16. Endpoints Structure Rules

Satisfy `ENDPOINTS_CAPABILITY_FOLDERS`:

- Audience/capability folders (`Admin/`, `Seller/`, `Storefront/`, …).
- Root: composition `*EndpointModule.cs` (+ shared `Errors/`, `Resources/` as allowed).
- No capability `*Endpoints.cs` at root.
- No Domain/Infrastructure imports as a structure concern (report if found; ownership repair is Migrate/Certify).

## 17. Visual Studio / Tooba.slnx Grouping Rules

Verify **filesystem** and **Solution Explorer** separately.

Canonical:

```text
/Modules/<Module>/
  Tooba.<Module>.Contracts
  Tooba.<Module>.Domain
  Tooba.<Module>.Application
  Tooba.<Module>.Infrastructure
  Tooba.<Module>.Endpoints
```

Do not:

- rename assemblies merely for visuals;
- move `.csproj` unnecessarily;
- create decorative Solution Folders disconnected from disk;
- treat namespace-only organization as sufficient;
- leave Endpoints project absent from `src/backend/Tooba.slnx` when the project exists on disk.

## 18. Physical Path ↔ Namespace Exactness

- Every production `.cs` namespace must equal the path-derived namespace for its project (`PATH_NAMESPACE_ALIGNMENT` / EXACT).
- Namespace alias workarounds that hide folder debt are forbidden.
- EF `Persistence/Migrations` / designer / snapshot exemptions follow existing repository locks.

## 19. Root Allowlist / Forbidden Root Rules

- Enforce project `rootAllowlist`, `forbiddenRootFiles`, `forbiddenTopLevelFolders` from `tmar-module-structure-manifests.json` when the module is listed.
- Capability/implementation files at root ⇒ `ROOT_DUMP` / `Root-Allowlist-State: VIOLATION`.
- Do not widen allowlists to hide debt.

## 20. Stale / Duplicate Physical Copy Detection

After moves/renames:

- no leftover path with the same types;
- no dual live homes for one responsibility;
- Solution entries must not point at deleted paths;
- report exact offending paths in evidence.

## 21. Certified Module Preservation

- Do not broad-rewrite certified modules.
- Inspect only the touched/current scope authorized by the task.
- If physical structure contradicts an existing `structureCertified` claim on the touched surface, report **certification drift** explicitly.
- Do not hide drift by weakening guards or changing the standard.

## 22. Host Final Closure Preservation

Preserve SoT checkpoints:

- `HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED`
- `HOST_ROOT_FINAL_CERTIFIED`

Hard rules:

- Host is **never** a destination to fix module foldering.
- New Host production folders/files are prohibited unless an explicit Architect decision proves genuine `HOST_COMPOSITION_ROOT`, `GLOBAL_HOST_PLATFORM_BOUNDARY`, or canonical platform/runtime seam.
- Moving business/module responsibility into Host ⇒ `HOST_FINAL_CLOSURE_REGRESSION` ⇒ `Structure-State: BLOCKED`.

## 23. Manifest Interaction

When structure repair is authorized:

- update `docs/architecture/tmar-module-structure-manifests.json` physical allowlists/forbidden lists honestly for the touched module;
- do **not** flip whole-module `structureCertified` unless the invoking task is explicitly a Certify task;
- keep locks vocabulary aligned with ARCH-COMPLETE-002.

This skill prepares for Certify; it does not replace Certify.

## 24. Durable Structure Guard Requirements

When repairing/authorizing durable enforcement, add or extend **scoped** architecture guards that can machine-detect, as applicable:

- single-file request/use-case leaf folders (scoped trees only);
- technical-axis-first request trees on multi-capability modules;
- forbidden root dumps;
- exact path ↔ namespace;
- stale/duplicate copies;
- root allowlists;
- solution grouping (`/Modules/<Module>/`, Endpoints membership);
- forbidden Host growth after final closure;
- manifest/project physical consistency.

**Do not** demand one generic repo-wide regex that rejects all single-file folders (Resources, Migrations, generated, special-purpose folders must remain safe).

## 25. Focused Validation

No broad solution-wide test runs.

Allowed:

- structure-specific guards;
- affected project build(s) if paths/namespaces changed;
- .slnx parse/check if solution grouping changed;
- module-specific architecture tests;
- manifest/recovery guards when those files changed.

No open-ended repair loop. Stop when READY_FOR_CERTIFY, REPAIR_REQUIRED with clear blockers, or BLOCKED.

## 26. Required Evidence

When invoked on a module/task, produce under the task evidence root:

- `physical-tree-before.md`
- `physical-tree-after.md` (if changed)
- `folder-granularity.md`
- `capability-map.md`
- `solution-explorer.md`
- `path-namespace.md`
- `root-allowlist.md`
- `stale-duplicate-copy.md`
- `cohesion-balance.md`
- `manifest-structure.md` (if applicable)
- `validation.md`

Evidence must list **exact offending paths**, not generic statements. Include classification states from section 4.

## 27. Completion States

`READY_FOR_CERTIFY` only when **all** hold:

- Folder-Granularity-State = `PROFESSIONAL_SHALLOW`
- Solution-Explorer-State = `CANONICAL` or `NOT_APPLICABLE`
- Path-Namespace-State = `EXACT`
- Physical-Copy-State = `CLEAN`
- Root-Allowlist-State = `ENFORCED`
- no unjustified single-file request leaf folders
- no unjustified technical-axis-first request tree
- no root dump
- no unresolved god-file / over-split structure blocker
- Host final closure preserved
- focused structure guards pass

Otherwise: `REPAIR_REQUIRED` or `BLOCKED` (or `RECOVERY_CONFLICT`).

After READY_FOR_CERTIFY, hand off to `tooba-architecture-certify`. Do not self-authorize the next Architect task.

## 28. Hard Rules

1. Capability-first, shallow-by-default for multi-capability modules.
2. Single-file Command/Query/UseCase leaf folders are OVER_FOLDERED by default.
3. Count source files, not types.
4. Technical-axis-first request trees are non-canonical by default for multi-capability modules.
5. Do not invent capability names mechanically.
6. Do not flatten legitimate multi-file use-case cohesion into god-files.
7. Do not split cosmetically to game size/structure guards.
8. Filesystem and Solution Explorer are both required.
9. Path ↔ namespace must be EXACT (with locked exemptions only).
10. Host is not a foldering dump; preserve Host final closure.
11. Do not broad-rewrite certified modules; report certification drift honestly.
12. Do not own Certify verdict, validator semantic coverage, Result/localization/telemetry semantics, or schema/behavior redesign.
13. Do not rewrite Analyze/Migrate/Certify skills from this skill unless a separate Architect task authorizes integration.
14. Production behavior and schema remain unchanged unless the invoking task explicitly authorizes otherwise (structure moves must be behavior-preserving).
15. Prefer existing TMAR terminology and locks; do not invent a parallel structure doctrine.
