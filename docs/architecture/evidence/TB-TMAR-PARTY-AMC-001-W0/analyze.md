# TB-TMAR-PARTY-AMC-001-W0 — Analyze

## Mode

`ARCHITECT_DIRECT_AMSC` — Analyze only.

## Ownership

| Surface | Owner |
|---|---|
| GET/PUT `/v1/seller/settings` | Party.Endpoints.Seller |
| GET/POST `/v1/admin/sellers` (+ query) | Party.Endpoints.Admin.Sellers |
| Seller settings CQRS + Admin sellers CQRS | Party.Application |
| Aggregates / membership / org profile | Party.Domain |
| Directory / grid / seeds / adapters | Party.Infrastructure |
| Cross-module ports (`IPartyLookup`, sellers grid, seed gateway) | Party.Contracts |
| Thin seller authorizer | Host (`HostPartySellerAuthorizer`) |

## Foreign coupling

- Foreign Application/Infrastructure/Domain: **ZERO**
- Legal Contracts-only composition: `Offer.Contracts` + `Order.Contracts` ports used by Admin sellers list/grid (HTTP composition, not persistence join)
- Host composition/authorizer seams allowed

## Blockers for COMPLETE_REFERENCE_PATTERN

1. **Solution Explorer:** Party projects sit under flat `/Modules/` — missing `/Modules/Party/` folder (Endpoints already listed but not grouped).
2. **God / root dumps:**
   - Domain `PartyDomain.cs` (~542 lines: enums+aggregates+event)
   - Application root `PartyContracts.cs` (ports+models)
   - Contracts root interface dump
   - Infrastructure root: Directory, Outbox, DevelopmentSeedGateway, MembershipProjectionHandler
3. **Error ownership:** Catalog + `.resx` live in Endpoints; seller error codes live in Application.Seller (should be Contracts.Errors / Contracts resources).
4. **Failure semantics:** Domain/Directory throw `InvalidOperationException` with message text; UpdateSellerSettings broadly catches `InvalidOperationException` → should be typed `SemanticException` + `PartyOperation`/`Result`.
5. **Validator inventory:** Seller Get = NO_VALIDATOR (auth-scoped, OK); Update validator present; Admin List = NO_VALIDATOR; Grid query validator present — needs durable exhaustive classification guard.
6. **Structure-Handoff-State:** `REQUIRED`

## Wave plan

| Wave | Focus |
|---|---|
| W1 | `/Modules/Party/` slnx grouping |
| W2 | Split Domain/App/Contracts/Infra physical tree |
| W3 | Contracts error catalog/resx + SemanticException/Result hardening + validator guard |
| W4 | Structure + Certify + SoT/manifest |

## Microservice extractability

Blocked until structure + typed faults + Contracts-owned errors close. Offer/Order Contracts ports remain the only cross-module seams (legal).
