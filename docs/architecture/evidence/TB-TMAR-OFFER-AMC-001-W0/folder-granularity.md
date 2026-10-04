# Offer folder granularity — W0

Folder-Granularity-State: `TECHNICAL_AXIS_FIRST` + single-file request leaves

## Single-file request leaf folders (Application) — 11

Counted by **production source files**, not declared types. Every folder below contains exactly one `.cs` file.

| Offending folder | Files | Verdict |
| --- | --- | --- |
| `Tooba.Offer.Application/Commands/ActivateOffer` | 1 | OVER_FOLDERED |
| `Tooba.Offer.Application/Commands/ArchiveOffer` | 1 | OVER_FOLDERED |
| `Tooba.Offer.Application/Commands/CreateOffer` | 1 | OVER_FOLDERED |
| `Tooba.Offer.Application/Commands/SetOfferInventory` | 1 | OVER_FOLDERED |
| `Tooba.Offer.Application/Commands/SetOfferPrice` | 1 | OVER_FOLDERED |
| `Tooba.Offer.Application/Commands/SetOrderQuantityLimits` | 1 | OVER_FOLDERED |
| `Tooba.Offer.Application/Commands/SetReturnPolicy` | 1 | OVER_FOLDERED |
| `Tooba.Offer.Application/Commands/SuspendOffer` | 1 | OVER_FOLDERED |
| `Tooba.Offer.Application/Commands/UpdateOffer` | 1 | OVER_FOLDERED |
| `Tooba.Offer.Application/Queries/GetOffer` | 1 | OVER_FOLDERED |
| `Tooba.Offer.Application/Queries/ListSellerOffers` | 1 | OVER_FOLDERED |

## Technical-axis-first detection

`Application/Commands/<UseCase>/`, `Application/Queries/<UseCase>/`, `Application/Validators/<File>`, `Application/Mappings`, `Application/Ports`, `Application/ReadModels`, `Application/Policies` are the **primary** axes of the tree. The module is multi-capability (listing authoring/lifecycle, pricing delegation, inventory delegation, return-policy governance, read projection, selection policy), so the technical axis must not be primary.

## Non-request folders (not single-file-request violations)

| Folder | Files | Verdict |
| --- | --- | --- |
| `Application/Validators` | 7 | not a use-case leaf, but **misplaced** (shared rules + 5 request validators co-located away from their requests) |
| `Application/Mappings` | 1 | technical axis; move under capability |
| `Application/Policies` | 1 | technical axis; move under capability |
| `Application/Ports` | 1 | technical axis; move under capability |
| `Application/ReadModels` | 1 | technical axis; move under capability |
| `Domain/Aggregates` | 1 | allowed by `AllowedDomainFolders`; cohesive aggregate |
| `Domain/Errors` | 1 | allowed; shared stable codes |
| `Domain/Events` | 1 | allowed; cohesive event set |
| `Domain/ValueObjects` | 2 | allowed |
| `Contracts/Dtos` | 6 | allowed; cohesive boundary DTOs |
| `Contracts/Errors` | 1 | allowed |
| `Contracts/Ports` | 7 | allowed, but `ReturnPolicyContracts.cs` is a multi-responsibility dump (see cohesion-balance.md) |
| `Endpoints/Errors`, `Endpoints/Resources` | 1 / 3 | allowed shared boundaries |
| `Endpoints/Seller` | 2 | allowed audience folder |
| `Infrastructure/Persistence/Migrations` | 5 | allowed (generated) |
| `Infrastructure/Adapters/Tracing` | 2 | allowed cohesive sub-folder |

## Empty ceremonial folders

**None.** All folders contain at least one production file.

## Target state (W2)

`Application/`
- `Offers/Commands/{CreateOffer,UpdateOffer,ActivateOffer,SuspendOffer,ArchiveOffer,SetReturnPolicy,SetOrderQuantityLimits,SetOfferPrice,SetOfferInventory}/`
- `Offers/Queries/{GetOffer,ListSellerOffers}/`
- `Offers/{Mappings,Ports,Policies,ReadModels}/`
- `ReturnPolicy/`
- `Validation/`
