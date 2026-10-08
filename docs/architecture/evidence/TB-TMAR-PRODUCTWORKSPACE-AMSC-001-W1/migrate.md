# TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W1 — Migrate (tooba-architecture-migrate)

## Scope

`src/backend/Modules/ProductWorkspace/Tooba.ProductWorkspace.*` plus the two touched Catalog surfaces that
own the product write capability:

```text
src/backend/Modules/Catalog/Tooba.Catalog.Contracts/
  Ports/CatalogAdminProductWorkspaceMutationContracts.cs      (new)
src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/
  Adapters/CatalogAdminProductWorkspaceMutationGateway.cs     (new)
  CatalogModule.cs                                            (registration only)
```

W1 consumes the W0 Analyze verdict `READY_TO_MIGRATE` (commit `56909122`) and repairs the certification
blockers `F1` (illegal foreign `Catalog.Application` reference), `F2` (error-catalog ownership) and `F3`
(foreign `ValidationException` seam) recorded in
`docs/architecture/evidence/TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W0/analyze.md`.

Behavior is preserved: all 17 routes, their verbs, paths, permission gates, `201 Created` shape, the
actor display resolution, every stable code value and the whole Catalog schema are unchanged.

---

## Repair 1 — F1: remove the illegal foreign `Application` reference

`Tooba.ProductWorkspace.Endpoints.csproj` **no longer references `Tooba.Catalog.Application`**. The
endpoint module no longer compiles against:

- 14 Catalog Application commands (`CreateWorkspaceProductCommand`, `UpdateProductCatalogTitleCommand`,
  `UpdateProductCoreCommand`, `UpdateProductQuantityPolicyCommand`, `AssignProductCategoryCommand`,
  `AddAdditionalCategoryCommand`, `RemoveAdditionalCategoryCommand`, `AssignProductBrandCommand`,
  `PublishProductCommand`, `UnpublishProductCommand`, `ArchiveProductCommand`, `RestoreProductCommand`,
  `CreateProductWorkspaceVariantCommand`, `PatchProductWorkspaceVariantCommand`);
- 2 Catalog Application write models (`WorkspaceProductCreateWriteModel`,
  `ProductWorkspaceVariantWriteModel`) and the 7 sibling workspace write models;
- `Catalog.Application.Shared.ICatalogActorContext` resolved from `HttpContext.RequestServices`.

Post-W1 `Tooba.ProductWorkspace.Endpoints` dependency set is exactly the W0-required set:

```text
ProductWorkspace.Application, ProductWorkspace.Contracts,
Catalog.Contracts, OperatorProfile.Contracts, BuildingBlocks
```

No `Catalog.Application`, no `Catalog.Domain`, no `Catalog.Infrastructure`.

## Repair 2 — the Contracts-only write seam (owner-side adapter)

Following the repository's established composing-BFF pattern (`IFulfillmentAdminOperations`,
`IAdminOrderFulfillmentOperations`, `IReturnAdminOperations` — a narrow Contracts port implemented by the
owning module's Infrastructure), the Catalog product write capability is now published as a Contracts port
and mirrored against the existing
`ICatalogAdminProductWorkspaceReadGateway` / `…ListGateway` read seam:

- **New** `Tooba.Catalog.Contracts/Ports/CatalogAdminProductWorkspaceMutationContracts.cs`
  - `ICatalogAdminProductWorkspaceMutationGateway` with 15 narrow methods returning `Result` /
    `Result<Guid>` — `CreateProductAsync`, `UpdateCatalogTitleAsync`, `UpdateCoreAsync`,
    `UpdateQuantityPolicyAsync`, `AssignPrimaryCategoryAsync`, `AddAdditionalCategoryAsync`,
    `RemoveAdditionalCategoryAsync`, `AssignBrandAsync`, `PublishAsync`, `UnpublishAsync`, `ArchiveAsync`,
    `RestoreAsync`, `CreateVariantAsync`, `PatchVariantAsync`.
  - Narrow transport DTOs only: `CatalogAdminProductWorkspaceActor(Guid ActorUserId, string ActorDisplayName)`
    plus the request records for each body. **No** EF entity, **no** `DbContext`, **no** `IQueryable`,
    **no** Catalog Application command/write model, **no** actor context.
  - The acting admin is an **explicit parameter** so the boundary stays stateless and can be re-implemented
    as a remote call without ambient scoped state.
- **New** `Tooba.Catalog.Infrastructure/Adapters/CatalogAdminProductWorkspaceMutationGateway.cs`
  - Maps the Contracts DTOs onto the existing Catalog Application commands, dispatches them through the
    canonical `ISender` pipeline and returns the handler `Result` **unchanged** (stable codes preserved 1:1).
  - Binds actor attribution **owner-side** onto the scoped `ICatalogActorContext`, so the consumer never
    depends on Catalog Application state. This is the "owner-side mapping keeps the wire shape stable"
    pattern already used by `FulfillmentAdminOperationsAdapter`.
- `CatalogModule.AddServices` registers
  `AddScoped<ICatalogAdminProductWorkspaceMutationGateway, CatalogAdminProductWorkspaceMutationGateway>()`
  beside the two existing workspace read gateways. Registration only; no behavior change.

## Repair 3 — module-local CQRS commands (F3 seam removal)

- **New** `Tooba.ProductWorkspace.Application/Composition/Commands/` — 14 module-local MediatR commands,
  one authoritative request type each, all `IRequest<Result>` / `IRequest<Result<Guid>>` with a real
  `IRequestHandler<,>` that calls the Contracts gateway through
  `ProductWorkspaceOperation.ExecuteAsync(...)`:

  ```text
  CreateWorkspaceProductCommand                     -> Result<Guid>
  UpdateWorkspaceProductCatalogTitleCommand         -> Result
  UpdateWorkspaceProductCoreCommand                 -> Result
  UpdateWorkspaceProductQuantityPolicyCommand       -> Result
  AssignWorkspaceProductCategoryCommand             -> Result
  AddWorkspaceProductAdditionalCategoryCommand      -> Result
  RemoveWorkspaceProductAdditionalCategoryCommand   -> Result
  AssignWorkspaceProductBrandCommand                -> Result
  PublishWorkspaceProductCommand                    -> Result
  UnpublishWorkspaceProductCommand                  -> Result
  ArchiveWorkspaceProductCommand                    -> Result
  RestoreWorkspaceProductCommand                    -> Result
  CreateWorkspaceProductVariantCommand              -> Result
  PatchWorkspaceProductVariantCommand               -> Result
  ```

- `ProductWorkspaceEndpointModule.cs` dispatches these module-local commands through `ISender`; every one of
  the 17 routes, its verb, path, permission predicate (`CanEditCatalog` / `CanPublish`) and the
  `201 Created` variant-create shape is byte-identical to `HEAD`.
- **F3 closed**: because the mutations now travel through the Contracts `Result` boundary instead of
  in-process Catalog Application dispatch, the foreign `ValidationException` seam is gone. Transport shape
  is validated by the Catalog write capability, which returns the stable `workspace.*` codes through the
  boundary; therefore no module-local validator tree was created — it would fork that canonical code set.

## Repair 4 — F2: canonical error-catalog ownership (no duplicate descriptors)

W0 recorded that `workspace.*` was declared and registered by Catalog. The **first W1 attempt** re-homed the
descriptors into a new `ProductWorkspaceErrorCatalogContributor`. That was **wrong** and was corrected inside
this wave: it produced a duplicate `ErrorDescriptor` for `workspace.product.missing` and
`workspace.permission.denied`, which the composed `ErrorDefinitionCatalog` rejects fail-fast and the
canonical rule forbids ("duplicate **usage** is allowed; duplicate **descriptor ownership** is not"). It was
caught by the repository's own `ErrorCatalogUniqueCodeGuardTests`.

Final canonical split, mirroring the certified `PaymentErrorCodes` / `CartErrorCodes` precedent:

| Concern | Owner | Evidence |
|---|---|---|
| Declared stable code constants | Catalog (`CatalogErrorCodes`) — the natural bounded context and primary producer | `CatalogErrorCodes.WorkspaceProductMissing` / `.WorkspacePermissionDenied` / `.CategoryAssignmentStale` |
| Registered `ErrorDescriptor` (404 / 403 / 400) | **Catalog only** (`CatalogErrorCatalogContributor`) — exactly one owner | `ErrorCatalogUniqueCodeGuardTests` green |
| Declared-code guard for this module's fault seam | ProductWorkspace | `ProductWorkspaceErrorCodes.IsKnown(string?)` over the 3 declared codes |
| Bilingual localization text | ProductWorkspace (`ProductWorkspaceErrorResourceSet` + `.resx` / `.fa.resx`) | same split as `CartErrorResourceSet`, `FulfillmentErrorResources` |

- **New** `Tooba.ProductWorkspace.Contracts/Errors/ProductWorkspaceErrorCodes.cs` — `KnownCodes` (ordinal
  `HashSet`) + `IsKnown(string? code)` (null/whitespace false, exact ordinal membership only) over
  `workspace.product.missing`, `workspace.permission.denied`, `catalog.category.assignment.stale`. The
  ownership split is documented in the file so a later wave cannot re-register the descriptors.
- **New** `Tooba.ProductWorkspace.Contracts/Errors/ProductWorkspaceErrorResourceSet.cs` +
  `Resources/ProductWorkspaceErrors.resx` / `.fa.resx` — the two `workspace.*` keys with the exact shipped
  text (`Product was not found.` / `محصول پیدا نشد.`, `Forbidden` / `Forbidden`).
- **Deleted** `ProductWorkspaceErrorCatalogContributor.cs`; `ProductWorkspaceModule` now registers only
  `AddSingleton<IErrorResourceSet, ProductWorkspaceErrorResourceSet>()` and no longer registers an
  `IErrorCatalogContributor`.
- Catalog's contributor, `CatalogErrorCodes`, `CatalogErrors.resx` / `.fa.resx` and
  `CatalogErrorResourceSet` are **untouched** — no localization key was renamed, moved or repurposed.

## Repair 5 — canonical typed-fault seam

**New** `Tooba.ProductWorkspace.Application/Composition/ProductWorkspaceOperation.cs`, mirroring the
certified `ProductQnAOperation` / `OperatorProfileOperation` / `PricingOperation` shape exactly:

```csharp
catch (ContractOperationException ex) when (ProductWorkspaceErrorCodes.IsKnown(ex.Code))
    => Result.Failure<T>(new SemanticError(ex.Code));
catch (SemanticException ex) => Result.Failure<T>(ex.Error);
```

- `ArgumentNullException.ThrowIfNull` on both overloads; a value-less `ExecuteAsync` and a generic
  `ExecuteAsync<T>`.
- Classification is by **typed code only** — never by message/prose. An uncatalogued contract code and every
  unknown exception propagate untouched to the canonical global exception boundary.
- The seam returns `Result` / `Result<T>` (never `IResult`), so the boundary contract is unchanged.

## Repair 6 — Contracts/Infrastructure project hygiene

- `Tooba.ProductWorkspace.Contracts.csproj` gains the `Tooba.BuildingBlocks` reference required by
  `IErrorResourceSet` / `ResourceManager` (same reference the certified `Payment.Contracts` carries).
- `Tooba.ProductWorkspace.Infrastructure.csproj` gains the explicit `Tooba.BuildingBlocks` reference for
  `IErrorResourceSet` / `IErrorCatalogContributor` types. No foreign module reference was added; the
  module still references only its own projects + BuildingBlocks + ModuleContracts.
- `Tooba.ProductWorkspace.Application.csproj` is unchanged and remains Contracts-only
  (Catalog / Offer / Pricing / Inventory / Tax / Party / OperatorProfile Contracts + BuildingBlocks).

---

## Verification

- `dotnet build src/backend/Tooba.slnx` — **Build succeeded, 0 errors** (149 pre-existing warnings).
- New durable guard
  `ProductWorkspaceModuleAmsc001W1MigrateGuardTests` (`src/backend/Host/Tooba.Host.Tests/Architecture/`)
  — **6 passed / 0 failed**, pinning: the Contracts gateway is the only Catalog write seam; the 14
  module-local CQRS commands (exact file set, `IRequest<Result*>` + `IRequestHandler` + gateway +
  `ProductWorkspaceOperation.ExecuteAsync`, no `Tooba.Catalog.Application`); the declared-code guard +
  bilingual resource set **and the absence of any ProductWorkspace descriptor contributor**; the
  `ProductWorkspaceOperation` shape with no message-text classification and no parallel validator tree; zero
  foreign Application/Infrastructure/Domain coupling (any foreign reference must end `.Contracts.csproj`)
  and no foreign DbContext/EF/Host usage; and the composition-only Host surface with exactly 17 preserved
  routes.
- `ErrorCatalogUniqueCodeGuardTests` — **3 passed / 0 failed**. This is the decisive proof that Repair 4 is
  correct: the composed production contributor set registers each machine code exactly once and the
  fail-fast `ErrorDefinitionCatalog` materializes.
- Focused filter
  `ProductWorkspace|CatalogModuleAmc|HostAdminAmc|ErrorCatalogUniqueCodeGuard` —
  **29 failed / 185 passed**, and the failing set is **identical to the pre-W1 baseline** (verified by
  stashing the wave and re-running). All 29 are pre-existing, out-of-scope Catalog/Admin drift and are
  unchanged by this wave: `Host/Admin` file count is 17 while legacy guards assert 15/52, and several guards
  read non-existent `Tooba.Catalog.Domain/CatalogDomain.cs` / `CatalogCategoryTreeRules.cs`. No
  ProductWorkspace test fails.
- Updated guards (`HostAdminAmcW26PwLifecycleGuardTests`, `W27PwVariantsGuardTests`,
  `W29PwIdentityGuardTests`, `W30PwTaxonomyGuardTests`) now assert the **module-local** command names plus
  `ICatalogAdminProductWorkspaceMutationGateway` and `Assert.DoesNotContain("Tooba.Catalog.Application")`.
  Intent is preserved and strengthened; no assertion was weakened.
- `Tooba.ProductWorkspace.Domain` remains an empty declared boundary project (F4) — untouched by W1;
  justification is a W2 manifest obligation.

## State

- `contractsBoundaryState`: `VIOLATED_ENDPOINTS_TO_CATALOG_APPLICATION`
  → `COMPLIANT_CONTRACTS_ONLY_MUTATION_GATEWAY`.
- `crossModuleCouplingState`: `ILLEGAL_FOREIGN_APPLICATION_PROJECT_REFERENCE`
  → `LEGAL_CONTRACTS_ONLY`.
- `stableErrorCodeState`: `FOREIGN_CATALOG_OWNED`
  → `DECLARED_AND_LOCALIZED_HERE_DESCRIPTOR_OWNED_ONCE_BY_CATALOG` (no duplicate descriptor ownership).
- `localizationState`: `FOREIGN_OWNED_INVERSION` → `CANONICAL_SPLIT_OWNER_DESCRIPTOR_MODULE_LOCALIZATION`.
- `cqrsState`: `FOREIGN_APPLICATION_DISPATCH` → `MODULE_LOCAL_MEDIATR_CQRS_14_COMMANDS`.
- `validatorCoverageState`: `FOREIGN_APPLICATION_OWNED`
  → `EXHAUSTIVE_0_OF_14_NO_VALIDATOR_REQUIRED_CATALOG_OWNS_TRANSPORT_SHAPE`.
- `microserviceExtractable`:
  `TARGET_TRUE_BLOCKED_BY_F1_AND_F2` → `TRUE_HTTP_SURFACE_DEPENDS_ON_CONTRACTS_ONLY`.
- Verdict: `READY_TO_STRUCTURE`. `Structure-Handoff-State = REQUIRED`.

Production code changed; schema unchanged; routes/behavior preserved; guards strengthened; zero guards
weakened; zero unrelated files touched.
