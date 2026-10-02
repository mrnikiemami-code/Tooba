# TB-TMAR-STORY-AMC-001-W6 — ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

Mode: CERTIFY
Parent: TB-TMAR-STORY-AMC-001-W5
Verdict: `COMPLETE_REFERENCE_PATTERN` / `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`

## Microservice readiness (architect goal)

Story is extractable as an independent service:

| Boundary | State |
| --- | --- |
| Foreign Application/Infrastructure/Domain | **ZERO** |
| Foreign module ProjectReference | **ZERO** (platform only: BuildingBlocks, Persistence, ModuleContracts) |
| Endpoints → Infrastructure / Domain / DbContext | **ZERO** |
| Cross-module EF/SQL join | **ZERO** (own `StoryDbContext` schema) |
| Host Story business folder | **ABSENT / CLOSED_HOST_ZERO** |
| Host composition only | `MapStoryModuleEndpoints`, `HostStorySellerAuthorizer` (neutral seam) |

## Certification checklist (compressed)

1. Physical tree — capability-first `Stories/*`, Domain Aggregates/Enums/Rules/Tenant, Infra Directory/Development/Grid/Adapters/Persistence (+`Persistence/Migrations`), Endpoints Admin/Seller/Storefront/Errors/Models/Resources
2. Root allowlists — Contracts/Domain/Application empty; Endpoints=`StoryEndpointModule.cs`; Infra=`StoryModule.cs`; top-level `Migrations/` forbidden (moved under Persistence)
3. Path↔namespace EXACT; no alias workaround
4. Endpoint ownership — 25 module routes; Host Story routes 0
5. CQRS/MediatR — all routes `ISender` → `IRequest<Result<T>>`
6. Validator matrix — 15 REQUIRED + 10 NO_VALIDATOR (W5 guard)
7. Localization — `StoryErrorCodes` + catalog + resx; SemanticException codes (W1/W2)
8. API result — `ApiResponseFactory.From` / `Created` (W4)
9. Logging/OTel — no parallel pipeline; foundation CQRS behaviors
10. Cross-module — Contracts-only / platform-only
11. Persistence — module-owned migrations under `Persistence/Migrations`; namespace realigned; Up/Down/snapshot semantics unchanged
12. Durable guards — W1..W6 + HostStoryAmc
13. Manifest — `structureCertified: true`, `lockVersion: ARCH-COMPLETE-002`
14. SoT — `storyModuleAmc001W6Cert`

## Residual non-blocking debt

- `StoryDirectory.cs` (~727 LOC) classified WATCH under ARCH-SIZE threshold (&lt;800 new-file); cohesive persistence adapter — optional future split, not a cert blocker.
- Domain references Story.Contracts for stable error codes (same-module boundary; ships with the service).

## Focused validation

- `dotnet build` Story.Endpoints
- `dotnet test` filter `StoryModuleAmc`
- HostStoryAmcGuardTests path updated for `Errors/StoryHttpErrors.cs`
