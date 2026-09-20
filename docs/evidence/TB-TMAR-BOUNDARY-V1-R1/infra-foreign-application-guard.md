# Infra → foreign Application freeze — TB-TMAR-BOUNDARY-V1-R1

Status: FROZEN against NEW expansion (legacy edges allowed temporarily).

Baseline: `src/backend/Host/Tooba.Host.Tests/Baselines/tmar-infra-to-foreign-application.json`

Guard: `TmarSourceSizeAndInfraAppTests.Infrastructure_to_foreign_Application_edges_do_not_expand_beyond_baseline`

Exact legacy edges (33):

```
Tooba.AccessControl.Infrastructure -> Tooba.Catalog.Application
Tooba.BulkInquiry.Infrastructure -> Tooba.Catalog.Application
Tooba.Content.Infrastructure -> Tooba.Localization.Application
Tooba.Fulfillment.Infrastructure -> Tooba.Inventory.Application
Tooba.Fulfillment.Infrastructure -> Tooba.Order.Application
Tooba.Fulfillment.Infrastructure -> Tooba.Payment.Application
Tooba.Inventory.Infrastructure -> Tooba.Catalog.Application
Tooba.Inventory.Infrastructure -> Tooba.Offer.Application
Tooba.Notification.Infrastructure -> Tooba.Fulfillment.Application
Tooba.Notification.Infrastructure -> Tooba.Order.Application
Tooba.Notification.Infrastructure -> Tooba.Payment.Application
Tooba.Notification.Infrastructure -> Tooba.Returns.Application
Tooba.Offer.Infrastructure -> Tooba.Catalog.Application
Tooba.Offer.Infrastructure -> Tooba.Party.Application
Tooba.Order.Infrastructure -> Tooba.Catalog.Application
Tooba.Order.Infrastructure -> Tooba.Inventory.Application
Tooba.Order.Infrastructure -> Tooba.Payment.Application
Tooba.Payment.Infrastructure -> Tooba.Wallet.Application
Tooba.Pricing.Infrastructure -> Tooba.Offer.Application
Tooba.ProductQnA.Infrastructure -> Tooba.Catalog.Application
Tooba.Returns.Infrastructure -> Tooba.Fulfillment.Application
Tooba.Returns.Infrastructure -> Tooba.Inventory.Application
Tooba.Returns.Infrastructure -> Tooba.Order.Application
Tooba.Returns.Infrastructure -> Tooba.Payment.Application
Tooba.Returns.Infrastructure -> Tooba.Wallet.Application
Tooba.Reviews.Infrastructure -> Tooba.Catalog.Application
Tooba.Reviews.Infrastructure -> Tooba.Order.Application
Tooba.Settlement.Infrastructure -> Tooba.Order.Application
Tooba.Settlement.Infrastructure -> Tooba.Payment.Application
Tooba.Settlement.Infrastructure -> Tooba.Returns.Application
Tooba.Support.Infrastructure -> Tooba.Notification.Application
Tooba.Wallet.Infrastructure -> Tooba.Notification.Application
Tooba.Wishlist.Infrastructure -> Tooba.Catalog.Application
```

No edges refactored in this repair. Baseline must shrink during Contracts migration. No wildcard suppression.
