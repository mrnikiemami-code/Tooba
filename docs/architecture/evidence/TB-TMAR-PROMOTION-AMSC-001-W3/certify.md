# TB-TMAR-PROMOTION-AMSC-001-W3 — Certify (`Tooba.Promotion.*`)

- **Module**: `src/backend/Modules/Promotion/Tooba.Promotion.*`
- **Skill**: `tooba-architecture-certify`
- **Lock**: `ARCH-COMPLETE-002`
- **Verdict**: `COMPLETE_REFERENCE_PATTERN`
- **Lineage**: W0 Analyze `d00cf666` → W1 Migrate `06858933` → W2 Structure `597147ea` → **W3 Certify (this wave)**
- **Mode**: certify only — **no production file changed**; the wave promotes the manifest/SoT certification
  truth, adds one durable guard and records evidence.

## 1. Certification result

| Gate | Result |
| --- | --- |
| Structure certified under `ARCH-COMPLETE-002` | `true` |
| HTTP applicability | `HTTP_OWNING` |
| Endpoint ownership | `MODULE_OWNED_21_ROUTES_HOST_ZERO` |
| Endpoint-reachable requests | `21 / 21` via `ISender` |
| Validator matrix | `EXHAUSTIVE_18_VALIDATOR_REQUIRED_3_NO_VALIDATOR_REQUIRED` |
| Declared stable codes | `40` (`13` HTTP-reachable + `27` domain invariants) |
| Registered descriptors | `13` (unique per code) |
| Localization | `40` bilingual keys (`PromotionErrors.resx` + `.fa.resx`) |
| Contracts boundary | `CLEAN_CONTRACTS_ONLY` |
| Foreign Application/Infrastructure/Domain coupling | `ZERO` |
| Cross-module joins | `NONE` |
| Persistence ownership | `OWN_PROMOTION_SCHEMA_OWN_OUTBOX` |
| Schema preservation | `PRESERVED_TWO_UNCHANGED_MIGRATIONS` |
| Path ↔ namespace | `EXACT` |
| Root allowlist | `ENFORCED` |
| Folder granularity | `PROFESSIONAL_SHALLOW` |
| Solution explorer | `CANONICAL` (6 projects under `/Modules/Promotion/`) |
| File cohesion | `COHESIVE` |
| Microservice-extractable | `true` |
| Host final closure | `PRESERVED` |
| Blocking residual debt | `ZERO` |

## 2. Endpoint ownership and CQRS

Promotion owns **21** HTTP routes, all mapped by `PromotionEndpointModule.MapPromotionEndpoints`:

- `/v1/seller/promotions` ×6 — list, create, get, update, activate, deactivate.
- `/v1/admin/promotions` ×3 — list, get, deactivate.
- `/v1/admin/merchandising-campaigns` ×12 — list, types, offer-candidates, get, create, update, publish,
  archive, add member, remove member, reorder members, set member price.

Every route dispatches a real `IRequest<T>` through `ISender` (`MEDIATR_12_5_ISENDER_21_OF_21`); the endpoint
layer never invokes a concrete handler and never calls `SendAsync`. Host owns **zero** Promotion route —
`Host/Program.cs` keeps only the composition root (`AddPromotionEndpointPresentation()`,
`app.MapPromotionEndpoints()`, the Application-assembly CQRS registration and the accepted Host platform
security adapters). No `Host/Tooba.Host/Promotion` production folder exists.

## 3. Stable-code ownership, catalog uniqueness and localization

- The single canonical stable-code home is `Tooba.Promotion.Contracts/Errors/PromotionErrorCodes.cs`
  (`namespace Tooba.Promotion.Contracts.Errors`) — **40** declared codes, unique by identity.
- Reachability split is exact and guarded: **13 HTTP-reachable** codes each get exactly one descriptor from
  `PromotionErrorCatalogContributor`; **27 domain invariants** are `Result`-carried identities that are
  localized but never catalogued with their own HTTP descriptor. `IsKnown` is the union and is what
  `PromotionOperation` filters on.
- No foreign-owned descriptor is re-registered: the cross-cutting `seller.authorization.denied` /
  `admin.authorization.denied` codes belong to Foundation and are deliberately not declared by Promotion.
  `ErrorCatalogUniqueCodeGuardTests` proves uniqueness over the composed catalog.
- `PromotionErrorResourceSet` owns the `promotion.*` / `merchandising.*` / `campaign.*` keyspaces and is
  registered **exactly once** by `PromotionEndpointModule`; `Resources/PromotionErrors.resx` and
  `PromotionErrors.fa.resx` carry one key per declared code (40), so no Promotion key falls back to the
  generic title.

## 4. Typed-fault seam

`Tooba.Promotion.Application/Composition/PromotionOperation.cs` is the single seam. It maps
`ContractOperationException` **only when** `PromotionErrorCodes.IsKnown` and `SemanticException` **by code**
(`SemanticException`), never by message text. Unknown codes and unexpected exceptions propagate untouched to
the canonical global exception boundary. Zero `ex.Message` classification remains in production.

## 5. Validator matrix (exhaustive)

- **18** transport-shape FluentValidation validators over **17** stable `promotion.validation.*` machine codes.
- **18** `VALIDATOR_REQUIRED` requests — every endpoint-reachable request whose transport shape can be
  malformed. `ReorderMerchandisingCampaignMembersCommandValidator` contributes four member-order rules
  (required, non-empty, duplicate-free, entry-non-empty).
- **3** `NO_VALIDATOR_REQUIRED` requests — `ListSellerPromotionsQuery`, `ListAdminPromotionsQuery` and
  `ListMerchandisingCampaignTypesQuery` take no malformable transport input (the seller list derives its
  scope from the authorizer-resolved seller party id; the admin list takes only an optional server-side
  filter; the campaign-types read takes only an optional locale).
- Validators emit machine codes only (`WithErrorCode`), never prose (`WithMessage(` absent), and never
  duplicate a business rule — all domain/business rules stay in Domain/Application.

## 6. Contracts-only boundary and persistence

- `Tooba.Promotion.Endpoints` references `Tooba.Promotion.Application` + `Tooba.Promotion.Contracts` +
  BuildingBlocks only — **no** `Promotion.Infrastructure` and **no** `Promotion.Domain` edge.
- `Tooba.Promotion.Contracts` carries zero foreign module type in any signature and references no foreign
  module project; the W1 removal of `Tooba.Offer.Contracts` from the Contracts signature is preserved.
- Outbound cross-module edges are Contracts-only (Offer / Pricing / Inventory / Catalog / Party). The W1
  removal of the illegal `Promotion.Infrastructure → Inventory.Application/Domain` edge and the
  `Promotion.Application → Offer.Contracts` edge is preserved; no foreign `Application`/`Domain` type appears
  anywhere in Promotion production.
- The module owns exactly one `PromotionDbContext` with its own `promotion` schema and its own outbox, and
  exactly the two unchanged migrations (`20260823210000_InitialPromotion`,
  `20260919134400_MerchandisingCampaignFoundation`). No AMSC wave introduced a schema change.

## 7. Canonical API results

Zero raw `Results.Json(` / `Results.BadRequest(` / `Results.Problem(` payload mappings remain in the module.
The seller-create `201` path is produced by `ApiResponseFactory.Created` with a `Location` header; the failure
path stays `ProblemDetails`.

## 8. Microservice extractability

The target dependency set for `Tooba.Promotion.Endpoints` after AMSC is
`Promotion.Application` + `Promotion.Contracts` + BuildingBlocks only — zero foreign
Application/Infrastructure/Domain project edge, zero cross-module join, zero foreign DbContext/EF usage and
zero foreign type in any Promotion Contracts signature. The module is extractable as an independent service
without further decoupling.

## 9. SoT / manifest / recovery promotion

- `docs/architecture/tmar-module-structure-manifests.json` — `Promotion` promoted from `preCertModules`
  into `modules[]` with `structureCertified: true`, `lockVersion: ARCH-COMPLETE-002` and the full W3
  `certificationNote`; `preCertModules` is now empty; `Promotion` is absent from
  `uncertifiedHttpOwningModules`.
- `docs/architecture/tmar-current-state.json` — new `promotionAmsc001W3` block
  (`verdict COMPLETE_REFERENCE_PATTERN`, `state CERTIFIED`); `structureLock.certifiedModules` gained
  `Promotion` exactly once; `promotionAmsc001W2.commit` recorded as `597147ea`.
- `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` — new module-local section
  `Promotion AMSC W3 Certify (module-local)`.

## 10. Durable guards

- `src/backend/Host/Tooba.Host.Tests/Architecture/PromotionModuleAmsc001W3CertGuardTests.cs` (6) — W3.
- `PromotionModuleAmsc001W2StructureGuardTests` (7) — repointed to the certified manifest array.
- `PromotionModuleAmsc001W1MigrateGuardTests` (10), `PromotionArchitectureGuardTests`,
  `ErrorCatalogUniqueCodeGuardTests`, and the repointed `HostAdminAmcW33` / `PromotionFoundation` /
  `PromotionPanel` / `ContractsW6` guards.

No guard was weakened and no baseline was widened in any AMSC wave.

### 10.1 Validation evidence

- Full Host suite in this working tree: **2116 passed / 77 failed / 130 skipped**.
- Clean git worktree at HEAD (`597147ea`): **77 failed** — the same failure **name set**, byte-identical
  (zero new failures, zero fixed). The 77 pre-existing failures are unrelated to Promotion (Catalog
  deviations, stale recovery pins, DI/service-descriptor validation, Fulfillment/Correlation runtime tests).
- Promotion-focused run: **32 passed / 2 skipped / 0 failed**.
- The only promotion-driven guard updates are the repository-global certified-module expectation lists
  (`TmarCompleteReferenceStructureGateTests` ×2, `TmarDurableGuardTests` ×1,
  `PricingModuleAmsc001W3R3CertGuardTests` ×2 arrays) and the W2 structure guard manifest assertion, which
  now legitimately expect `Promotion` in the certified set. No assertion was weakened and no baseline was
  widened.

## 11. Host final closure and global recovery lock

- Host final closure `PRESERVED`: zero new Host folder, file, route or `PromotionDbContext` reference;
  `HOST_ROOT_FINAL_CERTIFIED` untouched.
- Repository-global recovery lock preserved exactly: `currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED`,
  `lastAcceptedTask = TB-TMAR-HOST-ROOT-FINAL-CERT-001`,
  `lastAcceptedCommit = 7a6c353a98a761df9124beb1fce23ed8424230de`,
  `workflowStop = USER_REVIEW_HOST_ROOT_FINAL_CERT_001`, `automaticNextImplementationTask = NONE`.

## 12. Stop gate

`USER_REVIEW_PROMOTION_AMSC_001_W3` — `automaticNextImplementationTask = NONE`.
