# COMPLETION-LEDGER — TB-282-R9

| Item | Production change | Before → after | Proof |
| --- | --- | --- | --- |
| InMemory recovery | AccessControl Contracts + Infrastructure | skip-bootstrap empty engine → 403 | Host log users=3; Story admin 200 |
| Tenant member | projected from platform-admin assignment | Snapshot alone insufficient | AdminPanelAccess Allow |
| Party member | projected from seller-owner assignment | empty → seller denied | Seller Story 200 assigned |
| Capability sync | SyncUserCapabilityTuplesAsync | process-local only | directory path |
| Host seam | Program skip-bootstrap branch | no EnsureBootstrap | Program.cs |
| Tests | 3 behavioral facts | — | Host.Tests 3/3 |
| ZERO durable grant | assignments stay 3 | — | pg count |
