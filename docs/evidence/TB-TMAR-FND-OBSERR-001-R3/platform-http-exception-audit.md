# PlatformHttpException audit — TB-TMAR-FND-OBSERR-001-R3

## Classification

| Area | Finding |
| --- | --- |
| SafeErrorMapper | Maps as transitional legacy input; status from exception; catalog used when code registered |
| Offer Domain/Application | No `PlatformHttpException` throws (architecture guard) |
| Offer Endpoints | No local catch; bubbles to global presentation |
| Host / Catalog / Media / other modules | Broad existing usage — **LEGACY_OTHER_MODULE** / **FUTURE_MIGRATION** (not migrated in R3) |

## Rule

- Keep compatibility
- Do not introduce new Offer Domain/Application throws
- Preferred path remains `SemanticError` / typed results
