# error-localization — TB-TMAR-HOST-ADMIN-ACCESS-AMC-001-W3-CERT

## Ownership

- FoundationErrorResourceSet: validation.* / platform.* / admin.*
- OrderErrorResourceSet: order.* (incl. authorization.unavailable + operation.denied)
- SupportErrorResourceSet: support.* (registered once)
- WalletErrorResourceSet: wallet.* (registered once)

## Access-path uniqueness (Foundation+Order+Support+Wallet)

All Access-path codes register exactly once with EN+FA resources resolving.

## Note

Full-repo `ErrorCatalogUniqueCodeGuardTests` still reports pre-existing Order/Catalog duplicate for `reservation.policy.*` — outside Access path; not introduced by Access AMC; not repaired in CERT (would be separate production task).
