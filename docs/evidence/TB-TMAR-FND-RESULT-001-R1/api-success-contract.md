# API Success Contract — TB-TMAR-FND-RESULT-001-R1

## Decision

**Preserve raw DTO / array JSON on success** for Offer seller HTTP routes.

Do **not** wrap success payloads in `{ "data": ..., "meta": ... }` for these routes.

## Evidence

Shipped seller client `src/frontend/app/vendor-panel/seller-api.ts`:

- `mapSellerOfferDetail(await readJson(response))` expects root object fields (`offerId`, …)
- list mapper expects a root JSON array
- failures already read `body.errorCode` from ProblemDetails

Introducing a `data` envelope would break create/get/patch/price/inventory success parsing without a frontend change (frontend is frozen in TMAR backend-only mode).

## Canonical mapping (ApiResponseFactory)

| Input | HTTP | Body |
| --- | --- | --- |
| `Result<T>` success | 200 | raw `T` JSON |
| `Result` success | 204 | empty |
| `Created(location, Result<T>)` success | 201 + `Location` | raw `T` JSON |
| any Result failure | catalog status | ProblemDetails (`errorCode`, `traceId`, `correlationId`, localized `title`) |

Optional future `{data,meta}` envelope may be introduced for **new** surfaces only after client coordination. Offer Golden retains raw success JSON as the compatibility adapter.

## Create Location

`POST /v1/seller/offers` now sets `Location: /v1/seller/offers/{offerId}` on success (additive; body shape unchanged).
