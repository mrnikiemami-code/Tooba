# TB-TMAR-PROMOTION-AMSC-001-W2 — cohesion balance

## File-Cohesion-State: `COHESIVE`

W2 performed **pure relocation** plus the two namespace repairs; no file was split or merged, so the W1
cohesion verdict carries forward unchanged and no new god-file or over-split was introduced.

## God-file check

* No Promotion file appears in `docs/architecture/tmar-source-size-baseline.json`.
* The W1 cohesion splits are intact:
  * `MerchandisingCampaignAdminCqrs.cs` → 8 Commands + 4 Queries + `MerchandisingAdminErrorCodes.cs` + `MerchandisingAdminResult.cs`
  * `PromotionDirectoryPorts.cs` → 6 individual `Promotions/Ports/*.cs`
  * `MerchandisingCampaignPorts.cs` → `IMerchandisingCampaignDirectory.cs` + `MerchandisingCampaignReferences.cs`
  * `PromotionDirectory.cs` → `PromotionDirectory.cs` + `OpenPromotionUseCaseGuard.cs` + `DeferredPromotionRedemptionLedger.cs`
  * `PromotionErrors.cs` → `Contracts/Errors/PromotionErrorCodes.cs` + `Application/Composition/PromotionOperation.cs`
* Remaining larger files are single-responsibility and deliberately **not** cosmetically split
  (`OVERSIZED_ONLY` → WATCH only): `Infrastructure/Directories/MerchandisingCampaignDirectory.cs`,
  `Infrastructure/Merchandising/MerchandisingCampaignAdminComposer.cs`,
  `Domain/Aggregates/PromotionDefinition.cs`, `Infrastructure/Queries/MerchandisingCampaignQuery.cs`.

## Over-split check

* No folder exists purely to hold one file for a request/use case (asserted by the W2 guard).
* No file was created to satisfy a size/structure metric.
* `Application/Checkout` and `Application/Composition` hold exactly one cohesive cross-cutting file each
  and are shared seams, not request leaves; `Application/Validation` holds the two related validation
  artifacts (validators + codes) that belong together.

## Root vs. capability balance

| Layer | Primary axis | Balance |
| --- | --- | --- |
| Contracts | boundary kind (`Checkout`, `Merchandising`, `Errors`, `Resources`) | balanced |
| Domain | DDD kind (`Aggregates`, `Merchandising`, `Events`, `Policies`, `ValueObjects`) | balanced |
| Application | **capability first** (`Promotions`, `Merchandising`) + shared seams | balanced |
| Infrastructure | integration kind (`Adapters`, `Directories`, `Persistence`, `Messaging`, …) | balanced |
| Endpoints | audience (`Admin`, `Seller`) + shared `Errors` | balanced |

No `MULTI_RESPONSIBILITY_COHESION_VIOLATION` and no `OVER_SPLIT` remains.
