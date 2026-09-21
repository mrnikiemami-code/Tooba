# behavior-preservation-audit

Task: TB-TMAR-NEXT-MODULE-BATCH-002-R1
Baseline: `2a7e171eff38a54ec81a403689393c088cf7ad73` (pre BATCH-002)
Compared to: HEAD after R1 repairs (post BATCH-002 physical split + Contracts boundary)

Method: `git diff` / marker scan of WalletDirectory, WalletPaymentGateway, PaymentDirectory, webhook inbox vs baseline paths
(`.../WalletDirectory.cs`, `.../WalletPaymentGateway.cs` at baseline; current under Directories/Providers).

Classification key:
- **structural-only** — move/split/namespace/folder; logic unchanged
- **intentional infrastructure abstraction** — Contracts ports, IClock/IIdGenerator, stable codes (BATCH-002)
- **intentional behavior change** — none required by R1 except dependency surface
- **accidental behavior change** — none found (would repair)

## Wallet

| Flow | Classification | Parity |
|------|----------------|--------|
| Get/Create wallet account | structural-only (+ IClock/IIdGenerator intentional) | PRESERVED |
| gift-card issue | structural-only | PRESERVED |
| gift-card redeem | structural-only; notification via Contracts port | PRESERVED (kind/copy/recipient/idempotency/route) |
| gift-card revoke | structural-only | PRESERVED |
| admin adjustment | structural-only; notification Contracts | PRESERVED |
| order-payment debit | structural-only; Serializable + idempotency key retained | PRESERVED |
| refund credit | structural-only; `wallet-refund-credit:{id}` key retained | PRESERVED |
| wallet quote | structural-only / Contracts port for Payment | PRESERVED |
| idempotency checks | intentional infrastructure abstraction (codes); logic same | PRESERVED |
| Serializable transaction boundaries | structural-only (still Serializable on spend/refund) | PRESERVED |
| insufficient balance | same reject code `wallet.rejected.2YXZiNis` | PRESERVED |
| currency mismatch | same reject path | PRESERVED |
| notification side effects | intentional Contracts extraction; semantic strings identical | PRESERVED |

## Payment

| Flow | Classification | Parity |
|------|----------------|--------|
| create/initiate payment | structural-only (folder split) | PRESERVED |
| success/failure transitions | structural-only; domain ApplyVerifiedSuccess/Failure | PRESERVED |
| refund path | structural-only | PRESERVED |
| manual payment flow | structural-only | PRESERVED |
| webhook verification/inbox idempotency | structural-only; provider+eventId dedup | PRESERVED |
| payment hold settings | intentional Application port absorption (BATCH-002) | PRESERVED |
| pending-payment/admin query | intentional Application port absorption | PRESERVED |
| wallet payment gateway call | intentional Wallet.Contracts + tracer; ComposeReference semantics same | PRESERVED |
| provider selection/config | structural-only | PRESERVED |
| outbox/integration event emission | structural-only (Messaging folder) | PRESERVED |

## R1-only delta vs BATCH-002 tip

Wallet notification dependency Application/Domain → Contracts. No amount/currency/idempotency/isolation changes.

## Accidental behavior changes found

None.
