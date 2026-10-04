# TB-TMAR-ADDRESSBOOK-AMSC-001-W3 — api-result-error-mapping

`ApiResultPatternState = CANONICAL`. `RawResultsState = ZERO`. `MessageTextClassificationState = ZERO`.

## Canonical factory usage (all 6 routes)

| Route | Endpoint call | Notes |
| --- | --- | --- |
| `GET /v1/customer/addresses` | `api.From(result)` | handler `Result<IReadOnlyList<CustomerAddressRecord>>` |
| `GET /v1/customer/addresses/{addressId:guid}` | `api.From(result)` | handler `Result<CustomerAddressRecord>`; missing/foreign → `Result.Failure(customer.address.missing)` |
| `POST /v1/customer/addresses` | `api.Created($"/v1/customer/addresses/{created.Value.AddressId}", created)` / `api.From(created)` | 201 location comes from the canonical factory, not a manual `StatusCodes.Status201Created` |
| `PUT /v1/customer/addresses/{addressId:guid}` | `api.From(updated)` | |
| `DELETE /v1/customer/addresses/{addressId:guid}` | `api.From(result)` | handler `Result` |
| `POST /v1/customer/addresses/{addressId:guid}/default` | `api.From(updated)` | |

Session guard paths (untrusted actor, all six routes) use
`api.FromFailure(new SemanticError(AddressBookErrorCodes.SessionRequired))` — the canonical failure path,
not a hand-built problem object.

## Forbidden patterns — verified absent in the Endpoints project

Asserted by `AddressBookModuleAmsc001W3CertGuardTests.AddressBook_endpoints_use_only_the_canonical_result_factory`
over every `.cs` file in `Tooba.AddressBook.Endpoints`:

| Pattern | Count |
| --- | --- |
| `Results.Json` | 0 |
| `Results.NoContent` | 0 |
| `Results.BadRequest` | 0 |
| `Results.Problem` | 0 |
| `new ProblemDetails` | 0 |
| `StatusCodes.Status201Created` | 0 |
| `Accept-Language` | 0 |
| `.Message.StartsWith` / `.Message.Contains` (prose classification) | 0 |
| `catch (...)` blocks in endpoints | 0 |
| local error mapper / local ProblemDetails builder | 0 |

The only `StatusCodes.*` references in the module are inside
`Endpoints/Errors/AddressBookErrorCatalogContributor.cs`, where HTTP status belongs (as descriptor data),
matching the Offer/AccessControl catalog shape.

## Fault → result → HTTP mapping

| Failure | Producer | Stable code | HTTP |
| --- | --- | --- | --- |
| address not found for this owner (read) | `GetCustomerAddressQuery` (`AddressBookOperation.NotFoundIfNull`) | `customer.address.missing` | 404 |
| address not found/foreign (delete) | `AddressBookDirectory.DeleteAsync` → `SemanticException` | `customer.address.missing` | 404 |
| address not found/foreign (update, set-default) | `AddressBookDirectory.RequireOwnAsync` → `SemanticException` | `customer.address.missing` | 404 |
| untrusted/empty actor | `AddressBookDirectory.EnsureActor` / `CustomerAddress.Create` → `SemanticException` | `customer.address.actor_required` | 400 |
| field-shape invariants (10 sites) | `CustomerAddress.ApplyFields` → `SemanticException` | `customer.address.*` | 400 |
| half recipient name | `CustomerAddress.ApplyRecipientNames` → `SemanticException` | `customer.address.recipient_name_parts_invalid` | 400 |
| missing session | endpoint session seam | `customer.session.required` | 401 (Foundation descriptor) |
| transport shape | FluentValidation validators | `customer.address.*_required` | 400 via catalogued `validation.failed` |

This is the **W1 bounded defect repair** (W0 F2): the foreign/missing-address path moved from
`500 platform.unexpected` to the already-declared `404 customer.address.missing`, and the escaped
field-shape/actor faults from `500` to catalogued `400`s. No route, DTO, success payload or schema changed;
`RoutesChanged = NONE`, `StatusCodesChanged = NONE`, `DtoShapeChanged = NONE`.

## Unknown/expected separation

| Check | Result |
| --- | --- |
| unknown exceptions silently converted to business failures | NO — `SafeErrorMapper.MapUnexpected()` keeps `platform.unexpected` / 500 |
| expected failures classified by parsing exception prose | NO |
| duplicate-suppression mechanism introduced | NO |
| new shared-errors project/layer created | NO — the Foundation owns only the genuinely cross-cutting `customer.session.required` |
| unresolved conflicting HTTP/classification semantics for one code | NONE |

## Success response shape preserved

`CustomerAddressRecord` remains the shipped success payload, returned raw through the canonical factory
(`Assert.Contains("api.From(result)", ...)`). No envelope was invented.

## Guard coverage

- `AddressBookModuleAmsc001W3CertGuardTests.AddressBook_endpoints_use_only_the_canonical_result_factory` — PASS
- `AddressBookCanonicalPresentationGuardTests` (canonical presentation stack, no parallel problem pipeline,
  catalog contributor + resource set registration) — 3 PASS
- `AddressBookFoundationTests.Endpoint_uses_session_and_rejects_missing_production_actor` — PASS
