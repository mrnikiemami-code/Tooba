# TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001 — Settlement pre-cert validation

## 1. Scope

Bounded pre-certification validator wave only. Settlement is **NOT** structure-certified here.
No Settlement behavior redesign, no endpoint/handler change, no namespace/folder move, no
manifest certification, no Host/Payment/Checkout/frontend change, no schema/migration change.

- Parent-Task: `TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-AUDIT-001-R1` (ARCHITECT-ACCEPTED at `dad29faf`)
- Discovery: existing `AddToobaCqrsFoundation` / `AddValidatorsFromAssembly` only. MediatR 12.5.0.

## 2. Exact 10-request classification

| Request | Surface | Classification | Validator |
| --- | --- | --- | --- |
| `RequestSellerPayoutCommand` | Seller | `VALIDATOR_REQUIRED_PRESENT` | `RequestSellerPayoutCommandValidator` |
| `GetSellerSettlementBalanceQuery` | Seller | `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY` | — |
| `ListSellerSettlementEntriesQuery` | Seller | `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY` | — |
| `ListSellerSettlementStatementsQuery` | Seller | `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY` | — |
| `ListSellerPayoutRequestsQuery` | Seller | `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY` | — |
| `ProcessAdminPayoutCommand` | Admin | `VALIDATOR_REQUIRED_PRESENT` | `ProcessAdminPayoutCommandValidator` |
| `RetryAdminPayoutCommand` | Admin | `VALIDATOR_REQUIRED_PRESENT` | `RetryAdminPayoutCommandValidator` |
| `ListAdminSettlementBalancesQuery` | Admin | `NO_VALIDATOR_REQUIRED_NO_INPUT` | — |
| `ListAdminPayoutQueueQuery` | Admin | `NO_VALIDATOR_REQUIRED_NO_INPUT` | — |
| `QueryAdminPayoutGridQuery` | Admin | `VALIDATOR_REQUIRED_PRESENT` | `QueryAdminPayoutGridQueryValidator` |

Totals: 10 endpoint-reachable, 4 required (4 present, 0 missing), 6 no-validator-required
(4 AUTH_SCOPED_QUERY + 2 NO_INPUT).

## 3. Four validator names and rules

| Validator | Folder / namespace | Rules (transport shape only) |
| --- | --- | --- |
| `RequestSellerPayoutCommandValidator` | `Validators/Seller` → `Tooba.Settlement.Application.Validators.Seller` | `Amount > 0`; `IdempotencyKey` non-null/non-empty/non-whitespace |
| `ProcessAdminPayoutCommandValidator` | `Validators/Admin` → `Tooba.Settlement.Application.Validators.Admin` | `PayoutRequestId != Guid.Empty` |
| `RetryAdminPayoutCommandValidator` | `Validators/Admin` → `Tooba.Settlement.Application.Validators.Admin` | `PayoutRequestId != Guid.Empty` |
| `QueryAdminPayoutGridQueryValidator` | `Validators/Admin` → `Tooba.Settlement.Application.Validators.Admin` | `Request` not null |

Deliberately NOT validated: `SellerPartyId` and `ActorUserId` (trusted authorizer values),
available balance, payout eligibility, seller existence, idempotency uniqueness, payout
state/business policy, payout existence/retry eligibility. No invented `IdempotencyKey`
max length. Grid paging normalization, field/operator/sort/connector whitelists, filter and
advanced-filter/search semantics stay owned by `AdminPayoutGridQueryPolicy` and are not
duplicated.

Stable transport codes live in `SettlementValidationCodes` (`settlement.validation.*`),
kept separate from the business `SettlementErrorCodes`.

## 4. Six NO_VALIDATOR_REQUIRED classifications/reasons

- `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY` — `GetSellerSettlementBalanceQuery`,
  `ListSellerSettlementEntriesQuery`, `ListSellerSettlementStatementsQuery`,
  `ListSellerPayoutRequestsQuery`: `SellerPartyId` is produced only by
  `ISettlementSellerAuthorizer.RequireAuthorizedAsync(...)`; no untrusted payload.
- `NO_VALIDATOR_REQUIRED_NO_INPUT` — `ListAdminSettlementBalancesQuery`,
  `ListAdminPayoutQueueQuery`: parameterless requests created only after admin authorization.

No ceremonial validators exist for any of the six.

## 5. Central discovery proof

`SettlementValidatorCoverageGuardTests` builds a real `ServiceCollection` with
`AddToobaCqrsFoundation(typeof(RequestSellerPayoutCommand).Assembly)` and proves each of the
four required validators resolves as `IValidator<TRequest>` to its exact concrete type, and
that no validator is registered for any of the six NO_VALIDATOR_REQUIRED requests. MediatR
package assertion remains `12.5.0`.

## 6. Focused tests

- `SettlementValidatorTests` (direct in-memory, no host/DB): amount <= 0 rejected;
  `IdempotencyKey` null/empty/whitespace rejected; minimal valid command passes;
  `SellerPartyId`/`ActorUserId` are not validator rules; `Guid.Empty` `PayoutRequestId`
  rejected and non-empty accepted for both admin commands with `ActorUserId` never
  validated; null grid `Request` rejected; a policy-hostile non-null grid request passes
  (grid policy not duplicated).
- `SettlementValidatorCoverageGuardTests`: exact 10-request inventory, endpoint-construction
  match for Seller/Admin, foundation-DI resolution, ISender-only endpoints, no
  `IValidator`/`ValidateAsync` in endpoints or relevant handlers, exact `Seller`/`Admin`
  validator folder layout, MediatR 12.5.0, Settlement still not structure-certified.

Result: 15 passed, 0 failed.

## 7. Project build

`dotnet build src/backend/Modules/Settlement/Tooba.Settlement.Tests/Tooba.Settlement.Tests.csproj --no-restore` — 0 errors.

## 8. Structure certification

**PENDING** — `structureCertification = PENDING_TB_TMAR_SETTLEMENT_ARCH_COMPLETE_002_STRUCTURE_001`.
Settlement is not added to `structureLock.certifiedModules` and remains in
`uncertifiedHttpOwningModules`. Settlement → Host = ZERO; Host Settlement residue = two thin
security adapters. No schema/migration change. Checkout = `PAUSED_AT_SAFE_W5_CHECKPOINT`;
`frontendFrozen = true`.
