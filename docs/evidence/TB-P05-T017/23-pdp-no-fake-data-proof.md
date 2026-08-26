# 23 — PDP No Fake Data Proof

Task: `TB-P05-T017`

| Risk | Control |
| --- | --- |
| Shopeiva `sampleQA` | Not imported; public list is Published ProductQnA only |
| Bulk client discount math | Not ported; form submits inquiry id only |
| Product.Price / Product.Stock | Absent; Offer/Pricing/Inventory remain authority |
| Fake review stars | Reviews aggregate only when Published count > 0 |
| Fabricated Shopeiva PNG runtime | Slots 02–08 are source/runtime absence proofs, not mocked screenshots |
| Generic single-tab renderer | Six distinct tab bodies in `storefront-pdp.tsx` |

Development seed for ProductQnA is idempotent Published Q&A on `demo-mobile-1` only inside Development bootstrap after Storefront demo catalog.
