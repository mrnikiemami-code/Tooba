# TB-TMAR-BOUNDARY-V1 Order.Application Hub Analysis

## Foreign Application ProjectReferences
| Dependency | Why it exists (from contracts/directories usage pattern) | Class |
|---|---|---|
| Cart.Application | convert cart → order / read cart lines | sync read + orchestration |
| Offer.Application | resolve offer commercial facts at checkout | sync read / validation |
| Pricing.Application | quote line prices | sync read |
| Inventory.Application | reservation / availability at commit | sync write/orchestration + transaction participant |
| Tax.Application | tax calculation | sync read |
| Promotion.Application | campaign/price adjustments | sync read / validation |

No ProjectReference to Payment.Application or Fulfillment.Application or Party.Application in Order.Application.csproj (Payment/Fulfillment composition remains Host-side or other layers).

## Verdict
**Both**: legitimate checkout process orchestrator **and** over-coupled sync hub.

Order owns the purchase consistency boundary today (cart→priced→taxed→reserved→ordered). That justifies temporary sync orchestration inside a Modular Monolith. It does **not** justify long-term App→App project references once Contracts exist.

## Migration recommendation (ordered)
1. Extract `Order.Contracts` + consumer Contracts for Offer/Pricing/Inventory/Tax/Promotion/Cart **read/write ports** used by checkout
2. Replace ProjectReferences with Contracts interfaces; Infrastructure adapters remain in-process
3. Keep sync checkout transaction locally (no distributed saga yet) unless multi-DB commit failure evidence requires outbox saga
4. Publish integration events for post-commit (payment capture, fulfillment) — already partially via Outbox patterns elsewhere
5. Do **not** invent distributed saga in this program phase without proven cross-DB atomicity requirement

## Freeze
Edges already listed exactly in `tmar-app-to-app-edges.json` (must only shrink).
