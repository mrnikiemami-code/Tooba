# TB-TMAR-PROMOTION-AMSC-001-W2 — folder granularity

Skill: `tooba-architecture-structure` (third of the four AMSC skills).
Parent wave: `TB-TMAR-PROMOTION-AMSC-001-W1` (`06858933`), verdict `READY_TO_STRUCTURE`.

## Classification

| State | Before | After |
| --- | --- | --- |
| Module applicability gate | `HTTP_OWNING` (21 module-owned routes) | `HTTP_OWNING` (unchanged) |
| Folder-Granularity-State | `TECHNICAL_AXIS_FIRST` + `OVER_FOLDERED` | `PROFESSIONAL_SHALLOW` |
| Single-file request leaf folders | 9 (`Application/Commands/<UseCase>`, `Application/Queries/<UseCase>`) | `ZERO` |
| Technical-axis-first request tree | yes (`Application/Commands` / `Application/Queries` as primary axis) | `RETIRED` |
| Solution-Explorer-State | `CANONICAL` | `CANONICAL` |
| Path-Namespace-State | `MISMATCH` (2 half-moved files) | `EXACT` |
| Physical-Copy-State | `CLEAN` | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` | `ENFORCED` |
| File-Cohesion-State | `COHESIVE` | `COHESIVE` |
| **Structure-State** | `REPAIR_REQUIRED` | **`READY_FOR_CERTIFY`** |

## Exact offending paths removed in this wave

The nine unjustified single-file use-case leaf folders (each wrapped exactly one production source file)
were flattened onto the `Promotions` capability axes:

```text
Application/Commands/ActivateSellerPromotion/ActivateSellerPromotionCommand.cs
Application/Commands/CreateSellerPromotion/CreateSellerPromotionCommand.cs
Application/Commands/DeactivateAdminPromotion/DeactivateAdminPromotionCommand.cs
Application/Commands/DeactivateSellerPromotion/DeactivateSellerPromotionCommand.cs
Application/Commands/UpdateSellerPromotion/UpdateSellerPromotionCommand.cs
Application/Queries/GetAdminPromotion/GetAdminPromotionQuery.cs
Application/Queries/GetSellerPromotion/GetSellerPromotionQuery.cs
Application/Queries/ListAdminPromotions/ListAdminPromotionsQuery.cs
Application/Queries/ListSellerPromotions/ListSellerPromotionsQuery.cs
```

Retired empty/technical-axis Application folders (deleted from disk and now forbidden in the manifest):

```text
Application/Commands
Application/Queries
Application/Models
Application/Ports
Application/Promotions/Validators   (empty)
Application/Merchandising/Admin/Validators   (empty)
```

Retired generic mixed bundles from W0/W1 (must never return):

```text
Application/Ports/PromotionDirectoryPorts.cs
Application/Merchandising/MerchandisingCampaignPorts.cs
Application/Merchandising/Admin/MerchandisingCampaignAdminCqrs.cs
Application/Errors/PromotionErrors.cs
```

## Final capability-first shallow Application tree

```text
Application/
  Promotions/
    Commands/   5 files (promotion lifecycle commands)
    Queries/    4 files (promotion reads)
    Models/     PromotionMutationInput.cs
    Ports/      6 files (directory/evaluator/seams)
  Merchandising/
    Ports/      2 files (admin composer + campaign directory ports)
    Models/     2 files (admin DTOs + merchandising references)
    Admin/
      Commands/ 8 files
      Queries/  4 files
  Checkout/     CheckoutPromotionAdapter.cs      (shared cross-capability seam)
  Composition/  PromotionOperation.cs            (single typed-fault seam)
  Validation/   PromotionRequestValidators.cs + PromotionValidationCodes.cs (shared)
```

## Legitimate single-file folders kept (NOT violations)

Per the skill's section 8/9, these are shared/framework homes, never use-case-named request leaves:
`Contracts/Checkout`, `Domain/Aggregates`, `Infrastructure/{Development,Events,Merchandising,Messaging,Persistence,Queries}`,
`Endpoints/Errors`, `Tests/{Architecture,Behavior}`. `Application/Checkout` and `Application/Composition`
each hold one cohesive cross-cutting file and are shared seams, not request leaves.

## Folder depth / explosion

Depth never exceeds capability → technical axis → file. No `Commands/<Capability>/<UseCase>/…` nesting.
No empty ceremonial folders remain and no `.gitkeep` exists anywhere in the module.
