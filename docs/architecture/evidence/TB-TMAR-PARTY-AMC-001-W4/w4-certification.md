# TB-TMAR-PARTY-AMC-001-W4 — Certification

## Verdict

`COMPLETE_REFERENCE_PATTERN` + `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`

## HTTP / CQRS

- Applicability: `HTTP_OWNING`
- Ownership: `MODULE_ENDPOINTS` via `MapPartyEndpoints`
- CQRS: MediatR via `AddToobaCqrsFoundation(GetSellerSettingsQuery assembly)`
- Endpoints: thin `ISender` + `ApiResponseFactory` — zero `Results.Json`
- Fault mapping: `PartyOperation` / `SemanticException` → `Result` (no `InvalidOperationException` catch)

## Validator inventory

| Request | Classification |
|---|---|
| GetSellerSettingsQuery | NO_VALIDATOR_REQUIRED_NO_TRANSPORT_INPUT |
| UpdateSellerSettingsCommand | VALIDATOR_REQUIRED_PRESENT |
| ListAdminSellersQuery | NO_VALIDATOR_REQUIRED_NO_TRANSPORT_INPUT |
| QueryAdminSellersGridQuery | VALIDATOR_REQUIRED_PRESENT |

## Boundaries

- Foreign Application/Infrastructure/Domain coupling: **ZERO**
- Cross-module Admin sellers composition: Offer/Order/Party `Contracts.Ports` only
- Error catalog/resources: `Tooba.Party.Contracts` registered by `PartyModule`
- Host retains thin `HostPartySellerAuthorizer` + composition/seed seams
- Microservice-extractable: **true**

## Structure handoff

Structure-State `READY_FOR_CERTIFY` in `w4-structure-gate.md` for the same surface.
