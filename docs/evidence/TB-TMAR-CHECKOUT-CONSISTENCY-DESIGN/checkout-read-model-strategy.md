# Checkout read-model strategy

| Decision data | Current | Future strategy |
|---|---|---|
| Cart state | SYNC Cart gateway | KEEP_SYNC_IN_PROCESS_UNTIL_EXTRACTION → CONTRACT_SYNC_QUERY then LOCAL_PROJECTION optional |
| Offer validity | Offer.Contracts lookup | CONTRACT_SYNC_QUERY |
| Price quote | Pricing.Contracts | CONTRACT_SYNC_QUERY; later EVENT_MAINTAINED_READ_MODEL for catalog prices |
| Tax | Tax.Contracts | CONTRACT_SYNC_QUERY |
| Promotion | Promotion.Application evaluate | replace with Contracts; KEEP_SYNC until extraction |
| Inventory availability | Inventory gateway batch | CONTRACT_SYNC_QUERY / LOCAL_PROJECTION of availability |
| Seller facts | via offer/cart | CONTRACT_SYNC_QUERY |
| Abuse settings | Host gate | LOCAL_PROJECTION / Order-owned settings |

Goal: avoid naive 6+ network round-trips; prefer projections for availability/price where freshness SLA allows; keep reserve as sync command.
