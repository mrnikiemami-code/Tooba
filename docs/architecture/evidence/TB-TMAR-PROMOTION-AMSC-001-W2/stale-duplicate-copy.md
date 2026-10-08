# TB-TMAR-PROMOTION-AMSC-001-W2 — stale / duplicate copies

## Physical-Copy-State: `CLEAN`

After the W2 moves, each responsibility has exactly one live home.

## Retired paths verified absent on disk

| Retired path | Reason | Verified absent |
| --- | --- | --- |
| `Application/Commands/ActivateSellerPromotion/…` … (5 folders) | single-file use-case leaf | yes |
| `Application/Queries/GetAdminPromotion/…` … (4 folders) | single-file use-case leaf | yes |
| `Application/Commands`, `Application/Queries`, `Application/Models`, `Application/Ports` | retired technical axes | yes |
| `Application/Promotions/Validators`, `Application/Merchandising/Admin/Validators` | empty ceremonial folders | yes |
| `Application/Errors/PromotionErrors.cs` (+ folder) | stable codes moved to `Contracts/Errors` in W1 | yes |
| `Application/Ports/PromotionDirectoryPorts.cs` | mixed bundle split in W1 | yes |
| `Application/Merchandising/MerchandisingCampaignPorts.cs` | mixed bundle split in W1 | yes |
| `Application/Merchandising/MerchandisingCampaignAdminCqrs.cs` | technical-axis bundle split in W1 | yes |
| `Application/Merchandising/MerchandisingCampaignAdminModels.cs` | moved to `Merchandising/Models` | yes (only the Models copy exists) |
| `Application/Merchandising/MerchandisingCampaignReferences.cs` | moved to `Merchandising/Models` | yes (only the Models copy exists) |
| `Application/Merchandising/IMerchandisingCampaignAdminComposer.cs` | moved to `Merchandising/Ports` | yes (only the Ports copy exists) |
| `Application/Merchandising/IMerchandisingCampaignDirectory.cs` | moved to `Merchandising/Ports` | yes (only the Ports copy exists) |

Git records every one of the above as a rename/move (`RM`) or delete, so there is no dual live home and
no orphaned path retained in the index.

## Duplicate responsibility scan

* Only one `PromotionDbContext` (Infrastructure/Persistence) — no copy anywhere else.
* Only one `PromotionErrorCodes` class in the whole module (Contracts/Errors) — asserted by the W1 guard.
* Only one `MerchandisingCampaignAdminModels.cs` and one `MerchandisingCampaignReferences.cs`.
* Only one `IMerchandisingCampaignAdminComposer` / `IMerchandisingCampaignDirectory` declaration.
* No `TypeForwardedTo` shim was used to preserve an old namespace.

## Solution entry integrity

All six `.slnx` entries resolve to existing `.csproj` files; no entry points at a deleted path.
