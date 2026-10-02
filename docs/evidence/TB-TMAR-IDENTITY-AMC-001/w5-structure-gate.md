# TB-TMAR-IDENTITY-AMC-001 — W5 Structure gate

Touched surface: `Tooba.Identity.*` (Application/Domain/Contracts/Endpoints/Infrastructure + Host composition seams)

## Structure classification

| State | Value |
| --- | --- |
| Structure-State | READY_FOR_CERTIFY |
| Folder-Granularity-State | PROFESSIONAL_SHALLOW |
| Solution-Explorer-State | CANONICAL (`/Modules/Identity/` + 5 projects) |
| Path-Namespace-State | EXACT (Contracts.Auth namespace aligned to Auth/) |
| Physical-Copy-State | CLEAN |
| Root-Allowlist-State | ENFORCED |
| Technical-Axis-First-State | ZERO (no Application/Commands or Application/Queries roots; Auth capability owns Commands/Queries/Validators) |
| Single-File-Request-Leaf-State | ZERO |
| Host-Final-Closure-State | PRESERVED (Host Authentication retains middleware/session/throttle platform only) |

## Physical notes

- Infrastructure root allowlist: `IdentityModule.cs` only; `IdentityOutboxRegistration` under `Persistence/`
- Endpoints root allowlist: `IdentityEndpointModule.cs`; Auth HTTP under `Auth/` + `Errors/`
- Application capability-first: `Auth/` + shared Ports/Options/Models/Composition/Validators

Structure skill gate: SATISFIED for Certify.
