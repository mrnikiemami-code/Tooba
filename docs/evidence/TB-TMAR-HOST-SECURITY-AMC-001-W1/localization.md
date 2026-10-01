# localization — TB-TMAR-HOST-SECURITY-AMC-001-W1

| Code | Descriptor owner | HttpStatus | EN resource | FA resource |
| --- | --- | --- | --- | --- |
| seller.actor.missing | OrderErrorCatalogContributor | 401 | OrderErrors.resx | OrderErrors.fa.resx |
| seller.identity.missing | OrderErrorCatalogContributor | 400 | OrderErrors.resx | OrderErrors.fa.resx |
| seller.authorization.unavailable | OrderErrorCatalogContributor | 503 | OrderErrors.resx | OrderErrors.fa.resx |
| seller.authorization.denied | FoundationErrorCatalogContributor (unique) | 403 | Order seller.* set resolves via LocalizationKey; Foundation owns descriptor | OrderErrors.fa.resx key present |

No new codes/descriptors created. SellerSecurityErrorCodes strings unchanged.
