# TB-TMAR-PROMOTION-AMSC-001-W3-R2 — Bounded Validator Classification Repair

- **Parent-Task**: `TB-TMAR-PROMOTION-AMSC-001-W3-R1`
- **Mode**: `BOUNDED_VALIDATOR_CLASSIFICATION_REPAIR`
- **Skill**: `tooba-architecture-migrate`
- **Channel**: `tooba-main`
- **Starting HEAD**: `85d9818d79fa0b1b906c52af16b1a33f1c2149ce`
- **Verdict**: `READY_FOR_FRESH_CERTIFY` (NOT CERTIFIED)

## 1. Defect (verified by W3-R1)

`MerchandisingCampaignAdminEndpoints.ListTypesAsync` binds a user-supplied `string? locale` and
dispatches `new ListMerchandisingCampaignTypesQuery(locale)`; the request forwards it to
`IMerchandisingCampaignAdminComposer.ListTypesAsync`. No FluentValidation validator covered it, so the W3
claim `EXHAUSTIVE_18_VALIDATOR_REQUIRED_3_NO_VALIDATOR_REQUIRED` was false. The two sibling
locale-bearing reads were already validated with `BeWellFormedLocale` + `PromotionValidationCodes.LocaleInvalid`.

## 2. Independent 21-request matrix (verified from disk)

| # | Request | Transport shape | Class |
| --- | --- | --- | --- |
| 1 | `CreateSellerPromotionCommand` | `SellerPartyId`, `Input` | REQUIRED |
| 2 | `UpdateSellerPromotionCommand` | `SellerPartyId`, `PromotionId`, `Input` | REQUIRED |
| 3 | `ActivateSellerPromotionCommand` | `SellerPartyId`, `PromotionId` | REQUIRED |
| 4 | `DeactivateSellerPromotionCommand` | `SellerPartyId`, `PromotionId` | REQUIRED |
| 5 | `GetSellerPromotionQuery` | `SellerPartyId`, `PromotionId` | REQUIRED |
| 6 | `GetAdminPromotionQuery` | `PromotionId` | REQUIRED |
| 7 | `DeactivateAdminPromotionCommand` | `PromotionId` | REQUIRED |
| 8 | `ListMerchandisingCampaignsQuery` | `Skip`,`Take`,`Lifecycle`,`RuntimeWindow`,`Locale` | REQUIRED |
| 9 | `ListMerchandisingCampaignTypesQuery` | `Locale` | **REQUIRED (repaired)** |
| 10 | `ListMerchandisingOfferCandidatesQuery` | `Skip`,`Take` | REQUIRED |
| 11 | `GetMerchandisingCampaignQuery` | `CampaignId`,`Locale` | REQUIRED |
| 12 | `CreateMerchandisingCampaignCommand` | `Body` | REQUIRED |
| 13 | `UpdateMerchandisingCampaignCommand` | `CampaignId`,`Body` | REQUIRED |
| 14 | `PublishMerchandisingCampaignCommand` | `CampaignId` | REQUIRED |
| 15 | `ArchiveMerchandisingCampaignCommand` | `CampaignId` | REQUIRED |
| 16 | `AddMerchandisingCampaignMemberCommand` | `CampaignId`,`SellerOfferId` | REQUIRED |
| 17 | `RemoveMerchandisingCampaignMemberCommand` | `CampaignId`,`SellerOfferId` | REQUIRED |
| 18 | `ReorderMerchandisingCampaignMembersCommand` | `CampaignId`,`OrderedSellerOfferIds` | REQUIRED |
| 19 | `SetMerchandisingCampaignMemberPriceCommand` | `CampaignId`,`SellerOfferId`,`Body` | REQUIRED |
| 20 | `ListSellerPromotionsQuery` | authorizer-resolved seller party id only | NO_VALIDATOR_REQUIRED |
| 21 | `ListAdminPromotionsQuery` | optional server-side filter only | NO_VALIDATOR_REQUIRED |

**Corrected matrix**: `19 VALIDATOR_REQUIRED + 2 NO_VALIDATOR_REQUIRED = 21`. The two exemptions were
independently re-validated: neither request binds a client-supplied value (`ListSellerPromotionsQuery`
takes its scope from `RequireSellerPartyIdAsync`; `ListAdminPromotionsQuery` takes only an optional
server-side filter).

## 3. Repair

Added exactly one validator in the canonical home
`Tooba.Promotion.Application/Validation/PromotionRequestValidators.cs`:

```csharp
public sealed class ListMerchandisingCampaignTypesQueryValidator : AbstractValidator<ListMerchandisingCampaignTypesQuery>
{
    public ListMerchandisingCampaignTypesQueryValidator()
        => RuleFor(x => x.Locale)
            .Must(BeWellFormedLocale)
            .WithErrorCode(PromotionValidationCodes.LocaleInvalid);

    private static bool BeWellFormedLocale(string? value) =>
        string.IsNullOrWhiteSpace(value)
        || value.Trim().All(c => char.IsLetter(c) || c == '-' || c == '_');
}
```

- No new code, descriptor, locale parser, normalization or business rule was introduced.
- `BeWellFormedLocale` is byte-identical to the two existing locale validators.
- Validator DI discovery is unchanged (`AddToobaCqrsFoundation` → `AddValidatorsFromAssembly` of the
  Promotion Application assembly, `Host/Program.cs`); the `ValidationBehavior` pipeline now rejects a
  malformed locale before the handler runs.
- Missing/empty/blank locale stays valid, preserving the existing `locale ?? "fa-IR"` behavior in the
  composer.

## 4. Behavior preservation

- Routes, verbs, signatures, DTOs and composer behavior unchanged; only a new transport-shape guard was
  added.
- Malformed locale now fails with the canonical `validation.failed` envelope carrying
  `promotion.validation.locale_invalid` (HTTP 400) via `SafeErrorMapper` before the composer is invoked.
- Valid/absent locale reaches the composer unchanged.

## 5. Truth reconciliation (historical W3 flagged, not rewritten)

- `tmar-current-state.json` → `promotionAmsc001W3`: `validatorMatrixState` corrected to
  `EXHAUSTIVE_19_VALIDATOR_REQUIRED_2_NO_VALIDATOR_REQUIRED`; `state` → `CERTIFICATION_SUPERSEDED_STRUCTURE_VERIFIED`;
  added `verdictState=BLOCKED_PENDING_FRESH_CERTIFY`, `certificationState=BLOCKED_SUPERSEDED_BY_W3_R1_PENDING_FRESH_CERTIFY`,
  `supersededByTask/Commit`, `repairTask`, `supersessionReason`, `freshCertifyState`, `historicalCertificationNote`.
- `tmar-module-structure-manifests.json` → Promotion `certificationNote`: corrected to 19+2 and prefixed
  `STRUCTURE_VERIFIED; VERDICT_BLOCKED_SUPERSEDED_BY_W3_R1_PENDING_FRESH_CERTIFY`; added
  `certificationState`, `supersededByTask/Commit`, `repairTask`. Exactly one Promotion entry remains.
- Added SoT block `promotionAmsc001W3R2`.
- **Retention decision**: `structureCertified: true` and `structureLock.certifiedModules` membership were
  retained. Promotion's *structure* is independently verified; only the W3 *verdict* was false. Removing it
  would also have forced edits to repository-global certified-module locks, which this task forbids. The
  false verdict is instead explicitly flagged as blocked/superseded pending a fresh Certify wave.
- Historical W3 and W3-R1 evidence are preserved as immutable records; this file adds the R2 correction.

## 6. Guard corrections (no weakening, no frozen counts)

- `PromotionModuleAmsc001W3CertGuardTests`: matrix corrected to 19+2 with exact request identity
  (`Contains ListMerchandisingCampaignTypesQuery`, `DoesNotContain` the two exemptions), added the
  supersession/fresh-certify assertions, and corrected the `state` + `validatorMatrixState` pins.
- `PromotionModuleAmsc001W1MigrateGuardTests`: added the new validator type to the coverage list.

## 7. Focused validation (exact counts)

| Command | Result |
| --- | --- |
| `dotnet build Tooba.Promotion.Tests` | 0 errors |
| `dotnet test Tooba.Promotion.Tests` | **23 passed / 0 failed / 0 skipped** (14 new locale tests) |
| `dotnet build Tooba.Host.Tests` | 0 errors |
| Promotion architecture guards (W1+W2+W3+`PromotionArchitectureGuardTests`+`ErrorCatalogUniqueCodeGuardTests`) | **26 passed / 0 failed** |
| `Promotion|TmarCompleteReferenceStructureGateTests|TmarDurableGuardTests|PricingModuleAmsc001W3R3CertGuardTests|ErrorCatalogUniqueCodeGuardTests` | **61 passed / 3 failed / 2 skipped** |
| Promotion-adjacent guards (`PromotionFoundationTests`,`PromotionPanelTests`,`HostAdminAmcW33`,`ContractsW6`) | **10 passed / 0 failed / 2 skipped** |

The 3 failures are **pre-existing and unrelated**, reproduced byte-identically in a clean worktree at
`85d9818d`: `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`
(the known `Tooba.Catalog.Contracts/Cart` namespace deviation), and
`TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` +
`Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative` (stale recovery-text pins
`Current Grid work checkpoint (CURRENT` and a stale `certifiedModules` list). No Promotion guard fails.

## 8. Forbidden-scope compliance

- No endpoint route/verb/signature change, no business behavior change.
- No new error/localization code, no alternative locale semantics, no new dependency.
- No schema/migration, structural relocation or project graph change.
- No unrelated module, Host runtime or frontend change.
- No repository-global Host lock, accepted checkpoint or unrelated certified-module change.
- No guard weakened, no baseline widened, no Host failure hidden.
- No fresh Certify claimed; the success state is `READY_FOR_FRESH_CERTIFY`.

## 9. Next step (separately issued Task only)

A fresh `tooba-architecture-certify` wave must independently re-check every ARCH-COMPLETE-002 gate against
the corrected SoT/manifest truth before the Promotion certification verdict is reinstated.
