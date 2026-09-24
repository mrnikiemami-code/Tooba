# TB-TMAR-PAYMENT-PRECERT-VALIDATION-002 — Payment Admin + Webhook transport validation

Parent: `TB-TMAR-PAYMENT-PRECERT-VALIDATION-001` (ARCHITECT-ACCEPTED on main at
`7ce560c18454a8f8b44d95cade00be6f8605696b`)

Scope: this task closes the remaining Payment endpoint-reachable transport validation coverage.
It does **not** structure-certify Payment under ARCH-COMPLETE-002.

## 1. Remaining 6-request inventory closed by this task

| # | Request | Surface | Classification |
| - | - | - | - |
| 1 | `GetAdminPaymentQuery` | Admin | VALIDATOR_REQUIRED_PRESENT |
| 2 | `ReconcileAdminPaymentCommand` | Admin | VALIDATOR_REQUIRED_PRESENT |
| 3 | `ConfirmAdminDepositCommand` | Admin | VALIDATOR_REQUIRED_PRESENT |
| 4 | `RejectAdminDepositCommand` | Admin | VALIDATOR_REQUIRED_PRESENT |
| 5 | `QueryAdminPaymentsGridQuery` | Admin | VALIDATOR_REQUIRED_PRESENT |
| 6 | `ProcessPaymentWebhookCommand` | Webhook | VALIDATOR_REQUIRED_PRESENT |

## 2. Six validator names added

`Tooba.Payment.Application/Validators/Admin/` (namespace `Tooba.Payment.Application.Validators.Admin`):

1. `GetAdminPaymentQueryValidator`
2. `ReconcileAdminPaymentCommandValidator`
3. `ConfirmAdminDepositCommandValidator`
4. `RejectAdminDepositCommandValidator`
5. `QueryAdminPaymentsGridQueryValidator`

`Tooba.Payment.Application/Validators/Webhooks/` (namespace `Tooba.Payment.Application.Validators.Webhooks`):

6. `ProcessPaymentWebhookCommandValidator`

No additional Payment validator was created.

## 3. Complete 16-request coverage totals

| Surface | Requests | Required present | No-validator-required |
| - | - | - | - |
| Storefront | 10 | 9 | 1 (`ListStorefrontPaymentMethodsQuery`) |
| Admin | 5 | 5 | 0 |
| Webhook | 1 | 1 | 0 |
| **Total** | **16** | **15** | **1** |

- `paymentValidationStorefront` = `9_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED`
- `paymentValidationAdmin` = `5_REQUIRED_PRESENT`
- `paymentValidationWebhook` = `1_REQUIRED_PRESENT`
- `paymentValidationOverall` = `COMPLETE_15_OF_15_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED`
- Zero missing validators.

The Admin inventory is derived from `PaymentAdminEndpoints.cs` (5 routes) and the Webhook inventory
from `PaymentWebhookEndpoints.cs` (1 route); the guard extracts every `new <Command|Query>` from
those files and requires exact equality with the manifest.

## 4. Worker-only exclusion

`ReconcileStalePaymentsCommand` is reachable only from
`Tooba.Payment.Infrastructure/Workers/PaymentReconciliationWorker.cs` via `ISender`.
It is **not** counted in the endpoint inventory (which is exactly 16) and remains
`NO_VALIDATOR_REQUIRED_INTERNAL_WORKER`; the guard asserts no `IValidator<ReconcileStalePaymentsCommand>`
is registered.

## 5. Grid validation boundary

`QueryAdminPaymentsGridQueryValidator` shapes only the primitive envelope:

- `Input` not null
- `Input.Filters` not null
- `Input.SortField` non-blank after trim
- `Input.SortDirection` non-blank after trim
- `Page >= 1`
- `PageSize >= 1`

Deliberately **not** validated here (still owned by `PaymentAdminGridQueryNormalizer` and its stable
semantic grid codes): allowed field names, allowed operators, advanced connector rules, sort whitelist,
search semantics, supply/reservation enrichment semantics. The focused test proves an unknown
field/operator and a `supply`-field filter still pass transport validation.

## 6. Webhook validation boundary

`ProcessPaymentWebhookCommandValidator` shapes only the transport envelope:

- `ProviderCode` non-blank after trim
- `RawBody` not null and `Length > 0`
- `BodyText` non-blank after trim
- `SignatureHeader` may be `null`; a supplied value must not be whitespace-only

Deliberately **not** done here (still owned by `ProcessPaymentWebhookHandler` and
`IPaymentWebhookSignatureVerifier`): JSON parsing, payload/`PaymentId`/`AttemptId`/provider-event
field validation, signature verification, provider allowlists, signature format/length rules.
The focused test proves a non-JSON body without payload fields still passes transport validation.

## 7. Discovery proof

- FluentValidation only; no second validation pipeline, no endpoint-specific middleware.
- Discovery stays `services.AddValidatorsFromAssembly(assembly)` in
  `Tooba.BuildingBlocks/TmarFoundation.cs` via `AddToobaCqrsFoundation(...)`, already registering the
  `Tooba.Payment.Application` assembly from `Host/Program.cs`.
- MediatR remains `12.5.0`.
- No manual validator invocation from endpoints or handlers (guard asserts `IValidator`,
  `Validator` and `ValidateAsync` are absent from all three Payment endpoint files and the relevant
  handler files).

## 8. Focused tests result

```
dotnet test src/backend/Modules/Payment/Tooba.Payment.Tests/Tooba.Payment.Tests.csproj --no-build \
  --filter "FullyQualifiedName~PaymentAdminWebhookValidatorTests|FullyQualifiedName~PaymentValidatorCoverageGuardTests|FullyQualifiedName~PaymentStorefrontValidatorTests|FullyQualifiedName~PaymentArchitectureGuardTests"
Passed! - Failed: 0, Passed: 42, Skipped: 0, Total: 42
```

Direct/in-memory. No `WebApplicationFactory`. No database.

## 9. Project build result

```
dotnet build src/backend/Modules/Payment/Tooba.Payment.Tests/Tooba.Payment.Tests.csproj --no-restore
Build succeeded. 0 Error(s)
```

## 10. Structure certification remains PENDING

- Payment is still `PENDING_TB_TMAR_PAYMENT_ARCH_COMPLETE_002_STRUCTURE_001`.
- Not certified in `tmar-module-structure-manifests.json`; not added to
  `structureLock.certifiedModules`; still present in `uncertifiedHttpOwningModules`.
- Payment -> Host dependency = ZERO.
- Host Payment residue = exactly `HostPaymentAdminAuthorizer` + `HostPaymentStorefrontAuthorizer`.
- `PaymentContractBridge` and the focused directory split intact; no schema/migration change.
- Cart/Order/StoreContext/Offer certifications unchanged.
- Checkout = `PAUSED_AT_SAFE_W5_CHECKPOINT`; `frontendFrozen = true`.

## 11. Recovery next

- `nextTask` = `TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001`
- `nextTaskGate` = `NEXT_TMAR_WAVE_AFTER_PAYMENT_PRECERT_VALIDATION_002`
