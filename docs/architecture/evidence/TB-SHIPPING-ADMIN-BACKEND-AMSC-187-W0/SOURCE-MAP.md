# TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W0 — Source map and applicability

Inspected commit: `68d1aeb3525e8b2d43c719578b433874d26e09d4` (branch `main`, `HEAD == origin/main`).
Mode: `BOUNDED_AMSC_ANALYZE_CONTRACTS_AND_LEGACY_MAPPING_ONLY`. Analysis only; no production, schema, test, config or runtime change.
All statements below are read from the repository at the inspected commit unless explicitly marked `UNKNOWN`.

## 1. Analyzed surface (bounded)

| Module | Projects in scope | Why in scope |
|---|---|---|
| `Tooba.Shipping` | Domain, Contracts, Application, Infrastructure, Endpoints, Tests | Owns shipping rate/coverage/packaging/schedule **policy** and the Admin authoring routes |
| `Tooba.Fulfillment` | Contracts/Shipping/*, Endpoints/Shipping/*, Application/Shipping/* | Owns **method identity/catalog/translations/activation** consumed by the same Admin screen |
| `Tooba.Geography` | Contracts/Places/* used by Shipping; Admin geography read routes called by the Admin client | Owns **destination identity and hierarchy** |
| `Tooba.Localization` | Contracts/Ports/LanguageLookupContracts.cs, Infrastructure/Languages/LanguageDirectory.cs, Adapters/LanguageLookupBridge.cs, Endpoints/Admin/LocaleAdminEndpoints.cs, Domain/Aggregates/Language.cs | Owns **language authority** (interface locale + content language registry) |
| `Tooba.StoreContext` | Contracts/Current/StoreCommerceContext.cs, Infrastructure/Current/StoreCommerceContextAccessor.cs | Read-only store commerce context seam |
| `Tooba.BuildingBlocks` | CommerceContext.cs (Edition/Tenant), Security/IPlatformAccessSeams.cs, Results, Presentation | Platform edition/context + canonical result/authorization seams |
| `Tooba.Host` | MultiTenancy/TenantResolutionMiddleware.cs, Configuration/*, Composition/ToobaModuleComposition.cs, Program.cs, Security/Seller/SellerPanelAccess.cs, Admin access seams, Persistence/MigrationRunner | Assigns the store context, composes the Shipping module, owns the platform auth boundaries |

Explicitly **not** re-audited: certified module internals outside the direct shipping authoring path, Host folders unrelated to this workflow, the opt-in findings register.

## 2. Applicability classification (`HTTP_OWNING` / `INTERNAL_ONLY` / `MIXED_OR_UNKNOWN`)

| Owner | Classification | Proof |
|---|---|---|
| `Tooba.Shipping` | `HTTP_OWNING` | `Tooba.Shipping.Endpoints/Admin/ShippingRateCoverageEndpoints.cs:55-64` (+ financial/schedule/packaging/operational maps) and `ShippingEndpointModule.MapShippingEndpoints` called from `Host/Tooba.Host/Program.cs:479` |
| `Tooba.Fulfillment` (shipping capability) | `HTTP_OWNING` | `Tooba.Fulfillment.Endpoints/Shipping/ShippingServiceEndpoints.cs:46-80`, `Shipping/ShippingMethodsEndpoints.cs:14`; mapped under `/v1/admin` by the certified Fulfillment endpoint module |
| `Tooba.Geography` | `HTTP_OWNING` | Admin client calls `/v1/admin/geography/countries|administrative-areas|cities|service-areas` (see `src/frontend/app/admin/shipping-services/**` clients) |
| `Tooba.Localization` | `HTTP_OWNING` (registry) + `INTERNAL_ONLY` (lookup seam) | `Localization.Endpoints/Admin/LocaleAdminEndpoints.cs:15-22` maps `/v1/admin/languages`; `ILanguageLookup` is a Contracts-only internal seam |
| `Tooba.StoreContext` | `INTERNAL_ONLY` | No route; only `ICurrentStoreCommerceContext` / `IStoreCommerceContextAssigner` Contracts |
| `Tooba.BuildingBlocks` (edition/tenant) | `INTERNAL_ONLY` | Typed platform contracts only; assigned by Host middleware |
| Shipping read seams (`IShippingRateQuoteLookup`, `IShippingPackagingFeeLookup`, `IShippingShipmentCostLookup`, `IShippingOperationalQuoteLookup`) | `INTERNAL_ONLY` | Contracts + DI in `ShippingModule.AddServices`; no HTTP route |

No module in scope is `MIXED_OR_UNKNOWN`.

## 3. Shipped Shipping admin routes (module-owned)

Source: `Tooba.Shipping.Endpoints/Admin/*.cs`; group prefix `/v1/admin` from `ShippingEndpointModule.MapShippingEndpoints`.

| # | Verb + path | Endpoint delegate | Permission |
|---|---|---|---|
| 1 | `GET /v1/admin/shipping-services/{serviceCode}/rates` | `ShippingRateCoverageEndpoints.ListAsync` | `fulfillment.view` |
| 2 | `POST /v1/admin/shipping-services/{serviceCode}/rates` | `CreateRateAsync` | `fulfillment.manage` |
| 3 | `PUT /v1/admin/shipping-services/{serviceCode}/rates/{ratePolicyId:guid}` | `UpdateRateAsync` | `fulfillment.manage` |
| 4 | `PATCH /v1/admin/shipping-services/{serviceCode}/rates/{ratePolicyId:guid}/active` | `SetRateActiveAsync` | `fulfillment.manage` |
| 5 | `POST /v1/admin/shipping-services/{serviceCode}/coverage-grants` | `CreateGrantAsync` | `fulfillment.manage` |
| 6 | `PATCH /v1/admin/shipping-services/{serviceCode}/coverage-grants/{coverageGrantId:guid}/active` | `SetGrantActiveAsync` | `fulfillment.manage` |
| 7 | `PUT /v1/admin/shipping-services/{serviceCode}/method-coverage` | `SetMethodCoverageAsync` | `fulfillment.manage` |
| 8 | `GET /v1/admin/shipping-services/{serviceCode}/financial-status` | `ShippingMethodFinancialStatusEndpoints.ReadAsync` | `fulfillment.view` |
| 9 | `GET /v1/admin/shipping-services/{serviceCode}/operating-schedules` | `ShippingOperatingScheduleEndpoints.ListAsync` | `fulfillment.view` |
| 10 | `PUT /v1/admin/shipping-services/{serviceCode}/operating-schedules` | `UpsertAsync` | `fulfillment.manage` |
| 11 | `PATCH /v1/admin/shipping-services/{serviceCode}/operating-schedules/{scheduleId:guid}/active` | `SetActiveAsync` | `fulfillment.manage` |
| 12 | `POST /v1/admin/shipping-services/{serviceCode}/operational-quote` | `ShippingOperationalQuoteEndpoints.EvaluateAsync` | `fulfillment.view` (read-only evaluation) |
| 13 | `GET /v1/admin/shipping-services/{serviceCode}/packaging-policies` | `ShippingPackagingPolicyEndpoints.ListAsync` | `fulfillment.view` |
| 14 | `POST /v1/admin/shipping-services/{serviceCode}/packaging-policies` | `CreateAsync` | `fulfillment.manage` |
| 15 | `PUT /v1/admin/shipping-services/{serviceCode}/packaging-policies/{packagingPolicyId:guid}` | `UpdateAsync` | `fulfillment.manage` |
| 16 | `PATCH /v1/admin/shipping-services/{serviceCode}/packaging-policies/{packagingPolicyId:guid}/active` | `SetActiveAsync` | `fulfillment.manage` |

Note: `ShippingAdminPermissions.View = "fulfillment.view"`, `Manage = "fulfillment.manage"` — reused platform permissions, no new catalog entries (`ShippingRateCoverageEndpoints.cs:33-40`).

Fulfillment-owned routes the same Admin screen consumes (certified module; listed for boundary clarity, **not** re-audited):

`GET|POST /v1/admin/shipping-services/`, `GET|PUT /v1/admin/shipping-services/{serviceId}`, `POST /v1/admin/shipping-services/{serviceId}/deactivate`, `POST /v1/admin/shipping-services/ensure-seed`, `GET /v1/admin/shipping-services/shipping-methods/one-level`, `GET /v1/admin/shipping-services/shipping-methods/one-level/{methodCode}/binding`, `PUT .../{methodCode}/translations/{languageId}`, `PUT .../{methodCode}/activation`, `POST /v1/admin/shipping-services/shipping-methods/one-level`, plus `GET /v1/admin/shipping-methods`.

Documented pre-existing exception in the certified module: `ShippingMethodsEndpoints.ListShippingMethodsAsync` carries **no admin auth gate** with the explicit comment "Preserve prior public behavior". Recorded as a known lock, not as a new finding to repair in W0.

## 4. Project dependency graph (Shipping module)

Verified from the `.csproj` files:

```
Tooba.Shipping.Contracts        -> BuildingBlocks, Geography.Contracts
Tooba.Shipping.Domain           -> BuildingBlocks, Shipping.Contracts
Tooba.Shipping.Application      -> BuildingBlocks, Shipping.Domain, Shipping.Contracts
Tooba.Shipping.Infrastructure   -> BuildingBlocks, ModuleContracts, Persistence,
                                   Shipping.Contracts, Shipping.Domain, Shipping.Application,
                                   Geography.Contracts, Fulfillment.Contracts
Tooba.Shipping.Endpoints        -> BuildingBlocks, Shipping.Contracts, Shipping.Application
```

Findings:
- No reference to `Tooba.Fulfillment.Application|Infrastructure|Domain`, no `Tooba.Geography.Application|Infrastructure|Domain`, no foreign `DbContext`. Contracts-only is proven by project references, not by convention.
- `Tooba.Shipping.Contracts` -> `Tooba.Geography.Contracts` is a **Contracts→Contracts** edge (reuses `DestinationIdentifiers` in `ShippingRatePolicyContracts.cs`, `ShippingPackagingContracts.cs`, `ShippingOperationalQuoteContracts.cs`). Legal, but it makes Geography's contract assembly a transitive dependency of every Shipping consumer. Reported as an observation, not a violation.
- Runtime composition: `Tooba.Host/Composition/ToobaModuleComposition.cs:98` registers `new ShippingModule()`; `Program.cs:203` registers the Shipping Application assembly for CQRS + validators; `Program.cs:246` binds `IShippingAdminAuthorizer` -> `ShippingAdminAuthorizer`.
- Migrations: `Tooba.Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs:93` describes `ShippingDbContext` for the **explicit CLI** (`status|plan|apply`) only. Host startup registers **no** Shipping `IModuleSchemaMigrator`, so no Shipping DDL is emitted at boot. Schema changes therefore require an operator-run migration step — a handoff constraint, not a defect.

## 5. Persistence surface (`shipping` schema)

`Tooba.Shipping.Infrastructure/Persistence/ShippingDbContext.cs` (`Schema = "shipping"`), tables:

| Table | Aggregate | Key | Notable invariants |
|---|---|---|---|
| `rate_policies` | `ShippingRatePolicy` | `RatePolicyId` | unique `(service_code, option_code, country, area, city, service_area, min/max weight, min/max count)` with `NULLS NOT DISTINCT`; CHECK mirrors for charge/day/currency/tier coherence |
| `coverage_grants` | `ShippingCoverageGrant` | `CoverageGrantId` | unique `(service_code, option_code, scope)` |
| `method_coverages` | `ShippingMethodCoverage` | `MethodCoverageId` | unique `(service_code, option_code)`; `mode` constrained to `Unrestricted|Restricted` |
| `operating_schedules` + `operating_windows` | `ShippingOperatingSchedule` / `ShippingOperatingWindow` | `ScheduleId` / `WindowId` | unique `(service_code, option_code)`; windows half-open `end_time > start_time`; weekday 1..7; capacity positive |
| `packaging_policies` | `ShippingPackagingPolicy` | `PackagingPolicyId` | unique `(service_code, option_code, scope)`; payer mode constrained |
| `outbox_messages` | `OutboxMessage` | — | module outbox skeleton, no consumer |

**No table carries an owner, seller, merchant or tenant column.** Every identity key is `(service_code, option_code[, scope])`.

Migrations present: `InitialShipping`, `OptionLevelRatePolicy`, `OperatingSchedule`, `WeightAndCountRateTiers`, `ShippingPackagingPolicy`, `OptionScopedCoverageRestriction` (directory `Infrastructure/Persistence/Migrations`).

## 6. Direct live Admin client evidence

`src/frontend/app/admin/shipping-services/**` calls exactly the routes in §3 plus the Fulfillment and Geography routes above; content languages are read from `GET /v1/admin/languages` (`src/frontend/features/admin-languages/api/language-api.ts:18` and `src/frontend/app/admin/shipping-services/shipping-method-language-model.ts:8-16`). The design preview at `src/frontend/app/admin/shipping-services/design-preview/**` is **UX evidence only** and is not imported by any production client or by the backend.
