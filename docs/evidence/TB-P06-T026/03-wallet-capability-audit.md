# 03 — Backend capability audit

Task: TB-P06-T026

| Item | Finding |
|------|---------|
| Customer Wallet module | **None** |
| GiftCard module | **None** |
| Closest ledger | Settlement (seller) — account + immutable entries + derived balance |
| Closest module recipe | Support (Host endpoints + ACC + FE + seed) |
| FE customer wallet/gift-cards | Honest unavailable stubs |
| Vendor wallet | Live Settlement UI (not customer store credit) |
| PermissionCatalog | No wallet.* / giftcard.* yet |

Checkout wallet spend + refund-to-wallet: **defer honestly** unless proven safe in this Task.
