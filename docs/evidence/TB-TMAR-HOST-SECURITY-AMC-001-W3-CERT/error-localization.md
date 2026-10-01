# error-localization — TB-TMAR-HOST-SECURITY-AMC-001-W3-CERT

## Seller path codes (Security)

| Code | HTTP | Descriptor owner | EN/FA owner |
| --- | --- | --- | --- |
| seller.actor.missing | 401 | OrderErrorCatalogContributor (once) | OrderErrors.resx / .fa.resx |
| seller.identity.missing | 400 | Order (once) | Order |
| seller.authorization.unavailable | 503 | Order (once) | Order |
| seller.authorization.denied | 403 | FoundationErrorCatalogContributor (once) | Order resource set (seller.* Owns) |

## Checkout

| Code | HTTP | Owner |
| --- | --- | --- |
| checkout.authentication_required | 401 | Foundation (canonical) |

## Notes

- Security-path codes unique in composed Foundation+Order catalogs for this Cert.
- Unrelated `reservation.policy.*` historical catalog duplicates (if any) = OUTSIDE_SECURITY_CERT_SCOPE; not repaired.
- Runtime titles under Security: ZERO hard-coded; presentation via SemanticError codes + localizer.
