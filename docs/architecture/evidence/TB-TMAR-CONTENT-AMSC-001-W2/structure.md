# TB-TMAR-CONTENT-AMSC-001-W2 — Structure

## Verdict

`READY_FOR_CERTIFY`

## Structured state

| Field | Value |
|---|---|
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` |
| Solution-Explorer-State | `CANONICAL` |
| Path-Namespace-State | `EXACT` |
| Physical-Copy-State | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` |
| File-Cohesion-State | `COHESIVE` |
| Structure-State | `READY_FOR_CERTIFY` |

## Capability map (Application)

Capability is the primary axis; `Commands/Queries/Models/Ports/Validators` are secondary:

`Articles/`, `Authors/`, `Categories/`, `Comments/`, `Media/`, `Tags/`, plus genuinely shared
`Composition/ContentOperation.cs` and `Validators/ContentValidationCodes.cs`.

No top-level `Commands` / `Queries` / `Models` / `Ports` technical-axis root exists.

## Single-file leaf audit

Every remaining single-file folder is a **semantic axis folder**, not a per-use-case request folder:
`<Capability>/Models`, `<Capability>/Ports`, `<Capability>/Validators`, `Contracts/Errors`,
`Contracts/Enums`, `Contracts/Storefront`, `Endpoints/Errors`, `Endpoints/Resources`,
`Infrastructure/Persistence`, `Application/Composition`.

Zero use-case-named (`*Command` / `*Query`) leaf folders exist. `Comments/Queries` and
`Media/Queries` are capability axis folders, not use-case folders.

## Path ↔ namespace

All production `.cs` namespaces equal the project + path-derived namespace (`EXACT`). New files added in
W1/W2 (`Contracts/Enums/ArticleCommentStatus.cs`, `Domain/Rules/ContentArticleBodyRules.cs`) comply.

## Root allowlists

| Project | Root `.cs` |
|---|---|
| Contracts | *(none)* |
| Domain | *(none)* |
| Application | *(none)* |
| Infrastructure | `ContentModule.cs` |
| Endpoints | `ContentEndpointModule.cs`, `GlobalUsings.cs` |

Matches `tmar-module-structure-manifests.json` exactly; all `forbiddenRootFiles` absent.

## Solution Explorer

`/Modules/Content/` folder contains exactly the five Content projects; no duplicate or stray entries.

## Semantic Contracts ownership

`Tooba.Content.Contracts` holds boundary semantics only: `Errors/ContentErrorCodes.cs` (single stable-code
owner), `Enums/ArticleCommentStatus.cs`, `Storefront/ContentStorefrontArticleContracts.cs`. Zero
`IRequest`/`IRequestHandler`/`DbContext`/Application or Domain leakage inside Contracts.

## Physical copies

No stale root copy, no duplicate live home; W1 relocations left no residue.

## Durable guard added

`ContentModuleAmsc001W2StructureGuardTests` (8 facts) locks: capability-first axis, no single-file request
leaves, Contracts boundary semantics, single stable-code owner, Endpoints ↛ Domain, root allowlists,
exact path↔namespace, and `/Modules/Content/` solution grouping.

## Validation

`dotnet test --filter ContentModuleAmsc001W2StructureGuard` → **8 passed, 0 failed**.
`dotnet build src/backend/Tooba.slnx` → succeeded, 0 errors.
