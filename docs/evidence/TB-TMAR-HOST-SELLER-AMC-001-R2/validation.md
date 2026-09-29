# Host/Seller — Seller-R2 — Validation

**Task:** TB-TMAR-HOST-SELLER-AMC-001-R2
**Environment:** local PowerShell + `git`, `dotnet`, `D:\Users\User\source\repos\SarvNewVer`
**Scope:** focused only. No solution-wide test run. No unrelated module suites.

## 1. Focused builds

| Project | Result |
| --- | --- |
| `Tooba.Catalog.Application` | PASS — 0 errors |
| `Tooba.Catalog.Infrastructure` | PASS — 0 errors |
| `Tooba.Catalog.Endpoints` | PASS — 0 errors |
| `Tooba.Host` | PASS — 0 errors |
| `Tooba.Host.Tests` | PASS — 0 errors (12 warnings, all pre-existing/xUnit analyzer style) |

## 2. Focused tests

```
dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj \
  --filter "HostSellerAmcR2GuardTests|HostSellerAmcR1GuardTests|HostOrderReverseAuditGuardTests|HostModuleEndpointOwnershipTests|HostAdminAmcW10R1GuardTests|SellerPanelCompositionTests"
```

Result: **Passed! Failed: 0, Passed: 40, Skipped: 0, Total: 40.**

The new durable guard `HostSellerAmcR2GuardTests` passes its full set:

| Guard fact | Proves |
| --- | --- |
| `Host_seller_owns_exactly_four_routes_and_no_catalog_route` | no Catalog route in Host; Host mappings = 2 + settings 2 |
| `Catalog_owns_the_three_seller_routes_exactly_once` | all three templates in Catalog; `ISender`/`ApiResponseFactory`/`ICatalogSellerAuthorizer` present; no `DbContext`/`ICatalogDirectory`/`Results.Json`; single `MapCatalogSellerEndpoints()`; `Program.cs` uses `MapCatalogModuleEndpoints()` only |
| `Catalog_seller_endpoint_reachable_requests_follow_canonical_cqrs_and_validator_classification` | query + both reused commands reachable; AUTH_SCOPED_QUERY has no validator; W10/W11 classification unchanged |
| `Host_seller_has_zero_catalog_persistence_and_zero_catalog_layer_leakage` | scans **every** `Host/Seller/*.cs` line for `CatalogDbContext`/`Tooba.Catalog.Domain`/`.Infrastructure`/`.Application`; composer has no `ListCatalogVariantsAsync`/`CatalogPublicationStatus`/`LocalizedTexts`; models have no `SellerCatalogVariantOption` |
| `Catalog_owns_seller_variant_model_parity_and_published_ordering` | DTO field parity; directory keeps Published/Take(100)/UpdatedAt/fa-prefix/CatalogCodeSeam; module registration |
| `Seller_catalog_error_codes_keep_parity_and_no_duplicate_descriptor_is_registered` | `seller.missing` consumed not re-registered; `catalog.attribute.invalid` + `catalog.variant.axes.duplicate` descriptor-backed |
| `Host_catalog_seller_authorizer_is_a_thin_security_adapter_inside_the_r1a_boundary` | adapter namespace/port/`ISellerPanelAccess`; no `RequestServices`/`DbContext`/Catalog layer; `Program.cs` registration; R1A gate codes intact |
| `No_route_sink_regression_and_no_new_host_seller_business_file_was_added` | `Host/Seller` remains exactly 5 business files; no `HostCatalogSellerAuthorizer.cs` under `Host/Seller` |

`HostSellerAmcR1GuardTests` (R1A boundary) still passes — the R1A boundary was not weakened.
`HostOrderReverseAuditGuardTests` and `HostModuleEndpointOwnershipTests` still pass.
`SellerPanelCompositionTests` still passes (header constants + dashboard query intact).
`HostAdminAmcW10R1GuardTests` was updated in place: the Seller `SetProductAttributeRequest`
RawValue/EnumOptionId shape assertion now points at the Catalog-owned surface and additionally
asserts the Host file no longer contains it (no assertion weakened — the invariant is preserved,
only its new location is asserted).

## 3. Recovery durable guard

`TmarDurableGuardTests` is the durable recovery-SoT freshness guard. Its current-checkpoint
assertions were updated in place to the R2 checkpoint (`lastAcceptedTask` = R2, top-level and
`currentHostEvacuation` `workflowStop`/`nextTask` = `USER_REVIEW_HOST_SELLER_AMC_001_R2`,
`currentTask` = R2), the historical Seller R1A/R1B and Development assertions were preserved, and
new negative/uniqueness assertions were added for the R2 stop value. The two SHA/pointer facts
(`implementationCommit`, `sotStamp` exist on `main` and are ancestors of HEAD) are satisfied after
the R2 commit/push; the guard reads those SHAs from `tmar-current-state.json`.

## 4. Structural verification

| Command | Result |
| --- | --- |
| `dir src/backend/Host/Tooba.Host/Seller` | exactly 5 business files |
| `dirname HostCatalogSellerAuthorizer.cs` | `Host/Security/Seller` (inside the R1A boundary) |
| `Select-String -Path Host/Seller/*.cs -Pattern CatalogDbContext` | 0 matches |
| `Select-String -Path Host/Seller/*.cs -Pattern 'Tooba.Catalog.(Domain|Infrastructure|Application)'` | 0 matches |
| `Select-String -Path Host/Seller/SellerPanelComposer.cs -Pattern ListCatalogVariantsAsync` | 0 matches |
| `Select-String -Path Host/Seller/SellerPanelModels.cs -Pattern SellerCatalogVariantOption` | 0 matches |
| `tmar-source-size-baseline.json` | `SellerPanelComposer.cs` oversized entry removed (934 -> 27 LOC) |
| `git diff --name-only -- src/frontend` | empty — frontend unchanged |

## 5. Scope discipline

No frontend change. No schema/migration change. No reset/clean/rebase/force-push. Seller-R3 not
started. User work preserved (untracked user artifacts and pre-existing modifications left
untouched).
