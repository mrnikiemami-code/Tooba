# TB-TMAR-NOTIFICATION-AMSC-001-W3-R2 — folder-granularity

Per `.cursor/skills/tooba-architecture-structure/SKILL.md` §8 (single-file leaf folder rule) and §11
(depth rules), counting production source files, not declared types.

## Before

| Leaf folder (under Customer/Seller) | Production files | Verdict |
| --- | --- | --- |
| `Customer/Commands/MarkCustomerNotificationRead/` | 2 (request + validator) | use-case leaf, unjustified |
| `Customer/Commands/MarkAllCustomerNotificationsRead/` | 1 | SINGLE-FILE use-case leaf |
| `Customer/Commands/DismissCustomerNotification/` | 2 (request + validator) | use-case leaf, unjustified |
| `Customer/Queries/ListCustomerNotifications/` | 2 (request + validator) | use-case leaf, unjustified |
| `Customer/Queries/GetCustomerUnreadNotificationCount/` | 1 | SINGLE-FILE use-case leaf |
| `Seller/Commands/MarkSellerNotificationRead/` | 2 (request + validator) | use-case leaf, unjustified |
| `Seller/Commands/MarkAllSellerNotificationsRead/` | 1 | SINGLE-FILE use-case leaf |
| `Seller/Commands/DismissSellerNotification/` | 2 (request + validator) | use-case leaf, unjustified |
| `Seller/Queries/ListSellerNotifications/` | 2 (request + validator) | use-case leaf, unjustified |
| `Seller/Queries/GetSellerUnreadNotificationCount/` | 1 | SINGLE-FILE use-case leaf |

- singleFileRequestLeafBefore = **4**
- perUseCaseRequestLeafBefore = **10** (all ten rejected: §9 exception not met — no policy/mapper/
  multi-responsibility cohesion; request + colocated validator does not justify a folder)
- Folder-Granularity-State before = `OVER_FOLDERED`
- No complexity justification was recorded for any leaf.

## After

| Axis | Child directories | Production files directly colocated |
| --- | --- | --- |
| `Customer/Commands/` | 0 | 5 (3 requests + 2 validators) |
| `Customer/Queries/` | 0 | 3 (2 requests + 1 validator) |
| `Seller/Commands/` | 0 | 5 (3 requests + 2 validators) |
| `Seller/Queries/` | 0 | 3 (2 requests + 1 validator) |

- singleFileRequestLeafAfter = **0**
- perUseCaseRequestLeafAfter = **0**
- Folder-Granularity-State after = `PROFESSIONAL_SHALLOW`
- Shared capability folders unchanged: `Composition/`, `Models/`, `Ports/`, `Rendering/`, `Validators/`.
- Stale leftover `Customer/Validators/` and `Seller/Validators/` empty shells deleted.

Durable enforcement: `NotificationModuleAmsc001W3R2StructureRepairGuardTests` proves ZERO child
directories under the four axes, exact colocated file lists, absence of all ten stale leaf paths,
and the `Commands/Queries`-only branch shape.
