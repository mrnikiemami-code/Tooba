# PlatformHttpException containment — TB-TMAR-FND-OBSERR-001-R4

Repo-wide production use remains outside Offer Domain and Application. The Offer architecture guard fails if those projects mention `PlatformHttpException`.

| Class | Meaning in this tree | R4 action |
| --- | --- | --- |
| FOUNDATION_TRANSITIONAL | `PlatformHttpException` type and `SafeErrorMapper.MapPlatform` accept it as compatibility input | Kept |
| LEGACY_HOST | Host composers/endpoints still throw or catch it (catalog admin, wallet, seller panel access, grids, orders) | Not migrated |
| LEGACY_MODULE_ENDPOINT | Catalog landing/domain and Media infrastructure still throw it | Not migrated |
| BLOCKS_OFFER_GOLDEN | Would be Offer Domain/Application or new seller-endpoint usage | None found |
| NEW_VIOLATION | New Golden Offer or foundation heuristic use | None found |

Offer seller endpoints do not catch it. No unrelated module was migrated.

Verdict: PASS for Offer Golden. Legacy remains explicitly out of R4 scope.
