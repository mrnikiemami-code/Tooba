# Current Result Pattern Gap (pre-mutation)

## Offer seller success shapes (observed)

| Route | Status | Body today |
| --- | --- | --- |
| `GET /v1/seller/offers` | 200 | Raw JSON array of `SellerOfferListItem` |
| `POST /v1/seller/offers` | 201 | Raw JSON `SellerOfferDetailPage` (no Location header) |
| `GET /v1/seller/offers/{id}` | 200 | Raw JSON `SellerOfferDetailPage` |
| `PATCH /v1/seller/offers/{id}` | 200 | Raw JSON `SellerOfferDetailPage` |
| `POST|PUT .../price` | 200 | After Pricing write: raw `SellerOfferDetailPage` via `GetOfferQuery` |
| `POST|PUT .../inventory` | 200 | After Inventory write: raw `SellerOfferDetailPage` via `GetOfferQuery` |

Endpoint source: `OfferSellerEndpoints` uses `Results.Json(...)` exclusively — no `ApiResponseFactory`.

## Offer seller error shapes (observed)

Expected business failures throw `SemanticException` from Application/Domain/ReturnPolicy/owner gateways.
Global `IExceptionHandler` → `IExceptionPresentationService` → `ApiResponseFactory.CreateProblemDetails` → ProblemDetails with:

- `status`, `title` (localized), `errorCode`, `traceId`, `correlationId`
- optional `errors` for FluentValidation

No endpoint-local catch/ProblemDetails on Offer seller routes.

## Consumers

### Shipped seller frontend (`src/frontend/app/vendor-panel/seller-api.ts`)

- Success: parses **root DTO / array** (`mapSellerOfferDetail(await readJson(response))`).
- Failure: reads `body?.errorCode` from ProblemDetails-like JSON.
- **Breaking risk:** wrapping success in `{ data, meta }` would break `mapSellerOfferDetail` / list mappers.

### Host / seed callers (MediatR direct)

- `StorefrontDemoCatalogBootstrap`, `ProductWorkspaceDevelopmentBootstrap`, `AccessControlDevelopmentSeed`, `CatalogAttributeSchemaDevelopmentBootstrap`
- `ReviewEndpoints` uses `ListSellerOffersQuery` and enumerates list items
- Host test `OfferTestSender` adapts Create/Activate for fixtures

### Tests asserting shapes

- Offer handler tests assert thrown `SemanticException` codes (not HTTP)
- Architecture guards currently **require** `Results.Json(await sender.Send(...))` and **forbid** `ApiResponseFactory` in endpoints (exception-pipeline era)

## Current foundation pieces

| Piece | State |
| --- | --- |
| `SemanticError` / `SemanticException` | Present in BuildingBlocks (`TmarFoundation.cs`) |
| `ErrorDescriptor` + catalog | Present; status from explicit descriptors |
| `SafeErrorMapper` | Exception → `MappedSafeError` only |
| `ApiResponseFactory` | Exception/ProblemDetails only; injects `IHttpContextAccessor` via ctor (not static locator) |
| `ValidationBehavior` | Throws `ValidationException` (exception pipeline) |
| `TracingBehavior` / `LoggingBehavior` | Treat thrown SemanticException as expected; Result not considered |
| Shared `Result` / `Result<T>` | **Absent** |

## Pricing / Inventory seller write gateways

```text
ISellerOfferPricingGateway.SetPriceAsync(SetSellerOfferPrice) → Task
ISellerOfferInventoryGateway.SetInventoryAsync(SetSellerOfferInventory) → Task
```

Implementations throw `SemanticException` for expected failures (invalid amount/qty, offer not found / seller mismatch). Offer alias routes await then `GetOfferQuery` + `Results.Json`.

## Gap summary

1. No shared Result model for expected business outcomes
2. ApiResponseFactory cannot map success Result or Result failure without rethrow
3. Offer seller CQRS returns raw DTOs and throws SemanticException for expected paths
4. Endpoints still construct JSON locally
5. Success envelope `{data,meta}` would break shipped seller client — **must preserve raw DTO success JSON**
