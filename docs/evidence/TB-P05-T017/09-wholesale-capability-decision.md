# 09 — Wholesale Capability Decision

Task: `TB-P05-T017`

## Source intent

Shopeiva `bulkOrderTab.jsx` is a **bulk purchase request form** with client-side discount math and localStorage orders — not a real B2B pricing engine.

## Decision

Own a minimal **BulkInquiry** module (`schema: bulk_inquiry`):

- Persist inquiry contact + quantity + address against published ProductId
- Guest submit allowed (storefront form)
- **No** unit price, discount %, or total amount stored or displayed as authority
- UI states: request form + success with inquiry id only

## Non-goals

- Full B2B organization accounts
- Tiered Offer pricing
- Client-authority discounts

Status: **LIVE** as inquiry/request seam.
