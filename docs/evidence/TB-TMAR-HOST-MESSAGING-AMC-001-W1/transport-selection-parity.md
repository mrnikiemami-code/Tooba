# transport-selection-parity — TB-TMAR-HOST-MESSAGING-AMC-001-W1

Fail-closed selection preserved exactly:

| Condition | Publisher |
|---|---|
| UseInProcessTestDouble = true (+ Testing) | InProcessIntegrationEventPublisher |
| messaging Enabled = true | MassTransitIntegrationEventPublisher (+ SQL Transport) |
| otherwise | MessagingDisabledPublisher (throws; no silent drop) |

Also preserved:

- endpoint name `tooba-integration`
- retry Immediate(2) + 5s/15s/30s
- CanonicalTransport = PostgreSql
- RabbitMQ absent from registration
- MassTransit 8.5.10 SQL Transport topology
- disabled publisher fail-closed throw
