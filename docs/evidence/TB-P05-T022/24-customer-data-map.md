# 24 — Customer Data Map

| UI | Source of truth |
| --- | --- |
| Display name | CustomerProfile / authenticated actor |
| Order reference/status/totals/lines | Checkout/Order Host |
| Payment state labels | Mapped from Host enums (localized) |
| Wishlist products/prices | Wishlist + Offer Pricing |
| Addresses | AddressBook ownership |
| Profile editable fields | CustomerProfile write |
| Email/mobile | Identity-owned read-only |
| Wallet/tickets/gift/notifications/settings | No backend — honest unavailable UI only |

Forbidden: frontend identity authority, fake balances, fake tickets, fake shipment tracking.
