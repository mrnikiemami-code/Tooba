# TB-TMAR-ADDRESSBOOK-AMSC-001-W0 — Canonical Pattern Gaps

States required by the Analyze skill's structured output, with the exact evidence for each.

| State field | Value | Evidence |
| --- | --- | --- |
| `Localization-State` | `EXCEPTION_MESSAGE_BASED` | see §1 |
| `API-Result-Pattern-State` | `AD_HOC` | see §2 |
| `Stable-Error-Code-State` | `UNREGISTERED_CODES` (partial) | see §3 |
| `Logging-State` | `CANONICAL` | see §4 |
| `Sensitive-Logging-State` | `NONE` | see §4 |
| `OpenTelemetry-State` | `CANONICAL` | see §5 |
| `Correlation-Trace-State` | `CANONICAL` | see §5 |
| `CQRS-State` | `PARTIAL` | see §6 |
| `Validator-Coverage-State` | `EXHAUSTIVE` | see §7 |
| `Contracts-Boundary-State` | `CLEAN` | see §8 |
| `Schema-Migration-State` | `UNCHANGED` | see §9 |

## 1. Localization — `EXCEPTION_MESSAGE_BASED`

`AddressBookErrors.resx` and `AddressBookErrors.fa.resx` each contain exactly **one** key:

```text
customer.address.missing
```

The module's real user-facing failure vocabulary is instead hard-coded as Persian
`InvalidOperationException` messages:

| File | Lines | Text |
| --- | --- | --- |
| `Domain/Aggregates/CustomerAddress.cs` | 119 | `"Actor معتبر الزامی است."` |
| `Domain/Aggregates/CustomerAddress.cs` | 197 | `"نام گیرنده الزامی است."` |
| `Domain/Aggregates/CustomerAddress.cs` | 198 | `"شمارهٔ تماس معتبر نیست."` |
| `Domain/Aggregates/CustomerAddress.cs` | 200 | `"کشور الزامی است."` |
| `Domain/Aggregates/CustomerAddress.cs` | 201 | `"نام استان بیش از حد بلند است."` |
| `Domain/Aggregates/CustomerAddress.cs` | 202 | `"شهر الزامی است."` |
| `Domain/Aggregates/CustomerAddress.cs` | 203 | `"کدپستی معتبر نیست."` |
| `Domain/Aggregates/CustomerAddress.cs` | 204 | `"نشانی پستی الزامی است."` |
| `Domain/Aggregates/CustomerAddress.cs` | 205 | `"واحد ساختمان بیش از حد بلند است."` |
| `Domain/Aggregates/CustomerAddress.cs` | 206 | `"برچسب نشانی بیش از حد بلند است."` |
| `Domain/Aggregates/CustomerAddress.cs` | 223 | `"نام و نام خانوادگی هر دو الزامی‌اند."` |
| `Infrastructure/Adapters/AddressBookDirectory.cs` | 123 | `"نشانی متعلق به این مشتری پیدا نشد."` |
| `Infrastructure/Adapters/AddressBookDirectory.cs` | 155 | `"نشانی متعلق به این مشتری پیدا نشد."` |
| `Infrastructure/Adapters/AddressBookDirectory.cs` | 216 | `"Actor معتبر الزامی است."` |

These are **not** log-only strings: `InvalidOperationException` escapes to
`ExceptionPresentationService`, is classified `Unexpected`, and the client receives the generic
`platform.unexpected` problem — so the Persian text never reaches the client, while the *intended*
localizable outcome is lost entirely. Both halves are defects.

Non-defect Persian literals (recorded for completeness, **not** user-facing):

| File | Lines | Nature |
| --- | --- | --- |
| `Infrastructure/Adapters/AddressBookDevelopmentSeed.cs` | 31–57 | development seed **data values** (recipient name, city, street) for the demo storefront guest |
| `Domain/Aggregates/CustomerAddress.cs` | XML docs | documentation comments |

Also verified clean:

- no `exception.Message` / `ex.Message` used as a user-facing contract;
- no endpoint-level `Accept-Language` parsing (culture resolution is inside `ApiResponseFactory`);
- no second localization system.

## 2. API result / error mapping — `AD_HOC`

`ApiResponseFactory` **is** injected into all six endpoints and **is** used for the two guard paths:

```csharp
return api.FromFailure(new SemanticError(AddressBookErrorCodes.SessionRequired));
return api.FromFailure(new SemanticError(AddressBookErrorCodes.AddressMissing));
```

All six **success** paths bypass it:

| File | Line | Code |
| --- | --- | --- |
| `AddressBookCustomerReadEndpoints.cs` | 39 | `return Results.Json(items);` |
| `AddressBookCustomerReadEndpoints.cs` | 59 | `: Results.Json(item);` |
| `AddressBookCustomerWriteEndpoints.cs` | 50 | `return Results.Json(created, statusCode: StatusCodes.Status201Created);` |
| `AddressBookCustomerWriteEndpoints.cs` | 71 | `return Results.Json(updated);` |
| `AddressBookCustomerWriteEndpoints.cs` | 91 | `return Results.NoContent();` |
| `AddressBookCustomerWriteEndpoints.cs` | 111 | `return Results.Json(updated);` |

Additional findings:

- **HTTP-layer business classification:** `AddressBookCustomerReadEndpoints.GetAsync` performs the
  missing/foreign decision with `item is null ? api.FromFailure(…) : Results.Json(item)` — a
  business outcome decided in the presentation layer because the handler cannot express failure.
- **Manual status threading:** `StatusCodes.Status201Created` is passed by hand instead of using
  `ApiResponseFactory.Created(location, result)`.
- No `Results.Problem(...)`, no local `ProblemDetails` builder, no ad-hoc exception `catch`-and-map,
  no `ex.Message` parsing — so this is `AD_HOC` (partial adoption), not a parallel pipeline.
- Handler return types are the root cause: `CustomerAddressRecord`, `CustomerAddressRecord?`,
  `IReadOnlyList<CustomerAddressRecord>`, `Unit` — none is `Result`/`Result<T>`.

## 3. Stable error codes — `UNREGISTERED_CODES` (partial)

Declared (2):

| Code constant | Value | Catalogued? |
| --- | --- | --- |
| `AddressBookErrorCodes.AddressMissing` | `customer.address.missing` | yes — `AddressBookErrorCatalogContributor`, `NotFound` / 404 |
| `AddressBookErrorCodes.SessionRequired` | `customer.session.required` | **not** re-registered by design — owned by the shared Foundation contributor (correct) |

Missing (no stable code exists for any of these real outcomes):

| Outcome | Current code path |
| --- | --- |
| foreign or missing address on `Update` | `InvalidOperationException` → `platform.unexpected` 500 |
| foreign or missing address on `Delete` | `InvalidOperationException` → `platform.unexpected` 500 |
| foreign or missing address on `SetDefault` | `InvalidOperationException` → `platform.unexpected` 500 |
| empty/invalid actor | `InvalidOperationException` → `platform.unexpected` 500 |
| domain field-shape rejection (10 sites) | `InvalidOperationException` → `platform.unexpected` 500 |
| half-supplied first/last name | `InvalidOperationException` → `platform.unexpected` 500 |

`SafeErrorMapper.Map(SemanticError)` resolves unknown codes to `400 Business` with the fallback
`"Request rejected."` — so *if* these were `SemanticException`s with new codes they would still need
descriptors to get the right status and localized title. Both the code and the descriptor must be
added together.

## 4. Logging / sensitive data — `CANONICAL` / `NONE`

Scan across the module for `Console.`, `Debug.Write`, `ActivitySource`, `new Meter`, `AsyncLocal`,
`traceparent`, `Guid.NewGuid().ToString()`, `HttpClient`: **zero hits**.

No logger is injected anywhere in AddressBook. Nothing is logged, so there is no sensitive-data
logging surface. The global `ExceptionPresentationService` performs the single structured log with
`ErrorCode`/`Classification`/`StatusCode`/`CorrelationId`/`TraceId`/`SpanId`/`RequestId`/`TenantId`/
`StoreId`/`ActorId`/`Path`/`Method` — canonical.

## 5. OpenTelemetry / correlation — `CANONICAL`

- No `ActivitySource.StartActivity`, no second `Meter`, no manual `traceparent` parsing.
- No custom correlation header, no `AsyncLocal`, no `Guid.NewGuid()`-as-correlation.
- `ProblemDetails` `traceId` / `correlationId` / `requestId` are supplied by
  `ProblemDetailsContextProvider` through `ApiResponseFactory`.
- AddressBook makes **no** cross-module calls, so `IModuleCallTracer` decoration is not applicable
  (`LOST_PROPAGATION` does not apply).

## 6. CQRS — `PARTIAL`

| Check | Result |
| --- | --- |
| 6 real `IRequest<T>` types | ✅ |
| 6 real `IRequestHandler<,>` implementations | ✅ |
| Endpoints dispatch only through `ISender` | ✅ |
| Endpoints touch `IAddressBookDirectory` | ❌ none — clean |
| Endpoints call `ValidateAsync` | ❌ none — clean |
| Handlers return canonical `Result` / `Result<T>` | ❌ **none — all raw** |
| Host bypass around module CQRS | ✅ none |

## 7. Validator coverage — `EXHAUSTIVE`

| Request | Route | Classification | Validator |
| --- | --- | --- | --- |
| `ListCustomerAddressesQuery` | `GET /v1/customer/addresses` | `NO_VALIDATOR_REQUIRED` | none (no transport input) |
| `GetCustomerAddressQuery` | `GET /v1/customer/addresses/{addressId:guid}` | `VALIDATOR_REQUIRED` | `GetCustomerAddressQueryValidator` |
| `CreateCustomerAddressCommand` | `POST /v1/customer/addresses` | `VALIDATOR_REQUIRED` | `CreateCustomerAddressCommandValidator` |
| `UpdateCustomerAddressCommand` | `PUT /v1/customer/addresses/{addressId:guid}` | `VALIDATOR_REQUIRED` | `UpdateCustomerAddressCommandValidator` |
| `DeleteCustomerAddressCommand` | `DELETE /v1/customer/addresses/{addressId:guid}` | `VALIDATOR_REQUIRED` | `DeleteCustomerAddressCommandValidator` |
| `SetDefaultCustomerAddressCommand` | `POST /v1/customer/addresses/{addressId:guid}/default` | `VALIDATOR_REQUIRED` | `SetDefaultCustomerAddressCommandValidator` |

Guarded by `AddressBookValidatorCoverageGuardTests` (inventory, DI resolution, `ISender`-only
dispatch). Validators emit stable machine codes (`customer.address.*`), never localized text.
`ActorUserId` is deliberately never validated as payload (trusted server-side actor).

## 8. Contracts boundary — `CLEAN`

| Contracts type | Module-boundary semantic | Verdict |
| --- | --- | --- |
| `CustomerAddressRecord` | cross-module read snapshot (Order, CustomerProfile) | correct in Contracts |
| `IAddressBookCheckoutLookup` | cross-module read port (Order) | correct in Contracts |
| `IAddressBookCountPort` | cross-module read port (CustomerProfile) | correct in Contracts |
| `AddressBookErrorCodes` | module-owned stable code vocabulary | correct in Contracts |

No CQRS request dump, no `*Contracts.cs` mixed bundle, no Application-internal type leaked into
Contracts. `IAddressBookDirectory` correctly stays in Application and *extends* the boundary port.

## 9. Schema / migration — `UNCHANGED`

| Artifact | Value |
| --- | --- |
| Migrations | `20260825171858_InitialAddressBook`, `20260913180000_AddRecipientNameParts` |
| Snapshot | `AddressBookDbContextModelSnapshot` |
| Schema | `address_book` |
| Table | `customer_addresses` |
| Indexes | `ix_customer_addresses_owner_created` (composite), `ix_customer_addresses_one_default_per_owner` (unique, filtered `is_default = TRUE`) |

W1 must not regenerate, reorder, or edit any of these. No schema change is required by the planned
work (it is contract/presentation only).
