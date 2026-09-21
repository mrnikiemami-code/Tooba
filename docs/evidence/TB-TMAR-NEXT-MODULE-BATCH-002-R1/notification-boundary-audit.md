# notification-boundary-audit

Task: TB-TMAR-NEXT-MODULE-BATCH-002-R1
Baseline tip before BATCH-002: `2a7e171eff38a54ec81a403689393c088cf7ad73`

## Defect (Architect)

`Tooba.Wallet.Infrastructure` referenced `Tooba.Notification.Application` and imported `Tooba.Notification.Domain` types (via `WalletDirectory`).

## Repair

| Before | After |
|--------|--------|
| Wallet.Infra → Notification.Application | Wallet.Infra → **Notification.Contracts only** |
| `INotificationDirectory` | `INotificationCreationPort` |
| `NotificationCopy.*` wallet constants | `NotificationSemanticTypes.*` (same string values) |
| `NotificationTargetRoutes` in Application | `NotificationTargetRoutes` in Contracts |
| `CreateNotificationCommand` in Application | `CreateNotificationCommand` in Contracts |
| `NotificationRecipientKind` in Domain | Moved to Contracts.Dtos; Domain entity uses Contracts enum |

## New project

`src/backend/Modules/Notification/Tooba.Notification.Contracts/`

Folders: Commands, Copy, Dtos, Ports, Routes (root-dump=0).

## Wiring

- `NotificationDirectory` implements `INotificationDirectory` + `INotificationCreationPort`
- `NotificationModule` registers both as scoped → same instance
- No TypeForwardedTo, wrappers, or service locator

## Side effects preserved (Wallet)

Gift redeem, admin adjust, wallet payment success, refund credited — same Type strings, recipient Customer+actor, TargetRoute `/customer-panel/wallet`, SourceEventId / SourceType patterns unchanged.

## Guards

- `WalletArchitectureGuardTests.Infrastructure_references_Notification_Contracts_only`
- `NotificationContractsArchitectureGuardTests`
- Host baseline `tmar-infra-to-foreign-application.json`: removed `Wallet.Infrastructure -> Notification.Application`
