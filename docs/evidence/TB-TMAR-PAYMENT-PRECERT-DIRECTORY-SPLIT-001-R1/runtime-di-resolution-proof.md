# TB-TMAR-PAYMENT-PRECERT-DIRECTORY-SPLIT-001-R1 — runtime DI resolution proof

## Objective (single)

Repair the focused DI proof only. The parent Task PASS required "four ports resolve to four focused
implementations", but the parent test `Directory_ports_resolve_to_distinct_focused_implementations`
only inspected `IServiceCollection` descriptors — it never built a provider, created a scope, or
resolved anything at runtime.

No Payment production behavior was changed. No validators, no structure certification, no folder
reorganization, no contract/event/schema/migration change, no Host ownership change, no
Cart/Order/StoreContext/Offer change, Checkout not resumed, frontend untouched.

## Exact test

`src/backend/Modules/Payment/Tooba.Payment.Tests/Behavior/PaymentPrecertHygieneTests.cs`

`Directory_ports_resolve_to_distinct_focused_implementations`

It now, in addition to the preserved descriptor and single-port reflection assertions:

1. Builds the real `ServiceProvider` from the Payment module registration
   (`services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true })`).
2. Creates a real `IServiceScope` (`provider.CreateScope()`).
3. Resolves all four ports from runtime DI, **admin first** so a broken
   `PaymentAdminDirectory -> IPaymentDirectory -> PaymentAdminDirectory` cycle would surface as a
   real circular-dependency failure instead of passing on registrations alone.
4. Asserts exact concrete runtime types.
5. Asserts the four resolved instances are four distinct objects **and** four distinct runtime types.
6. Asserts scoped lifetime: `Assert.Same` within one scope, and `Assert.NotSame` in a fresh scope.

### Test-only service registrations added (test harness, not production)

The graph needs the same stand-ins a Host test host would supply. None are production code:
`IHostEnvironment` (EnvironmentStub), `ICurrentCommerceContext` (FixedCurrentCommerceContext),
`IDatabaseConnectionResolver`, `IIntegrationEventSerializer` (noop), `IModuleCallTracer`,
`OutboxSaveChangesInterceptor`, `IClock`, `IIdGenerator`, `IPayableCheckoutReader`
(PermissivePayableCheckoutReader), `ICommerceHoldPolicySource`, `IWalletOrderPaymentPort`,
`IOutboxPollTargetSource`, `IWorkerCommerceContextFactory`, `IBackgroundWorkerRegistry`.

## Exact four runtime types

| Port | Resolved concrete runtime type |
| --- | --- |
| `IPaymentDirectory` | `PaymentDirectory` |
| `IPaymentReconciliationDirectory` | `PaymentReconciliationDirectory` |
| `IPaymentAdminDirectory` | `PaymentAdminDirectory` |
| `IPaymentExpiryDirectory` | `PaymentExpiryDirectory` |

## Focused validation result

`dotnet test ... --filter "FullyQualifiedName~Directory_ports_resolve_to_distinct_focused_implementations"`

`Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1`

## Project build result

`dotnet build src/backend/Modules/Payment/Tooba.Payment.Tests/Tooba.Payment.Tests.csproj --no-restore`

`Build succeeded. 0 Error(s)`

## Production DI change

`Production-DI-Change-State = UNCHANGED`. `PaymentModule.cs` was not edited. The runtime-resolution
test proved the existing four focused scoped registrations already resolve correctly, so the
no-production-change rule applied.

## Construction cycle

NONE. `Func<IPaymentAdminDirectory>` inside `PaymentDirectory` still breaks the core↔admin cycle.

## Payment -> Host / residue / certification

- `Payment -> Host` = ZERO
- Host Payment residue = exactly the two approved thin security adapters
- Payment structure certification = still `PENDING_TB_TMAR_PAYMENT_ARCH_COMPLETE_002_STRUCTURE_001`
- Checkout = `PAUSED_AT_SAFE_W5_CHECKPOINT`; `frontendFrozen = true`
- Validation coverage is NOT claimed here.

## Recovery state

- Parent `paymentPrecertDirectorySplit`: `parentAcceptance = ACCEPTED_AFTER_R1_RUNTIME_DI_RESOLUTION_PROOF`,
  `runtimeDiResolution = PROVEN`.
- New `paymentPrecertDirectorySplitR1` block records the R1 objective, resolved ports, `constructionCycle = NONE`,
  `productionDiChanged = false`, and the still-PENDING certification.
- `nextTask = TB-TMAR-PAYMENT-PRECERT-VALIDATION-001`,
  `nextTaskGate = NEXT_TMAR_WAVE_AFTER_PAYMENT_PRECERT_DIRECTORY_SPLIT_R1`.
- `TOOBA-TMAR-MASTER-RECOVERY.md`, `TOOBA-ARCHITECT-BOOTSTRAP.md` and the durable guard updated to match.
