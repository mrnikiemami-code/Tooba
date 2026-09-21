# Support audit — TB-TMAR-NEXT-MODULE-BATCH-003

## Pre → Post

| Defect | Repair |
|---|---|
| Infra → Notification.Application | `INotificationCreationPort` + `NotificationSemanticTypes.SupportAdminReply` |
| Root dump / god SupportTicket | Aggregates + Entities + ValueObjects split |
| Oversized SupportDirectory | Remains orchestration; moved under Directories with IClock/IIdGenerator |
| UuidV7 / UtcNow | `_ids.NewId()` / `_clock.UtcNow` |
| Localized exception prose | Stable `support.*` codes |

## Notification boundary
CONTRACTS_ONLY — no Application/Domain/Infrastructure refs.

## CQRS / Endpoint
Directory/ports; Host owns HTTP. Endpoint-State: NOT_APPLICABLE.
