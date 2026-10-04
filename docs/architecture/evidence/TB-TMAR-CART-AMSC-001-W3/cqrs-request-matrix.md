# TB-TMAR-CART-AMSC-001-W3 — Request → handler → validator matrix

| # | Route | Request | Kind | Handler | Validator | Classification |
|---|---|---|---|---|---|---|
| 1 | `POST /v1/storefront/carts` | `CreateGuestCartCommand` | Command | ✅ `IRequestHandler<CreateGuestCartCommand, …>` | — | `NO_VALIDATOR_REQUIRED` (no transport input; cart owner/credential created server-side) |
| 2 | `POST /v1/storefront/carts/merge` | `MergeCartAfterLoginCommand` | Command | ✅ | — | `NO_VALIDATOR_REQUIRED` (guest credential resolved from access context, not body) |
| 3 | `POST /v1/storefront/carts/lines` | `AddCartLineCommand` | Command | ✅ | ✅ `AddCartLineCommandValidator` | `VALIDATOR_REQUIRED` |
| 4 | `PATCH /v1/storefront/carts/lines` | `ChangeCartLineQuantityCommand` | Command | ✅ | ✅ `ChangeCartLineQuantityCommandValidator` | `VALIDATOR_REQUIRED` |
| 5 | `DELETE /v1/storefront/carts/lines` | `RemoveCartLineCommand` | Command | ✅ | ✅ `RemoveCartLineCommandValidator` | `VALIDATOR_REQUIRED` |
| 6 | `GET /v1/storefront/carts/{cartId}` | `GetCartQuery` | Query | ✅ | ✅ `GetCartQueryValidator` | `VALIDATOR_REQUIRED` |
| 7 | `GET /v1/storefront/carts/current` | `GetCurrentAuthenticatedCartQuery` | Query | ✅ | — | `NO_VALIDATOR_REQUIRED` (identity comes from the authenticated session, not transport) |

- 7 endpoint-reachable requests, 7 request→handler pairs, 0 orphan handlers, 0 duplicate request shapes.
- Every handler dispatches through `ISender` from `CartStorefrontEndpoints.cs`; endpoints perform no
  persistence/directory access.
- All handlers map faults through the canonical `CartOperation.ExecuteAsync` seam; no handler throws
  raw exceptions or classifies outcomes by message text.
- Validators emit stable machine codes (`CartValidationCodes`), never localized prose.
- Durable coverage lock: `CartEndpointValidatorCoverageGuardTests` +
  `CartModuleAmsc001W3CertGuardTests.Cart_application_is_capability_first_with_no_single_file_use_case_leaves`.

MediatR version: `12.5.0` (repository standard, registered via `AddToobaCqrsFoundation`).
