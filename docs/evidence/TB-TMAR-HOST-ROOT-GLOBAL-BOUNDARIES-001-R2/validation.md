# TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R2 — Validation

Skill: `tooba-architecture-certify` (validated against ARCH-COMPLETE-002 boundary rules).

## 1. Focused build

Command:

```text
dotnet build src/backend/Tooba.slnx --nologo -v m
```

Result: **Build succeeded. 0 Error(s).**

Covers the required focused set: `Order.Contracts`, `Order.Application`, `Order.Infrastructure`,
`Payment.Contracts`, `Host`, `Host.Tests`, and all modified Contracts projects
(`Payment.Contracts`, `Catalog.Contracts`, `AccessControl.Contracts`).

## 2. Focused tests

| Filter | Result |
| ------ | ------ |
| `HostRootGlobalBoundariesGuardTests` | PASS |
| `UnpaidOrderExpiryTests` | PASS |
| `ReservationCycleFoundationTests` | PASS |
| `CommerceHoldPolicySourceTests` | PASS |
| `OrderInfrastructureForeignLayerBoundaryGuardTests` (new durable guard) | **4 / 4 PASS** |
| Host focused set total | **26 / 26 PASS** |

Command:

```text
dotnet test src/backend/Host/Tooba.Host.Tests --filter "FullyQualifiedName~HostRootGlobalBoundariesGuardTests|FullyQualifiedName~UnpaidOrderExpiryTests|FullyQualifiedName~ReservationCycleFoundationTests|FullyQualifiedName~CommerceHoldPolicySourceTests"
dotnet test src/backend/Modules/Order/Tooba.Order.Tests --filter "FullyQualifiedName~OrderInfrastructureForeignLayerBoundaryGuardTests"
```

`PaymentFoundationTests` (included in the focused Host run earlier) also passes after the assertion
was aligned to the canonical Contracts-only boundary.

## 3. Boundary verification (independent command evidence)

```text
Tooba.Order.Infrastructure.csproj foreign Application/Infrastructure/Domain ProjectReference:
  (only Order.Application / Order.Contracts remain)          -> ZERO foreign

.cs files under Order.Infrastructure with foreign Application/Infrastructure/Domain FQN/import:
  (no matches)                                                -> ZERO

Tooba.Payment.Contracts.csproj in Order.Infrastructure.csproj -> PRESENT (explicit direct)
Tooba.Payment.Application.csproj in Order.Infrastructure.csproj -> ABSENT

Payment.Infrastructure project reference to Catalog -> Catalog.Contracts only
```

## 4. Pre-existing failures (parity with `main`, not introduced by R2)

| Test | State on `main` | State on R2 | Note |
| ---- | --------------- | ----------- | ---- |
| `PaymentArchitectureGuardTests.Payment_endpoints_cqrs_and_host_ownership_are_enforced` | FAIL (line ~207, `HostPaymentStorefrontAuthorizer.cs` missing) | FAIL (identical) | Not touched by R2; pre-existing Host-evacuation debt already present on `main` |
| `HostOrderReverseAuditGuardTests` | pre-existing failures | unchanged | documented in R1 evidence |

`Tooba.Order.Tests` is pre-existing broken on `main` for non-guard tests; the R2 guard tests
pass 4/4.

## 5. Behavior parity checks

| Check | State |
| ----- | ----- |
| Six Host root files absent (all six) | CONFIRMED |
| Settlement global-usings (any file) | ZERO |
| `Tooba:UnpaidOrderExpiry` `Enabled=true`, `PollIntervalSeconds=15`, `BatchSize=20` | UNCHANGED |
| worker name `unpaid-order-expiry` | UNCHANGED |
| metric `tooba.unpaid_expiry.expired` | UNCHANGED |
| hold precedence method > store > gateway | UNCHANGED |
| `IStorefrontShippingDraftStore` DI registration | RESTORED (accidental deletion repaired) |
| schema / migration | NONE |
| routes / frontend | UNCHANGED |
| Authorization (`Host/Authorization` absent; AccessControl 7-file slice present) | PRESERVED |

## 6. Repair iterations

`MAX_REPAIR_ITERATIONS=1` — one repair iteration used to restore the accidentally-deleted
`IStorefrontShippingDraftStore` DI registration. No ambiguous second failure encountered.
