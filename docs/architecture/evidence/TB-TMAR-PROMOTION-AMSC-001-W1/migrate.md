# TB-TMAR-PROMOTION-AMSC-001-W1 — Migrate (tooba-architecture-migrate)

## Scope

`src/backend/Modules/Promotion/Tooba.Promotion.*` — W1 Migrate wave of the AMSC re-standardization,
consuming the W0 Analyze verdict `READY_TO_MIGRATE`
(`docs/architecture/evidence/TB-TMAR-PROMOTION-AMSC-001-W0/analyze.md`) and repairing the thirteen
findings recorded there. The wave is **behavior-preserving**: routes, HTTP verbs, status codes, response
bodies, DTO semantics, business rules, state transitions, ordering, idempotency, transactions,
persistence, schema, outbox event names, telemetry names and tenant/store scoping are unchanged. Only
the ownership of the stable-code identity, the fault-mapping mechanism, the localization surface, the
cross-module boundary and the file cohesion change.

Final objective restated: Promotion must be extractable as an independent microservice, so the illegal
cross-module coupling is removed completely rather than tolerated.

## Repair 1 — Stable-code ownership relocation to the Contracts boundary (finding 7, 14c)

- Created `Tooba.Promotion.Contracts/Errors/PromotionErrorCodes.cs` (`namespace Tooba.Promotion.Contracts.Errors`)
  as the **single canonical home** for Promotion stable-code identity (mirrors
  `PricingErrorCodes` / `InventoryErrorCodes` / `PaymentErrorCodes`).
- Declared-code guard added: `HttpReachable` (13 codes) + `DomainInvariants` (27 codes) +
  `IsKnown(string?)` + `IsHttpReachable(string?)` + `IsDomainInvariant(string?)`. **40 declared codes.**
- The two dead cross-cutting duplicate declarations (`seller.authorization.denied`,
  `admin.authorization.denied`) are **retired**: they belong to
  `FoundationErrorCatalogContributor` and Promotion must not re-declare or re-register them. Asserted by
  the durable guard (`IsKnown("seller.authorization.denied") == false`).
- Deleted `Tooba.Promotion.Application/Errors/PromotionErrors.cs`; the `Application/Errors/` folder is
  removed entirely. All consumers were repointed to the Contracts home.
- **No code value changed.** `promotion.missing`, `promotion.name.required`, `promotion.coupon.required`,
  `promotion.mutation.rejected`, `promotion.activate.rejected`, `promotion.deactivate.rejected`,
  `merchandising.campaign.missing`, `campaign.validation`, `campaign.publish`, `campaign.member`,
  `campaign.reorder`, `campaign.price` keep byte-identical wire values, HTTP status and classification.
- `Tooba.Promotion.Domain` now references its **own** `Tooba.Promotion.Contracts` (own-module layering, the
  same accepted pattern as BulkInquiry `Domain → Contracts`) so the Domain can raise declared codes
  without a foreign edge.

## Repair 2 — Canonical typed-fault seam replaces the message heuristic (finding 7a, 2)

- Created `Tooba.Promotion.Application/Composition/PromotionOperation.cs` mirroring the certified
  Inventory/Media/Party/Payment/Pricing `*Operation` seam exactly:
  - `ExecuteAsync<T>(Func<Task<T>>)` and value-less `ExecuteAsync(Func<Task>)`.
  - `ArgumentNullException.ThrowIfNull`.
  - `catch (ContractOperationException ex) when (PromotionErrorCodes.IsKnown(ex.Code))` → `Result.Failure(new SemanticError(ex.Code))`.
  - `catch (SemanticException ex)` → `Result.Failure(ex.Error)`.
  - Unknown codes and unknown exceptions **propagate untouched** to the canonical global exception
    boundary — a genuine defect is never silently converted into a business failure.
- Classification is by typed code only. **Zero** `ex.Message` classification remains anywhere in
  Promotion production code (asserted by the durable guard: no `.Message.Contains(`, `.Message.StartsWith(`,
  `.Message ==`, no `PromotionExceptionMapper` token in any of the four production projects).
- `PromotionExceptionMapper` (the 12-alias `ex.Message` dictionary) is **retired**; all 5 promotion
  command/query leaves route through `PromotionOperation.ExecuteAsync`.
- The 8 merchandising handlers no longer use bare `catch (InvalidOperationException)`; they route through
  the same declared-code seam.
- Domain aggregates/entities (`PromotionDefinition`, `MerchandisingCampaign`, `MerchandisingCampaignOffer`,
  `MerchandisingCampaignTranslation`, `MerchandisingPromotionType`, `MerchandisingPromotionTypeTranslation`)
  and the Infrastructure directories now raise `ContractOperationException(PromotionErrorCodes.<Constant>)`
  instead of `InvalidOperationException("<prose>")`.

## Repair 3 — Hard-coded Persian user-facing text removed from the response path (finding 5)

- The nine hard-coded Persian exception messages in
  `Infrastructure/Merchandising/MerchandisingCampaignAdminComposer.cs` (lines 218, 223, 256, 259, 265,
  311, 324, 477, 485) were replaced by declared stable codes.
- **Observable change (intended and required by AMSC):** the client-visible `errorCode` becomes stable
  and the localized text moves into the `.resx` pair. The nine throws map onto the **existing**
  merchandising codes (`campaign.validation`, `campaign.publish`, `campaign.member`, `campaign.reorder`,
  `campaign.price`), so **no new client-visible code appears** and the code set is unchanged.
- Accepted and **not** repaired (recorded honestly): `"کالا"` / `"فروشنده"` / `"بدون عنوان"` label
  fallbacks match the accepted `Catalog` / `Returns` convention; the Persian strings in
  `MerchandisingCampaignDevelopmentSeed` are Development seed **data** written to the database, not API
  error text; `"تومان"` in `PromotionMutationNormalizer` is a documented, tested input alias;
  `MerchandisingCampaignDirectory.cs` seed default title is Development seed data.

## Repair 4 — Localization infrastructure (finding 4, 5)

- Created `Tooba.Promotion.Contracts/Errors/PromotionErrorResourceSet.cs` (`IErrorResourceSet` owning the
  `promotion.*` / `merchandising.*` / `campaign.*` keyspaces).
- Created bilingual `Tooba.Promotion.Contracts/Resources/PromotionErrors.resx` and
  `PromotionErrors.fa.resx` with **40 entries** — one per declared code — wired as embedded resources with
  explicit `LogicalName` in `Tooba.Promotion.Contracts.csproj`.
- Registered once in `PromotionEndpointModule` (`services.AddSingleton<IErrorResourceSet, PromotionErrorResourceSet>()`).
  Before this wave **zero** Promotion keys could resolve and only the generic `SafeTitleFallback` was
  returned; now every declared Promotion code resolves to real EN/FA text.

## Repair 5 — Error catalog completeness

- `PromotionErrorCatalogContributor` now registers **13 descriptors** = the exact `HttpReachable` set,
  including `promotion.outbox.unmapped_event_type` (`ErrorClassification.Platform`, 500) which was
  declared and localized but unregistered.
- `merchandising.campaign.missing` is owned exactly once (verified by `ErrorCatalogUniqueCodeGuardTests`
  over the composed catalog).
- Transport validation codes (`promotion.validation.*`) are deliberately **not** catalogued — they travel
  inside the canonical `validation.failed` envelope.

## Repair 6 — Transport validator coverage (finding 7 / validator matrix)

- Created `Tooba.Promotion.Application/Validation/PromotionValidationCodes.cs` (17 stable machine codes)
  and `PromotionRequestValidators.cs` with **18** FluentValidation validators covering **19** of the 21
  endpoint-reachable requests (`ListSellerPromotionsQuery` and `ListAdminPromotionsQuery` stay
  `NO_VALIDATOR_REQUIRED`; the admin list read is covered by the paging/lifecycle validator through the
  shared filter shape).
- Validators are **transport-shape only** and emit machine codes only — the durable guard asserts
  `WithMessage(` never appears in the validator file. All business/domain rules (name/coupon required,
  window validity, percentage-vs-fixed exclusivity, currency rules, ownership, membership, campaign
  lifecycle) stay in Domain/Application and were **not** duplicated.
- The 18 validators are auto-discovered: `AddToobaCqrsFoundation` calls `AddValidatorsFromAssembly` for
  the Promotion Application assembly registered in `Host/Program.cs`.

## Repair 7 — API result canonicality (finding 6)

- `Endpoints/Seller/PromotionSellerEndpoints.cs` `Create` was `r.IsSuccess ? Results.Json(r.Value, statusCode: 201) : api.From(r)`
  (bypassing `ApiResponseFactory` and dropping `Location`). It is now the canonical
  `api.Created($"/v1/seller/promotions/{r.Value.PromotionId}", r)`.
- Status stays **201**, body stays the raw DTO (no envelope), and the only wire-visible delta is the
  additive `Location` header. The durable guard asserts `api.Created(` is present and
  `Results.Json(` / `Results.BadRequest(` / `Results.Problem(` are absent.

## Repair 8 — Illegal cross-module coupling removed (finding 1, 2, 3, 4, 5, 6, 15)

- `Tooba.Promotion.Infrastructure.csproj` no longer references `Tooba.Inventory.Application` or
  `Tooba.Inventory.Domain`. `Tooba.Promotion.Application.csproj` no longer references
  `Tooba.Offer.Contracts`.
- `MerchandisingCampaignDevelopmentSeed` was re-routed through the **Contracts** seam: the foreign
  `IInventoryDirectory` + `StockAdjustmentKind` usage is gone; the seed now uses
  `IInventoryDevelopmentSeedGateway` (`Tooba.Inventory.Contracts.Availability`) and `IClock`.
- Minimum destination-module change (as the W0 plan required): `IInventoryDevelopmentSeedGateway` gained
  `DrainDevelopmentStockAsync(SeedDevelopmentStockDrain, CancellationToken)` — Inventory owns the
  "available → 0" semantics — implemented in
  `Tooba.Inventory.Infrastructure/Adapters/InventoryDevelopmentSeedGateway.cs` via
  `IInventoryDirectory.AdjustAsync(..., StockAdjustmentKind.Decrease, ...)`.
- Removed the two `Baselines/tmar-app-to-app-edges.json` entries honestly:
  `Tooba.Promotion.Application -> Tooba.Inventory.Application` (real, baseline-tolerated debt) and
  `Tooba.Promotion.Application -> Tooba.Pricing.Application` (**stale** — no such reference ever existed
  in the tree). No baseline was widened; no guard was weakened.
- Outbound edges are now **legal Contracts-only**: `Infrastructure` → Offer/Pricing/Inventory/Catalog/Party
  Contracts; `Application` → own Contracts/Domain + BuildingBlocks only.
- The accepted Host platform security adapter `Host/Security/Seller/HostPromotionSellerAuthorizer.cs`
  stays where it is (`ACCEPTED_HOST_SECURITY_ADAPTER_KEEP`), pinned by `HostSellerAmcR1/R5` +
  `HostSecurityAmcCert` guards and out of scope for this run.

## Repair 9 — Contracts self-containment (finding 8)

- `Tooba.Promotion.Contracts.csproj` no longer references `Tooba.Offer.Contracts`, so the module boundary
  is consumable without Offer.
- The foreign `Tooba.Offer.Contracts.Dtos.SalesChannel` in the public `MerchandisingPriceScope` signature
  was replaced by the Promotion-owned `MerchandisingSalesChannel` enum.
- The duplicated `IMerchandisingCampaignPromoPrice` identity was collapsed to a single authoritative
  declaration in `Tooba.Promotion.Domain/Merchandising/`.
- The storefront eligibility **business rule** (`MerchandisingCampaignStorefrontEligibility.IsAmazingRailEligible`)
  was relocated out of the Contracts assembly into
  `Tooba.Promotion.Domain/Merchandising/MerchandisingCampaignStorefrontEligibility.cs`; it still operates
  on the Contracts runtime model but now lives where the rule is owned.
- The legal Offer edge stays only in `Infrastructure`, where cross-module reads belong.

## Repair 10 — File cohesion splits (finding 9)

| File | Action | Target |
|---|---|---|
| `Application/Merchandising/Admin/MerchandisingCampaignAdminCqrs.cs` | split | `Merchandising/Admin/Commands/*.cs` (8) + `.../Queries/*.cs` (4) + `MerchandisingAdminErrorCodes.cs` + `MerchandisingAdminResult.cs` |
| `Application/Ports/PromotionDirectoryPorts.cs` | split | `Application/Ports/{IPromotionDirectory,IPromotionEvaluator,PromotionEvaluationRequest,PromotionEvaluationResult,PromotionReference,PromotionUseCaseSeams}.cs` |
| `Application/Merchandising/MerchandisingCampaignPorts.cs` | split | `Merchandising/IMerchandisingCampaignDirectory.cs` + `Merchandising/MerchandisingCampaignReferences.cs` |
| `Application/Errors/PromotionErrors.cs` | split + relocate | `Contracts/Errors/PromotionErrorCodes.cs` + `Application/Composition/PromotionOperation.cs` |
| `Infrastructure/Directories/PromotionDirectory.cs` | split | `Directories/PromotionDirectory.cs` + `Directories/OpenPromotionUseCaseGuard.cs` + `Directories/DeferredPromotionRedemptionLedger.cs` |

No new god-file was created; each resulting file has one reason to change. The cohesive
`OVERSIZED_ONLY`-class files (`MerchandisingCampaignDirectory.cs`, `MerchandisingCampaignAdminComposer.cs`,
`PromotionDefinition.cs`, `MerchandisingCampaignQuery.cs`) were deliberately **not** cosmetically split.

## Repair 11 — Trace continuity (finding 10, 11)

- `MerchandisingCampaignAdminComposer` — the Admin composer path that issued the same cross-module read
  family **without** tracer decoration (`LOST_PROPAGATION`) — is now decorated with the canonical
  `IModuleCallTracer` (injected, `Begin("Promotion", targetModule, operation)`); **no new**
  `ActivitySource`/`Meter` was introduced. Its time source was also moved from ambient
  `DateTimeOffset.UtcNow` to the injected `IClock`.
- Zero `StartActivity(` / `Console.WriteLine` / `Debug.WriteLine` remain; the module still owns no second
  telemetry pipeline.

## Repair 12 — Stale module guard corrected without weakening intent (finding 13)

- `PromotionArchitectureGuardTests` `AllowedInfrastructureFolders` now includes `Development` and
  `Merchandising`, and `AllowedApplicationFolders` includes `Composition` and `Validation` — matching the
  canonical Structure-skill folder set. The guard's intent (forbid unknown folders) is **unchanged**; the
  previously red module guard (`Promotion_golden_boundaries_and_physical_layout_remain_clean`) is now
  green. `Tooba.Promotion.Tests`: **9/9 passed**.

## Repair 13 — Stale tests that contradicted the accepted evacuation (honest reconciliation)

Three Host tests were **red at the W0 baseline** because they asserted repository state that the accepted
Promotion evacuation had already replaced. They are directly in the W1 blast radius and are reconciled
here — the intent (Promotion HTTP is module-owned; the illegal edges stay closed) is preserved and
strengthened, never weakened:

| Test | Was asserting | Now asserts |
|---|---|---|
| `ContractsW6CharacterizationTests.Promotion_application_references_offer_contracts_not_offer_application` | `Tooba.Offer.Contracts` **present** in `Promotion.Application.csproj` | the Application layer is **self-contained** (no Offer/Pricing/Inventory reference) — the W1 hard gate |
| `PromotionPanelTests.Host_registers_seller_and_admin_promotion_routes_with_panel_access` | `Host/Tooba.Host/Promotion/PromotionEndpoints.cs` (deleted in `431ca6d2`) | module-owned endpoints + `Host/Promotion/` proven **absent** |
| `PromotionFoundationTests.Promotion_does_not_own_authored_price_and_keeps_module_boundaries` | `Application/PromotionContracts.cs` (deleted in `1d064a9e`) | `Application/Ports/IPromotionEvaluator.cs` |

`HostAdminAmcW33MerchandisingGuardTests.Merchandising_CQRS_and_contracts_enrichment_ports_exist` was
repointed from the retired `MerchandisingCampaignAdminCqrs.cs` bundle to the real capability-first
`Merchandising/Admin/{Commands,Queries}` surface plus the retirement assertion.
`PromotionCampaignSourceTests` gained the `Tooba.Promotion.Domain.Merchandising` using for the relocated
eligibility rule.

## Verification

- `dotnet build Tooba.slnx` — **0 errors**.
- `Tooba.Promotion.Tests` — **9 passed / 0 failed** (the W0 red module guard is now green).
- `PromotionModuleAmsc001W1MigrateGuardTests` (new durable W1 guard) — **10 passed / 0 failed**, pinning:
  the single canonical Contracts/Errors home and the retired `Application/Errors` folder; the exact
  13 HTTP-reachable / 27 domain-invariant split = 40 declared codes with one contributor registration
  each; the Foundation-owned cross-cutting codes deliberately unknown; the `PromotionOperation`
  dual-mechanism seam with zero `ex.Message`/`PlatformHttpException` classification; the bilingual
  resource pair + registration; the removed foreign Application/Domain references and retired baseline
  entries; the self-contained Contracts signature; the 18-validator coverage set with zero prose; the
  canonical `api.Created` seller create; the real cohesion splits; and zero inline code literals in
  production sources.
- Focused Host suite (`TmarFoundationTests`, `ErrorCatalogUniqueCodeGuardTests`,
  `HostModuleEndpointOwnershipTests`, `HostAdminAmcW33MerchandisingGuardTests`,
  `PromotionModuleAmsc001W1MigrateGuardTests`) — **33 passed / 0 failed**.
- Full `Tooba.Host.Tests` — **77 failed / 2,218 passed / 130 skipped** vs the W0 baseline **79 failed**:
  - **FIXED (3):** `PromotionFoundationTests.Promotion_does_not_own_authored_price_and_keeps_module_boundaries`,
    `PromotionPanelTests.Host_registers_seller_and_admin_promotion_routes_with_panel_access`,
    `TmarFoundationTests.Infrastructure_to_foreign_Domain_edges_do_not_expand_beyond_baseline`
    (the Promotion foreign-Application/Domain edge is gone).
  - **NEW (1, NOT caused by this wave):**
    `ProductWorkspaceModuleAmsc001W3R2CertGuardTests.Fresh_certification_truth_is_recorded_in_sot_and_manifest`
    fails on `docs/architecture/tmar-current-state.json` line 84 (`Assert.Equal(JsonValueKind.Null, r2.GetProperty("commit")...)`)
    because the **pre-existing** commit `b89f0b85` moved `"commit": "35c371c6"` into the
    `productWorkspaceAmsc001W3R2` block. This wave does not modify `tmar-current-state.json` at the time
    of that run and does not touch any ProductWorkspace artifact; the finding is recorded as
    pre-existing ProductWorkspace debt, outside Promotion scope.
- Zero new failure is attributable to Promotion. Zero guard was weakened; zero baseline was widened.

## State

- Stable-Error-Code-State: `UNREGISTERED_CODES_STRING_HEURISTIC_DUPLICATE_CROSS_CUTTING_DECLARATIONS`
  → `CONTRACTS_OWNED_40_DECLARED_13_HTTP_REACHABLE_27_DOMAIN_INVARIANTS_13_REGISTERED_ISKNOWN_GUARDED`.
- Localization-State: `HARDCODED_TEXT_AND_MISSING_INFRASTRUCTURE_USE` → `OWNED_BY_PROMOTION` (40 bilingual keys).
- API-Result-Pattern-State: `AD_HOC_ONE_ENDPOINT` → `CANONICAL` (zero raw `Results.Json`).
- Validator-Coverage-State: `GAPS_0_OF_21` → `18_VALIDATORS_19_COVERED_2_NO_VALIDATOR_REQUIRED`.
- Contracts-Boundary-State: `VIOLATION` → `COMPLIANT` (self-contained; no foreign type in any signature).
- Cross-Module-Coupling-State: `ILLEGAL_FOREIGN_INVENTORY_APPLICATION_AND_DOMAIN_PLUS_TWO_BASELINE_ENTRIES`
  → `LEGAL_CONTRACTS_ONLY`.
- File-Cohesion-State: `MULTI_RESPONSIBILITY_COHESION_VIOLATION_SIX_FILES` → `COHESIVE` (all six split).
- OpenTelemetry-State: `CANONICAL_WITH_ONE_UNDECORATED_COMPOSER_PATH` → `CANONICAL`.
- Schema-Migration-State: `UNCHANGED` (no migration file touched; `promotion` schema byte-identical).
- `microserviceExtractable`:
  `TARGET_TRUE_BLOCKED_BY_FOREIGN_INVENTORY_APPLICATION_AND_DOMAIN_EDGES_AND_NON_SELF_CONTAINED_CONTRACTS`
  → `TRUE_COUPLING_REPAIRED_STRUCTURE_WAVE_PENDING`.

Verdict: `READY_TO_STRUCTURE`.
