# 05 — Customer panel completion (TB-P06-T018)

## Selected Customer gaps closed

### 1) Navigation honesty

- Primary nav shows only live Host-backed routes: dashboard, orders, wishlist, addresses, profile, settings.
- Hidden from primary nav (deep-link capability shells remain):
  - `/customer-panel/wallet`
  - `/customer-panel/tickets`
  - `/customer-panel/gift-cards`
  - `/customer-panel/notifications`
- Evidence constant: `CUSTOMER_DEFERRED_NAV_HREFS` in `customer-panel-shell.tsx`.
- Nav test surface: `data-testid="customer-panel-nav-live-only"`.

### 2) Settings — live subset + honest unavailable

| Section | Behavior |
|---|---|
| Profile / account identity | Live bridge to `/customer-panel/profile` (`/v1/customer/profile` read/save) |
| Locale preference | Cookie-based locale preference (no fake notification/security persistence) |
| Security prefs | Honestly unavailable — no fake password/session/2FA save |
| Notification prefs | Honestly unavailable — no fake channel preference save |

### 3) Dashboard quick actions

- Only live routes remain as actionable tiles.
- Settings marked live.
- Wallet quick action removed (not advertised as “به‌زودی” on the dashboard).

## Explicit non-claims

- No customer wallet ledger.
- No tickets inbox.
- No notification unread counts.
- No gift-card balance.
- No fake settings mutation for unavailable sections.

## Shopeiva binding

- Shell geometry/header/sidebar/mobile drawer remain Shopeiva Account-derived (Tooba blue accent retained).
- Capability shells for deferred deep-links stay honest unavailable pages (no static demo balances).
