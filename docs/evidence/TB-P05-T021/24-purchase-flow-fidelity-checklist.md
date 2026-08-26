# 24 — Purchase Flow Fidelity Checklist

| Item | Grade | Notes |
| --- | --- | --- |
| Cart shell (hero, breadcrumb, density) | MATCH | Blue accent vs red = MINOR TECHNICAL DEVIATION |
| Cart item row | MATCH | OfferId hidden from customer UI |
| Quantity / remove | MATCH | Live Host mutate |
| Sticky summary | MATCH | `lg:sticky lg:top-24` |
| Coupon area | MATCH | Honest unavailable (no fake accept) |
| Cart shipping methods | MATCH | Honest deferral; no fake carriers |
| Empty cart | MATCH | Hero + empty body |
| Step indicator | MATCH | Cart → Shipping → Payment |
| Shipping / address | MATCH | New + saved AddressBook path |
| Guest path | MATCH | No forced auth |
| Payment panel | MATCH | Honest pending/Host handoff |
| Confirmation | MATCH | Shopeiva success-card geometry; Paid from Host |
| Error states | MATCH | Customer messages; tax/coupon honesty |
| RTL | MATCH | |
| Mobile 390×844 | MATCH | Stacked; CTA reachable |
| Spacing / density | MATCH | |
| Home/PDP/Listing regression | MATCH | `test:critical-storefront` green |

**PASS:** no unresolved material visual deviation on purchase flow.
