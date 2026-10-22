# Semantic evidence preservation (W0/W1 handoff, changed paths only)

W2 moved **one type** and one import. No request, validator, resource, route, DTO, permission,
migration or schema file changed. This file records that the upstream semantic evidence still holds
after the move.

## Route → request → validator matrix (preserved, 16/16)

`route-matrix.js` and `validator-matrix.js` (in this folder) re-derive the matrix from the current
post-W2 sources.

* 16 routes (7 rates/coverage, 1 financial status, 3 operating schedule, 4 packaging policy,
  1 operational quote) — count and order unchanged.
* 16 distinct `ISender` requests, each request validator present (`16` request-level validators) plus
  `6` nested write-model validators = **22 validators across 4 files** (unchanged).
* Request → validator reachability survives the move: the validators are registered from the
  Application assembly (`AddValidatorsFromAssembly`/`AddToobaCqrsFoundation` in the module smoke
  tests), so no namespace change in Endpoints can hide a validator.
* Input provenance unchanged: `serviceCode` from the route (authoritative), `ratePolicyId` /
  `coverageGrantId` / `scheduleId` / `packagingPolicyId` from route constraints, payload from the
  typed JSON body, actor from the Host-injected authorizer.

## Error catalog → resources → fallback (W1 parity, preserved)

* Single stable-code home: `Tooba.Shipping.Contracts/Errors/ShippingErrorCodes.cs` — 44 declared
  `shipping.*` codes, unchanged.
* Single descriptor owner: `Endpoints/Errors/ShippingErrorCatalogContributor.cs` — 44 descriptors,
  HTTP 400 / `ErrorClassification.Business` / `ErrorSeverity.Warning`, unchanged.
* Single resource owner: `Endpoints/Resources/ShippingErrorResources.cs` + `ShippingErrors.resx` +
  `ShippingErrors.fa.resx` — neutral + Persian, canonical fallback, unchanged. Resource base name is
  still `Tooba.Shipping.Endpoints.Resources.ShippingErrors` because those files did not move; the
  embedded logical names therefore still resolve after the extraction (verified by the resource
  resolution assertions in `ShippingErrorCatalogAndResourcesTests`, which pass).
* Exactly one production registration site remains, and it is now the composition root
  `Endpoints/ShippingEndpointModule.cs` (guarded).

## Authorization (W1 parity, preserved)

* `Admin/ShippingAdminAuthorizer.cs` is untouched: it still consumes the scoped `ICurrentEdition` and
  `ICurrentTenant`, builds `AuthorizationCallContext` from `SingleStore`/`Marketplace` only, and fails
  closed with `admin.authorization.unavailable` when the context is missing/inactive/inconsistent or
  the edition is unset.
* The 20-case `ShippingAdminAuthorizerContextTests` matrix passed unchanged (part of the 74).

## Contracts-only / Host closure (preserved)

* No new project reference, no foreign Application/Infrastructure/Domain/Endpoints edge, no
  cross-module `DbSet`/`DbContext`/join, no schema change: W2 added **zero** references and touched no
  `.csproj`.
* Host delta is a single `using Tooba.Shipping.Endpoints;` in `Program.cs`; no Host file or folder was
  added, no business authority moved into the Host. `HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` /
  `HOST_ROOT_FINAL_CERTIFIED` checkpoints are untouched.
* The module remains `HTTP_OWNING`; its internal quote/lookup seams stay internal.

## Not claimed

No semantic certification, no validator-coverage verdict, no localization completeness verdict and no
module certification is claimed by this Structure wave.
