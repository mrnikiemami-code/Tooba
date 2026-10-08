# TB-TMAR-PROMOTION-AMSC-001-W3-R3 — Validation Evidence

Independent input-provenance / set-equality matrix and the focused build/test results, all re-derived from
disk at `HEAD == origin/main == 3737b4d56e35b868c9f0ef7f4c378f46a04db926`.

## 1. The 21-row request → route → validator matrix

Derived from the actual endpoint request construction (not from prose or historical counts).

| # | Route + verb | Request | Client-bound inputs | Class | Validator / reason |
| --- | --- | --- | --- | --- | --- |
| 1 | GET `/v1/seller/promotions` | `ListSellerPromotionsQuery` | none (scope from authorizer) | NO_VALIDATOR_REQUIRED | `SellerPartyId` produced by `RequireSellerPartyIdAsync`; no bound transport value |
| 2 | POST `/v1/seller/promotions` | `CreateSellerPromotionCommand` | `SellerPartyId`, body `Input` | VALIDATOR_REQUIRED | `CreateSellerPromotionCommandValidator` |
| 3 | GET `/v1/seller/promotions/{id}` | `GetSellerPromotionQuery` | `SellerPartyId`, `PromotionId` | VALIDATOR_REQUIRED | `GetSellerPromotionQueryValidator` |
| 4 | PUT `/v1/seller/promotions/{id}` | `UpdateSellerPromotionCommand` | `SellerPartyId`, `PromotionId`, body | VALIDATOR_REQUIRED | `UpdateSellerPromotionCommandValidator` |
| 5 | POST `/v1/seller/promotions/{id}/activate` | `ActivateSellerPromotionCommand` | `SellerPartyId`, `PromotionId` | VALIDATOR_REQUIRED | `ActivateSellerPromotionCommandValidator` |
| 6 | POST `/v1/seller/promotions/{id}/deactivate` | `DeactivateSellerPromotionCommand` | `SellerPartyId`, `PromotionId` | VALIDATOR_REQUIRED | `DeactivateSellerPromotionCommandValidator` |
| 7 | GET `/v1/admin/promotions` | `ListAdminPromotionsQuery` | optional `Guid? sellerPartyId` filter | NO_VALIDATOR_REQUIRED | optional server-side filter routed to `ListForAdminAsync`; no malformable transport shape; reachable only after admin authorizer |
| 8 | GET `/v1/admin/promotions/{id}` | `GetAdminPromotionQuery` | `PromotionId` | VALIDATOR_REQUIRED | `GetAdminPromotionQueryValidator` |
| 9 | POST `/v1/admin/promotions/{id}/deactivate` | `DeactivateAdminPromotionCommand` | `PromotionId` | VALIDATOR_REQUIRED | `DeactivateAdminPromotionCommandValidator` |
| 10 | GET `/v1/admin/merchandising-campaigns/` | `ListMerchandisingCampaignsQuery` | `search,lifecycle,promotionTypeId,runtimeWindow,skip,take,locale` | VALIDATOR_REQUIRED | `ListMerchandisingCampaignsQueryValidator` |
| 11 | GET `/v1/admin/merchandising-campaigns/types` | `ListMerchandisingCampaignTypesQuery` | `locale` | VALIDATOR_REQUIRED (repaired) | `ListMerchandisingCampaignTypesQueryValidator` → `PromotionValidationCodes.LocaleInvalid` |
| 12 | GET `/v1/admin/merchandising-campaigns/offer-candidates` | `ListMerchandisingOfferCandidatesQuery` | `search,skip,take` | VALIDATOR_REQUIRED | `ListMerchandisingOfferCandidatesQueryValidator` |
| 13 | GET `/v1/admin/merchandising-campaigns/{campaignId}` | `GetMerchandisingCampaignQuery` | `CampaignId`, `locale` | VALIDATOR_REQUIRED | `GetMerchandisingCampaignQueryValidator` |
| 14 | POST `/v1/admin/merchandising-campaigns/` | `CreateMerchandisingCampaignCommand` | body | VALIDATOR_REQUIRED | `CreateMerchandisingCampaignCommandValidator` |
| 15 | PUT `/v1/admin/merchandising-campaigns/{campaignId}` | `UpdateMerchandisingCampaignCommand` | `CampaignId`, body | VALIDATOR_REQUIRED | `UpdateMerchandisingCampaignCommandValidator` |
| 16 | POST `/v1/admin/merchandising-campaigns/{campaignId}/publish` | `PublishMerchandisingCampaignCommand` | `CampaignId` | VALIDATOR_REQUIRED | `PublishMerchandisingCampaignCommandValidator` |
| 17 | POST `/v1/admin/merchandising-campaigns/{campaignId}/archive` | `ArchiveMerchandisingCampaignCommand` | `CampaignId` | VALIDATOR_REQUIRED | `ArchiveMerchandisingCampaignCommandValidator` |
| 18 | POST `/v1/admin/merchandising-campaigns/{campaignId}/members` | `AddMerchandisingCampaignMemberCommand` | `CampaignId`, `SellerOfferId` | VALIDATOR_REQUIRED | `AddMerchandisingCampaignMemberCommandValidator` |
| 19 | DELETE `.../members/{sellerOfferId}` | `RemoveMerchandisingCampaignMemberCommand` | `CampaignId`, `SellerOfferId` | VALIDATOR_REQUIRED | `RemoveMerchandisingCampaignMemberCommandValidator` |
| 20 | PUT `.../members/order` | `ReorderMerchandisingCampaignMembersCommand` | `CampaignId`, `OrderedSellerOfferIds` | VALIDATOR_REQUIRED | `ReorderMerchandisingCampaignMembersCommandValidator` (4 rules) |
| 21 | PUT `.../members/{sellerOfferId}/price` | `SetMerchandisingCampaignMemberPriceCommand` | `CampaignId`, `SellerOfferId`, body | VALIDATOR_REQUIRED | `SetMerchandisingCampaignMemberPriceCommandValidator` |

Set equality: 21 route-reachable requests == 19 REQUIRED + 2 NO_VALIDATOR_REQUIRED, each exactly once;
19 distinct `AbstractValidator<T>` targets, one (`ReorderMerchandisingCampaignMembersCommandValidator`)
carrying four member-order rules. Validators emit machine codes only (zero `WithMessage(`); no business rule
duplicated.

## 2. Discovery / pipeline evidence

- Validators are discovered by `AddValidatorsFromAssembly(<Promotion Application assembly>)` in the
  Foundation CQRS registration; the single `ValidationBehavior<TRequest,TResponse>` is registered exactly
  once (`BuildingBlocks/Tooba.BuildingBlocks/TmarFoundation.cs:240`).
- Promotion requests are registered on the same Application assembly in Host `Program.cs:180`.
- Malformed locale → `SafeErrorMapper` → canonical `validation.failed` (400) carrying
  `promotion.validation.locale_invalid`, short-circuited before the composer; valid/absent locale reaches
  the composer unchanged (see the 14 behavior tests below).

## 3. Focused build / test results (exact counts)

| Command | Result |
| --- | --- |
| `dotnet build Tooba.Promotion.Tests` | 0 errors (2 pre-existing CS1574 warnings) |
| `dotnet test Tooba.Promotion.Tests` | **23 passed / 0 failed / 0 skipped** |
| `dotnet build Tooba.Host.Tests` | 0 errors |
| Promotion architecture guards (`PromotionModuleAmsc001*`, `PromotionArchitectureGuardTests`, `ErrorCatalogUniqueCodeGuardTests`, `HostAdminAmcW33`, `PromotionFoundationTests`, `PromotionPanelTests`, `PromotionContractsW6`) | **32 passed / 0 failed / 2 skipped** |
| Broader Promotion + global-structure run (`Promotion*`, `TmarCompleteReferenceStructureGateTests`, `TmarDurableGuardTests`, `PricingModuleAmsc001W3R3CertGuardTests`, `ErrorCatalogUniqueCodeGuardTests`) | **61 passed / 3 failed / 2 skipped** |

## 4. The 3 broader-run failures (pre-existing, unrelated, unchanged baseline)

| Failure | Cause | Relation to Promotion |
| --- | --- | --- |
| `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment` | known `Tooba.Catalog.Contracts.Cart` vs `Tooba.Catalog.Contracts` namespace deviation | NONE |
| `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` | stale recovery-text pin `Current Grid work checkpoint (CURRENT` | NONE |
| `TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative` | stale `certifiedModules` expectation list | NONE |

This is the byte-identical failure set recorded by W3-R2 at `85d9818d` (`61 passed / 3 failed / 2 skipped`).
No `Promotion*` guard appears in the failure set; no guard was weakened and no baseline was widened.
