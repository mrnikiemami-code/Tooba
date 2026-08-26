# 09 — Retry / redelivery policy (TB-P06-T003-R1)

## Consumer (MassTransit SQL Transport)

`MessagingRetryConfigurator.ApplyConsumerRetry`:

- Immediate: 2
- Intervals: 5s, 15s, 30s
- No infinite retry

## Outbox dispatcher

- Base delay × 2^(attempt-1), cap attempt 8
- `MaxAttempts = 5` → dead-letter in outbox store

## Classification

| Type | Handling |
|---|---|
| Transient | consumer retry then outbox retry (layer-specific) |
| Business rejection | exception propagates; no silent swallow |
| Poison | consumer exhausts retries → SQL transport error path |
| Config/permanent | fail-fast at startup via options validators |
