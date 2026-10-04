# Folder granularity — AccessControl (W0)

## Classification

| State | Verdict |
| --- | --- |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` |
| Solution-Explorer-State | `CANONICAL` |
| Path-Namespace-State | `EXACT` |
| Physical-Copy-State | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` |
| File-Cohesion-State | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (single file, see below) |
| Structure-State | `READY_FOR_CERTIFY` (structure) / `REPAIR_REQUIRED` (cohesion) |

## Application capability-first tree (verified)

Capability roots: `Access`, `Assignments`, `Authorization`, `Bootstrap`, `Ceiling`, `Composition`,
`Development`, `Exceptions`, `Models`, `Permissions`, `Ports`, `Roles`, `Validators`.

- No technical-axis-first root: `Application/Commands/**` and `Application/Queries/**` do **not**
  exist.
- No per-use-case single-file leaf folder: every `Commands/` and `Queries/` directory contains
  request files directly; `Directory.GetDirectories` on those axes is empty.
- No empty decorative folder.

### Single-file leaf folders (legitimate, non-request)

| Folder | Files | Justification |
| --- | --- | --- |
| `Development/Seller/` | 3 | cohesive seller-dev capability (query + port + models) |
| `Composition/` | 1 | cross-capability Result mapper |
| `Exceptions/` | 1 | cross-capability typed-fault vocabulary — **cohesion flag F3** |
| `Models/` | 1 | shared Application-internal scope value + **mixed bundle — cohesion flag F1** |
| `Ports/` | 1 | module directory port |
| `Validators/` | 2 | shared validation codes + reusable FluentValidation fragments |

`Exceptions/` and `Models/` are the only folders whose contents warrant consolidation in W1.

## Cohesion findings

| File | LOC | Types | Verdict |
| --- | --- | --- | --- |
| `Application/Models/AccessControlDtos.cs` | 76 | 8 unrelated top-level records | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` |
| `Infrastructure/Directories/AccessControlDirectory.cs` | 877 | 1 cohesive directory implementation | `OVERSIZED_ONLY` (watch; baselined) |
| `Endpoints/Admin/AccessControlAdminEndpoints.cs` | 401 | 1 endpoint file + 1 private body record | `COHESIVE` |
| `Endpoints/Seller/AccessControlSellerEndpoints.cs` | 401 | 1 endpoint file | `COHESIVE` |
| `Endpoints/Admin/AccessControlAdminSellerEndpoints.cs` | 341 | 1 endpoint file | `COHESIVE` |
| all remaining production files | ≤ 344 | single responsibility | `COHESIVE` |

No `OVER_SPLIT` (cosmetic) structure was observed. The three `*AccessRoleCommand` records inside
`AccessControlDtos.cs` are command-shaped types placed in a shared Models dump beside their
authoritative MediatR requests in `Roles/Commands/` — the concrete violation to repair in W1.
