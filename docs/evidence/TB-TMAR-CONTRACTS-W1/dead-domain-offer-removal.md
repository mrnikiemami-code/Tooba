# Dead Domain → Offer.Domain removal — TB-TMAR-CONTRACTS-W1

## Re-verification

BOUNDARY-V1 claimed Cart/Order/Pricing.Domain → Offer.Domain were unused type symbols. Re-check found **SalesChannel enum WAS used** (via `using Tooba.Offer.Domain`). Guid OfferId fields are local; SalesChannel was the live coupling.

## Actions

1. Extracted `SalesChannel` into `Tooba.Offer.Contracts` (public contract enum; values unchanged).
2. Removed `SalesChannel` definition from `Offer.Domain`; added `TypeForwardedTo` for binary/source compatibility of existing Offer.Domain consumers.
3. Replaced Cart/Order/Pricing.Domain → Offer.Domain ProjectReferences with → Offer.Contracts.
4. Restored `using Tooba.Offer.Domain;` in those Domain files (enum namespace retained for contract stability).

## Baseline shrink

`tmar-domain-to-foreign-domain.json` edges: 3 → **0** (empty).

No replacement foreign Domain dependency introduced.
