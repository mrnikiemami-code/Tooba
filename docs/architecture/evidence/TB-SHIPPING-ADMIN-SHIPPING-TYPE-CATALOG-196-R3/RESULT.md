# TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R3 — RESULT

Mode: `BOUNDED_TYPE_CATALOG_REAL_DELEGATE_AUTH_INPUT_EVIDENCE_ONLY`
Status: **PASS (bounded API/classification evidence only)**
Expected-Base-SHA: `46a136dc5e365e29a380f76d16e0ab4f6892b332`
Base-SHA: `46a136dc5e365e29a380f76d16e0ab4f6892b332`
Branch: `main`

This slice closes the missing actual-delegate/auth/body/validation proof and the exact four-route
classification for the existing shipping type catalog. Nothing in R2 (domain repairs) was modified and no
production C# / DI / Contracts / resource / schema / EF mapping / migration / snapshot file changed.

## Actual delegate, DI pipeline and fixture limits

Test file: `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/ShippingTypeCatalogEndpointTests.cs`
(42 tests; xUnit; EF **InMemory** explicitly labelled, never PostgreSQL proof).

Real composition exercised per request:

| Layer | Real or fixture |
| --- | --- |
| Endpoint discovery | Production `FulfillmentEndpointModule.MapFulfillmentEndpoints` on a built, never-started `WebApplication`; `RouteEndpoint.RequestDelegate` invoked directly — no Kestrel, no listener, no network |
| Route → delegate | The shipped four `Map*` calls (no test-side mapping of business routes) |
| Gate | Production `ShippingTypeAdminAuthorizer` resolved from production `AddFulfillmentEndpointPresentation` |
| Body | Production `ShippingTypeBodyReader` |
| Pipeline | Real MediatR `ISender` → `TracingBehavior` → `ValidationBehavior` → `LoggingBehavior` |
| Handlers | Real `CreateShippingTypeCommand` / `UpdateShippingTypeCommand` / `ListShippingTypesQuery` / `GetShippingTypeQuery` handlers |
| Store | Real `ShippingTypeStore` over `FulfillmentDbContext` (EF InMemory) |
| Language facts | Real `LocalizationShippingTypeLanguageFacts` over a controlled `ILanguageLookup`, with the production `ModuleCallTracer` registration from `AddToobaObservabilityFoundation` |
| Error surface | Real `ApiResponseFactory` + real `ExceptionPresentationService` + real `FulfillmentErrorCatalogContributor` + real `FulfillmentErrorResourceSet` |

Controlled fixtures (explicitly not production claims): `IAdminPanelAccess` (allow / reject-seller-only),
`IAuthorizationService` decision + call-context recorder, `ICurrentEdition` / `ICurrentTenant` snapshot,
`IClock` (fixed `2026-10-22T10:00:00Z`), `IIdGenerator` (production `UuidV7IdGenerator`), and the
`ILanguageLookup` rows: `fa` (active, `IsActive=true`, `SortOrder=1`, **default**) and `en` (active,
`IsDefault=false`, `SortOrder=2`, non-fa). This is one fixed registry fixture, not a live Localization DB and
not a dynamic-registry matrix.

`ISender` is decorated only to count dispatches; handlers, validation and the MediatR graph are untouched.
Language-lookup calls are counted at the `ILanguageLookup` boundary. Body-byte reads are counted on a
wrapping stream. All counters reset after the single seeded row is written, so every assertion counts only
the request under test.

## Bounded executed facts

1. **Authorized lifecycle (real delegates).** `POST` valid `code` + `sortOrder=0` + default-language name →
   `201`, canonical body, `isActive=false`, `revision=1`, `Location` = `/v1/admin/shipping-types/{realId}`.
   `GET` list and detail return the created row. `PUT` activate → `revision=2`; `PUT` deactivate and
   reactivate work through the real handler/store. Repeating the identical nonempty text is a true no-op:
   `revision` stays `2` and stored `UpdatedAt` is byte-identical. A stale identical `PUT`
   (`expectedRevision=1` against revision `2`) returns `409` `shipping_type.revision_conflict` and the stored
   revision stays `2`. A differing `code` rejects with `400` `shipping_type.code.invalid` and the code/revision
   are unchanged; an identical `code` in the body is accepted as immutable identity.

2. **Independent permissions / trusted installation.** With only `fulfillment.view`: both `GET`s succeed
   (one authorization check) while `POST`/`PUT` return `403 admin.authorization.denied` with **zero**
   additional dispatches and **zero** language lookups. Admin-panel rejection (labelled seller-only actor)
   denies all four routes with `403`, **zero** authorization checks, **zero** dispatches, **zero** body reads,
   **zero** language lookups — the seller-only claim is *not* made on the basis of a seller seam; it is the
   platform admin gate that refuses. Panel accepted but the capability denied also blocks before dispatch.
   Invalid installation context (`missing-edition`, `unknown-edition`, `single-store-no-tenant`,
   `single-store-disabled-tenant`, `marketplace-leftover-tenant`) yields `503`
   `admin.authorization.unavailable` with **zero** authorization checks and **zero** dispatches. Valid
   SingleStore/active-tenant forwards `Edition=SingleStore, TenantId=tenant-01`; valid Marketplace with no
   tenant forwards `Edition=Marketplace, TenantId=null`. Query string and headers carrying
   `tenantId`/`edition`/`ownerId` do not alter the trusted forwarded context.

3. **Authorize BEFORE body.** Unauthorized `POST`/`PUT` with malformed JSON: `403`,
   `stream ReadCalls = 0`, `SentCount = 0`, no handler, no registry read. Authorized malformed bodies
   (empty, `{`, `[]`, `null`, wrong scalar kinds, object-instead-of-array, non-guid language id) all resolve
   `400 shipping_type.body.malformed` through the real factory with **zero** dispatch (body shape is rejected
   before `ISender`) and nothing persisted. This is measured behaviour, not a source-order assertion.

4. **Lossless validation matrix.** `POST`: missing/`null`/negative `sortOrder`, stray-whitespace `code`,
   missing/`null` `translations`, `[valid, null]`, duplicate `LanguageId`, `Guid.Empty` language, blank name
   → `400`, `Dispatches = 1`, `LanguageLookups = 0` (handler never reached), persisted rows unchanged (only
   the seeded row). `PUT`: missing `sortOrder`/`isActive`/`expectedRevision`/`translations`, `null`
   translations, `expectedRevision=0`, `[valid, null]` → `400`, dispatched once, handler not reached,
   revision unchanged. No `null` element is filtered into a partial save. `GET` with `Guid.Empty` is rejected
   by the real `GetShippingTypeQueryValidator` (dispatched once, handler not reached).

5. **Four-route classification set equality.** Enumerated only the `/v1/admin/shipping-types*` endpoints from
   the production endpoint data source: exactly four —
   `GET /v1/admin/shipping-types`, `GET /v1/admin/shipping-types/{shippingTypeId:guid}`,
   `POST /v1/admin/shipping-types`, `PUT /v1/admin/shipping-types/{shippingTypeId:guid}` — compared in both
   directions against the capability-scoped, test-owned classification (no production framework, no global
   manifest edit). Classification: `ListShippingTypesQuery` → `NO_VALIDATOR_REQUIRED` (justified: zero
   properties, no route/query/body input, zero `IValidator<ListShippingTypesQuery>` in DI);
   `GetShippingTypeQuery` → `GetShippingTypeQueryValidator`; `CreateShippingTypeCommand` →
   `CreateShippingTypeCommandValidator`; `UpdateShippingTypeCommand` → `UpdateShippingTypeCommandValidator`.
   The three validators resolve with the exact expected types from the real DI pipeline.
   Negative detection: a synthetic extra route (`GET /v1/admin/shipping-types/export`) and a dropped
   classification (routes minus `PUT`) each throw — a count of 4 alone is not accepted.

6. **List stays parameterless.** `?languageId=<en>&culture=en-US` plus `Accept-Language: en-US` returns a
   byte-identical payload to the plain list; both raw stored `LanguageId`s (`fa` default, `en` non-default)
   survive for the row that carries both. The removed `languageId` binding is not reintroduced and no culture
   projection exists.

## Focused commands and counts

```
dotnet build src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Tooba.Fulfillment.Tests.csproj  -> 0 errors
dotnet test  ... --filter "FullyQualifiedName~ShippingTypeCatalogEndpointTests"
                                                                        -> Failed: 0, Passed: 42, Total: 42
```

Both commands ran inside the 120 s bound. No full Shipping/Fulfillment/Host/solution suite, no broad
Architecture filter, no Host build, no containers, migrations, seeds, server, browser or network probes.

## Explicitly still pending (not waived)

- Full multilingual registry lifecycle, `ActivityListener` assertions, exact-constraint
  (`ix_shipping_types_code`, `ix_shipping_type_translations_type_language`) negative proof,
  PostgreSQL/snapshot/relational/race proof: `PENDING_NOT_RUN`.
- TB-196 acceptance remains `PARTIAL_PENDING_REMAINING_PROOF`; this PASS is bounded API/classification
  evidence, not API-backed live merchant DB/browser proof and not production functional acceptance.
- EF InMemory was used; it cannot prove uniqueness constraints, relational integrity or snapshot behaviour.

## Exact failing behavior

None. No production defect surfaced in this slice.

## Changed files

- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/ShippingTypeCatalogEndpointTests.cs` (new)
- `docs/ai/tasks/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R3.task.md` (received artifact, verbatim)
- `docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R3/RESULT.md` (this file)
- `docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md` (Worker status pointer only)

Production schema, contracts, DI, resources, EF mapping, migrations and snapshots: **NONE**.
Shared database/data/flag changes: **NONE**. TB-185 untouched; PendingAllocation/default-OFF unchanged.
