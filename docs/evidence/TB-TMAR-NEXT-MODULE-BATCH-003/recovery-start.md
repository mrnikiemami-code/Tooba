# Recovery start — TB-TMAR-NEXT-MODULE-BATCH-003

- Claim: e50b5d48-b7ef-4c95-81bc-20812378cde0
- Channel: 	ooba-main
- Worker: 	ooba-worker-01
- Bridge: http://127.0.0.1:17321
- Baseline tip: 8f3747593d3fd313f9bff6ea32610dac93f29492 (HEAD==origin/main)
- Parent: TB-TMAR-NEXT-MODULE-BATCH-002-R1 ACCEPTED
- Scope: Notification + Support golden recovery ONLY
- Explicit non-scope: Tax/Pricing physical; Wallet/Payment logic reopen; Checkout; frontend

## Pre-recovery defects (characterized)

### Notification
- Domain → Contracts project reference (inversion)
- Domain uses Contracts.Dtos.NotificationRecipientKind
- Domain uses UuidV7.New(); Infrastructure uses DateTimeOffset.UtcNow
- Domain/Infrastructure/Contracts exception prose (localized)
- Infrastructure references Payment/Fulfillment/Returns.Application for integration events
- Root-dumped sources in Domain/Application/Infrastructure

### Support
- Infrastructure references Notification.Application (INotificationDirectory + NotificationCopy)
- Root-dumped SupportTicket + TicketMessage + enums; oversized SupportDirectory
- UuidV7.New / DateTimeOffset.UtcNow; localized exception prose

## Behavior characterization (preserve)

Notification: CreateIfAbsent duplicate suppression; Customer/Seller recipient filters; MarkRead/MarkAllRead/SoftDelete idempotency; route allowlist; Wallet semantic types; payment.succeeded projection path; SourceEventId/SourceType; outbox registration.

Support: ticket create/reply; status transitions; Admin public reply → notification (type/route/SourceEventId/SourceType); idempotency keys.

## Strategy
1. Domain-owned recipient kind + explicit App/Infra mapping; caller-provided ids + IClock/IIdGenerator
2. Smallest event extraction to Payment/Fulfillment/Returns.Contracts; Notification consumes Contracts only
3. Support → Notification.Contracts (INotificationCreationPort + semantic type)
4. Physical folders + namespace alignment; architecture guards; focused tests; evidence; SoT update
