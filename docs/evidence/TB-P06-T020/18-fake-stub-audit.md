# 18 — Fake / stub audit (TB-P06-T020)

Date: 2026-08-27  
Rule: no fake coupon / review / notification data or actions.

## Coupons / promotions

| Risk | Audit result |
|---|---|
| Fake coupon rows | **PASS** — list from `GET /v1/seller/promotions` / admin list |
| Fake usage / maxUses charts | **PASS** — Shopeiva mock progress bars **not** ported |
| Fake discount at cart | **PASS** — UI shows Host `discountAmount` only; composer passes real `CouponCode` |
| Fake activate/deactivate | **PASS** — Host POST activate/deactivate |
| Client-side Product.Price mutation | **PASS** — none; Pricing unchanged |

## Reviews

| Risk | Audit result |
|---|---|
| Fake review counts | **PASS** — Host `PublishedCount` / `PendingCount` / `RejectedCount` |
| Fake seller approve/reject/delete | **PASS** — actions omitted; admin owns moderation |
| Fake seller reply | **PASS** — `SellerResponseSupported: false` + honest UI note |
| Seeded stub list in UI | **PASS** — empty/loading/denied real states |

## Notifications

| Risk | Audit result |
|---|---|
| Fake notification rows | **PASS** — inbox not implemented |
| Fake unread badge | **PASS** — no Bell / unread chrome added |
| Fake push / realtime | **PASS** — not wired |
| Fake preference save | **PASS** — customer settings prefs remain honest unavailable |

## Nav / shells

| Risk | Audit result |
|---|---|
| Live nav for deferred domains | **PASS** — notifications/tickets stay deferred/hidden |
| Capability shells pretending live | **PASS** — coupons/reviews replaced with live UIs; notifications remain deferred shell |

## Verdict

No fake coupon, review, or notification data/actions introduced in Wave 2.
