# TB-P04-GATE — Deferred / P05+ Concerns

Non-blocking. Do not implement in this Gate.

## Carried from P04

- Cart replace-hold release→reserve competition window (T008)
- Guest BuyerPartyId improvement
- Customer persistent Address / Party seam
- Missing `IdempotencyKey` on payment initiate currently surfaces as 500 NRE instead of customer 400 (frontend always sends key; harden later)

## Explicit P05+ / later

- Real production PSP adapter(s)
- Refund / capture / void
- Seller settlement / payout
- Fulfillment / Shipment
- Returns / RMA
- Grid virtualization
- Advanced multilingual / LTR UI
- Advanced multi-currency UX
- Theme configurator
- Advanced Search
- Media binary / CDN pipeline

## Historical evidence note

T007/T008 files labeled `*mobile*` that are `1920x1080` predate the strict CDP `390×844` clip rule. Canonical true-mobile evidence for Gate recovery is T009 `repair-2` and T010 `05-payment-success-result-mobile-390x844.png` (verified `390×844`).
