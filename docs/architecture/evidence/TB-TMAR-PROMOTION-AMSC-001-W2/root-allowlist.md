# TB-TMAR-PROMOTION-AMSC-001-W2 — root allowlist

## Root-Allowlist-State: `ENFORCED`

Root `.cs` files and top-level folders were compared against the manifest entry now recorded for
Promotion in `docs/architecture/tmar-module-structure-manifests.json` → `preCertModules[Promotion]`.

| Project | Root `.cs` on disk | Manifest `rootAllowlist` | Forbidden root files | Forbidden top-level folders |
| --- | --- | --- | --- | --- |
| `Tooba.Promotion.Contracts` | *(none)* | `[]` | `PromotionContracts.cs`, `MerchandisingCampaignContracts.cs`, `PromotionErrorCodes.cs`, `PromotionErrorResourceSet.cs` | — |
| `Tooba.Promotion.Domain` | *(none)* | `[]` | `PromotionDefinition.cs`, `MerchandisingCampaign.cs`, `PromotionDomainEvents.cs` | — |
| `Tooba.Promotion.Application` | *(none)* | `[]` | `PromotionContracts.cs`, `PromotionDirectoryPorts.cs`, `MerchandisingCampaignPorts.cs`, `PromotionErrorCodes.cs`, `PromotionOperation.cs` | `Commands`, `Queries`, `Models`, `Ports`, `Validators`, `Errors`, `Handlers`, `Requests` |
| `Tooba.Promotion.Infrastructure` | *(none)* | `[]` | `PromotionModule.cs`, `PromotionDbContext.cs`, `PromotionDirectory.cs`, `MerchandisingCampaignAdminComposer.cs`, `PromotionOutboxRegistration.cs` | `Migrations`, `Repositories` |
| `Tooba.Promotion.Endpoints` | `PromotionEndpointModule.cs` | `["PromotionEndpointModule.cs"]` | `PromotionAdminEndpoints.cs`, `PromotionSellerEndpoints.cs`, `MerchandisingCampaignAdminEndpoints.cs`, `PromotionErrorCatalogContributor.cs` | — |
| `Tooba.Promotion.Tests` | *(none)* | `[]` | — | — |

Notes:

* `ROOT_DUMP` = none. Every capability/implementation type lives under a capability folder.
* The Application `forbiddenTopLevelFolders` list is the *enforcement* form of the W2 repair: the
  retired technical-axis roots cannot silently return even if a file is added back.
* The allowlists were **narrowed to reality**, not widened to hide debt: `Infrastructure/Migrations` and
  `Infrastructure/Repositories` are explicitly forbidden (migrations live under `Persistence/Migrations`).
* No `.gitkeep` (or any extension-less placeholder) exists anywhere under `src/backend/Modules/Promotion`.
