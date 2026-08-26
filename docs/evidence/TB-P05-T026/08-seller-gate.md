# 08 — Seller gate (TB-P05-T026)

Live shell: `http://127.0.0.1:3000/vendor-panel`

Prior accepted: TB-P05-T001 (multi-seller isolation) · TB-P05-T023 (seller panel fidelity) · T025 seller smoke PASS.

| Surface | Result | Notes |
|---|---|---|
| Shell | **PASS** | Shopeiva-adjacent vendor layout; Tooba accent preserved |
| Dashboard | **PASS** | Live seller dashboard composition |
| Products / Offers | **PASS** | Offer DataGrid (Product ≠ Offer preserved) |
| Pricing / Inventory | **PASS** | Via offer/workspace composition — backend-owned amounts/units |
| Orders | **PASS** | Seller-scoped orders |
| Profile | **PASS** | Seller profile surface |
| Seller isolation | **PASS** | Actor ≠ SellerPartyId; `X-Tooba-Seller-Party-Id` is context, not authority (T001) |

## Honesty

| Risk | Gate treatment |
|---|---|
| Fake revenue / settlement | Not shown as live money movement |
| Fake analytics | Honestly unavailable where no backend |
| Cross-seller access | Denied by Host authz / party filter |

**Seller gate: PASS**
