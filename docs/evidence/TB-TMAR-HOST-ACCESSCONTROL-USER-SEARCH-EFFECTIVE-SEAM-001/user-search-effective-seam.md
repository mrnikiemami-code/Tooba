# Evidence — TB-TMAR-HOST-ACCESSCONTROL-USER-SEARCH-EFFECTIVE-SEAM-001

Track: HOST_FIRST_ACCESSCONTROL
Parent: TB-TMAR-HOST-ACCESSCONTROL-ADMINPLATFORM-ASSIGNMENTS-EFFECTIVE-001 (commit `99d59d894a4464b57d1f53a6df9800843a76a592`)
Scope: Admin user search, Seller user search, Seller effective (3 routes)

## 1. Route ownership — before / after

| Route | Before (Host) | After (module) |
| --- | --- | --- |
| `GET /v1/admin/access-control/users` | `Tooba.Host/AccessControl/AccessControlEndpoints.cs` → `AdminSearchUsersAsync` | `Tooba.AccessControl.Endpoints/Admin/AccessControlAdminEndpoints.cs` → `SearchUsersAsync` |
| `GET /v1/seller/access-control/users` | Host → `SellerSearchUsersAsync` | `Tooba.AccessControl.Endpoints/Seller/AccessControlSellerEndpoints.cs` → `SearchUsersAsync` |
| `GET /v1/seller/access-control/users/{userId:guid}/effective` | Host → `SellerEffectiveAsync` | Endpoints → `EffectiveAsync` |

## 2. Exact contracts used

| Need | Contract | Owner |
| --- | --- | --- |
| Batch contact (email/mobile) enrichment | `IActorContactLookup` / `ActorContactProjection` | `Tooba.Identity.Contracts` |
| Batch display-name enrichment | `IActorDisplayLookup` / `ActorDisplayProjection` | `Tooba.OperatorProfile.Contracts` |
| Neutral email/phone → user id resolution | `IActorIdentifierResolver` / `ActorIdentifierKind` | `Tooba.Identity.Contracts` (new) |

No `Tooba.Identity.Application`/`Domain` or `Tooba.OperatorProfile.Application` reference is used by
`Tooba.AccessControl.Application` or `Tooba.AccessControl.Endpoints`.

## 3. New Identity identifier resolver (contract + Identity-owned implementation)

- Contract: `src/backend/Modules/Identity/Tooba.Identity.Contracts/ActorIdentifierResolverContracts.cs`
  - `enum ActorIdentifierKind { Email, Phone, Username }` — neutral, no Identity login types leaked
  - `interface IActorIdentifierResolver { Task<Guid?> FindUserIdAsync(ActorIdentifierKind, string, CancellationToken); }`
- Implementation (Identity-owned): `src/backend/Modules/Identity/Tooba.Identity.Infrastructure/ActorIdentifierResolverAdapter.cs`
  - wraps the existing `IIdentityAuthenticationService.FindUserIdByIdentifierAsync`
  - maps neutral kind → internal `LoginIdentifierKind` internally
  - returns `null` for blank identifier or unknown kind
- DI registration (Identity-owned): `IdentityModule.cs` →
  `services.AddScoped<Contracts.IActorIdentifierResolver, ActorIdentifierResolverAdapter>();`

`Identity.Contracts.csproj` still has zero project references (pure contract assembly), so the new contract
introduces no new assembly dependency.

## 4. Search CQRS

New `Queries/SearchAccessUsers/SearchAccessUsersQuery.cs`:

- `SearchAccessUsersQuery(AccessOwnerScopeKind OwnerScopeKind, Guid? OwnerScopeId, string? TenantId, string? Query) : IRequest<IReadOnlyList<AccessUserHitDto>>`
- Handler depends on `IAccessControlDirectory`, `IActorContactLookup`, `IActorDisplayLookup`, `IActorIdentifierResolver`
- No `HttpRequest` / `HttpContext` / `IResult` / `Results.*` in Application

## 5. Search parity statement

Behavior is a line-for-line port of the removed Host `EnrichUserHitsAsync`:

- `string.IsNullOrWhiteSpace(q) ? null : q.Trim()` — blank/whitespace ⇒ null semantic
- `Guid.TryParse(q)` ⇒ that user id considered directly
- `q.Contains('@')` ⇒ resolved as email via `IActorIdentifierResolver.Email`
- otherwise `q.Any(char.IsDigit) && q.Length >= 8` ⇒ resolved as phone
- resolved id inserted into the scope-hit dictionary only when not already present (empty `RoleCodes`)
- enrichment: contact email/mobile and display name; `FirstNonEmpty(profile.DisplayName, contact.Email, contact.Mobile)` with `Trim()` — display name preferred
- `q is null` ⇒ all hits ordered by `DisplayName ?? Email ?? UserId.ToString("D")`, `OrdinalIgnoreCase`
- otherwise filter on DisplayName / Email / Mobile / userId string / any RoleCode with `OrdinalIgnoreCase`, then the same ordering

One intentional improvement without observable change: contacts/displays are fetched in one batch each
(`GetActorContactsAsync` / `GetActorDisplaysAsync`) instead of per-user, yielding identical projections.

## 6. Authorization / owner-scope parity

| Route | Auth | Capability | Owner scope |
| --- | --- | --- | --- |
| Admin `/users` | `IAdminPanelAccess.RequireAuthorizedAsync` | `accesscontrol.view` | `Platform`, `null`, current tenant |
| Seller `/users` | `ISellerPanelAccess.RequireAuthorizedAsync` | `accesscontrol.view` | `Seller`, authorized `sellerId`, current tenant |
| Seller effective | `ISellerPanelAccess.RequireAuthorizedAsync` | `accesscontrol.view` | `GetEffectiveAccessQuery(userId, Seller, authorized sellerId, tenant)` |

Response parity: all three return `Results.Json(...)` and remain uncaught (no new error translation).

## 7. Boundary audit

- `AccessControl.Application` project references: `BuildingBlocks`, `AccessControl.Domain`, `Identity.Contracts`,
  `OperatorProfile.Contracts` only.
- `AccessControl.Application` → `Identity.Application` / `Identity.Domain` / `OperatorProfile.Application` /
  `OperatorProfile.Domain` / Host = **ZERO**.
- `AccessControl.Endpoints`: `ISender` only; no `IAccessControlDirectory`, no Infrastructure/DbContext, no Host types.
- Host `AccessControlEndpoints.cs`: `Tooba.Identity.Application`, `Tooba.Identity.Domain`,
  `Tooba.OperatorProfile.Application` usings = **ZERO**.

## 8. Host removals

Removed from `src/backend/Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs`:

- mappings: `admin.MapGet("/users", …)`, `seller.MapGet("/users", …)`,
  `seller.MapGet("/users/{userId:guid}/effective", …)`
- handlers: `AdminSearchUsersAsync`, `SellerSearchUsersAsync`, `SellerEffectiveAsync`
- helpers now unused: `EnrichUserHitsAsync`, `FirstNonEmpty`, `PlatformScope`, `SellerScope`
- foreign usings: `Tooba.Identity.Application`, `Tooba.Identity.Domain`, `Tooba.OperatorProfile.Application`

Retained: `RequireSellerAsync` (still used by the Seller scope-resource handlers), `Trace`, `MapError`,
`AdminDemoPreviewAsync`, and all scope-resource handlers.

## 9. Focused builds

- `Tooba.Identity.Contracts` → succeeded, 0 errors
- `Tooba.OperatorProfile.Contracts` → succeeded, 0 errors
- `Tooba.Identity.Infrastructure` → succeeded, 0 errors (new adapter + DI registration)
- `Tooba.AccessControl.Application` → succeeded, 0 errors
- `Tooba.AccessControl.Endpoints` → succeeded, 0 errors
- `Tooba.Host` → succeeded, 0 errors

## 10. Focused tests

No focused tests were added. There is no AccessControl.Application test project, and the only AccessControl test
(`Tooba.Host.Tests/AccessControlFoundationTests.cs`) is a group/boundary assertion, not a search-behavior suite.
Standing up a new test project plus solution wiring would exceed the hard timebox, so — per the task's explicit
allowance — parity is documented here and verified by focused builds plus the exact line-for-line port above.

## 11. Mandatory audit results

- `AdminSearchUsersAsync` = ZERO production
- `SellerSearchUsersAsync` = ZERO production
- `SellerEffectiveAsync` = ZERO production
- `EnrichUserHitsAsync` / `FirstNonEmpty` = ZERO in Host (only the re-implemented `FirstNonEmpty` inside
  `SearchAccessUsersQueryHandler`)
- each of the three routes = EXACTLY ONE module mapping; Host = ZERO

## 12. Residual Host AccessControl families

Admin + Seller `scope-resources/*` and `demo-preview` (plus shared helpers `RequireSellerAsync`, `Trace`,
`MapError`) remain Host-owned. AccessControl remains **IN_PROGRESS**; Host residue non-zero.
No COMPLETE_REFERENCE_PATTERN, no structure certification.
