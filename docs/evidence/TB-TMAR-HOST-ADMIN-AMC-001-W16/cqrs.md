# CQRS — W16 Publish Readiness

| Item | State |
|---|---|
| Query | `GetProductPublishReadinessQuery(ProductId, Locale)` |
| Handler | `GetProductPublishReadinessHandler` → `Result<ProductPublishReadinessView>` |
| Dispatch | Endpoints `ISender.Send` |
| Port | `IProductPublishReadinessReader.GetAsync` |
| Commands | None (W16 READ ONLY; lifecycle mutations stay Host) |
| MediatR | Catalog Application assembly via existing foundation |
