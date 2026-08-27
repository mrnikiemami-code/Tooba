# 07 — Runtime order detail scope

Task: TB-P06-T024-R1

## Implementation

**File:** `SellerPanelComposer.GetOrderAsync`

1. Resolve `order.view` scope (same as list).
2. Scope denied → `403 seller.order.view.denied`.
3. Load order by `sellerOrderId` + `sellerPartyId` (tenant boundary).
4. Filter lines to authorized categories only.
5. **If zero authorized lines** → `403 seller.order.view.denied` (Books-only direct URL case).
6. Detail DTO built from **authorized lines only** — subtotal, tax, discount, grand total, line items.

## Scenarios

| Case | Expected | Proven by |
|------|----------|-----------|
| Mobile order detail | ALLOW — full authorized lines | Runtime scope test (mobile order in visible set) |
| Books-only detail | DENY — 403 after line filter | Runtime scope test — books order not in visible; detail path throws when `authorizedLines.Count == 0` |
| Guessed foreign ID | 404 if wrong seller; 403 if no authorized lines | Composer checks `SellerPartyId` + scope |
| Mixed order | Mobile lines only; Books lines never in DTO | Test asserts single authorized mixed line = Mobile |

## Leakage prevention

- Unauthorized line titles, amounts, and counts are omitted from response.
- Checkout recipient fields still shown when at least one authorized line exists (whole-order header metadata — acceptable for partial-scope view; monetary totals scoped to authorized lines).

## API surface

Seller panel order detail endpoint delegates to `GetOrderAsync` — enforcement is server-side.
