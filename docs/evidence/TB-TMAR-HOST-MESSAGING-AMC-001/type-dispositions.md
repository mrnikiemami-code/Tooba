# type-dispositions — TB-TMAR-HOST-MESSAGING-AMC-001

| Type | Disposition | Rationale |
|---|---|---|
| `InProcessIntegrationEventPublisher` | `KEEP_AS_EXPLICIT_TEST_ONLY_HOST_DOUBLE` | Registered only when `UseInProcessTestDouble` + `Testing`; reflection/`IServiceProvider` intentional test dispatch |
| `MassTransitIntegrationEventPublisher` | `KEEP_AS_THIN_HOST_TRANSPORT_ADAPTER` | Thin Outbox→MassTransit adapter; no business logic |
| `MessagingDisabledPublisher` | `KEEP_AS_GLOBAL_HOST_MESSAGING_PLATFORM` | Fail-closed when messaging disabled; no silent drop |
| `MessagingHostOptions` | `KEEP_AS_GLOBAL_HOST_MESSAGING_PLATFORM` | Host deployment messaging options |
| `MessagingOptionsValidator` | `KEEP_AS_GLOBAL_HOST_MESSAGING_PLATFORM` | Startup validation; file cohesion split recommended |
| `MessagingRegistration` | `KEEP_AS_GLOBAL_HOST_MESSAGING_PLATFORM` | Composition root for bus + publisher selection |
| `MessagingRetryConfigurator` | `KEEP_AS_GLOBAL_HOST_MESSAGING_PLATFORM` | Global consumer retry topology for SQL Transport |

No `DEAD_ZERO_CONSUMER_RESIDUE`. No `MOVE_TO_*` required in analyze.
Folder cohesion note: `MessagingHostOptions.cs` = **MUST_SPLIT** (options vs validator) under path/namespace repair.
