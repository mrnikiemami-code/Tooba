# TB-TMAR-OPERATORPROFILE-AMC-001-W0 — Analyze

## Mode

`ARCHITECT_DIRECT_AMSC` — Analyze only (no production moves in this wave).

## Ownership

| Surface | Owner |
|---|---|
| Admin GET/PUT `/v1/admin/operator/profile` | OperatorProfile.Endpoints |
| CQRS Upsert/Get | OperatorProfile.Application |
| Aggregate + invariants | OperatorProfile.Domain |
| Directory / EF / seed / display adapter | OperatorProfile.Infrastructure |
| `IActorDisplayLookup` cross-module port | OperatorProfile.Contracts |
| Thin admin authorizer | Host (retained) |

## Foreign coupling

- Outbound foreign Application/Infrastructure/Domain: **ZERO** (csproj refs = BuildingBlocks/Persistence/ModuleContracts + own layers only).
- Inbound consumers (AccessControl, Order, Catalog, ProductWorkspace) use `OperatorProfile.Contracts` only — legal.
- Host composition/seed/migration seams retain Infrastructure references (platform, allowed).

## Blockers for COMPLETE_REFERENCE_PATTERN

1. **Solution Explorer:** OperatorProfile projects sit under flat `/Modules/`; Endpoints project **missing** from `Tooba.slnx`; no `/Modules/OperatorProfile/` folder.
2. **Root dumps / path↔namespace:**
   - Domain root `OperatorProfile.cs` (needs `Aggregates/`)
   - Application root `OperatorProfileContracts.cs` mixed models+port
   - Contracts root `ActorDisplayContracts.cs` (needs `Ports/`)
   - Infrastructure root: Directory, ActorDisplayLookupAdapter, Outbox co-located in Module
3. **HTTP/Result:** Endpoints use `Results.Json` + try/catch; handlers return bare snapshots, not `Result<T>`; no `OperatorProfileOperation`.
4. **Validators:** Upsert validator co-located in command file; Get query unclassified; validation codes are inline strings, not catalog-backed `OperatorProfileErrorCodes`.
5. **Error presentation:** Catalog lives in Endpoints; no Contracts `IErrorResourceSet` / `.resx`.
6. **Structure handoff:** `REQUIRED` — physical tree must be normalized before Certify.

## Microservice extractability

Blocked until W3/W4 close Result pipeline + Contracts-owned errors + zero Results.Json. Coupling already ZERO — extractability becomes true after AMSC PASS.

## Wave plan

| Wave | Skill focus | Outcome |
|---|---|---|
| W1 | Structure foundation | `/Modules/OperatorProfile/` + Endpoints in slnx |
| W2 | Migrate physical layers | capability folders, Ports/Models/Aggregates/Adapters |
| W3 | Complete CQRS/Result | ISender+api.From, Result, validators, catalog/resx |
| W4 | Structure + Certify | READY_FOR_CERTIFY + COMPLETE_REFERENCE_PATTERN |

## Structure-Handoff-State

`REQUIRED`
