# TB-TMAR-IDENTITY-AMC-001 — W0 Analyze

Mode: ANALYSIS_ONLY (Architect-direct AMSC)  
Target: `Tooba.Identity.*`  
Skills: Analyze → Migrate → Structure → Certify  
Goal: COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002, microservice-extractable, ZERO foreign App/Infra/Domain coupling

## Current physical state

| Project | Observation |
| --- | --- |
| Application | ROOT DUMP: single `IdentityContracts.cs` (~190 LOC) holding Options + Ports + Models; **no CQRS/MediatR** |
| Domain | GOD FILE: `IdentityDomain.cs` (~590 LOC) enums + value objects + aggregates + domain events |
| Contracts | Partial folders (`Auth/`, `Problems/`); root dumps `ActorContactContracts.cs`, `ActorIdentifierResolverContracts.cs`; Resources under Contracts (ok for Identity historical pattern) |
| Infrastructure | Capability folders already (Authentication/Otp/Sessions/…); composition `IdentityModule.cs` |
| Endpoints | **MISSING** — `FOUNDATION_MISSING` |
| Solution Explorer | Identity projects under flat `/Modules/` — **not** `/Modules/Identity/` |
| Manifest | Identity **absent** from `tmar-module-structure-manifests.json` |

## Host boundary (locked prior decisions)

- Host `Authentication/*` = global auth/session HTTP/runtime platform (docs/architecture/41-authentication-http-boundary.md; option A executed).
- 13 `/v1/auth/*` routes live in Host `AuthenticationHttpBoundary.cs` calling `IIdentityAuthenticationService` (Contracts-only) — good for coupling, **bad for module endpoint ownership / COMPLETE_REFERENCE_PATTERN**.
- Host→`Identity.Infrastructure` limited to composition (`IdentityModule`) + Development bootstrap `IdentityDbContext` (composition/dev seam).

## Coupling scan

- Identity → foreign App/Infra/Domain: **ZERO** (csproj + usings)
- Host Authentication → Identity.Application/Domain: **ZERO** (guarded)
- Cross-module EF joins from Identity: **NONE** (own `IdentityDbContext`)

## Structure-Handoff-State

**REQUIRED** — Application/Domain/Contracts foldering + Solution Explorer + Endpoints foundation required before Certify.

## Blockers for COMPLETE_REFERENCE_PATTERN

1. `ENDPOINTS_FOUNDATION_MISSING` — no `Tooba.Identity.Endpoints`; auth routes not module-owned
2. `APPLICATION_NOT_CQRS` — no Commands/Queries/MediatR/validators
3. `APPLICATION_ROOT_DUMP` — `IdentityContracts.cs`
4. `DOMAIN_GOD_FILE` — `IdentityDomain.cs`
5. `SOLUTION_EXPLORER_MISSING_GROUPING` — no `/Modules/Identity/`
6. `MANIFEST_ABSENT`

## Capability map (business axes, not invented)

| Capability | Contents |
| --- | --- |
| Auth | register/login/refresh/logout/password/otp/verification (`IIdentityAuthenticationService`) |
| Sessions | lifecycle/session credential |
| Otp | challenge + delivery |
| Contacts | actor contact / identifier lookup |
| ExternalIdentity | issuer+subject binding |
| Mfa | enrollment store |
| SecurityEvents | audit sink |
| PasswordHashing | hashing port/impl |

## Wave plan (Architect-direct)

| Wave | Mode | Scope | Commit |
| --- | --- | --- | --- |
| W0 | Analyze | this evidence | docs |
| W1 | Migrate | `.slnx` `/Modules/Identity/` | impl |
| W2 | Migrate+Structure | Domain Aggregates/Enums; Application Ports/Options/Models; Contracts folders | impl |
| W3 | Migrate | Create `Tooba.Identity.Endpoints` + evacuate `/v1/auth` from Host; Host retains middleware/session platform only | impl |
| W4 | Migrate | Application CQRS + FluentValidation for endpoint-reachable requests; Result/`api.From` | impl |
| W5 | Structure | capability-first shallow Application; durable guards | impl |
| W6 | Certify | ARCH-COMPLETE-002 + manifest + SoT | cert |

Host final closure preserved. Frontend frozen. No schema/migration changes unless forced by Endpoints project creation (none expected).

## Microservice extractability target

After W6: Identity owns persistence + business + HTTP presentation; Host only composition + generic auth middleware/cookie platform seams; Contracts-only inbound/outbound.
