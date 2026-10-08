# TB-TMAR-PROMOTION-AMSC-001-W3-R3 — Fresh Independent ARCH-COMPLETE-002 Certification

- **Parent-Task**: `TB-TMAR-PROMOTION-AMSC-001-W3-R2`
- **Mode**: `FRESH_INDEPENDENT_CERTIFY_AFTER_BOUNDED_REPAIR`
- **Channel**: `tooba-main`
- **Skill**: `tooba-architecture-certify`
- **Starting HEAD**: `b002d4596dbec4851bc77c6810c7c4b3e3a1b08c` (W3-R2 repair commit)
- **Verdict**: `COMPLETE_REFERENCE_PATTERN` (all applicable gates pass)

## 0. Precheck and baseline recovery

- Branch `main`; `git fetch origin` performed.
- The task-declared `STARTING HEAD` `b002d459` was the local `HEAD`. Remote `origin/main` had advanced
  four documentation-only commits that **enforce the input-provenance validator-coverage gate** in the four
  architecture skills (`a49acf22` → `2a146245` → `2c5a3cc5` → `3737b4d5`). None touches Promotion
  production, SoT, manifest or recovery.
- `b002d459` is a strict ancestor of `origin/main` and the working tree was clean, so local `main` was
  fast-forwarded to `origin/main` `3737b4d56e35b868c9f0ef7f4c378f46a04db926` (no reset, no stash, no
  rebase, no force-push). `HEAD == origin/main`; `b002d459` remains in ancestry.
- Authority ancestry verified with `git merge-base --is-ancestor`: `d00cf666` (W0), `06858933` (W1),
  `597147ea` (W2), `6bf74745` (historical W3), `85d9818d` (W3-R1), `b002d459` (W3-R2) are all ancestors
  of `HEAD`. No SHA was substituted.

This wave follows the freshly landed §6a **Independent Input-Provenance and Set-Equality Gate (HARD
BLOCKER)**: the matrix below was re-derived from disk (endpoint construction → request → validator), not
adopted from W3/W3-R1/W3-R2 prose, and proves exact set equality rather than a validator count.

## 1. Gate A — Applicability / routes (HTTP_OWNING) — PASS

Enumerated every `Map(Get|Post|Put|Delete)` literal across `Tooba.Promotion.Endpoints`: **exactly 21**
distinct verb+path pairs across three groups, mapped only from `PromotionEndpointModule.MapPromotionEndpoints`.

| # | Verb | Path | Request | Handler |
| --- | --- | --- | --- | --- |
| 1 | GET | `/v1/seller/promotions` | `ListSellerPromotionsQuery` | `ListSellerPromotionsQueryHandler` |
| 2 | POST | `/v1/seller/promotions` | `CreateSellerPromotionCommand` | `CreateSellerPromotionCommandHandler` |
| 3 | GET | `/v1/seller/promotions/{id:guid}` | `GetSellerPromotionQuery` | `GetSellerPromotionQueryHandler` |
| 4 | PUT | `/v1/seller/promotions/{id:guid}` | `UpdateSellerPromotionCommand` | `UpdateSellerPromotionCommandHandler` |
| 5 | POST | `/v1/seller/promotions/{id:guid}/activate` | `ActivateSellerPromotionCommand` | `ActivateSellerPromotionCommandHandler` |
| 6 | POST | `/v1/seller/promotions/{id:guid}/deactivate` | `DeactivateSellerPromotionCommand` | `DeactivateSellerPromotionCommandHandler` |
| 7 | GET | `/v1/admin/promotions` | `ListAdminPromotionsQuery` | `ListAdminPromotionsQueryHandler` |
| 8 | GET | `/v1/admin/promotions/{id:guid}` | `GetAdminPromotionQuery` | `GetAdminPromotionQueryHandler` |
| 9 | POST | `/v1/admin/promotions/{id:guid}/deactivate` | `DeactivateAdminPromotionCommand` | `DeactivateAdminPromotionCommandHandler` |
| 10 | GET | `/v1/admin/merchandising-campaigns/` | `ListMerchandisingCampaignsQuery` | `ListMerchandisingCampaignsQueryHandler` |
| 11 | GET | `/v1/admin/merchandising-campaigns/types` | `ListMerchandisingCampaignTypesQuery` | `ListMerchandisingCampaignTypesQueryHandler` |
| 12 | GET | `/v1/admin/merchandising-campaigns/offer-candidates` | `ListMerchandisingOfferCandidatesQuery` | `ListMerchandisingOfferCandidatesQueryHandler` |
| 13 | GET | `/v1/admin/merchandising-campaigns/{campaignId:guid}` | `GetMerchandisingCampaignQuery` | `GetMerchandisingCampaignQueryHandler` |
| 14 | POST | `/v1/admin/merchandising-campaigns/` | `CreateMerchandisingCampaignCommand` | `CreateMerchandisingCampaignCommandHandler` |
| 15 | PUT | `/v1/admin/merchandising-campaigns/{campaignId:guid}` | `UpdateMerchandisingCampaignCommand` | `UpdateMerchandisingCampaignCommandHandler` |
| 16 | POST | `/v1/admin/merchandising-campaigns/{campaignId:guid}/publish` | `PublishMerchandisingCampaignCommand` | `PublishMerchandisingCampaignCommandHandler` |
| 17 | POST | `/v1/admin/merchandising-campaigns/{campaignId:guid}/archive` | `ArchiveMerchandisingCampaignCommand` | `ArchiveMerchandisingCampaignCommandHandler` |
| 18 | POST | `/v1/admin/merchandising-campaigns/{campaignId:guid}/members` | `AddMerchandisingCampaignMemberCommand` | `AddMerchandisingCampaignMemberCommandHandler` |
| 19 | DELETE | `/v1/admin/merchandising-campaigns/{campaignId:guid}/members/{sellerOfferId:guid}` | `RemoveMerchandisingCampaignMemberCommand` | `RemoveMerchandisingCampaignMemberCommandHandler` |
| 20 | PUT | `/v1/admin/merchandising-campaigns/{campaignId:guid}/members/order` | `ReorderMerchandisingCampaignMembersCommand` | `ReorderMerchandisingCampaignMembersCommandHandler` |
| 21 | PUT | `/v1/admin/merchandising-campaigns/{campaignId:guid}/members/{sellerOfferId:guid}/price` | `SetMerchandisingCampaignMemberPriceCommand` | `SetMerchandisingCampaignMemberPriceCommandHandler` |

- **21** `ISender.Send` call sites; zero `SendAsync`, zero concrete-handler invocation, zero duplicate
  registration. Endpoint-reachable request set (21) == handler set (21) — exact one-to-one.
- Host owns **zero** Promotion route literal (`/v1/seller/promotions`, `/v1/admin/promotions`,
  `/v1/admin/merchandising-campaigns` appear only inside Host **test** guards, never Host production);
  no `Host/Tooba.Host/Promotion` production folder.

## 2. Gate B — Input-provenance / validator matrix (HARD BLOCKER) — PASS

Fresh per-request provenance matrix derived from endpoint construction. **Exact set equality**:
21 endpoint-reachable requests == 21 classified rows, each exactly once.

- **19 `VALIDATOR_REQUIRED`** — every one has a discoverable `AbstractValidator<T>` in the single
  canonical `Application/Validation/PromotionRequestValidators.cs`, registered by
  `AddValidatorsFromAssembly` of the Promotion Application assembly and executed by the Foundation
  `ValidationBehavior` pipeline before the handler.
- **2 `NO_VALIDATOR_REQUIRED`** — `ListSellerPromotionsQuery` (its `SellerPartyId` is produced by
  `IPromotionSellerAuthorizer.RequireSellerPartyIdAsync`; the request is not constructed from any bound
  client transport value) and `ListAdminPromotionsQuery` (its optional `Guid? sellerPartyId` query filter
  is routed through `PromotionDirectory.ListForAdminAsync` and cannot reach an unsupported/malformed state;
  the request is reachable only after `RequireAuthorizedAsync`, and the server applies the default
  admin-visible scope when absent).
- **Repaired row**: `ListMerchandisingCampaignTypesQuery` binds the client-supplied `string? locale`
  (`MerchandisingCampaignAdminEndpoints.ListTypesAsync`) and is now covered by the discoverable
  `ListMerchandisingCampaignTypesQueryValidator` using `PromotionValidationCodes.LocaleInvalid` and
  `BeWellFormedLocale` semantics byte-identical to the two sibling locale validators. Malformed locale
  fails with the canonical `validation.failed` envelope (400) before the composer runs; valid/absent locale
  reaches the composer unchanged.

See `validation.md` for the full 21-row matrix, discovery/pipeline evidence and the malformed/valid/absent
behavior proof.

## 3. Gate C — Canonical API results — PASS

- Zero raw `Results.Json(` / `Results.BadRequest(` / `Results.Problem(` / `TypedResults.*` in the module.
- All endpoints return `api.From(...)`; the seller-create `201` path uses `api.Created(...)` with a
  `Location` header.
- Zero message/exception-text failure classification in production; `PromotionOperation` maps
  `ContractOperationException` only under `PromotionErrorCodes.IsKnown` and `SemanticException` by code;
  the only other `catch` (`MerchandisingCampaignAdminComposer.TraceAsync`) is a tracer decorator that
  rethrows.

## 4. Gate D — Stable codes / localization — PASS

- **40** declared literal codes, **40 distinct** (composed uniqueness verified, not only a string count).
- **13** HTTP descriptors registered exactly once by the single `PromotionErrorCatalogContributor`
  (`D(PromotionErrorCodes.` count == 13); **27** domain-invariant codes carried by `Result` and never
  catalogued; `IsKnown` true for all 40; no `authorization.denied` re-registration.
- **40 EN + 40 FA** resource keys, identical key sets; `PromotionErrorResourceSet` registered exactly once
  by `PromotionEndpointModule.AddPromotionEndpointPresentation`.

## 5. Gate E — Structure (ARCH-COMPLETE-002) — PASS

- Capability-first `PROFESSIONAL_SHALLOW` Application layout; no technical-axis roots, no use-case leaf
  folders, no `.gitkeep` ceremony. Path↔namespace EXACT; root allowlists ENFORCED and equal to disk.
- `/Modules/Promotion/` solution group lists exactly the **six** projects (Domain, Contracts, Application,
  Infrastructure, Endpoints, Tests); no ceremonial project.

## 6. Gate F — Boundaries — PASS

- Project edges: Endpoints → Application + Contracts + BuildingBlocks only; Infrastructure → own
  Application/Contracts + foreign `*.Contracts` (Offer/Pricing/Inventory/Catalog/Party) only; Contracts →
  BuildingBlocks only (zero foreign module type in any signature).
- Production-source scan over all five non-test projects (including Endpoints) for
  `Tooba.<foreign>.(Application|Infrastructure|Domain)` returns **zero**. No cross-module join, no foreign
  `DbContext`/`DbSet`/`FromSql`/`ExecuteSql`, no `TransactionScope`/`BeginTransaction`/`UseTransaction`/
  `EnlistTransaction`. Legitimate foreign `*.Contracts` ports remain allowed.
- `microserviceExtractable` = **ARCHITECTURAL_READY_RUNTIME_UNPROVEN** (compile-time Contracts-only
  boundary verified; no host-less runtime/deployment proof asserted).

## 7. Gate G — Persistence / Host closure — PASS

- Own `PromotionDbContext` (`Schema = "promotion"`, `HasDefaultSchema`), own outbox, and the two migrations
  `20260823210000_InitialPromotion` + `20260919134400_MerchandisingCampaignFoundation` unchanged since W0
  (`git diff d00cf666 HEAD -- .../Persistence/Migrations` is empty) and unchanged vs `HEAD` (no
  production diff). No schema change.
- Host keeps only composition-root calls (`AddPromotionEndpointPresentation()`, the CQRS assembly
  registration, the accepted seller-authorizer adapter, `app.MapPromotionEndpoints()`); Host
  business/persistence authority ZERO; no `Host/Tooba.Host/Promotion` production folder.
- Global Host checkpoint preserved: `currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED`,
  `lastAcceptedTask = TB-TMAR-HOST-ROOT-FINAL-CERT-001`, `lastAcceptedCommit = 7a6c353a`,
  root `workflowStop = USER_REVIEW_HOST_ROOT_FINAL_CERT_001`.

## 8. Gate H — Validation baseline — PASS

Focused build/tests only; no full-suite retry loop; no guard weakened, no baseline widened. Exact counts in
`validation.md`. The 3 broader-run failures are pre-existing, unrelated and byte-identical to the W3-R2
recorded baseline: `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`
(known `Tooba.Catalog.Contracts/Cart` namespace deviation),
`TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` and
`TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative`
(stale recovery-text/certifiedModules pins). No `Promotion*` guard is in the failure set.

## 9. Verdict

Every applicable ARCH-COMPLETE-002 gate passes on the actual disk state. Promotion is reinstated as
`COMPLETE_REFERENCE_PATTERN` under `ARCH-COMPLETE-002`, with the honest `EXHAUSTIVE_19_VALIDATOR_REQUIRED_2_NO_VALIDATOR_REQUIRED`
matrix. This wave changed **no production file** (certify only). The historical W3 verdict remains
preserved as superseded; W3-R1/W3-R2 evidence is untouched.

Stop gate `USER_REVIEW_PROMOTION_AMSC_001_W3_R3`; `automaticNextImplementationTask = NONE`. Own cert
commit SHA is reported in the Bridge Result only (no self-referential SoT SHA).
