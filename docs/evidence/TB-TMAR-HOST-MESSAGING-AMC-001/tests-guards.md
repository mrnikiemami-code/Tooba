# tests-guards — TB-TMAR-HOST-MESSAGING-AMC-001

| Area | Coverage | Gap |
|---|---|---|
| Options validator / RabbitMQ forbid | MassTransitFoundationTests | OK |
| Disabled publisher type | MassTransitFoundationTests | OK |
| Production test-double rejection | composition throw path | assert present in registration; dedicated unit OK |
| In-process double | OutboxTestSupport / PaymentFoundationTests | OK for dispatch |
| MassTransit envelope/headers | MassTransitPostgresTests | OK |
| Retry / SQL transport | MassTransitPostgresTests + registration | OK |
| No RabbitMQ packages | MassTransitFoundationTests | OK |
| Path/namespace | **ABSENT durable Messaging AMC guard** | add in W1/CERT |
| Service locator prohibition (prod) | **ABSENT explicit Messaging guard** | add in W1/CERT |
| Message classification | source scan ZERO in Messaging | durable guard at CERT |

Hard-coded runtime text: `MessagingDisabledPublisher` InvalidOperationException English prose = **operator/misconfiguration** fault; unknown exceptions map via global presentation (no Message.Contains classification in Messaging).
