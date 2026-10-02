# consumers-di — TB-TMAR-HOST-MESSAGING-AMC-001

## Registration (Program)

- `AddOptions<MessagingHostOptions>().Bind(Tooba:Messaging).ValidateOnStart()`
- `AddSingleton<IValidateOptions<MessagingHostOptions>>(MessagingOptionsValidator)`
- Bind snapshot → `AddToobaIntegrationPublisher(environment, messagingOptions)`
- OpenTelemetry adds source `MessagingRegistration.MassTransitActivitySource`

## Publisher selection (`AddToobaIntegrationPublisher`)

| Condition | Registration | Lifetime |
|---|---|---|
| `UseInProcessTestDouble` + non-Testing | throw at composition | n/a |
| `UseInProcessTestDouble` + Testing | `InProcessIntegrationEventPublisher` | Scoped |
| `Enabled` | `AddToobaMassTransitMessaging` → `MassTransitIntegrationEventPublisher` | Scoped |
| else | `MessagingDisabledPublisher` | Scoped |

## Active consumers

| Type | Production | Testing | Notes |
|---|---|---|---|
| MassTransit publisher | YES when Enabled | YES in MassTransitPostgresTests | Via OutboxDispatcher |
| In-process publisher | NEVER in Production | YES (OutboxTestSupport, PaymentFoundationTests, registration path) | Explicit double |
| Disabled publisher | YES when messaging off | MassTransitFoundationTests | Fail-closed |
| MessagingRegistration | YES | YES | Composition |
| RetryConfigurator | YES when bus built | YES | Consumer retry only |
| Options/Validator | YES | YES | ValidateOnStart |

## MassTransit receive path (owned by Host/Transport)

- Consumer: `ToobaIntegrationTransportConsumer` registered inside `AddToobaMassTransitMessaging`
- Endpoint name: `tooba-integration` (stable deployment name)
