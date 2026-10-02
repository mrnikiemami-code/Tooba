# tests-guards — TB-TMAR-HOST-CONFIGURATION-AMC-001

## Existing

| Area | Location |
| --- | --- |
| HostNormalizer | `HostNormalizerTests` |
| PlatformOptionsValidator / StoreCommerce Production / edition | `PlatformOptionsValidatorTests` in `TenantResolutionTests.cs` |
| Tenant resolution + connection fail-closed | `TenantResolutionTests` |
| BuildRegistry usage in fixtures | Outbox/Payment/MassTransit tests |

## Gaps (non-blocking for Analyze PASS)

| Gap | Notes |
| --- | --- |
| Path/namespace Configuration folder guard | Missing until W1/CERT |
| Duplicate TenantId / host focused assert | Partially via validator throws; strengthen in W1/CERT |
| TrustedProxies fail-fast | Program silent-skip — deferred |
| Legacy ConnectionString absence of consumers | Guard optional at CERT |
| Offer.Contracts-only import guard | CERT durable |

## Guard impact this Analyze

Docs/SoT only. Durable `TmarDurableGuardTests` pointer update to Configuration Analyze stop. No production code.
