# TB-TMAR-NOTIFICATION-AMSC-001-W1 — Migrate evidence

- Starting HEAD: `5777ff4a` (W0), `HEAD == origin/main`
- Skill: `tooba-architecture-migrate` (behaviour-preserving canonical-mechanism wave)
- Target: `src/backend/Modules/Notification/Tooba.Notification.*`

## 1. What changed (old → new)

| Concern | Before | After |
| --- | --- | --- |
| Stable-code home | `Application/Errors/NotificationErrorCodes.cs` (2 codes) | `Contracts/Errors/NotificationErrorCodes.cs` (5 codes + `IsKnown`) + `NotificationSharedErrorCodes` alias for the Foundation-owned session code |
| Route allowlist faults | raw `InvalidOperationException("notification.target_route.*")` × 3 | typed `ContractOperationException(NotificationErrorCodes.TargetRoute*)` |
| Recipient-kind fault | raw `InvalidOperationException("notification.recipient_kind.invalid")` | typed `ContractOperationException(NotificationErrorCodes.RecipientKindInvalid)` |
| Result seam | none | `Application/Composition/NotificationOperation.cs` (typed-fault → `Result`, `IsKnown` filter; unknown codes propagate) |
| Localization | no module resource set | `Contracts/Errors/NotificationErrorResources.cs` (`IErrorResourceSet` claiming `notification.`) + `Contracts/Resources/NotificationErrors.resx` / `.fa.resx` (5 keys × 2 cultures) + `IErrorResourceSet` registration in `NotificationModule` |
| Error catalog | 1 descriptor (`notification.missing`) | 5 descriptors (single owner; `customer.session.required` still Foundation-owned, consumed only) |
| Validation | 0 validators | `Application/Validators/NotificationValidationCodes.cs` (5 transport codes) + 6 FluentValidation validators co-located with their requests |

New files (14):

```text
Contracts/Errors/NotificationErrorCodes.cs
Contracts/Errors/NotificationErrorResources.cs
Contracts/Resources/NotificationErrors.resx
Contracts/Resources/NotificationErrors.fa.resx
Application/Composition/NotificationOperation.cs
Application/Validators/NotificationValidationCodes.cs
Application/Commands/MarkCustomerNotificationRead/MarkCustomerNotificationReadCommandValidator.cs
Application/Commands/DismissCustomerNotification/DismissCustomerNotificationCommandValidator.cs
Application/Queries/ListCustomerNotifications/ListCustomerNotificationsQueryValidator.cs
Application/Commands/MarkSellerNotificationRead/MarkSellerNotificationReadCommandValidator.cs
Application/Commands/DismissSellerNotification/DismissSellerNotificationCommandValidator.cs
Application/Queries/ListSellerNotifications/ListSellerNotificationsQueryValidator.cs
Host.Tests/Architecture/NotificationModuleAmsc001W1MigrateGuardTests.cs
```

Deleted: `Application/Errors/NotificationErrorCodes.cs` (+ empty folder).
Modified: `Contracts.csproj` (+ BuildingBlocks ref for typed faults/resource-set seam),
`NotificationTargetRoutes.cs`, `NotificationRecipientKindMapping.cs`, `NotificationModule.cs`
(resource-set registration), `NotificationErrorCatalogContributor.cs` (+4 descriptors),
2 endpoint files + 4 command files + 2 test files (namespace repoints),
`NotificationArchitectureGuardTests.cs` (allowlist extended: `Validators`, `Composition`,
Contracts `Errors`/`Resources` — structure additive, not weakened),
`NotificationBehaviorCharacterizationTests.cs` (route-allowlist test now asserts the typed fault
carries the declared code — strictly stronger).

## 2. Behaviour preservation

- Routes: all 10 unchanged (`/v1/customer/notifications*`, `/v1/seller/notifications*`).
- Wire shapes (`NotificationListHttpResponse`, `NotificationUnreadCountResponse`,
  `NotificationMarkedCountResponse`) and camelCase fields: unchanged.
- Status codes: `notification.missing` → 404 unchanged; `customer.session.required` → 401 unchanged.
- **Accepted bounded expected-failure repair** (same precedent as Media/Cart/Localization W1): the
  four previously raw-`InvalidOperationException` route/kind violations surfaced as 500
  `platform.unexpected`; they now surface as 400 Business with catalogued stable codes. These were
  always expected failures; the transport now honours them.
- Idempotency, paging clamps, ordering, soft-delete semantics, copy rendering, localization keys:
  unchanged. Schema/migrations: untouched (`migrationFilesChanged = 0`).
- `INotificationCreationPort` / `NotificationSemanticTypes` contracts for Wallet/Support: unchanged.

## 3. Focused validation

| Run | Result |
| --- | --- |
| `dotnet build Tooba.Notification.Tests` | 0 errors |
| `dotnet build Tooba.Host` | 0 errors |
| `dotnet test Tooba.Notification.Tests` | **18 passed / 0 failed** |
| `dotnet test --filter NotificationModuleAmsc001W1` | **11 passed / 0 failed** (new durable guard) |

Pre-existing, unrelated, disclosed (identical to the Media AMSC W3 disclosure, reproduced on the
clean pre-wave HEAD by stashing the wave):

- `TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` and
  `Recovery_sot_sync_001...` pin a frozen 16-module `structureLock.certifiedModules` list — red at
  the starting HEAD, untouched here.
- `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy...` — Catalog Contracts
  namespace debt (`Tooba.Catalog.Contracts/Cart/` files declare `Tooba.Catalog.Contracts`), part of
  the disclosed pre-existing Architecture-namespace failures. Catalog is untouched by this task.

No guard weakened; no open-ended repair loop entered.

## 4. Post-migration audit highlights

- Foreign Application/Infrastructure/Domain coupling: **ZERO** (guard-pinned).
- Raw `notification.*` code literals outside the declaration file: **ZERO** (guard-pinned).
- Validators reference only `NotificationValidationCodes`, never business codes (guard-pinned).
- Transport codes not registered as catalog descriptors (guard-pinned).
- `ex.Message` classification / `Results.Json` / local mappers: ZERO (unchanged from W0).

`MIGRATE_COMPLETE` / `READY_FOR_STRUCTURE`. Structure-Handoff-State = REQUIRED (W2 normalizes
Application to capability-first Customer/Seller).
