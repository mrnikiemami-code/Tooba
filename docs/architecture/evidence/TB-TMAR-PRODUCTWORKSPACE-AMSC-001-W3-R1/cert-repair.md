# TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R1 — cert-blocker repair

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R1
Parent-Task: TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: CERT_BLOCKER_REPAIR_ONLY
Starting-HEAD: c0b86feacd897d26172fef7dff47b93db75d8e9d
Skill: tooba-architecture-certify
```

## 1. Architect verdict being repaired

W2 Structure was accepted. W3 certification was **not** accepted as final because the disk/SoT
violated two Certify hard rules:

| # | Blocker | Evidence in the superseded tree |
| - | ------- | ------------------------------- |
| B1 | Request inventory inconsistent + validator coverage unclassified | 17 routes / 3 queries + 14 commands on disk, but SoT recorded `endpointReachableRequests = 18` and `validatorCoverageState = EXHAUSTIVE_0_REQUIRED_0_NO_VALIDATOR_REQUIRED_...` (0/0 is not a classification; every request must be classified exactly once) |
| B2 | Raw 201 mapping | `ProductWorkspaceEndpointModule` returned `Results.Json(workspace.Value, statusCode: StatusCodes.Status201Created)` on both successful 201 paths (`CreateProductAsync` and the `MutateAsync(created: true)` path used by `CreateVariantAsync`) |

Goal: repair only these blockers and return `READY_FOR_FRESH_CERTIFY`. No fresh Certify in this task.

## 2. B2 — canonical API result (CLOSED)

Repair (2 lines, 1 file):

```text
-        return Results.Json(workspace.Value, statusCode: StatusCodes.Status201Created);
+        return api.Created($"/v1/admin/products/{workspace.Value.ProductId}", workspace);

-        return created
-            ? Results.Json(workspace.Value, statusCode: StatusCodes.Status201Created)
-            : api.From(workspace);
+        return created
+            ? api.Created($"/v1/admin/products/{workspace.Value.ProductId}", workspace)
+            : api.From(workspace);
```

| Field | Value |
| ----- | ----- |
| Raw `Results.Json` occurrences before | `2` |
| Raw `Results.Json` occurrences after | `0` (verified across every module production `.cs`) |
| `api.Created(` call sites | `2` (create product, create variant) |
| Api-Result-State | `CANONICAL` |
| Failure path | `UNCHANGED_PROBLEM_DETAILS_WITHOUT_LOCATION` |

**Contract preservation.** The canonical `ApiResponseFactory.Created<T>` already returned
`Microsoft.AspNetCore.Http.Results.Created(location, result.Value)` — status `201` plus a JSON body —
so this repair removes a duplicated mapping rather than changing the wire contract. The only additive
wire change is that the two paths now set `Location: /v1/admin/products/{productId}`, which is the
documented contract of `ApiResponseFactory.Created` ("موفقیت → 201 + Location + JSON خام") and was
missing before.

## 3. B1 — exhaustive 17-request validator matrix (CLOSED)

Inventory proven from disk: 17 module-owned routes on `/v1/admin/products`, 17 endpoint-reachable
module-local CQRS request types (`3` queries + `14` commands), each mapped exactly once.

```text
endpointReachableRequests = 17
  3  queries
 14  commands
validatorRequiredCount     =  0
noValidatorRequiredCount   = 17
classificationSumCheck     = "0 VALIDATOR_REQUIRED + 17 NO_VALIDATOR_REQUIRED = 17 = endpointReachableRequests = distinct IRequest types = mapped routes"
validatorCoverageState     = EXHAUSTIVE_0_REQUIRED_17_NO_VALIDATOR_REQUIRED_CATALOG_OWNS_MUTATION_BOUNDARY
```

### 3.1 Queries (3)

| Request | Route | Classification | Reason |
| ------- | ----- | -------------- | ------ |
| `GetProductWorkspaceQuery` | `GET /v1/admin/products/{productId:guid}` | `NO_VALIDATOR_REQUIRED` | `ROUTE_BOUND_GUID_AND_SERVER_RESOLVED_PERMISSION_SCOPE` — only transport-derived input is the route-constrained Guid plus `ProductWorkspacePermissions` derived server-side from the `X-Tooba-Workspace-Scope` header |
| `ListProductWorkspaceQuery` | `GET /v1/admin/products/` | `NO_VALIDATOR_REQUIRED` | `NO_TRANSPORT_INPUT` — parameterless; no route/query/header/body value exists to validate |
| `QueryProductWorkspaceGridQuery` | `POST /v1/admin/products/query` | `NO_VALIDATOR_REQUIRED` | `VALIDATED_BY_CANONICAL_MODULE_POLICY_NOT_FLUENTVALIDATION` — `AdminProductGridQueryPolicy` owns paging bounds, search length, sortable/filterable allowlists, per-field operator allowlists and advanced-filter connectors; it throws `GridQueryValidationException`, mapped by the handler to a `SemanticError` with the stable grid code. A FluentValidation validator here would duplicate that canonical policy |

### 3.2 Commands (14)

| Request | Route | Classification | Reason |
| ------- | ----- | -------------- | ------ |
| `CreateWorkspaceProductCommand` | `POST /v1/admin/products/` | `NO_VALIDATOR_REQUIRED` | `CATALOG_WRITE_CAPABILITY_OWNS_TRANSPORT_SHAPE` |
| `UpdateWorkspaceProductCatalogTitleCommand` | `PATCH /v1/admin/products/{productId:guid}/catalog-title` | `NO_VALIDATOR_REQUIRED` | `CATALOG_WRITE_CAPABILITY_OWNS_TRANSPORT_SHAPE` |
| `UpdateWorkspaceProductCoreCommand` | `PATCH /v1/admin/products/{productId:guid}/core` | `NO_VALIDATOR_REQUIRED` | `CATALOG_WRITE_CAPABILITY_OWNS_TRANSPORT_SHAPE` |
| `UpdateWorkspaceProductQuantityPolicyCommand` | `PATCH /v1/admin/products/{productId:guid}/quantity-policy` | `NO_VALIDATOR_REQUIRED` | `CATALOG_WRITE_CAPABILITY_OWNS_TRANSPORT_SHAPE` |
| `AssignWorkspaceProductCategoryCommand` | `PUT /v1/admin/products/{productId:guid}/category` | `NO_VALIDATOR_REQUIRED` | `CATALOG_WRITE_CAPABILITY_OWNS_TRANSPORT_SHAPE` |
| `AddWorkspaceProductAdditionalCategoryCommand` | `POST /v1/admin/products/{productId:guid}/categories/additional` | `NO_VALIDATOR_REQUIRED` | `CATALOG_WRITE_CAPABILITY_OWNS_TRANSPORT_SHAPE` |
| `RemoveWorkspaceProductAdditionalCategoryCommand` | `DELETE /v1/admin/products/{productId:guid}/categories/additional/{categoryId:guid}` | `NO_VALIDATOR_REQUIRED` | `ROUTE_BOUND_GUIDS_PLUS_ENDPOINT_ENFORCED_CONCURRENCY_STAMP` |
| `AssignWorkspaceProductBrandCommand` | `PUT /v1/admin/products/{productId:guid}/brand` | `NO_VALIDATOR_REQUIRED` | `CATALOG_WRITE_CAPABILITY_OWNS_TRANSPORT_SHAPE` |
| `PublishWorkspaceProductCommand` | `POST /v1/admin/products/{productId:guid}/publish` | `NO_VALIDATOR_REQUIRED` | `NO_BODY_ROUTE_GUID_PLUS_SERVER_RESOLVED_ACTOR` |
| `UnpublishWorkspaceProductCommand` | `POST /v1/admin/products/{productId:guid}/unpublish` | `NO_VALIDATOR_REQUIRED` | `NO_BODY_ROUTE_GUID_PLUS_SERVER_RESOLVED_ACTOR` |
| `ArchiveWorkspaceProductCommand` | `POST /v1/admin/products/{productId:guid}/archive` | `NO_VALIDATOR_REQUIRED` | `NO_BODY_ROUTE_GUID_PLUS_SERVER_RESOLVED_ACTOR` |
| `RestoreWorkspaceProductCommand` | `POST /v1/admin/products/{productId:guid}/restore` | `NO_VALIDATOR_REQUIRED` | `NO_BODY_ROUTE_GUID_PLUS_SERVER_RESOLVED_ACTOR` |
| `CreateWorkspaceProductVariantCommand` | `POST /v1/admin/products/{productId:guid}/variants` | `NO_VALIDATOR_REQUIRED` | `CATALOG_WRITE_CAPABILITY_OWNS_TRANSPORT_SHAPE` |
| `PatchWorkspaceProductVariantCommand` | `PATCH /v1/admin/products/{productId:guid}/variants/{variantId:guid}` | `NO_VALIDATOR_REQUIRED` | `CATALOG_WRITE_CAPABILITY_OWNS_TRANSPORT_SHAPE` |

### 3.3 Why no module-local validator was invented

The 14 mutations leave this module through the **Contracts-only** port
`Tooba.Catalog.Contracts.Ports.ICatalogAdminProductWorkspaceMutationGateway`. Its Catalog-owned adapter
(`Tooba.Catalog.Infrastructure/Adapters/CatalogAdminProductWorkspaceMutationGateway.cs`) maps each
narrow DTO onto the owning Catalog Application command, dispatches it through the canonical `ISender`
pipeline and returns the handler's `Result` unchanged, so the stable `workspace.*` codes are preserved
1:1 (`workspace.product.title.missing`, `workspace.product.slug.invalid`,
`workspace.product.category.missing`, `workspace.variant.axes.missing`,
`workspace.variant.status.invalid`, …).

A ProductWorkspace-local `AbstractValidator` would therefore either fork that canonical code set or
duplicate `AdminProductGridQueryPolicy`. Neither was done:

| Field | Value |
| ----- | ----- |
| Module-local validator tree | `ABSENT_BY_DESIGN` |
| Module-local validation codes | `NONE_INTRODUCED` |
| `AbstractValidator` anywhere in the module | `0` |

## 4. Durable guard

`src/backend/Host/Tooba.Host.Tests/Architecture/ProductWorkspaceModuleAmsc001W3R1CertRepairGuardTests.cs` (7 facts):

1. `Endpoint_reachable_inventory_is_exactly_17_proven_from_disk` — counts the module's own `IRequest`
   declarations (not folder filenames), asserts `3` queries + `14` commands + `17` total and `17` mapped routes.
2. `Validator_matrix_classifies_every_request_exactly_once_and_sums_to_17` — matrix size, distinctness,
   on-disk request equality, per-entry classification + concrete reason, `0/17` split, coverage-state
   string, classification-sum string, and zero module-local validator tree.
3. `Matrix_routes_bind_back_to_the_routes_the_module_actually_maps` — every matrix route is
   `/v1/admin/products` + a literal the endpoint module actually maps, and all 17 paths are distinct.
4. `Raw_results_json_is_zero_and_the_two_201_paths_use_canonical_api_created` — zero `Results.Json`
   anywhere in the module, exactly two `api.Created` call sites with the exact canonical expression.
5. `Both_blockers_are_recorded_closed_and_the_w3_record_is_superseded` — W3 marked
   `SUPERSEDED_PENDING_FRESH_CERTIFY` with its `historicalRecordCorrection` enumerating each corrected
   field; W3-R1 records both blockers `CLOSED`.
6. `Structure_manifest_and_global_host_checkpoint_are_untouched_by_this_repair` — structure
   `UNCHANGED_W2_ACCEPTED`, manifest `NOT_TOUCHED_THIS_WAVE`, schema `UNCHANGED`, `ProductWorkspace`
   certified exactly once with no pre-cert duplicate, repository-global Host checkpoint preserved.
7. `Repair_evidence_and_recovery_checkpoint_exist` — this evidence file plus the Master Recovery checkpoint.

`ProductWorkspaceModuleAmsc001W3CertGuardTests` was updated in step and **strengthened** (W3 `state`
is now the superseded value, the raw-`Results.Json` allowance was tightened to zero, and the 17-request
inventory is asserted from the module's `IRequest` declarations). No guard was weakened and no baseline
was widened.

## 5. Validation

| Check | Result |
| ----- | ------ |
| `dotnet build src/backend/Tooba.slnx` | Build succeeded, 0 errors (28 pre-existing warnings) |
| `ProductWorkspaceModuleAmsc001W3R1CertRepairGuardTests` | 7 passed / 0 failed |
| `ProductWorkspaceModuleAmsc001W3CertGuardTests` (strengthened) | 6 passed / 0 failed |
| `ProductWorkspaceModuleAmsc001W2StructureGuardTests` | 6 passed / 0 failed |
| `ProductWorkspaceModuleAmsc001W1MigrateGuardTests` | 6 passed / 0 failed |
| `ErrorCatalogUniqueCodeGuardTests` | 3 passed / 0 failed |
| Focused `ProductWorkspaceModuleAmsc001* | HostAdminAmcW27PwVariants | HostAdminAmcW29PwIdentity | ErrorCatalogUniqueCodeGuardTests` | 32 passed / 2 failed — both failures are the pre-existing `Host_Admin_count_15_StoreAppearance_evacuated_PW_shells_ABSENT` drift facts present in the W3 baseline |
| Full `Tooba.Host.Tests` | 79 failed / 2085 passed / 130 skipped (2294) — the failing set is **identical** to the W3 baseline `failed-names.txt` (79/79 same names, zero new failure, zero silently fixed). Raw output in `full-tests.txt`, names in `failed-names.txt` |
| Request-Inventory-State | `EXACT_17` |
| Validator-Matrix-State | `EXHAUSTIVE_17` |
| Raw-Results-State | `ZERO` |
| Api-Result-State | `CANONICAL` |
| Structure-State | `UNCHANGED_W2_ACCEPTED` |
| Schema-Migration-State | `UNCHANGED` |
| Global-Host-Checkpoint-State | `PRESERVED` |
| Fresh-Certify-State | `READY` |

## 6. Scope discipline

**Changed:** `ProductWorkspaceEndpointModule.cs` (2 lines), `tmar-current-state.json` (W3 superseded +
W3-R1 added), `TOOBA-TMAR-MASTER-RECOVERY.md` (checkpoint), the two ProductWorkspace guards, and this
evidence file.

**Not changed:** structure (no file moved/renamed/deleted), routes, verbs, DTO shapes, permission
predicates, Catalog business rules, schema/migrations, error ownership, Host ownership, manifest
project layout, manifest promotion/demotion, `structureLock.certifiedModules`. No guard weakened, no
baseline widened, no unrelated production change.

**Forward obligations recorded (fresh Certify wave):**

- Refresh `modules[ProductWorkspace].certificationNote` in `tmar-module-structure-manifests.json`,
  which still carries the pre-repair `0-REQUIRED 0-NO_VALIDATOR_REQUIRED` and 4-queries phrasing.
- `docs/architecture/evidence/TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3/certification.md` retains
  `18 (4 read + 14 write)` as its own historical artifact; this file is the authoritative corrected record.

## 7. Stop

```text
workflowStop = USER_REVIEW_PRODUCTWORKSPACE_AMSC_001_W3_R1
automaticNextImplementationTask = NONE
```

No fresh Certify. No R2. No next module.
