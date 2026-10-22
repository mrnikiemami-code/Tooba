# TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196 — Worker result evidence

Worker: tooba-worker-01 · Channel: tooba-main · Claim: 2026-10-22T10:06Z
Result: 2026-10-22T11:08Z · Elapsed: ~62 min — HARD TIMEBOX (45 min) EXCEEDED
Verdict: **PARTIAL** — usable catalog API/reader code is complete and builds; the required
relational/route/race proof was not executed inside the timebox.

## Claimed base

- Expected-Base-SHA: `ae969bfa3783a4d99df172f419408e9a0a451ccc`
- HEAD at claim: `06faa7d583cff2ec2c072a1e1da227324d8ecdbb` (fast-forwarded, ancestor verified)
- HEAD == origin/main == base before any edit; no reset/rebase/stash/force-push.

## Delivered (additive, Fulfillment-owned)

| Concern | Location |
| --- | --- |
| Domain entity + translation, code/sort/name/description guards, revision | `Fulfillment.Domain/Aggregates/ShippingType.cs` |
| Read-only Contracts seam `IShippingTypeReader` (+ snapshots) | `Fulfillment.Contracts/Shipping/ShippingTypeCatalogContracts.cs` |
| Stable errors `shipping_type.*` | `Fulfillment.Contracts/Errors/FulfillmentErrorCodes.cs` |
| CQRS create/update/list/get + validators | `Fulfillment.Application/ShippingTypes/**` |
| Language facts from authoritative `ILanguageLookup` | `Fulfillment.Infrastructure/Shipping/ShippingTypeLanguageFacts.cs` |
| Store + reader impl, SQLSTATE unique mapping, concurrency translator | `Fulfillment.Infrastructure/Shipping/ShippingTypeStore.cs` |
| EF mapping, checks, indexes, revision concurrency token | `Fulfillment.Infrastructure/Persistence/FulfillmentDbContext.cs` |
| One additive migration + designer + snapshot | `Persistence/Migrations/20261022101415_ShippingTypeCatalog.*` |
| Module-owned independent view/manage gate + 4 routes | `Fulfillment.Endpoints/Admin/ShippingTypeAdminEndpoints.cs` |
| Error descriptors + neutral/fa resources | `FulfillmentErrorCatalogContributor.cs`, `Resources/FulfillmentErrors*.resx` |
| DI wiring (store, reader, language facts, authorizer) | `FulfillmentModule.cs`, `FulfillmentEndpointModule.cs` |
| Focused rule tests | `Fulfillment.Tests/Behavior/ShippingTypeCatalogRulesTests.cs` |

Actual routes (composition prefix `/v1/admin` + group `/shipping-types`):

```text
GET  /v1/admin/shipping-types/
GET  /v1/admin/shipping-types/{shippingTypeId:guid}
POST /v1/admin/shipping-types/
PUT  /v1/admin/shipping-types/{shippingTypeId:guid}
```

## Semantics implemented

- Create is always `IsActive = false`, `Revision = 1`; client id/active/revision/owner/tenant ignored.
- Canonical immutable ASCII code (`^[a-z][a-z0-9_]*$`, 1..64), rejected not renamed; PUT with a
  different code is rejected (`shipping_type.code.invalid`).
- Sort order `0..1_000_000`, explicit. Name required nonblank ≤128; description optional ≤512.
- PUT requires explicit `expectedRevision`, `isActive`, `sortOrder`, `translations`; absent fields are
  rejected at the endpoint (`shipping_type.field_missing`) rather than silently becoming `false`/`0`.
- Translations patch only listed `LanguageId`s; `[]` preserves all text. Inactive-language stored text
  is never deleted and is returned raw by GET.
- Activation requires a stored/proposed name for the currently active default language; absence →
  `shipping_type.default_language.name_required`. Registry without usable active default →
  `shipping_type.default_language.unavailable` (fail closed, never empty success).
- Stale `expectedRevision` is checked before the no-op branch, so an identical payload with an old
  revision is still a conflict. A genuine no-op preserves `Revision` **and** `UpdatedAt`.
- Revision is an EF concurrency token plus `ck_shipping_types_revision_positive`; concurrency failure
  maps to `shipping_type.revision_conflict`; unique code/translation map from SQLSTATE `23505` on the
  named constraint only; other provider errors/cancellation propagate.
- `IShippingTypeReader` is a separate read-only registrations; `ListActiveAsync` filters active and
  `GetByIdsAsync` may return inactive facts for honest historical display. Legitimately empty catalog is
  an empty list; unavailable dependency/schema is not converted to `[]`.

## Commands actually executed

```text
git fetch origin; git merge-base --is-ancestor; git merge --ff-only origin/main
dotnet build .../Tooba.Fulfillment.Endpoints.csproj            -> succeeded, 0 warnings/0 errors
dotnet build .../Tooba.Fulfillment.Infrastructure.csproj       -> succeeded, 0 warnings/0 errors
dotnet ef migrations add ShippingTypeCatalog (regenerated after index rename)
dotnet ef migrations has-pending-model-changes                 -> "No changes ... since the last migration"
dotnet build .../Tooba.Fulfillment.Tests.csproj                -> succeeded
dotnet test  .../Tooba.Fulfillment.Tests.csproj --filter ShippingTypeCatalogRulesTests
                                                               -> Passed 21, Failed 0, Total 21
```

## Not executed (honest gaps)

- Real PostgreSQL 17 container migration + fresh-context roundtrip and raw-SQL negative matrix.
- Route/harness proof with a controlled platform fixture: view-only write denial, seller-only grant
  denial, unresolved/mismatched installation fail-closed, canonical `Location`/201 body.
- Deterministic two-reader revision race and concurrent unique-code create race.
- Module-call tracing assertion for the language-lookup call (no `IModuleCallTracer` span was added;
  the existing shipping-service language gate also reads `ILanguageLookup` without one).
- Whole-Fulfillment suite, Host.Tests, solution build, architecture guards.

## Pre-existing repository defect encountered

The committed `FulfillmentDbContextModelSnapshot.cs` was stale (2 `ToTable` entries vs 14 existing
migrations), so `dotnet ef migrations add` initially produced a migration that recreated every table.
The generated `Up`/`Down` were replaced with the two additive tables only; the snapshot was regenerated
from the real model (now consistent: `has-pending-model-changes` reports none). Historical migration
files are byte-identical (only new files added; snapshot modified).

## Limits

No production/functional acceptance, no frontend, no Shipping assignment, no activation/deployment,
no item/offer compatibility, no customer grouping/pricing. State is STOP_FOR_ARCHITECT.
