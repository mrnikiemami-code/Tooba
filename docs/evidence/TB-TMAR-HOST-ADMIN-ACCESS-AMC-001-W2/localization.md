# localization — TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W2

## Order

`OrderErrorResourceSet` + `OrderErrors.resx` / `OrderErrors.fa.resx`

- `order.authorization.unavailable` — EN+FA (preserved)
- `order.operation.denied` — EN+FA (added W2)

## Support

`SupportErrorResourceSet` + `SupportErrors.resx` / `SupportErrors.fa.resx` registered from `SupportEndpointModule`

- Owns `support.*`
- `support.authorization.unavailable` — EN+FA
- Does NOT own/duplicate `admin.authorization.denied`

## Wallet

`WalletErrorResourceSet` + `WalletErrors.resx` / `WalletErrors.fa.resx` registered from `WalletEndpointModule`

- Owns `wallet.*`
- `wallet.authorization.unavailable` — EN+FA
- Does NOT own/duplicate `admin.authorization.denied`

## Foundation admin.*

Preserved from W1 (EN+FA for five admin.* keys).
