# Folder granularity — AccessControl (W2)

## Classification

| Axis | State |
| --- | --- |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` |
| Technical-axis-first request tree | `ZERO` |
| Unjustified single-file request/use-case leaf folder | `ZERO` |
| Empty decorative folder | `ZERO` (see note) |
| Structure-State | `READY_FOR_CERTIFY` |

## Technical-axis-first detection

`Tooba.AccessControl.Application` has **six** real business capabilities
(`Access`, `Assignments`, `Bootstrap`, `Ceiling`, `Permissions`, `Roles`) plus the shared
`Authorization`/`Composition`/`Development`/`Models`/`Ports`/`Validation` seams.

The primary axis is **capability**; `Commands/`, `Queries/`, `Validators/`, `Models/` appear
**under** capability only. There is no `Application/Commands/**` or `Application/Queries/**`
root. Verdict: `TECHNICAL_AXIS_FIRST = ZERO`.

## Single-file request/use-case leaf folders

Every `Commands/` and `Queries/` directory holds request files **directly**; none contains a
per-use-case subdirectory:

```text
Access/Queries              -> 3 file(s), 0 subdirectories
Assignments/Commands        -> 2 file(s), 0 subdirectories
Assignments/Queries         -> 1 file(s), 0 subdirectories
Bootstrap/Commands          -> 1 file(s), 0 subdirectories
Ceiling/Commands            -> 1 file(s), 0 subdirectories
Ceiling/Queries             -> 1 file(s), 0 subdirectories
Permissions/Commands        -> 1 file(s), 0 subdirectories
Permissions/Queries         -> 3 file(s), 0 subdirectories
Roles/Commands              -> 4 file(s), 0 subdirectories
Roles/Queries               -> 2 file(s), 0 subdirectories
```

`Bootstrap/Commands`, `Ceiling/Commands`, `Ceiling/Queries` and `Permissions/Commands` currently
hold a single production file each, but they are **capability technical axes**, not
use-case-named leaf folders (`Commands/CreateRole/…`). They are therefore legitimate under the
Single-File Leaf Folder Rule (section 8) and are deliberately **not** flattened — flattening
would either delete the technical axis or recreate a capability-root dump.

## Justified non-request folders

| Folder | Files | Justification |
| --- | --- | --- |
| `Application/Development/Seller/` | 3 | cohesive seller-dev capability (query + port + models) |
| `Application/Composition/` | 1 | cross-capability operation/result mapper |
| `Application/Models/` | 1 | cross-capability `AccessOwnerScope` primitive |
| `Application/Ports/` | 1 | module directory port |
| `Application/Validation/` | 3 | cross-capability typed-fault + reusable Fluent rules + validation codes |
| `Endpoints/Errors/` | 2 | shared error catalog contributor + HTTP error mapping |
| `Endpoints/Resources/` | 1 | localized error resource set |
| `Endpoints/Seller/Development/` | 1 | seller-dev endpoint surface nested under its audience |
| `Infrastructure/Persistence/Migrations/` | 4 | EF migration + designer + snapshot (locked exemption) |
| `Infrastructure/Adapters/Security/` | 1 | platform effective-access reader adapter nested under `Adapters/` |

None of these is named after a single Command/Query/UseCase.

## Empty directory note

Three empty `artifacts/` directories exist on disk (`Application/artifacts`,
`Domain/artifacts`, `Infrastructure/artifacts`). They are **untracked build output** (they are
not in git, and the same pattern exists in 23 sibling modules), so they are **not** a
repository structure defect and are excluded from the `Empty decorative folder` verdict.
No tracked empty directory exists in the module.

## Folder depth

Maximum semantic depth is three (`Application/<Capability>/<TechnicalAxis>/<File>`), or four for
the two nested integration surfaces (`Endpoints/Seller/Development/`, `Infrastructure/Adapters/Security/`).
No `Commands/<Capability>/<UseCase>/<File>` explosion.
