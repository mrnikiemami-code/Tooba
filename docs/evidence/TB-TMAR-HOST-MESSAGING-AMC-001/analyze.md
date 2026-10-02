# analyze — TB-TMAR-HOST-MESSAGING-AMC-001

## Mode

ANALYSIS_ONLY — production change ZERO.

## Exact tree

```text
src/backend/Host/Tooba.Host/Messaging/
  InProcessIntegrationEventPublisher.cs
  MassTransitIntegrationEventPublisher.cs
  MessagingDisabledPublisher.cs
  MessagingHostOptions.cs
  MessagingRegistration.cs
  MessagingRetryConfigurator.cs
```

| Metric | Value |
|---|---|
| Production `.cs` count | 6 |
| Top-level production types | 7 |
| Nested types | 0 |
| Namespace (current) | `Tooba.Host` (all files) |
| Path-derived namespace | `Tooba.Host.Messaging` |
| Path↔namespace | **VIOLATION** |

## Types

| Type | File | Visibility | Role |
|---|---|---|---|
| `InProcessIntegrationEventPublisher` | InProcess… | internal sealed | Testing-only in-process handler dispatch |
| `MassTransitIntegrationEventPublisher` | MassTransit… | internal sealed | IIntegrationEventPublisher → MassTransit IBus adapter |
| `MessagingDisabledPublisher` | MessagingDisabled… | internal sealed | Fail-closed publisher when messaging off |
| `MessagingHostOptions` | MessagingHostOptions.cs | internal sealed | `Tooba:Messaging` options |
| `MessagingOptionsValidator` | MessagingHostOptions.cs | internal sealed | Startup options validator |
| `MessagingRegistration` | MessagingRegistration.cs | internal static | Composition + publisher selection |
| `MessagingRetryConfigurator` | MessagingRetry… | internal static | Consumer retry policy |

## Verdict (summary)

- Host/Messaging is the correct process-level composition root for SQL Transport + publisher selection.
- Path↔namespace MUST become `Tooba.Host.Messaging`.
- `MessagingHostOptions.cs` holds two types → cohesion **MUST_SPLIT** in W1.
- In-process `IServiceProvider`/`GetServices` is **Testing-gated** explicit double debt/seam — not production locator; classify `KEEP_AS_EXPLICIT_TEST_ONLY_HOST_DOUBLE`.
- Composition-time `sp.GetRequiredService` / MassTransit `context.GetRequiredService` = acceptable DI composition callbacks (not runtime app locator).
- Health→`MessagingHostOptions` + `IBusControl` = adjacent Host platform dependency (CERT preserved; no Messaging internals import).
- Recommended: **W1** (namespace + options/validator split + usings) → **W2-CERT**.
- Protected: Health/MultiTenancy/Errors/Security/Admin CERT **PRESERVED**.
- Implementation SHA unchanged: `ba8db8c6bcb22f0ad4c386073d3b612e3d318e00`
