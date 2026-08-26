# 15 — Shutdown / restart recovery (TB-P06-T003-R1)

| Concern | Implementation |
|---|---|
| Bus lifecycle | `MassTransitHostOptions.WaitUntilStarted = true` |
| Stop timeout | `StopTimeout = 30s` |
| Outbox dispatcher | `OutboxDispatcherHostedService` honors cancellation |
| Pending outbox | resumes poll on restart (durable store) |
| Transport | SQL transport receivers stop with bus |

Normal shutdown should not corrupt outbox rows; pending messages remain for dispatcher.
