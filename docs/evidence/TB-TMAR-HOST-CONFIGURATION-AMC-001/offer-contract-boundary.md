# offer-contract-boundary — TB-TMAR-HOST-CONFIGURATION-AMC-001

## Dependency

`using Tooba.Offer.Contracts.Dtos;` → `Enum.TryParse<SalesChannel>(...)` in `ValidateSalesChannel`.

## Classification

| Question | Answer |
| --- | --- |
| Why | Stable sales-channel value enum for Production validation |
| Contracts-only? | YES (`Offer.Contracts.Dtos`) |
| Application/Domain/Infra Offer? | ZERO |
| One-way? | YES (Host → Offer.Contracts) |
| Makes Configuration coupled to business module? | **YES** for a shared vocabulary enum |
| Blocker for Configuration CERT? | **NO** if Architect accepts Contracts-only enum reuse; document as debt |
| Prefer neutral owner later? | BuildingBlocks or StoreContext Contracts could own sales-channel vocabulary — **out of Analyze / not forced in W1** |

**Offer-Contract-Boundary-State:** `CONTRACTS_ONLY_SALESCHANNEL_ENUM_COUPLING_ACCEPTED_WITH_DEBT`

Do not move Offer types in Analyze.
