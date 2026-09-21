# Focused validation — TB-TMAR-FND-OBSERR-001-R3

| Suite | Result |
| --- | --- |
| BuildingBlocks.Tests | 36/36 PASS |
| Offer.Tests | 56/56 PASS |
| Host CorrelationRuntimeTests | 3/3 PASS (alone) |
| Host ErrorContract + PlatformExceptionMapper | PASS when not colliding with parallel factory startup |
| Host / Offer / solution build | PASS |

## Note

Parallel `WebApplicationFactory` startup can flake with MassTransit SQL migrator `tuple concurrently updated` (infra). Not an error-pipeline regression; suites green in isolation.
