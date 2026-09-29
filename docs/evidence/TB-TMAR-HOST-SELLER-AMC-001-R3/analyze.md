# Host/Seller — Seller-R3 — Analyze

**Task:** TB-TMAR-HOST-SELLER-AMC-001-R3
**Parent:** TB-TMAR-HOST-SELLER-AMC-001-R2
**Skill:** `.cursor/skills/tooba-architecture-analyze/SKILL.md`
**Mode:** BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
**Track:** SELLER_R3_PARTY_SETTINGS

## 1. Scope fixed by the task

Exactly the two Host-owned seller **settings** routes and the seller settings capability checks:

| # | Route | Verb | Host handler before R3 |
| --- | --- | --- | --- |
| 1 | `/v1/seller/settings` | GET | `SellerSettingsEndpoints.GetSettingsAsync` |
| 2 | `/v1/seller/settings` | PUT | `SellerSettingsEndpoints.UpdateSettingsAsync` |

Party representation inside `Host/Seller/SellerSettingsEndpoints.cs`:

- `IPartyDirectory` (Party **Application** port) used directly from the Host endpoint
- `OrganizationProfileWriteRequest` transport record owned by Host
- `seller.settings.view` / `seller.settings.manage` capability checks via `IAccessControlDirectory`
  (`Tooba.AccessControl.Application` + `Tooba.AccessControl.Domain`)
- `EnsureSellerCapabilityAsync` / `HasSellerCapabilityAsync` duplicated Host helper
- `catch (PlatformHttpException ex)` -> `Results.Json(new { title = ex.Message, errorCode = ex.ErrorCode })`
- `catch (InvalidOperationException)` -> 400 `seller.settings.rejected`

## 2. Ownership determination

| Capability | True owner | Evidence |
| --- | --- | --- |
| Seller operational Organization profile read (display/legal/description/support/address) | **Party** | the profile is the `Tooba.Party.Domain` `BusinessParty` Organization aggregate persisted by `PartyDbContext` and read through `IPartyDirectory.GetOrganizationProfileAsync` |
| Seller operational Organization profile write | **Party** | the canonical writer is `IPartyDirectory.UpdateOrganizationProfileAsync` (Party Application) over the same Organization aggregate |
| Seller settings response/request transport shape | **Party** | the request/response shape is the read model of the Party capability; it has no Host business consumer after the route evacuates |
| Seller **platform auth** (actor binding, effective-access projection, 403 fail-closed) | **Host** | `Host/Security/Seller` R1A canonical boundary plus `Tooba.BuildingBlocks.Security.IPlatformEffectiveAccessReader`; not a Party concern |

Conclusion: everything in scope is Party business/persistence authority inside Host — a category-A
Host residue. Seller platform security must stay Host-owned and must **not** be copied into Party.

## 3. Defect inventory found before migration

| Defect | Severity | R3 action |
| --- | --- | --- |
| `Host/Seller/SellerSettingsEndpoints.cs` binds the Party **Application** port `IPartyDirectory` directly in Host | Host -> foreign Application dependency | evacuated (Party CQRS + Party-owned persistence seam) |
| `Host/Seller/SellerSettingsEndpoints.cs` imports `Tooba.AccessControl.Application` + `Tooba.AccessControl.Domain` and uses `IAccessControlDirectory` | Host/Seller settings coupling to the AccessControl module | removed (Host platform seam is `IPlatformEffectiveAccessReader`) |
| Settings capability checks duplicated as a Host static helper | duplicated business-adjacent policy in Host | replaced by the Host security adapter behind a neutral Party port |
| Failure classification via `catch (PlatformHttpException ex)` returning `ex.Message` directly | makes Host the title authority for platform denials | delegated to canonical `ApiResponseFactory` / `SemanticError` |
| `Host/Seller/SellerSettingsEndpoints.cs` owns `OrganizationProfileWriteRequest` | Host ownership of a Party transport model | moved to `Tooba.Party.Endpoints.Seller` |
| Seller settings routes had no dedicated durable architecture guard | guard gap | `HostSellerAmcR3GuardTests` added |

## 4. Auth seam analysis

`Host/Seller` used `SellerPanelAccess.RequireAuthorizedAsync` plus `IAccessControlDirectory`
directly, and returned `(ActorUserId, SellerPartyId)`. Party must not consume Host types, the
platform authorization engine or the AccessControl module. The minimal neutral seam is a
Party-owned port:

```
Tooba.Party.Endpoints.Seller.IPartySellerAuthorizer
    Task<(Guid ActorUserId, Guid SellerPartyId, bool CanManage)> RequireViewAsync(HttpContext, CancellationToken)
    Task<(Guid ActorUserId, Guid SellerPartyId)> RequireManageAsync(HttpContext, CancellationToken)
```

implemented by the thin Host adapter `Tooba.Host.Security.Seller.HostPartySellerAuthorizer`
delegating to the unchanged R1A seam `ISellerPanelAccess` and the neutral platform seam
`IPlatformEffectiveAccessReader`. This mirrors the already accepted
`ICatalogSellerAuthorizer` / `HostCatalogSellerAuthorizer` pattern.

## 5. Cross-module analysis

- Party does not query Order/Catalog/AccessControl persistence from Party handlers. The settings
  read/write goes through the Party-owned `IPartySellerSettings` seam implemented over the module
  `IPartyDirectory` (`PartySellerSettingsAdapter`).
- Party.Endpoints references only `Party.Application`, `Party.Contracts` and `Tooba.BuildingBlocks`.
  It has no `DbContext`, no `Microsoft.EntityFrameworkCore` and no foreign module reference.
- `seller.authorization.denied` is a **shared** platform code whose canonical descriptor and
  localization stay owned by the Host/Foundation boundary; Party must **not** register a duplicate
  descriptor. `seller.settings.missing` (404) and `seller.settings.rejected` (400) are
  Party-owned and registered by `PartyErrorCatalogContributor`.

## 6. Out of scope (explicitly not touched)

`GET /v1/seller/dashboard`, `GET /v1/seller/dev-contexts`, `SellerPanelComposer.cs` dashboard work,
`SellerDevActorBootstrap.cs`, Order dashboard query, frontend, Seller-R4/R5/R6.
