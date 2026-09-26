# Authentication — Host Folder Inventory, Ownership Classification and Disposition

Host folder under analysis (ONLY this folder): `src/backend/Host/Tooba.Host/Authentication`

Baseline: `HEAD == origin/main == 9f660923c146eca48d02729282069f4cc21b6f7a` (clean tree).

Mode: BACKEND_ONLY. Skills run in order: `tooba-architecture-analyze` → (`tooba-architecture-migrate` only if `READY_TO_MIGRATE`) → `tooba-architecture-certify` only after a successful migration.

Outcome: **NO MIGRATION — MIGRATION HALTED ON OWNERSHIP DECISION.** Host production code changes = NONE. Guard weakening = NONE.

---

## 1. Exact file inventory

| # | File | Lines | Namespace | Types/capabilities |
| --- | --- | --- | --- | --- |
| 1 | `AuthenticationHttpBoundary.cs` | 702 | `Tooba.Host` | whole `/v1/auth` HTTP boundary: 13 routes, DTOs, session middleware, throttle seam, session state, tenant-spoof guard, ProblemDetails mapping |
| 2 | `AuthenticationInstrumentation.cs` | 17 | `Tooba.Host` | `tooba.authentication.event` counter |
| 3 | `AuthenticationRateLimitThrottleSeam.cs` | 45 | `Tooba.Host` | in-process IP+operation fixed-window throttle |
| 4 | `HostCurrentAuthenticatedUser.cs` | 10 | `Tooba.Host` | `CurrentAuthenticatedSession` → `BuildingBlocks` `ICurrentAuthenticatedUser` |

No other file exists in this folder. All four were read completely.

---

## 2. Member-level Content Disposition Map — `AuthenticationHttpBoundary.cs`

| Member | Kind | Responsibility | True owner |
| --- | --- | --- | --- |
| `CurrentAuthenticatedSession` (class) | per-request principal state | `UserId` / `SessionId` / `Edition` / `TenantId`, `IsAuthenticated`, `Assign` | **Host** (global session/current-user plumbing) |
| `IAuthenticationThrottleSeam` (interface) | seam | `TryAcquire(HttpContext, operation)` | **Host** (generic env/runtime seam) |
| `SessionAuthenticationMiddleware` (class) | middleware | resolve `Bearer {guid}` / `tooba_session` cookie into session state | **Host** (`global auth/session/tenant/correlation` middleware) |
| `AuthenticationHttpModels.*` (14 DTO/record types) | wire DTOs | `/v1/auth` JSON request/response shapes (incl. `JsonExtensionData` tenant-spoof probes) | module Endpoints *if* the boundary is evacuated (see §5) |
| `AuthenticationEndpointMapper.MapAuthenticationBoundary` | route registration | `app.MapGroup("/v1/auth")`, CORS, 13 route maps | module Endpoints *if* evacuated |
| `RegisterAsync` → `POST /v1/auth/register` | endpoint | `IIdentityAuthenticationService.RegisterAsync`, 201 / 409 / 400 | Identity (endpoint), Host (global boundary) |
| `LoginAsync` → `POST /v1/auth/login` | endpoint | `AuthenticateWithPasswordAsync` + throttle, 401 collapse | Identity (endpoint) |
| `RefreshAsync` → `POST /v1/auth/refresh` | endpoint | `RefreshSessionAsync` + throttle, opaque rotation | Identity (endpoint) |
| `LogoutAsync` → `POST /v1/auth/logout` | endpoint | `RevokeSessionAsync`, idempotent | Identity (endpoint) |
| `LogoutAllAsync` → `POST /v1/auth/logout-all` | endpoint | `RevokeAllSessionsAsync` | Identity (endpoint) |
| `RequestResetAsync` / `CompleteResetAsync` | endpoints | `IIdentityCredentialLifecycle`, enumeration-safe accepted payload, 400 `identity.challenge.invalid` | Identity (endpoint) |
| `RequestVerificationAsync` / `CompleteVerificationAsync` | endpoints | `IIdentityCredentialLifecycle` identifier verification | Identity (endpoint) |
| `RequestOtpLoginAsync` / `CompleteOtpLoginAsync` | endpoints | `IIdentityOtpLoginService` OTP login | Identity (endpoint) |
| `ChangePasswordAsync` → `POST /v1/auth/password-change` | endpoint | `ChangePasswordAsync`, requires authenticated session | Identity (endpoint) |
| `MeAsync` → `GET /v1/auth/me` | endpoint | `ICustomerProfileDirectory` + `IIdentityContactLookup` + `StorefrontAccountIdentity.CanonicalName` projection | Identity.Endpoints + CustomerProfile.Contracts (enrichment never in a ProjectReference) |
| `ToSessionResponse`, `TryReadBearerSessionId`, `TryParseKind`, `BlankToNull` | helpers | mapping/parse | with the endpoints |
| `RejectUntrustedTenant` | cross-cutting guard | reject `X-Tenant-Id`/`TenantId` header, query, cookie, body, extension-data | Host global tenant/platform trust boundary |
| `RejectIfThrottled` | cross-cutting guard | 429 `identity.rate_limited` | Host |
| `AuthProblem` | error helper | ProblemDetails + `traceId` + `errorCode` | Host (platform error mapping) |

`REMOVE candidates: NONE`. Every member is live; `MapAuthenticationBoundary(enableCors: true)` is called at `Program.cs:473`.

## 3. Member-level Content Disposition Map — the other three files

| File | Members | True owner |
| --- | --- | --- |
| `AuthenticationInstrumentation.cs` | `AuthenticationInstrumentation` (`tooba.authentication.event` counter, `Record`, `RecordThrottled`) | **Host** — platform observability for a global boundary (TMAR-HOST-EVACUATION-PROTOCOL line 71, `health/observability/platform hosting`) |
| `AuthenticationRateLimitThrottleSeam.cs` | `AuthenticationRateLimitThrottleSeam` (`TryAcquire`, `BuildKey`, `WindowCounter`) | **Host** — generic environment/runtime seam (line 71); deliberately *not* an anti-abuse product and *not* part of the Identity `NoOpAuthenticationThrottleSeam` contract |
| `HostCurrentAuthenticatedUser.cs` | `HostCurrentAuthenticatedUser` (`ICurrentAuthenticatedUser`) | **Host** — binding of Host session to the shared `BuildingBlocks.Security` abstraction (generic session/current-user plumbing, line 65, and `tiny security adapters`, line 70) |

## 4. Consumers and coupling inventory

Exact consumer count of `CurrentAuthenticatedSession` across production Host: **high and cross-cutting** (Admin, Content, Customer, Seller, Storefront, Order, Reviews, Story, Wishlist, Preferences, OperatorProfile, PageComposition, Media, Localization, Observability, ProductQnA plus every `Host*Authorizer` security adapter). This is the consumed neutral seam by design (`docs/architecture/41-authentication-http-boundary.md`).

Identity scratch dependencies in `AuthenticationHttpBoundary.cs`:

- `Tooba.Identity.Application` via **ProjectReference through `Tooba.Identity.Infrastructure`** (`Tooba.Host.csproj`) — ports `IIdentitySessionResolver`, `IIdentityAuthenticationService`, `IIdentityCredentialLifecycle`, `IIdentityOtpLoginService`, `IIdentityContactLookup`, `AuthenticatedIdentity`, `AuthenticationTicket`, `ChallengeConsumeOutcome`.
- `Tooba.Identity.Domain` (via the same transitive reference) for `LoginIdentifierKind`.
- `Tooba.CustomerProfile.Application` via a **direct** `Tooba.Host.csproj` ProjectReference, for `ICustomerProfileDirectory`.
- `Tooba.Host.Storefront` for `StorefrontAccountIdentity`.

**Cross-module joins / foreign DbContext reach-through / raw SQL: NONE** in this folder. All Identity access is through Application ports.

## 5. Ownership verdict and why migration is halted

The skill defines `authentication identity/authentication mechanisms -> Authentication` as the owner. In this repository that owner is the **`Tooba.Identity` module**, and post-TMAR the module-owned HTTP flow is `Module.Endpoints → ISender → Module.Application` (`HOST-MODULE-ENDPOINT-001`, `ARCH-COMPLETE-001`). The `/v1/auth` contract is **genuinely Identity-owned** (accounts, sessions, credentials, OTP, reset, verification, password change), so a faithful evacuation is a real architectural improvement, not a cosmetic move.

It nevertheless cannot be executed safely inside the current task envelope:

1. **`Tooba.Identity.Endpoints` does not exist.** `Modules/Identity` has only `Domain`, `Contracts`, `Application`, `Infrastructure` — no Endpoints project, no `MapIdentity…` composition, no `AddToobaCqrsFoundation` registration for Identity (`Program.cs:144-158` registers 13 module assemblies and Identity is absent). First Gate = `FOUNDATION_MISSING`.
2. **The correct foundation is a genuine module-foundation wave, not a bounded slice.** HTTP-owning foundation here means: new `Tooba.Identity.Endpoints` project + `.slnx` entry + Application-side CQRS (`IRequest`/`IRequestHandler<,>` for 13 endpoint-reachable requests), 13 transport validators or explicit `NO_VALIDATOR_REQUIRED` classification + durable coverage guard, and self-hosted `/v1/auth` mapping with behavior-identical ProblemDetails and tenant-spoof semantics.
3. **`GET /v1/auth/me` is cross-module and is *not* a ProjectReferencable boundary.** Host currently references `Tooba.CustomerProfile.Application` **directly** (`Tooba.Host.csproj`). An Identity endpoint cannot call `ICustomerProfileDirectory` without a direct `Identity.Endpoints → CustomerProfile.Application` ProjectReference, which the same `HostModuleEndpointOwnershipTests` explicitly forbids (`Assert.DoesNotContain(refs, r => r.Contains(".Infrastructure"))` plus Application-only composition rules) and `ARCH-COMPLETE-002` requires to be Contracts-only. `CustomerProfile.Contracts` does not currently carry `ICustomerProfileDirectory`. Deciding whether `me` belongs to Identity, to a storefront/session profile boundary, or needs a new `CustomerProfile.Contracts` port is an **unresolved architecture decision** — allowed stop condition #2.
4. **`CurrentAuthenticatedSession`, the middleware, the throttle seam and the ambient principal are NOT Identity HTTP.** They are the host-wide current-user plumbing consumed by production outside this folder. Evacuating the endpoint surface while leaving the session state in Host is coherent, but the split line between "Identity module HTTP" and "Host global auth/session/platform" is a canonical platform-boundary decision, not a mechanical folder move.
5. **Two durable guards pin the current path.** `CheckoutIdentityContractTests.cs:42` and `StorefrontAccountIdentityTests.cs:45` read `Authentication/AuthenticationHttpBoundary.cs` by path and assert `ICustomerProfileDirectory`, `IIdentityContactLookup`, `StorefrontAccountIdentity.CanonicalName`, `/otp-login/request`, `/otp-login/complete`. Relocating the file requires repointing those guards truthfully (not weakening them).
6. **Explicit repository evidence that this was already decided once.** `docs/architecture/41-authentication-http-boundary.md` declares the boundary `COMPLETE — Architect accepted TB-P02-T005`, records `DTOs are Host-owned`, and the Authentication item in the recovery checklist is literally `global authentication/session/tenant/correlation` (`TOOBA-TMAR-MASTER-RECOVERY.md:529`, `TOOBA-ARCHITECT-BOOTSTRAP.md:325`) — i.e. an explicitly Host-retained platform responsibility, with `TOOBA-RECOVERY-CONTEXT.md:634` recording `Authentication HTTP Boundary = COMPLETE`. `AGENTS.md` rule 11 forbids inventing requirements or redesigning locked architecture; `AGENTS.md` rule 15 forbids self-authorization.

### Per-file disposition

| File | Analyze verdict | Action |
| --- | --- | --- |
| `AuthenticationHttpBoundary.cs` | `NEEDS_ARCHITECT_DECISION` — genuine Identity HTTP capability, but destination foundation is `FOUNDATION_MISSING` and the `me` cross-module boundary plus the Host-global/platform split line are undecided; a full Identity Endpoints foundation is outside a bounded slice | **KEEP** (no move). Migrate only after Architect decides the Identity Endpoints foundation scope and the `me`/customer-profile boundary |
| `AuthenticationInstrumentation.cs` | `KEEP_AS_GENERIC_HOST_PLATFORM_INFRASTRUCTURE` (protocol line 71) | Retained |
| `AuthenticationRateLimitThrottleSeam.cs` | `KEEP_AS_GENERIC_HOST_PLATFORM_INFRASTRUCTURE` (protocol line 71) | Retained |
| `HostCurrentAuthenticatedUser.cs` | `KEEP_AS_GENERIC_HOST_PLATFORM_INFRASTRUCTURE` (protocol lines 65/70) | Retained |

Classification of the retained Host usage per the certify skill §7: `ALLOWED_COMPOSITION_ROOT`, `ALLOWED_SECURITY_ADAPTER`, `ALLOWED_CONTRACT_CONSUMPTION`. `ILLEGAL_ENDPOINT_OWNERSHIP` / `ILLEGAL_BUSINESS_AUTHORITY` / `ILLEGAL_PERSISTENCE_AUTHORITY` for this folder = **disputed-by-protocol, not clear-cut**; the protocol currently classifies these routes as Host-owned global auth. Because that classification is disputed, migration was halted instead of guessing (`AGENTS.md` 11/12/15; TMAR Host Evacuation stop conditions).

## 6. Run evidence

- Build: `dotnet build Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj -c Debug` → **Build succeeded, 0 Errors** (16 pre-existing warnings).
- Focused guards: `dotnet test … --filter CheckoutIdentityContractTests|StorefrontAccountIdentityTests|HostFolderStructureTests|TmarDurableGuardTests|HostModuleEndpointOwnershipTests|TmarCompleteReferenceStructureGateTests` → **Passed 24 / Failed 0 / Total 24**.
- No guard was weakened, no test was edited, no production file was changed.

## 7. Residual debt (honest, non-blocking)

- `AuthenticationHttpBoundary.cs` is 702 LOC and packs 8 responsibilities: session state holder, throttle seam interface, session middleware, 14 DTOs, 13 endpoints, 2 cross-cutting guards, error mapping. It is under `ARCH-SIZE-001` (800) but violates `ARCH-MODULE-FILE-001` (one cohesive responsibility per new file). Splitting it into `Authentication/SessionAuthenticationMiddleware.cs`, `Authentication/AuthenticationHttpModels.cs`, `Authentication/AuthenticationEndpointMapper.cs`, `Authentication/AuthenticationProblemMapping.cs` inside the same folder is a behavior-preserving, non-decision-bearing improvement and the recommended next bounded step for this folder — it does **not** relocate ownership.
- `Tooba.Identity` has no `Endpoints` project and is absent from `AddToobaCqrsFoundation`; `/v1/auth` therefore cannot ever satisfy `HOST-MODULE-ENDPOINT-001` until that foundation exists.
- `Identity.Application` exposes service interfaces rather than MediatR requests; converting `/v1/auth` to `Module.Endpoints → ISender → Module.Application` would be a behavior-risky rewrite of a locked, already-accepted boundary and requires explicit authorization.

## 8. Blocker (single, real)

**`NEEDS_ARCHITECT_DECISION`** — Decide `Authentication` folder ownership before any migration:

- **(A)** Keep `Authentication` as `KEEP_AS_GENERIC_HOST_PLATFORM_INFRASTRUCTURE` (consistent with `docs/architecture/41-authentication-http-boundary.md` `COMPLETE — Architect accepted TB-P02-T005`, `HOST-MODULE-ENDPOINT-001` line 162, and the `global authentication/session/tenant/correlation` Host checklist item). Then the only remaining work for this folder is the bounded internal split in §7.
- **(B)** Authorize a real Identity module foundation wave: `Tooba.Identity.Endpoints` project + `.slnx`, Application CQRS for the 13 requests (+ validator coverage guard), a `me`/customer-profile Contracts decision, and repointing the two path-pinned guards; then evacuate the `/v1/auth` boundary and certify Identity.

No other Host folder was inspected. Stopped here pending Architect/user review.
