# 06 — Permissions + seed

Task: TB-P06-T026

## PermissionCatalog

| Id | Module | Delegable |
|----|--------|-----------|
| wallet.view | Wallet | false |
| wallet.adjust | Wallet | false |
| giftcard.view | Wallet | false |
| giftcard.manage | Wallet | false |

Reconciled via existing `AccessControlDirectory.EnsureBootstrapAsync` catalog growth path.

## Development seed

- Host: `WalletDevelopmentSeedHost` after Support seed
- Customer actor: `aaaaaaaa-aaaa-4aaa-8aaa-000000000009`
- Migration: `20260827180000_InitialWallet`
- Snapshot: `WalletDemoSnapshotStore` / `GET /v1/admin/wallet/demo-preview`
- Demo unused code (preview only): `TOOBA-DEMO-GIFT-500K`
