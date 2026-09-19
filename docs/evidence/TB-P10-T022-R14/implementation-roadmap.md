# Implementation Roadmap (do not execute now)

## Phase 1 — Core schema/domain foundation

- MerchandisingCampaign + CampaignOffer + Type + translations
- Domain invariants + migrations
- **Not:** Builder UI, storefront, seed

Proof: migrate clean; unit tests on window/membership rules.

## Phase 2 — Query/resolver + seed/test data

- Active Amazing query + Host gateway
- Idempotent seed per test-data-plan
- **Not:** full Admin UX polish

Proof: API/composer returns expected filtered ordered set.

## Phase 3 — Admin management minimal UX

- CRUD campaign + membership picker (AppDataGrid Offers)
- **Not:** Digikala-parity browse site

Proof: operator can publish active campaign without raw SQL.

## Phase 4 — Builder Product Source integration

- Add `PromotionCampaign` source + labels
- Null CampaignId = active resolve
- **Not:** redesign section wizard

Proof: Admin Appearance/Review + save/publish config round-trip.

## Phase 5 — Published Storefront + SSR

- Renderer uses resolved items; timer/badge derived
- Cache invalidation
- **Not:** new carousel library

Proof: published Landing/Home rail; critical-storefront if shared components touched.

## Phase 6 — Hardening

- Overlap policy, performance indexes as needed, teasing/early-access if scheduled
- Visual verification

## What NOT to do across phases

P11 work; Offer.IsAmazing; duplicate Pricing/Inventory; checkout Promotion overload; schema in audit; overwrite user fixes.
