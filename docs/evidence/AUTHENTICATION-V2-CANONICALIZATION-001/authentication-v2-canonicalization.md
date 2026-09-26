# AUTHENTICATION-V2-CANONICALIZATION-001 — Host/Authentication V2 canonicalization

Scope: `src/backend/Host/Tooba.Host/Authentication` plus the **minimum** Identity/CustomerProfile
Contracts surfaces strictly required to repair its V2 boundary violations.

Ownership unchanged:

- Host retains the global authentication/session HTTP/runtime boundary (13 `/v1/auth` routes).
- Identity retains business/persistence ownership.
- No `Identity.Endpoints` project; no route moved out of Host.

## 1. Canonical error catalog (Stable-Error-Code-State = CATALOGUED)

New Identity-owned Contracts error surface:

- `Tooba.Identity.Contracts.Problems.IdentityErrorCodes`
- `Tooba.Identity.Contracts.Problems.IdentityErrorCatalogContributor`
- `Tooba.Identity.Contracts.Problems.IdentityErrorResourceSet` + `Resources/IdentityErrors.resx`

Explicit descriptors pin the locked HTTP semantics (no default-mapper guessing):

| Machine code | HTTP |
| --- | --- |
| `identity.validation.failed` | 400 |
| `identity.challenge.invalid` | 400 |
| `identity.authentication.failed` | 401 |
| `identity.session.invalid` | 401 |
| `identity.identifier.conflict` | 409 |
| `identity.rate_limited` | 429 |
| `identity.tenant.untrusted` | 400 |
| `identity.otp.delivery.unavailable` | 400 |
| `identity.password.change.failed` | 400 |

Registered by `IdentityModule.AddServices` as `IErrorCatalogContributor` + `IErrorResourceSet`.
`ErrorDefinitionCatalog` remains the single composed catalog (duplicate codes still fail fast).

## 2. Canonical API result / problem mapping (API-Result-Pattern-State = CANONICAL)

`AuthenticationHttpProblem` no longer builds a parallel `ProblemDetails` pipeline. The thin
tenant-spoof/throttle guards remain, but **all** error presentation is delegated to
`ApiResponseFactory.FromFailure(new SemanticError(code))`:

- status code comes from the explicit catalog descriptor;
- machine `errorCode` comes from the catalog;
- `traceId` / `correlationId` / `requestId` come from the canonical context provider;
- `title` comes from `IErrorMessageLocalizer` + `IdentityErrors.resx` (exact locked titles:
  `Bad Request`, `Unauthorized`, `Conflict`, `Too Many Requests`).

The boundary now references `IdentityErrorCodes` constants instead of scattered string literals.

## 3. Cross-module boundaries (LEGAL_CONTRACTS_ONLY, joins = NONE)

- `Tooba.Identity.Contracts.Problems.IdentityDuplicateIdentifierFault` is the typed, contract-safe
  duplicate-identifier fault. Identity Infrastructure throws it; Host catches the Contracts type.
  Host no longer references `Tooba.Identity.Infrastructure` from the Authentication folder.
- `Tooba.CustomerProfile.Contracts` now owns `ICustomerProfileDirectory`, `CustomerProfileSnapshot`,
  and `CustomerProfileWrite`. `CustomerProfile.Application` no longer exposes those types; the
  implementation and Host `/me` use the Contracts namespace.

No DbContext, entity, repository, infrastructure exception, or Application implementation is
exposed to Host/Authentication.

## 4. Behavior lock (behavior change = NONE)

All 13 routes, HTTP methods, status codes, response shapes, machine codes, Bearer + `tooba_session`
semantics, tenant-spoof rejection, throttle behavior, OTP/password/reset behavior, telemetry event
names, and schema/migrations are unchanged. No sensitive logging added.

## 5. Tests

Focused (no Docker): 38 passed / 8 skipped / 0 failed
(`AuthenticationHttpTests`, `AuthSecurityHttpTests`, `IdentityFoundationTests`,
`StorefrontAccountIdentityTests`, `CustomerProfileFoundationTests`,
`AuthenticationV2CanonicalizationGuardTests` = 21 durable guards).

`ArchitectureBoundaryTests` + `TmarFoundationTests` PASS; `Tooba.BuildingBlocks.Tests` 45 PASS.

Pre-existing, not caused by this task:

- `ErrorContractTests` (3) — full Host DI validation fails on unregistered
  `Tooba.Inventory.Contracts.Cart.ICartInventoryHoldPort` (Cart/Inventory untouched here).
- `TmarSourceSizeAndInfraAppTests` — red at the parent checkpoint.
- `Tooba.Order.Tests.OrderSellerPanelArchitectureGuardTests` — pre-existing missing
  `Tooba.AccessControl` reference.

Final state:

```text
API-Result-Pattern-State = CANONICAL
Stable-Error-Code-State = CATALOGUED
Localization-State = CANONICAL
Cross-Module-Coupling-State = LEGAL_CONTRACTS_ONLY
Cross-Module-Join-State = NONE
Identity business authority in Host = ZERO
Identity persistence authority in Host = ZERO
behavior change = NONE
```
