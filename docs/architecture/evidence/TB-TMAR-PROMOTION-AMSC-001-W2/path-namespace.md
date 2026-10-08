# TB-TMAR-PROMOTION-AMSC-001-W2 — path ↔ namespace

## Path-Namespace-State: `EXACT`

Method: for every production `.cs` (EF `Persistence/Migrations/**` and `*ModelSnapshot.cs` exempt per
the repository lock) the declared `namespace` was compared to the namespace derived from the physical
path relative to its project folder.

## Mismatches found at W2 entry (W1 half-move) — repaired

| File | Declared before | Required | Action |
| --- | --- | --- | --- |
| `Tooba.Promotion.Application/Merchandising/Models/MerchandisingCampaignAdminModels.cs` | `Tooba.Promotion.Application.Merchandising.Ports` | `Tooba.Promotion.Application.Merchandising.Models` | namespace corrected |
| `Tooba.Promotion.Application/Merchandising/Models/MerchandisingCampaignReferences.cs` | `Tooba.Promotion.Application.Merchandising.Ports` | `Tooba.Promotion.Application.Merchandising.Models` | namespace corrected |

Consumers that had been resolving those DTOs through the `…Merchandising.Ports` namespace gained the
explicit `using Tooba.Promotion.Application.Merchandising.Models;`:

```text
Tooba.Promotion.Application/Merchandising/Admin/Commands/AddMerchandisingCampaignMemberCommand.cs
Tooba.Promotion.Application/Merchandising/Admin/Commands/CreateMerchandisingCampaignCommand.cs
Tooba.Promotion.Application/Merchandising/Admin/Commands/RemoveMerchandisingCampaignMemberCommand.cs
Tooba.Promotion.Application/Merchandising/Admin/Commands/ReorderMerchandisingCampaignMembersCommand.cs
Tooba.Promotion.Application/Merchandising/Admin/Commands/SetMerchandisingCampaignMemberPriceCommand.cs
Tooba.Promotion.Application/Merchandising/Admin/Commands/UpdateMerchandisingCampaignCommand.cs
Tooba.Promotion.Application/Merchandising/Admin/Queries/GetMerchandisingCampaignQuery.cs
Tooba.Promotion.Application/Merchandising/Admin/Queries/ListMerchandisingCampaignsQuery.cs
Tooba.Promotion.Application/Merchandising/Admin/Queries/ListMerchandisingCampaignTypesQuery.cs
Tooba.Promotion.Application/Merchandising/Admin/Queries/ListMerchandisingOfferCandidatesQuery.cs
Tooba.Promotion.Application/Merchandising/Ports/IMerchandisingCampaignAdminComposer.cs
Tooba.Promotion.Application/Merchandising/Ports/IMerchandisingCampaignDirectory.cs
Tooba.Promotion.Endpoints/Admin/MerchandisingCampaignAdminEndpoints.cs
Tooba.Promotion.Infrastructure/Directories/MerchandisingCampaignDirectory.cs
Tooba.Promotion.Infrastructure/Merchandising/MerchandisingCampaignAdminComposer.cs
```

## Second finding — one-line file without a discoverable namespace declaration

`Tooba.Promotion.Endpoints/Seller/IPromotionSellerAuthorizer.cs` declared
`using …; namespace Tooba.Promotion.Endpoints.Seller;` on a single line. The namespace was correct, but
the file was reformatted into the repository's canonical multi-line, XML-documented shape so the
path↔namespace invariant is machine-verifiable and the public interface satisfies the mandatory
Persian XML-doc rule (CS1591 is `WarningsAsErrors`). Behavior is unchanged (same interface, same
method signature, same namespace).

## Result

* Production `.cs` scanned: 57 across the five production projects.
* Path↔namespace mismatches remaining: **0**.
* No namespace alias workaround exists anywhere in the module.
