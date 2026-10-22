# TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189 — Evidence

- Task: `TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189`
- Parent: `TB-SHIPPING-ADMIN-CONFIGURATION-CONTEXT-188`
- Program: `TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY`
- Mode: `BOUNDED_SERVER_AUTHORIZED_SHIPPING_RULE_OWNER_SCOPE`
- Channel: `tooba-main` (Bridge row `79428c67-7f0f-4e7a-b0b6-232a90ba780a`)
- Base: `5b74601fe6db10bde1575eb680b8104f2ac553a5` (HEAD == origin/main at claim)
- Stop state: `STOP_FOR_ARCHITECT`; automatic next task: none
- Status: `PASS` (bounded increment; no persisted owner isolation, no rule writes, no merchant-runtime acceptance)

## 1. What was delivered

Two real, authorized, read-only routes that report the CURRENT permitted Shipping rule owner, behind
one module-owned authorization seam and one shared Application use case:

| Route | Gate | Owner scope |
| --- | --- | --- |
| `GET /v1/admin/shipping-services/authoring-owner` | existing `IShippingAdminAuthorizer` + existing `fulfillment.view` | SingleStore => `Store` (resolved stable `TenantId`); Marketplace => `Marketplace` |
| `GET /v1/seller/shipping-services/authoring-owner` | resolved Marketplace context + `ISellerPanelAccess` (actor → SellerPartyId) + `IPlatformEffectiveAccessReader` for the SAME actor/owner with `fulfillment.view` at `GlobalWithinOwner` | `Seller` with that `SellerPartyId` |

Both are literal, parameterless routes that dispatch the single `GetShippingAuthoringOwnerQuery`
through `MediatR.ISender` and render `Result` via `ApiResponseFactory.From`.

## 2. Owner / permission / provenance matrix

| Situation | Owner kind | Authoritative source | Wire result |
| --- | --- | --- | --- |
| SingleStore, active tenant, admin `fulfillment.view` allowed | `Store` | `ICurrentEdition` + `ICurrentTenant` (stable `TenantId`, trimmed) via `IShippingAdminAuthorizer` | 200 `{ownerKind:"Store", sellerPartyId:null}` |
| Marketplace, admin `fulfillment.view` allowed | `Marketplace` | `ICurrentEdition` (no tenant allowed) via `IShippingAdminAuthorizer` | 200 `{ownerKind:"Marketplace", sellerPartyId:null}` |
| Marketplace, seller route, panel gate allowed, whole-owner `fulfillment.view` grant for that exact seller | `Seller` | `ISellerPanelAccess` (candidate header consumed only there) + `IPlatformEffectiveAccessReader(Seller, SellerPartyId)` | 200 `{ownerKind:"Seller", sellerPartyId:<authorized>}` |
| Seller route in SingleStore | — | capability unavailable, before seller resolution and before dispatch | 404 `shipping.authoring.owner_scope_unavailable` |
| Unresolved/Unset/unknown edition, inactive/absent tenant, Marketplace with leftover tenant | — | canonical trusted-context failure (same W1 gate) | 503 `admin.authorization.unavailable` |
| Panel gate fails (actor/party) | — | `ISellerPanelAccess` failure propagated unchanged | canonical Foundation error |
| Grant absent / category-only / product-only / brand-only / warehouse-only / `DeniedByCeiling` / other permission / other seller | — | effective-access read returns no whole-owner grant for the requested permission | 403 `shipping.authoring.owner_scope_denied` |
| `fulfillment.manage` requested while only `fulfillment.view` is granted | — | same reusable seam, different `permissionId` | 403 `shipping.authoring.owner_scope_denied` |
| Any header/query `ownerKind|sellerId|mode|tenant|edition` override | — | ignored: scope is built only from resolved context and canonical seams | unchanged result |

Owner identity is deliberately separate from `ActorUserId`, mode and display labels: the immutable
`ShippingAuthoringOwnerScope` exposes only `Kind`, `TenantId`, `SellerPartyId` and can only be created
through `ForStore` / `ForMarketplace` / `ForSeller`, whose invariants reject inconsistent combinations.
The compact view exposes only `ownerKind` and (for an authorized seller) `sellerPartyId`; no tenant id,
connection reference, mode, `canManageSellerRules`, `canSelectOwner`, foreign seller or token.

## 3. Real route / DI / span assertions

- `ShippingModuleStructureGuardTests` pins the composition entry: one `/v1/admin` group with **seven**
  mappers, one `/v1/seller` group with **one** mapper, and exactly one
  `IShippingAuthoringOwnerAuthorizer, ShippingAuthoringOwnerAuthorizer` registration; no `MapGroup`
  for either prefix outside `ShippingEndpointModule`.
- `ShippingRequestClassificationTests` derives each capability mapper's group prefix from the real
  composition entry (no hardcoded `/v1/admin`), keys manifest rows by request, and proves both owner
  routes map to exactly one classified request while every other route keeps its own row.
- `ShippingAuthoringOwnerEndpointTests` builds the **production** composition on a real
  `WebApplication` route builder and executes the real delegate, the real registered
  `ShippingAuthoringOwnerAuthorizer`, the real MediatR pipeline, the real handler, the real
  `ApiResponseFactory` and the real error catalog. A counting `IPipelineBehavior` proves
  `ISender` dispatch happened exactly once after authorization and zero times on every denial.
  Literal-vs-parameterized precedence is answered by ASP.NET Core's own `TemplateMatcher`
  sweep (both owner literals are claimed by exactly one template each).
- `ShippingAuthoringOwnerAuthorizerTests` exercises the production adapter with labelled fakes at
  the platform seam boundary and asserts the exact outgoing permitted-owner arguments
  (`actorUserId`, `PlatformAccessOwnerKind.Seller`, returned `SellerPartyId`).
- The effective-access read emits exactly one canonical child span
  `module.Shipping.AccessControl.GetEffectivePermissions` with `module.source=Shipping`,
  `module.target=AccessControl`, constant operation, inherited correlation id, correct parent and
  `Ok` status; failure records exception **type** only. Actor/seller ids are asserted absent from
  tags and the sentinel exception message is never exported.

## 4. Classification / input provenance

- `docs/architecture/shipping/shipping-request-classification.json` now declares 18 requests over
  19 routes (16 validated, 2 `NO_VALIDATOR_REQUIRED`, 6 nested transport validators, 22 declared).
- `GetShippingAuthoringOwnerQuery` is one row with **two** route provenance paths. `NO_VALIDATOR_REQUIRED`
  is legal because every field is derived by executed server authorization: no route value, query
  string or body is bound, both handlers construct the query from a server-built scope, and the guard
  proves the absence from source (no validator type, no route template parameter, no `Request.Query`,
  no `ReadFromJsonAsync`, no `[FromRoute]`/`[FromQuery]`). The seller handler is additionally asserted
  not to parse `Request.Headers` / `X-Tooba-Seller-Party` / `SellerPartyId` at all.
- No arbitrary unclassified route is tolerated: the guard still performs exact set equality between
  the shipped route universe and the manifest, both directions.

## 5. Error / localization / 188 parity

- New (explicitly classified) codes — nothing inferred from a name suffix:
  - `shipping.authoring.owner_scope_denied` — `Forbidden` / 403 / `Warning`
  - `shipping.authoring.owner_scope_unavailable` — `NotFound` / 404 / `Warning`
- Preserved unchanged: the 44 legacy `shipping.*` codes (400 / `Business` / `Warning`) and TB-188's
  single explicit 503 (`shipping.authoring.configuration_unavailable`, `Platform` / `Error`).
- Neutral + Persian resources added for both new codes; composed catalog ownership, fallback to the
  neutral resource for an uncovered locale, and the one-owner key space remain asserted.
- TB-188 review correction (1): the `ShippingAuthoringContextView.Edition` XML no longer claims that
  `Marketplace` means seller-scoped authoring is ready; it now states explicitly that edition alone
  does **not** establish that readiness. The route, the response shape and its 503 behavior are
  unchanged.

## 6. W1 extraction (mechanical, behavior-identical)

`ShippingInstallationContext.Resolve(ICurrentEdition, ICurrentTenant)` is the extracted private
edition/active-tenant validation that previously lived inside `ShippingAdminAuthorizer`. It keeps the
same `SingleStore`/`Marketplace` rules, the same `TenantId` trim, and the same
`admin.authorization.unavailable` failure. `ShippingAdminAuthorizer` now delegates to it; every W1
context test and every W1 source assertion still passes, and the owner gate reuses the same trusted
resolution instead of a parallel mode authority. No platform-wide authorizer, Host security file or
retained file set was modified.

## 7. Commands and counts actually run

| Command | Result |
| --- | --- |
| `dotnet build src/backend/Modules/Shipping/Tooba.Shipping.Tests/Tooba.Shipping.Tests.csproj` | succeeded, 0 errors (2 pre-existing analyzer warnings in `ShippingModuleStructureGuardTests.cs:99` and `ShippingRateCoverageAdminTests.cs:144`) |
| targeted `dotnet test` filter (owner scope/endpoint/authorizer + classification + structure + error catalog + 188 context + W1 context) | 149/149 passed |
| full `dotnet test src/backend/Modules/Shipping/Tooba.Shipping.Tests/Tooba.Shipping.Tests.csproj` (one final module suite) | 511/511 passed |

Only affected Shipping projects were built/tested; no full solution, Order, Host or architecture
suite was run in this task.

## 8. 188 review correction (2) — recorded scope deviation

The Architect noted that TB-188 ran `dotnet build src/backend/Tooba.slnx` against its
affected-project-only rule. That deviation is recorded here and was **not** repeated: this task built
and tested only the Shipping test project (which transitively builds the affected Shipping projects).

## 9. Legacy non-attribution / D3 boundary

Existing global rate/catalog/policy rows are not assigned, copied, exposed or listed by either new
route; nothing announces ownership of global Fulfillment catalog rows and no `canManageSellerRules`
flag exists. Quote behavior, persisted rule shape, schema and migrations are untouched. D3
persistence, legacy attribution and integrated UI remain separate future tasks, and this authority
model is not proof of stored-data isolation.

## 10. Source-vs-executed authority limitations

Source-bound platform seam contracts (`IShippingAdminAuthorizer`, `ISellerPanelAccess`,
`IPlatformEffectiveAccessReader`, `ICurrentEdition`, `ICurrentTenant`) are faked in tests and are
labelled as fakes. The production adapter, routing, MediatR pipeline, error rendering and tracing are
real. No real user identity store, merchant database, running server, container, migration or seed was
started, so real merchant-runtime authorization is `NOT_PROVEN`. `Worker PASS != Architect ACCEPT`;
no recertification is claimed and the W3-R1 certification remains the historical module baseline.

## 11. Isolation and cleanup

- Scoped worktree: only the files listed below changed; pre-existing untracked leftovers from earlier
  tasks (`.../TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1/worker-result.md`,
  `.../TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3/post-result.js`,
  `.../TB-SHIPPING-ADMIN-CONFIGURATION-CONTEXT-188/post-result.js`) were left untouched.
- Three pre-existing stashes were not applied, dropped or rewritten. No reset, rebase, stash or
  force-push. No database, container, package or migration action. No shared process restart.

## 12. Changed files

Implementation / guards:

- `src/backend/Modules/Shipping/Tooba.Shipping.Application/Authoring/Models/ShippingAuthoringOwnerKind.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Application/Authoring/Models/ShippingAuthoringOwnerScope.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Application/Authoring/Models/ShippingAuthoringOwnerView.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Application/Authoring/Queries/GetShippingAuthoringOwnerQuery.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Application/Authoring/Models/ShippingAuthoringContextView.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Contracts/Errors/ShippingErrorCodes.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Admin/ShippingInstallationContext.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Admin/ShippingAuthoringOwnerAuthorizer.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Admin/ShippingAuthoringOwnerEndpoints.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Seller/ShippingSellerAuthoringOwnerEndpoints.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Admin/ShippingAdminAuthorizer.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Errors/ShippingErrorCatalogContributor.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Resources/ShippingErrors.resx`
- `src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/Resources/ShippingErrors.fa.resx`
- `src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/ShippingEndpointModule.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Admin/ShippingAuthoringOwnerAuthorizerTests.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Authoring/ShippingAuthoringOwnerScopeTests.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Authoring/ShippingAuthoringOwnerEndpointTests.cs` (new)
- `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Admin/ShippingErrorCatalogAndResourcesTests.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Authoring/ShippingAuthoringContextEndpointTests.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Certification/ShippingRequestClassificationTests.cs`
- `src/backend/Modules/Shipping/Tooba.Shipping.Tests/Composition/ShippingModuleStructureGuardTests.cs`

Decision / manifests:

- `docs/architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md` (section 18, D1)
- `docs/architecture/shipping/shipping-request-classification.json`
- `docs/architecture/tmar-module-structure-manifests.json`
- `docs/architecture/tmar-current-state.json`

Evidence:

- `docs/ai/tasks/TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189.task.md`
- `docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189/RESULT.md` (this file)
- `docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189/task-receipt.json`
- `docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189/worker-result.md`

## 13. SHAs

- Expected base / base: `5b74601fe6db10bde1575eb680b8104f2ac553a5`
- Code commit: `504df9888238ad85d0b183fcba4bc41ff5a1b5ec`
- Evidence commit: the commit that adds this file; a commit cannot embed its own SHA, so the exact
  value is reported in the Bridge Result and in `worker-result.md`
- HEAD == origin/main after each push (verified by `git fetch` before and after)
- Elapsed: reported in `worker-result.md` (bounded well under the 45-minute hard timebox)
