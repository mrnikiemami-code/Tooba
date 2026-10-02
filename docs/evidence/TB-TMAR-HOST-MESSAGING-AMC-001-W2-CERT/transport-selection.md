# transport-selection — TB-TMAR-HOST-MESSAGING-AMC-001-W2-CERT

Fail-closed decision tree certified in `MessagingRegistration.AddToobaIntegrationPublisher`:

| Condition | Result |
|---|---|
| UseInProcessTestDouble + Testing | InProcessIntegrationEventPublisher |
| UseInProcessTestDouble + non-Testing | throws before registration |
| Enabled | MassTransit SQL Transport + MassTransitIntegrationEventPublisher |
| otherwise | MessagingDisabledPublisher (throws on publish) |

RabbitMQ registration: ZERO. Endpoint: `tooba-integration`. SQL Transport CreateDatabase=false / CreateInfrastructure=true / WaitUntilStarted / StopTimeout 30s.
