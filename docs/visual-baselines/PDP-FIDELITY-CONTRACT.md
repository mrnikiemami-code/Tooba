# PDP Fidelity Contract

Status: ACTIVE baseline for TB-P05-T019  
Locked UI: purchased Shopeiva PDP with structurally distinct tabs  
Live data: Tooba Host product detail composition

## Canonical source components

- `src/frontend/app/storefront/storefront-pdp.tsx`
- `storefront-pdp-reviews.tsx`, `storefront-pdp-qa.tsx`, `storefront-pdp-bulk.tsx`
- Accepted visual evidence: `docs/evidence/TB-P05-T017/` (+ R1 sticky-tab evidence)

## Accepted screenshots

| Surface | Path |
| --- | --- |
| T017 / R1 PDP evidence | `docs/evidence/TB-P05-T017/` |
| T019 desktop baseline | `docs/evidence/TB-P05-T019/08-pdp-desktop-baseline.png` |
| T019 tabs baseline | `docs/evidence/TB-P05-T019/09-pdp-tabs-baseline.png` |
| T019 mobile baseline | `docs/evidence/TB-P05-T019/10-pdp-mobile-baseline.png` |

## Protected structure

1. Top purchase block (gallery, variants, offer truth, wishlist/rating)
2. Sticky tab strip (`pdp-sticky-tabs`)
3. Distinct tabs: intro / full / specs / reviews / Q&A / wholesale (`pdp-tab-*`)
4. Distinct bodies: `pdp-intro`, `pdp-full`, `pdp-specs`, `pdp-reviews`, `pdp-qa`, `pdp-bulk`
5. Other sellers (`pdp-other-sellers`) when data supports
6. Related products (`pdp-related`) when data supports

## Critical geometry / patterns

- Sticky tab strip remains sticky under scroll
- Each tab body is structurally distinct (not one generic card for all)
- Offer amount / stock truth from composed Offer+Pricing+Inventory
- RTL and mobile overflow for tab strip

## Forbidden deviations

- Generic `TabContent` flattening / one generic card for all tabs
- Removing a tab because backend seam is empty (honest empty state instead)
- Fake Q&A / wholesale / ratings
- `Product.Price` or `Product.Stock` as UI authority

## Allowed minimal technical deviations

- Empty states when live lists are empty
- Non-visual `data-testid` hooks for regression guards

## Backend binding expectations

- Product identity from Catalog
- Purchase amount from Pricing via Offer
- Availability from Inventory
- Reviews/Q&A/Bulk modules remain dedicated owners
