# TB-TMAR-PROMOTION-AMSC-001-W3-R1 — Independent Certification Evidence Audit

- **Parent-Task**: `TB-TMAR-PROMOTION-AMSC-001-W3`
- **Mode**: `INDEPENDENT_CERTIFICATION_EVIDENCE_AUDIT_ONLY`
- **Channel**: `tooba-main`
- **Skill**: `tooba-architecture-certify`
- **Starting HEAD**: `6bf747455b0ab0d8468595ecf42fb73a4d15a52b` (authoritative remote `main`)
- **Verdict**: `CERTIFICATION_REVIEW_BLOCKED`

Authorities under audit: W0 `d00cf666`, W1 `06858933`, W2 `597147ea`, W3 `6bf74745`.
No production, guard, SoT, manifest, schema or baseline file was modified by this audit.

## 1. Independent result

| Gate | Result |
| --- | --- |
| Endpoint-Route-State | `VERIFIED_21` |
| Request-Validator-Matrix-State | `CONFLICT` |
| Boundary-State | `VERIFIED_CONTRACTS_ONLY` |
| Error-Catalog-State | `VERIFIED` |
| Structure-State | `VERIFIED_ARCH_COMPLETE_002` |
| Schema-Migration-State | `UNCHANGED` |
| Host-Checkpoint-State | `PRESERVED` |
| Host-Failure-Baseline-State | `SAME_FAILED_NAME_SET` |
| Extractability-State | `ARCHITECTURAL_ONLY_RUNTIME_UNPROVEN` |
| Certification-Review-State | `BLOCKED` |

## 2. Endpoint routes — `VERIFIED_21`

Enumerated from disk (`Tooba.Promotion.Endpoints`). 21 registrations, **21 distinct verb+path** pairs,
**21** `ISender` dispatches, zero concrete-handler invocation, zero `SendAsync`.

| Verb | Path | Request | Handler |
| --- | --- | --- | --- |
| GET | `/v1/seller/promotions` | `ListSellerPromotionsQuery` | `ListSellerPromotionsQueryHandler` |
| POST | `/v1/seller/promotions` | `CreateSellerPromotionCommand` | `CreateSellerPromotionCommandHandler` |
| GET | `/v1/seller/promotions/{id:guid}` | `GetSellerPromotionQuery` | `GetSellerPromotionQueryHandler` |
| PUT | `/v1/seller/promotions/{id:guid}` | `UpdateSellerPromotionCommand` | `UpdateSellerPromotionCommandHandler` |
| POST | `/v1/seller/promotions/{id:guid}/activate` | `ActivateSellerPromotionCommand` | `ActivateSellerPromotionCommandHandler` |
| POST | `/v1/seller/promotions/{id:guid}/deactivate` | `DeactivateSellerPromotionCommand` | `DeactivateSellerPromotionCommandHandler` |
| GET | `/v1/admin/promotions` | `ListAdminPromotionsQuery` | `ListAdminPromotionsQueryHandler` |
| GET | `/v1/admin/promotions/{id:guid}` | `GetAdminPromotionQuery` | `GetAdminPromotionQueryHandler` |
| POST | `/v1/admin/promotions/{id:guid}/deactivate` | `DeactivateAdminPromotionCommand` | `DeactivateAdminPromotionCommandHandler` |
| GET | `/v1/admin/merchandising-campaigns/` | `ListMerchandisingCampaignsQuery` | `ListMerchandisingCampaignsQueryHandler` |
| GET | `/v1/admin/merchandising-campaigns/types` | `ListMerchandisingCampaignTypesQuery` | `ListMerchandisingCampaignTypesQueryHandler` |
| GET | `/v1/admin/merchandising-campaigns/offer-candidates` | `ListMerchandisingOfferCandidatesQuery` | `ListMerchandisingOfferCandidatesQueryHandler` |
| GET | `/v1/admin/merchandising-campaigns/{campaignId:guid}` | `GetMerchandisingCampaignQuery` | `GetMerchandisingCampaignQueryHandler` |
| POST | `/v1/admin/merchandising-campaigns/` | `CreateMerchandisingCampaignCommand` | `CreateMerchandisingCampaignCommandHandler` |
| PUT | `/v1/admin/merchandising-campaigns/{campaignId:guid}` | `UpdateMerchandisingCampaignCommand` | `UpdateMerchandisingCampaignCommandHandler` |
| POST | `/v1/admin/merchandising-campaigns/{campaignId:guid}/publish` | `PublishMerchandisingCampaignCommand` | `PublishMerchandisingCampaignCommandHandler` |
| POST | `/v1/admin/merchandising-campaigns/{campaignId:guid}/archive` | `ArchiveMerchandisingCampaignCommand` | `ArchiveMerchandisingCampaignCommandHandler` |
| POST | `/v1/admin/merchandising-campaigns/{campaignId:guid}/members` | `AddMerchandisingCampaignMemberCommand` | `AddMerchandisingCampaignMemberCommandHandler` |
| DELETE | `/v1/admin/merchandising-campaigns/{campaignId:guid}/members/{sellerOfferId:guid}` | `RemoveMerchandisingCampaignMemberCommand` | `RemoveMerchandisingCampaignMemberCommandHandler` |
| PUT | `/v1/admin/merchandising-campaigns/{campaignId:guid}/members/order` | `ReorderMerchandisingCampaignMembersCommand` | `ReorderMerchandisingCampaignMembersCommandHandler` |
| PUT | `/v1/admin/merchandising-campaigns/{campaignId:guid}/members/{sellerOfferId:guid}/price` | `SetMerchandisingCampaignMemberPriceCommand` | `SetMerchandisingCampaignMemberPriceCommandHandler` |

Host owns zero Promotion route literal (`/v1/seller/promotions`, `/v1/admin/promotions`,
`/v1/admin/merchandising-campaigns` absent from `Tooba.Host/**`); no `Host/Tooba.Host/Promotion`
production folder exists. Every route is mapped from `PromotionEndpointModule.MapPromotionEndpoints`.

## 3. Request/validator matrix — `CONFLICT`

18 validators exist (verified by enumerating every `AbstractValidator<T>` in the module). The
`NO_VALIDATOR_REQUIRED` set claimed by W3 (`ListSellerPromotionsQuery`, `ListAdminPromotionsQuery`,
`ListMerchandisingCampaignTypesQuery`) is **not** defensible for the third member.

### Blocking finding A — `ListMerchandisingCampaignTypesQuery` is `VALIDATOR_REQUIRED`

- Route binding takes an endpoint transport input:
  `Tooba.Promotion.Endpoints/Admin/MerchandisingCampaignAdminEndpoints.cs:40`
  `ListTypesAsync(..., string? locale, CancellationToken cancellationToken)`.
- The request carries it: `Application/Merchandising/Admin/Queries/ListMerchandisingCampaignTypesQuery.cs:12`
  `record ListMerchandisingCampaignTypesQuery(string? Locale)`.
- The value reaches a datastore query: `Infrastructure/Merchandising/MerchandisingCampaignAdminComposer.cs:126`
  `_campaigns.ListPromotionTypeOptionsAsync(locale ?? "fa-IR", cancellationToken)`.
- No validator covers it: enumerating all 18 `AbstractValidator<T>` targets and grepping the whole
  module for any validator referencing `ListMerchandisingCampaignTypesQuery` returns `NONE`.
- It is not the same shape as the two legitimate exemptions. `ListAdminPromotionsQuery(Guid? SellerPartyId)`
  takes a server-side filter only, and `ListSellerPromotionsQuery` derives its scope from the
  authorizer-resolved seller party id — neither binds a client value. `ListMerchandisingCampaignTypesQuery`
  binds a client-supplied `locale` string, exactly the shape the module already validates elsewhere:
  `PromotionRequestValidators.cs:118-121` (`ListMerchandisingCampaignsQuery.Locale`) and
  `:157-160` (`GetMerchandisingCampaignQuery.Locale`) both use
  `.Must(BeWellFormedLocale).WithErrorCode(PromotionValidationCodes.LocaleInvalid)`.
- Governing rule: `docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md:101-103` —
  transport/input validation (primitive shape) belongs to FluentValidation, and every endpoint-reachable
  request is classified `VALIDATOR_REQUIRED`/`NO_VALIDATOR_REQUIRED` in a durable guard.

**Independent classification**: 19 `VALIDATOR_REQUIRED` + 2 `NO_VALIDATOR_REQUIRED` (18 validators, one of
which — `ReorderMerchandisingCampaignMembersCommandValidator` — carries four rules). W3's
`EXHAUSTIVE_18_VALIDATOR_REQUIRED_3_NO_VALIDATOR_REQUIRED` is one short.

### Evidence-consistency defect B (non-blocking)

W1 recorded the matrix as `18_VALIDATORS_19_COVERED_2_NO_VALIDATOR_REQUIRED`
(`docs/architecture/evidence/TB-TMAR-PROMOTION-AMSC-001-W1/migrate.md:99,243`). W3 silently changed the
classification to `18_VALIDATOR_REQUIRED_3_NO_VALIDATOR_REQUIRED` without recording the reclassification.
W3's classification happens to match the module's own guard, but it is the opposite direction from the W1
record and the change is undocumented.

### Guard blind spot C (non-blocking, hides A)

`PromotionModuleAmsc001W3CertGuardTests.cs:150-166` derives the matrix as
`Regex.Matches(validatorFile, "AbstractValidator<(?<t>\w+)>")` (18) plus
`Assert.Equal(21, validatedRequests.Count + 3)`. The `+ 3` is a hardcoded constant, so the guard asserts
"18 validators + 3 no-validator = 21" without proving that those exact 3 requests truly require no
validator. It cannot detect a wrongly exempted request.

## 4. Boundary — `VERIFIED_CONTRACTS_ONLY`

- All six `*.csproj` enumerated. `Tooba.Promotion.Endpoints` references only
  `Promotion.Application` + `BuildingBlocks` (no `Promotion.Infrastructure`, no `Promotion.Domain`).
- `Tooba.Promotion.Contracts` references only `BuildingBlocks` and contains **zero** foreign type
  (`Tooba.Offer|Pricing|Inventory|Catalog|Party|Cart|Order|Payment`) in any file.
- Repo-wide source scan for `Tooba.<foreign>.(Application|Infrastructure|Domain)` across **all** Promotion
  `.cs` files (not a hardcoded five-name list) returns `NO_FOREIGN_APP_INFRA_DOMAIN_REFERENCE`.
- `Promotion.Infrastructure` uses only `*.Contracts` namespaces for its outbound edges
  (Catalog/Inventory/Offer/Party/Pricing Contracts) — legal Contracts-only.
- No foreign `DbContext`/`DbSet`/`FromSql`/`ExecuteSql`/`DbContextOptions` outside
  `Promotion.Infrastructure`; no SQL cross-schema join; no `TransactionScope`/`BeginTransaction`/
  `UseTransaction`/`EnlistTransaction`.
- Pre-existing `Tooba.Catalog.Contracts/Cart` namespace deviation (fails the repository-global
  `TmarCompleteReferenceStructureGateTests` gate) is unrelated to Promotion and not counted against it.

## 5. Error catalog — `VERIFIED`

- Exactly **40** declared literal codes in the single canonical
  `Tooba.Promotion.Contracts/Errors/PromotionErrorCodes.cs` (40 distinct). Repo-wide, the only
  `class PromotionErrorCodes` production declaration is that file (the other two hits are guard
  string literals).
- Exactly **13** registered descriptors in `PromotionErrorCatalogContributor` = the `HttpReachable` set;
  27 domain invariants handled via `IsKnown`/`IsDomainInvariant`.
- **40/40** codes localized in both `PromotionErrors.resx` and `PromotionErrors.fa.resx`.
- Zero message-based classification (`.Message.Contains/StartsWith/==`, `ex.Message`) in production.
- Zero raw `Results.*` payload mapping in Endpoints; exactly one `api.Created(` (the seller-create 201 path).

## 6. Structure — `VERIFIED_ARCH_COMPLETE_002`

Six on-disk projects; root `.cs` only `PromotionEndpointModule.cs` in Endpoints; `.slnx`
`/Modules/Promotion/` lists exactly those six; Application folder depth ≤ 3
(`Promotions/{Commands,Models,Ports,Queries}`, `Merchandising/{Ports,Models,Admin/{Commands,Queries}}`,
shared `Checkout`/`Composition`/`Validation`); no use-case leaf folders; manifest membership certified once
(`modules[]`, `structureCertified: true`, `preCertModules` empty).

## 7. Schema/migrations — `UNCHANGED`

`git diff 597147ea HEAD -- src/backend/Modules/Promotion` is empty (W3 changed no production file);
`git diff d00cf666 HEAD -- .../Persistence/Migrations` is empty (migrations unchanged since W0);
`PromotionDbContext.Schema = "promotion"` unchanged.

## 8. Host checkpoint — `PRESERVED`

Host keeps only the composition root (`AddPromotionEndpointPresentation()`, the Application-assembly CQRS
registration, the accepted Host platform security adapter, `app.MapPromotionEndpoints()`); no Promotion
route literal and no `Host/Tooba.Host/Promotion` production folder. Global Host checkpoint untouched.

## 9. Host failure baseline — `SAME_FAILED_NAME_SET`

Independently reproduced (W3 evidence recorded totals only, no name list). Full Host suite run in this
working tree at `6bf74745` and in a clean git worktree at the W2 baseline `597147ea`, same build.
Both produce **77** failures; `Compare-Object` on the exact failure name sets yields **zero** new and
**zero** fixed. No `Promotion*` guard appears in the failure set — no pre-existing failure invalidates a
required Promotion cert guard. (The 3 `TmarSourceSizeAndInfraAppTests` failures are environment pollution
from a stray `.tmp-baseline/` directory on this machine and are identical in both runs; no Promotion file
is implicated.)

## 10. Extractability — `ARCHITECTURAL_ONLY_RUNTIME_UNPROVEN`

Compile-time Contracts-only boundaries are verified. This does **not** prove deployable standalone runtime:
no host-less execution, no independent schema/DI/startup validation, and no cross-service transport
verification was performed. Classified `ARCHITECTURAL_EXTRACTABILITY` only.

## 11. Proposed narrowly scoped repair (NOT implemented in this audit)

1. Add `ListMerchandisingCampaignTypesQueryValidator` in
   `Tooba.Promotion.Application/Validation/PromotionRequestValidators.cs` applying the existing
   `BeWellFormedLocale` rule + `PromotionValidationCodes.LocaleInvalid` to `Locale` (exact precedent:
   `ListMerchandisingCampaignsQueryValidator`, `GetMerchandisingCampaignQueryValidator`).
2. Reclassify the matrix to `19 VALIDATOR_REQUIRED + 2 NO_VALIDATOR_REQUIRED`, and record the W1→W3
   reclassification honestly.
3. Strengthen `PromotionModuleAmsc001W3CertGuardTests` so the `NO_VALIDATOR_REQUIRED` set is asserted by
   exact request identity with a per-request justification, instead of the hardcoded `+ 3` constant.
4. Optional hardening: include `Tooba.Promotion.Endpoints` in the W3 guard's generic `ProductionProjects`
   source enumeration (`:18-24` currently omits it; the W1 guard covers it).
5. Re-certify W3 after repair (W3-R2), then promote/reconcile SoT.

## 12. Files touched by this audit

- `docs/ai/tasks/TB-TMAR-PROMOTION-AMSC-001-W3-R1.task.md` (claim artifact)
- `docs/architecture/evidence/TB-TMAR-PROMOTION-AMSC-001-W3-R1/audit.md` (this file)
- `docs/architecture/evidence/TB-TMAR-PROMOTION-AMSC-001-W3-R1/audit.json` (machine-readable mapping)

No production, guard, SoT, manifest, schema or baseline change.
