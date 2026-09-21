# Notification audit — TB-TMAR-NEXT-MODULE-BATCH-003

## Pre → Post

| Defect | Repair |
|---|---|
| Domain → Contracts project reference | Removed; Domain owns `ValueObjects.NotificationRecipientKind` |
| Domain used Contracts DTO enum | Explicit mapping in Application (`NotificationRecipientKindMapping`) |
| UuidV7.New / UtcNow | Caller-provided id via `IIdGenerator`; orchestration uses `IClock` |
| Localized exception prose | Stable `notification.*` codes |
| Infra → Payment/Fulfillment/Returns.Application | Extracted events to owning `*.Contracts/Events`; Infra refs Contracts only |
| Root-dumped sources | Folders + namespaces aligned (Aggregates/Ports/Directories/…) |

## Public boundary
Cross-module creation: `INotificationCreationPort` + `CreateNotificationCommand` in Contracts only.

## CQRS / Endpoint
Directory/ports pattern; Host owns HTTP. Endpoint-State: NOT_APPLICABLE (no module Endpoints project).

## SoftDelete idempotency
SoftDeleteAsync now returns true when already soft-deleted (ownership verified), matching MarkRead idempotency semantics without changing successful first-delete behavior.
