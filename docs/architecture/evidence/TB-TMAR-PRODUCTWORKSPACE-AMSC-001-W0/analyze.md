# TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W0 — Analyze

Task: `TB-TMAR-PRODUCTWORKSPACE-AMSC-001` (AMSC-001, 4 waves)
Module: `Module\ProductWorkspace`
Standard: `ARCH-COMPLETE-002` (+ ARCH-MODULE-FILE-001, ARCH-SIZE-001, ARCH-STRUCT-001, ARCH-ALLOWLIST-001, ARCH-MICRO-001)
Baseline: `HEAD == origin/main == eb116af3`; `dotnet build Tooba.slnx` → 0 errors; `Tooba.Host.Tests` filter `ProductWorkspace` → 28/28 pass.

Wave 0 changes **zero** production code.

---

## 1. Bounded-context role

`ProductWorkspace` is the **Admin composed product aggregate** surface: a presentation/composition BFF over `/v1/admin/products` that reads a single product aggregate assembled from Catalog + Offer + Pricing + Inventory + Tax + Party + OperatorProfile, and forwards product write/mutation use-cases to the owning modules.

It is **not** a data owner: it has no `Domain` types, no `Infrastructure` persistence, no DbContext, no migration registration (`Tooba.MigrationRunner/ModuleMigrationRegistry.cs` has no ProductWorkspace entry), and no schema. Its `Infrastructure` project is a deliberate no-op module marker (`ProductWorkspaceModule`).

## 2. Capability inventory

| # | Capability | Surface | Home |
|---|---|---|---|
| C1 | Admin product grid (list + query) | `GET /v1/admin/products`, `POST /v1/admin/products/query` | Application `Composition/Queries` + `AdminProductGridQueryPolicy` |
| C2 | Product aggregate workspace view | `GET /v1/admin/products/{productId}` | Application `Composition/Queries/GetProductWorkspaceHandler` (6 Contracts ports) |
| C3 | Product write/mutation commands (17 routes total ⇒ 15 mutations) | `POST/PATCH/PUT/DELETE /v1/admin/products/...` | **Catalog** (`Catalog.Application` commands, reached directly today) |
| C4 | Admin authorization | all routes | `IProductWorkspaceAdminAuthorizer` (module-local port) |
| C5 | Error contract (`workspace.*`) | all routes | **Catalog** `CatalogErrorCodes` + `CatalogErrorCatalogContributor` + `CatalogErrors.resx` |

C1, C2, C4 are correctly owned and Contracts-only. **C3 and C5 are the AMSC findings.**

## 3. Coupling findings

### F1 — BLOCKER (illegal foreign Application reference)

`Tooba.ProductWorkspace.Endpoints/Tooba.ProductWorkspace.Endpoints.csproj`:

```xml
<ProjectReference Include="..\..\Catalog\Tooba.Catalog.Application\Tooba.Catalog.Application.csproj" />
```

`ProductWorkspaceEndpointModule.cs` therefore compiles against Catalog **Application** internals:

- write models: `WorkspaceProductCreateWriteModel`, `ProductWorkspaceVariantWriteModel`
- commands: `CreateWorkspaceProductCommand`, `UpdateProductCatalogTitleCommand`, `UpdateProductCoreCommand`, `UpdateProductQuantityPolicyCommand`, `AssignProductCategoryCommand`, `AddAdditionalCategoryCommand`, `RemoveAdditionalCategoryCommand`, `AssignProductBrandCommand`, `PublishProductCommand`, `UnpublishProductCommand`, `ArchiveProductCommand`, `RestoreProductCommand`, `CreateProductWorkspaceVariantCommand`, `PatchProductWorkspaceVariantCommand`
- application port: `ICatalogActorContext` (resolved from `HttpContext.RequestServices`)

Consequence: `ProductWorkspace` cannot be extracted into a microservice — its HTTP surface transitively requires the whole Catalog Application assembly and its DI graph.

### F2 — BLOCKER (error-catalog ownership)

`workspace.*` codes are declared and registered by **Catalog**:

- `CatalogErrorCodes.WorkspaceProductMissing`, `CatalogErrorCodes.WorkspacePermissionDenied`
- `CatalogErrorCatalogContributor` lines 186/188 (404 / 403)
- 13 `workspace.*` keys in `CatalogErrors.resx` and `CatalogErrors.fa.resx`

A `workspace.` prefix that belongs to ProductWorkspace living in the Catalog error catalog is a cross-module ownership inversion; it also makes the ProductWorkspace error surface disappear if Catalog is not composed.

### F3 — cross-module `ValidationException` leakage

Because commands are dispatched in-process, Catalog `ValidationException`s surface inside ProductWorkspace request handling and are mapped by the global boundary. That is a foreign-exception seam, not a Contracts boundary.

### F4 — `ProductWorkspace.Domain` is empty

The `Domain` project has zero production types. Keep as a declared boundary project (composition modules own no domain), but it must be explicitly justified in the manifest allowlist.

### F5 — empty-folder ceremony

`Application/Composition/{Ports,Validators}` hold only `.gitkeep`; `Models/` holds production code. Structural normalization is deferred to W2.

### Non-findings (already canonical — do not touch)

- `ProductWorkspace.Application` depends **only** on `ProductWorkspace.Contracts`, `Catalog.Contracts`, `Offer.Contracts`, `Pricing.Contracts`, `Inventory.Contracts`, `Tax.Contracts`, `Party.Contracts`, `OperatorProfile.Contracts`, `BuildingBlocks`. Contracts-only. ✅
- `GetProductWorkspaceHandler` composes 6 Contracts ports with no cross-module join. ✅
- `AdminProductGridQueryPolicy` derives from `GridQueryPolicyBase`; all 4 queries return `Result`/`Result<T>`. ✅
- `ProductWorkspaceEndpointModule` uses `ApiResponseFactory` on every route; no raw `Results.*` business payloads. ✅
- `Tooba.ProductWorkspace.Contracts` contains only the marker; no foreign dependency. ✅
- No `Host/Admin/ProductWorkspace*.cs` shell remains. ✅

## 4. Microservice-extractability verdict (W0)

`NOT_EXTRACTABLE` — blocked by F1 and F2.

Required post-W1 dependency set for `ProductWorkspace.Endpoints`:

```text
ProductWorkspace.Application, ProductWorkspace.Contracts,
Catalog.Contracts, OperatorProfile.Contracts, BuildingBlocks
```

(no `Catalog.Application`, no `Catalog.Domain`, no `Catalog.Infrastructure`)

## 5. W1–W3 plan (derived, not invented)

| Wave | Deliverable |
|---|---|
| W1 Migrate | Remove the Catalog `Application` reference; introduce the Contracts-only workspace write seam in `Catalog.Contracts` (module-owned DTOs/enums + a port implemented by `Catalog.Infrastructure`); re-home `workspace.*` codes + descriptors + resources into `ProductWorkspace.Contracts`/`Application` (localization keys preserved 1:1); introduce `ProductWorkspaceOperation` typed-fault translation; align the 15 mutation routes to the composing-BFF idiom; durable W1 guard test. |
| W2 Structure | Capability-first shallow normalization of `Application/Composition`, remove `.gitkeep` ceremony, exact path↔namespace, root allowlists, `.slnx` verification, durable W2 structure guard test. |
| W3 Certify | `ARCH-COMPLETE-002` certification, manifest promotion `preCertModules → modules`, `structureLock.certifiedModules`, Master Recovery checkpoint, durable W3 certification guard test. |

Each wave is a separate commit **and** separate push to `origin/main`.
