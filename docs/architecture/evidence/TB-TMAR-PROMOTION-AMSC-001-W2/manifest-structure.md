# TB-TMAR-PROMOTION-AMSC-001-W2 — manifest structure

File: `docs/architecture/tmar-module-structure-manifests.json` (`version` `ARCH-COMPLETE-002`).

## Change in this wave

1. **Promotion removed from `uncertifiedHttpOwningModules`.**
   * Before: `["Returns", "Support", "Wallet", "Promotion"]`
   * After: `["Returns", "Support", "Wallet"]`

2. **Promotion added to `preCertModules`** (previously empty) with:
   * `structureCertified: false` — this is a **Structure** wave, not a Certify wave;
   * `lockVersion: "ARCH-COMPLETE-002"`;
   * an honest `certificationNote` recording the W0 → W1 → W2 lineage and the
     `TECHNICAL_AXIS_FIRST → PROFESSIONAL_SHALLOW` transition;
   * a full `projects[]` array with per-project `rootAllowlist`,
     `rootAllowlistJustification`, `forbiddenRootFiles` and `forbiddenTopLevelFolders`.

3. **`structureLock.certifiedModules` was deliberately NOT extended.** Promotion is still not a
   `modules[]` entry. Promotion of the module to the certified array is the exclusive authority of the
   W3 `tooba-architecture-certify` wave; doing it here would be a false certification.

## Per-project structural records added

| Project | rootAllowlist | forbiddenRootFiles | forbiddenTopLevelFolders |
| --- | --- | --- | --- |
| `Tooba.Promotion.Contracts` | `[]` | 4 | — |
| `Tooba.Promotion.Domain` | `[]` | 3 | — |
| `Tooba.Promotion.Application` | `[]` | 5 | `Commands`, `Queries`, `Models`, `Ports`, `Validators`, `Errors`, `Handlers`, `Requests` |
| `Tooba.Promotion.Infrastructure` | `[]` | 5 | `Migrations`, `Repositories` |
| `Tooba.Promotion.Endpoints` | `["PromotionEndpointModule.cs"]` | 4 | — |
| `Tooba.Promotion.Tests` | `[]` | — | — |

## Consistency

* Every `rootAllowlist` entry equals the actual root `.cs` set on disk (asserted by
  `PromotionModuleAmsc001W2StructureGuardTests.Promotion_manifest_structure_allowlists_match_disk`).
* Every `forbiddenRootFiles` entry was verified absent on disk by the same guard.
* Every `forbiddenTopLevelFolders` entry was verified absent on disk by the same guard.
* The file parses as JSON after the edit (`ConvertFrom-Json` PASS).

## Guard alignment performed alongside the manifest change

Two Host guards that pinned the *pre-W2* physical paths were reconciled to the real W2 surface
(same intent, no weakening — none of their assertions was removed):

* `HostAdminAmcW33MerchandisingGuardTests.Merchandising_CQRS_and_contracts_enrichment_ports_exist`
  now resolves `Merchandising/Ports/IMerchandisingCampaignAdminComposer.cs` (was
  `Merchandising/IMerchandisingCampaignAdminComposer.cs`).
* `PromotionFoundationTests.Promotion_does_not_own_authored_price_and_keeps_module_boundaries`
  now resolves `Application/Promotions/Ports/IPromotionEvaluator.cs` (was `Application/Ports/…`).

The module's own `PromotionArchitectureGuardTests.AllowedApplicationFolders` was **narrowed** from the W1
superset to the real W2 layout (`Checkout`, `Composition`, `Merchandising`, `Promotions`, `Validation`),
which strengthens the guard.
