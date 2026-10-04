# TB-TMAR-ADDRESSBOOK-AMSC-001-W3 — cqrs-request-matrix

`CqrsState = COMPLIANT`. MediatR **12.5.0** (`AddToobaCqrsFoundation`).
Endpoint-reachable requests: **6**. Real `IRequestHandler<,>` pairs: **6**.

## Request → handler → validator matrix

| # | Request | Kind | Handler | Returns | Validator | Route |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `ListCustomerAddressesQuery` | `IRequest<Result<IReadOnlyList<CustomerAddressRecord>>>` | `ListCustomerAddressesQueryHandler` | `Result<IReadOnlyList<CustomerAddressRecord>>` | **none** (`NO_VALIDATOR_REQUIRED`) | `GET /v1/customer/addresses` |
| 2 | `GetCustomerAddressQuery` | `IRequest<Result<CustomerAddressRecord>>` | `GetCustomerAddressQueryHandler` | `Result<CustomerAddressRecord>` | `GetCustomerAddressQueryValidator` | `GET /v1/customer/addresses/{addressId:guid}` |
| 3 | `CreateCustomerAddressCommand` | `IRequest<Result<CustomerAddressRecord>>` | `CreateCustomerAddressCommandHandler` | `Result<CustomerAddressRecord>` | `CreateCustomerAddressCommandValidator` | `POST /v1/customer/addresses` |
| 4 | `UpdateCustomerAddressCommand` | `IRequest<Result<CustomerAddressRecord>>` | `UpdateCustomerAddressCommandHandler` | `Result<CustomerAddressRecord>` | `UpdateCustomerAddressCommandValidator` | `PUT /v1/customer/addresses/{addressId:guid}` |
| 5 | `DeleteCustomerAddressCommand` | `IRequest<Result>` | `DeleteCustomerAddressCommandHandler` | `Result` | `DeleteCustomerAddressCommandValidator` | `DELETE /v1/customer/addresses/{addressId:guid}` |
| 6 | `SetDefaultCustomerAddressCommand` | `IRequest<Result<CustomerAddressRecord>>` | `SetDefaultCustomerAddressCommandHandler` | `Result<CustomerAddressRecord>` | `SetDefaultCustomerAddressCommandValidator` | `POST /v1/customer/addresses/{addressId:guid}/default` |

## CQRS invariants

| Invariant | Result |
| --- | --- |
| Every endpoint-reachable request implements `MediatR.IBaseRequest` | PASS (`All_six_requests_are_real_mediatr_requests_and_endpoints_use_ISender`) |
| Every request has a real `IRequestHandler<,>` in the module assembly | PASS |
| Endpoints dispatch exclusively via `ISender` | PASS |
| Zero endpoint → directory/persistence calls | PASS (`Assert.DoesNotContain("IAddressBookDirectory", ...)`) |
| Zero endpoint-side `IValidator` / `ValidateAsync` | PASS |
| Zero Host bypass / custom dispatcher | PASS (no Host AddressBook folder) |
| Every handler returns the canonical `Result` / `Result<T>` | PASS (6/6, W1) |

## Handler fault composition

All six handlers wrap their directory call in the canonical seam
`Application/Composition/AddressBookOperation`:

```csharp
AddressBookOperation.ExecuteAsync(...)          // -> Result / Result<T>
AddressBookOperation.NotFoundIfNull(value, code) // -> Result<T>
```

`AddressBookOperation` catches `SemanticException` and maps `ex.Error` to `Result.Failure`. This mirrors
`UserPreferenceOperation`, `AccessControlOperation`, `ContentOperation`, `WishlistOperation`,
`PartyOperation` and `BulkInquiryOperation` — no parallel mechanism was invented.

## No duplicate CQRS request shape

| Check | Result |
| --- | --- |
| Request record duplicated in `Models/` beside the authoritative MediatR request | NONE |
| `Application/Models` content | only `CustomerAddressWrite` (write *input* payload, not a CQRS request) |
| `Endpoints/Customer` body record | `CustomerAddressWriteRequest` — the HTTP body shape, a sanctioned boundary split (W0 F7), not a duplicate request |

`DuplicateCqrsRequestShape = ZERO`.
