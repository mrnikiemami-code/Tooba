# Request → handler → validator matrix (certify §6a independent re-derivation)

Re-derived from shipped routes (`MapGet`/`MapPost` call sites) and actual `ISender.Send` call sites,
**not** adopted from the W0/W1/W2 evidence.

## 1. Route inventory

| # | Verb + route | Endpoint file | Dispatched request |
|---|---|---|---|
| 1 | `GET /v1/customer/returns` | `Customer/ReturnCustomerEndpoints.cs` | `ListCustomerReturnsQuery` |
| 2 | `GET /v1/customer/returns/{returnRequestId:guid}` | `Customer/ReturnCustomerEndpoints.cs` | `GetCustomerReturnQuery` |
| 3 | `POST /v1/customer/returns` | `Customer/ReturnCustomerEndpoints.cs` | `CreateReturnCommand` |
| 4 | `GET /v1/seller/returns` | `Seller/ReturnSellerEndpoints.cs` | `ListSellerReturnsQuery` |
| 5 | `GET /v1/seller/returns/{returnRequestId:guid}` | `Seller/ReturnSellerEndpoints.cs` | `GetSellerReturnQuery` |
| 6 | `POST /v1/seller/returns/{returnRequestId:guid}/approve` | `Seller/ReturnSellerEndpoints.cs` | `ApproveReturnCommand` |
| 7 | `POST /v1/seller/returns/{returnRequestId:guid}/reject` | `Seller/ReturnSellerEndpoints.cs` | `RejectReturnCommand` |
| 8 | `GET /v1/admin/returns` | `Admin/ReturnAdminEndpoints.cs` | `ListAdminReturnsQuery` |
| 9 | `POST /v1/admin/returns/query` | `Admin/ReturnAdminEndpoints.cs` | `QueryAdminReturnsGridQuery` |
| 10 | `GET /v1/admin/returns/{returnRequestId:guid}` | `Admin/ReturnAdminEndpoints.cs` | `GetAdminReturnQuery` |
| 11 | `POST /v1/admin/returns/{returnRequestId:guid}/retry-refund` | `Admin/ReturnAdminEndpoints.cs` | `RetryReturnRefundCommand` |

Routes 11 = `ISender.Send` call sites 11 = endpoint-reachable request types 11. **Set equality holds.**

## 2. Per-request provenance matrix

| Request | Route | Transport-derived input (source / trust) | Classification | Validator / executed policy | Stable error code | Discovery evidence |
|---|---|---|---|---|---|---|
| `CreateReturnCommand` | `POST /v1/customer/returns` | body `CreateReturnRequest` (untrusted client), actor from authenticated principal | `VALIDATOR_REQUIRED` | `CreateReturnCommandValidator` (SellerOrderId ≠ empty, IdempotencyKey required/≤128, Reason ≤512, Items non-empty, per-line OrderLineId ≠ empty, per-line Quantity > 0, RefundDestination in enum) | `returns.validation.*` | Returns Application assembly registered in `AddToobaCqrsFoundation` → `AddValidatorsFromAssembly` + `ValidationBehavior<,>` |
| `ApproveReturnCommand` | `POST /v1/seller/returns/{id:guid}/approve` | route `:guid` (constrained), body `ApproveReturnRequest.RefundDestination` (optional, untrusted), actor + sellerPartyId server-derived from authorizer | `VALIDATOR_REQUIRED` | `ApproveReturnCommandValidator` (ReturnRequestId ≠ empty, RefundDestination in enum when supplied) | `returns.validation.*` | same |
| `RejectReturnCommand` | `POST /v1/seller/returns/{id:guid}/reject` | route `:guid` (constrained), body `RejectReturnRequest.Reason` (optional, untrusted), actor + sellerPartyId server-derived | `VALIDATOR_REQUIRED` | `RejectReturnCommandValidator` (ReturnRequestId ≠ empty, Reason ≤512) | `returns.validation.*` | same |
| `QueryAdminReturnsGridQuery` | `POST /v1/admin/returns/query` | body `GridQueryRequest` (untrusted client, deeply shaped) | `VALIDATOR_REQUIRED` | `QueryAdminReturnsGridQueryValidator` (Request non-null) **plus** the module-owned `AdminReturnGridQueryPolicy` executed inside the handler (paging bounds, search length, sortable/filterable allowlists, per-field operator allowlists, advanced connectors) → `GridQueryValidationException` → `SemanticError(ex.ErrorCode)` | `returns.validation.grid_request_required` + grid codes | same |
| `ListCustomerReturnsQuery` | `GET /v1/customer/returns` | no route/body/query input; actor server-derived (`TryResolveActor`) | `NO_VALIDATOR_REQUIRED` | n/a — nothing client-shaped reaches the request | `customer.session.required` (Foundation) when actor absent | non-circular: parameterless request; provenance verified by reading the endpoint |
| `GetCustomerReturnQuery` | `GET /v1/customer/returns/{id:guid}` | route `:guid` constraint; actor server-derived | `NO_VALIDATOR_REQUIRED` | n/a | `customer.session.required` (Foundation) | non-circular: `:guid` rejects non-guid before the handler; actor not client-supplied |
| `ListSellerReturnsQuery` | `GET /v1/seller/returns` | sellerPartyId server-derived from `RequireAuthorizedAsync` | `NO_VALIDATOR_REQUIRED` | n/a | authorizer-owned | non-circular: single server-derived argument |
| `GetSellerReturnQuery` | `GET /v1/seller/returns/{id:guid}` | route `:guid`; sellerPartyId server-derived | `NO_VALIDATOR_REQUIRED` | n/a | authorizer-owned | non-circular |
| `ListAdminReturnsQuery` | `GET /v1/admin/returns` | none (parameterless) | `NO_VALIDATOR_REQUIRED` | n/a | authorizer-owned | non-circular: no transport input |
| `GetAdminReturnQuery` | `GET /v1/admin/returns/{id:guid}` | route `:guid` only | `NO_VALIDATOR_REQUIRED` | n/a | `return.missing` when not found | non-circular: `:guid` constraint; single value |
| `RetryReturnRefundCommand` | `POST /v1/admin/returns/{id:guid}/retry-refund` | route `:guid`; actorUserId server-derived | `NO_VALIDATOR_REQUIRED` | n/a | `refund.retry.invalid_state` etc. from the Domain | non-circular: no client body; both values constrained/server-derived |

## 3. Exemption soundness rules applied

- Optional input was **not** accepted as proof of safety: `ApproveReturnCommand.RefundDestination` and
  `RejectReturnCommand.Reason` are optional *and* validated; `CreateReturnCommand.Reason` likewise.
- Route-constrained `:guid` identifiers are treated as a real transport guarantee only where the
  constraint is present on the mapped route.
- Server-derived actor/seller-party values are traced to `IReturnCustomerAuthorizer.TryResolveActor`,
  `IReturnSellerAuthorizer.RequireAuthorizedAsync`, `IReturnAdminAuthorizer.RequireAuthorizedAsync` —
  never to a client header or body value.
- `RetryReturnRefundCommand` is a POST whose entire payload is route-constrained plus server-derived;
  no client body exists.

## 4. Set-equality proof

```text
shipped routes                              = 11
ISender.Send call sites                     = 11
endpoint-reachable request types            = 11
VALIDATOR_REQUIRED                          = 4
NO_VALIDATOR_REQUIRED                       = 7
4 + 7                                       = 11
duplicates / unclassified / unmapped        = 0
```

No endpoint-reachable request is missing from the matrix, and no request appears twice. The durable
guard `ReturnsModuleAmsc001W3CertGuardTests.Returns_cqrs_and_validator_matrix_are_exhaustive_and_discoverable`
re-derives the route count, the `ISender` count, the request/handler presence and the four validators,
so a newly added endpoint request cannot bypass classification silently.
