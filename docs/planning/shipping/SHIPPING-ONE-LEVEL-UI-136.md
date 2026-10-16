# SHIPPING ONE-LEVEL UI — TB-SHIPPING-ONE-LEVEL-TABS-136

Runtime acceptance of the TB-135 per-cart-line shipping method selection on a real same-seller,
two-line cart, in a disposable Development environment.

## 1. Why this task existed

TB-135 delivered per-cart-line method selection at source level but could not prove it at runtime:
`WorkspaceDemoMarketplaceSeed` provisions exactly one active offer per seller
(`ARM-LN-01` for Seller A, `DGS-LN-01` for Seller B) on the single live product
`workspace-live-shirt`. A cart therefore could never contain **two** lines of the **same** seller,
which is exactly the case that TB-131/TB-135 were meant to fix (order M1 planner invariant
`DuplicateSellerMethod`). This task closes that fixture gap with an app-owned, Development-only
fixture and observes the real customer journey.

## 2. Fixture extension (smallest additive Development-only change)

`offer.offers` has a unique index on `(SellerPartyId, CatalogVariantId, Channel)` where
`status <> 'Archived'`, so a second offer for the same seller on the same variant is impossible.
The fixture therefore adds **one extra live product with its own variant and its own offer from the
SAME existing seller**:

| Piece | Value |
| --- | --- |
| Product slug seam | `workspace-live-scarf` (`WorkspaceDemoProductSeed.CompanionSlug`) |
| Variant code seam | `LIVE-SCARF-NVY` |
| Seller A SKU | `ARM-SC-01` (`WorkspaceDemoMarketplaceSeed.SellerACompanionSku`) |
| Price | `1,450,000` IRR, market `IR`, channel `Marketplace` (Pricing-owned) |
| Stock | `6` at `WH-THR` (Inventory-owned) |
| Tax | reused `standard` category + `IR-NAT` 9% rule (Tax-owned) |
| Seller | reused Seller A (`فروشگاه آرمان`) identity — no new party |

Reuse of existing Catalog seams: same brand (`BrandSlugSeam`), the live product's own leaf category,
and the existing `color`/`black` attribute axis via `CatalogDevelopmentAttributeReuse`.

Reachability and safety:

- invoked only from `WorkspaceDemoSeed.SeedNewProductAsync` — the same create path that already
  seeds the marketplace demo — behind the **existing** `IHostEnvironment.IsDevelopment()` gate;
- `RefreshExistingCopyAsync` (the path taken when the demo product already exists) is untouched, so
  an already-seeded or shared Development database is never mutated;
- no production seed, no manual SQL, no DB bypass, no global seed reset, no user-facing fixture.

## 3. Proven runtime behaviour (isolated scratch environment)

Local real seller `فروشگاه آرمان` (`01a14369-706a-7000-abbc-6346a9cfe571`), two active offers
(`ARM-LN-01`, `ARM-SC-01`), two distinct cart lines, and the customer chose **two different
authoritative methods** for the two lines of that one seller:

| Cart line | Item | Chosen method | Group fee |
| --- | --- | --- | --- |
| `01a1436e-ff9a-…470c7` | پیراهن مردانه لینن | `post:express` (پیشتاز) | 200,000 IRR |
| `01a1436f-295e-…316a4` | شال مردانه لینن | `tipax:standard` (معمولی) | 180,000 IRR |

Persisted (isolated DB read-back): **2 groups, 1 distinct seller, 2 distinct methods, 2 disjoint
cart line ids, fee sum 380,000** — identical to the shipping card fee and to the payment projection
(`هزینه ارسال: ۳۸۰٬۰۰۰ ریال`, `قابل پرداخت: ۳٬۹۷۷٬۰۰۰ ریال`). Delivery date and window are the
server-derived shared intersection (earliest day `2026-10-20`, window `9-12`). One click saved
exactly one draft; no checkout submit, payment start, capture, reservation or cart clear occurred.
A single cold reload restored address, both per-line methods, date and window deterministically
(keyed by persisted `CartLineIds`, no guessed substitution). Desktop and 390×880 RTL mobile
viewports rendered the existing item-level tiles unchanged (no CSS/token/layout edits).

## 4. Residual notes (not repaired in scope)

1. `/fa/payment` renders the label `روش ارسال:` with an empty value for a multi-group draft
   (the scalar legacy shorthand is blank); the aggregate shipping charge and grand total are correct.
   Display-only observation, reported for Architect triage.
2. Settlement `PendingAllocation` and payout origin/eligibility policy blockers are unchanged and
   were not exercised. No end-to-end payment or production acceptance is claimed.

## 5. Evidence

See `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-136/` (`observation-136.txt`,
`api-prereq-136.txt`, `host-launch-136.txt`, `isolation-guard.json`, `cleanup-136.txt`, screenshots).
