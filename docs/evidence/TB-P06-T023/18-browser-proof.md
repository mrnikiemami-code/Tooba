# 18 — Browser proof (TB-P06-T023)

## Surfaces captured

| Surface | Route | Artifact |
|---|---|---|
| Customer list / unread / read | `/customer-panel/notifications` | `captures/01-customer-notifications.png` |
| Seller list / unread | `/vendor-panel/notifications` | `captures/02-seller-notifications.png` |
| Manifest | — | `browser-proof.json` (`at: 2026-08-27T11:34:03Z`) |

## Capture status

```text
captures/01-customer-notifications.png — customer inbox with live Host rows
captures/02-seller-notifications.png — seller inbox with live Host rows
notes: Shopeiva-locked inbox; no fake seed
```

UI is live in repo:

- Customer + vendor pages render `NotificationInbox`
- Nav Bell entries live in both shells
- Rows shown in captures came from sandbox commerce events (see `17-notification-e2e.md`)

## Header badge

Storefront header dropdown **not** wired with fake unread (see source map) — deferred / unbound preferred over mock counts.

## Honesty

Do **not** claim `USER_VISUAL_ACCEPTED`. Screenshots are Worker proof only.
