# service-location — TB-TMAR-HOST-MESSAGING-AMC-001

## Runtime locator (InProcessIntegrationEventPublisher)

| Check | Finding |
|---|---|
| Holds `IServiceProvider` | YES |
| `GetServices(handlerType)` + reflection Invoke | YES |
| Production registration possible | NO — composition throws unless Testing |
| Production can reach type | NO under fail-closed selection |
| Tests rely on it | YES (Outbox/Payment foundation helpers) |
| Classification | `EXPLICIT_TESTING_ONLY_DISPATCH_DOUBLE` — acceptable for analyze; W1 does not require redesign |

Reflection notes: `MakeGenericType` / `MethodInfo.Invoke`; exceptions unwrap via awaited Task (TargetInvocationException risk if sync throw before Task — document for CERT). Handler ordering = DI registration order; zero handlers = success with metric increment (parity gap vs production receive which may differ).

## Composition callbacks (MessagingRegistration)

`sp.GetRequiredService` / `context.GetRequiredService` inside DI/`AddMassTransit` callbacks = **ACCEPTABLE_FRAMEWORK_COMPOSITION** — not application runtime service locator.
