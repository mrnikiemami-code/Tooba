# AntiPattern-Gate

AntiPattern-Gate: **CLEAN**

R6 diff reviewed for:

- no god one-interface-per-Host-file (single `IOfferQueryGateway`)
- no Task.Run / magic sleeps
- storefront card path batched (no per-product Offer EF loop)
- merch/grid batch dictionaries preserved
- no catch-ignore / broad suppression
- Host `OfferStatus` global alias moved Domain → Contracts (boundary hygiene)
