# Recovery SoT — TB-TMAR-FND-RESULT-001-R1

## States

```text
Foundation-State: RESULT_PATTERN_FOUNDATION_COMPLETE
Offer-State: COMPLETE_REFERENCE_PATTERN
Module-Recovery-State: RESULT_PATTERN_FOUNDATION_COMPLETE
```

## Golden now includes

- `Result` / `Result<T>` with `SemanticError` (no second error model)
- `ApiResponseFactory.From` / `Created` for success + ProblemDetails failure
- Offer seller CQRS returns Result; endpoints use central mapping
- Exception pipeline only for unexpected / FluentValidation / transitional PlatformHttpException

## Compatibility

Offer seller success JSON remains raw DTO (not `{data,meta}`) for shipped seller client.

## Next

`TB-TMAR-REFBATCH-TP-001` — bounded Tax/Pricing Result Pattern delta only. Do not start next module batch.
