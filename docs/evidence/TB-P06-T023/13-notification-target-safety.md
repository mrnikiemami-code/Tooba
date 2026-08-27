# 13 — Notification target safety (TB-P06-T023)

## Allowlist

`NotificationTargetRoutes` (`Notification.Application`):

```text
Allowed prefixes:
  /customer-panel
  /vendor-panel
  /payment
  /checkout
  /cart
```

## Rejected patterns

- `javascript:` / scheme-like `:`
- `//` protocol-relative
- `\`, `<`, `>`, quotes
- Paths outside allowlist (e.g. `/admin/secret`)
- Non-relative / empty routes

Validated at create time (`RequireAllowed`) and domain `Create`.

## Typed helpers

| Helper | Example |
|---|---|
| `CustomerOrder(checkoutId)` | `/customer-panel/orders/{id}` |
| `CustomerPaymentResult(checkoutId)` | `/payment/result?checkoutId=` |
| `SellerOrder(sellerOrderId)` | `/vendor-panel/orders/{id}` |
| `CustomerReturn` / `SellerReturn` | panel returns routes |

## Ownership

Targets encode **own** checkout / seller-order / return ids from integration events after Order bridge resolution — not attacker-supplied ids from notification HTTP payloads.

```text
NOTIFICATION_DEEP_LINKS = LIVE (typed + allowlisted)
```
