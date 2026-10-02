# analyze — TB-TMAR-HOST-OBSERVABILITY-AMC-001

## Mode

ANALYSIS_ONLY — production change ZERO.

## Exact tree

```text
src/backend/Host/Tooba.Host/Observability/
  RequestObservabilityEnrichmentMiddleware.cs
```

| Metric | Value |
|---|---|
| Production `.cs` count | 1 |
| Top-level production types | 1 |
| Nested types | 0 |
| Namespace (current) | `Tooba.Host` |
| Path-derived namespace | `Tooba.Host.Observability` |
| Path↔namespace | **VIOLATION** |
| Visibility | `internal sealed` |

## Type disposition

| Type | Disposition |
|---|---|
| RequestObservabilityEnrichmentMiddleware | **KEEP_AS_GLOBAL_HOST_OBSERVABILITY_PLATFORM** |

Rationale: Host HTTP pipeline enrichment after Tenant + Session so log scope can include tenant/store/actor; consumes only BuildingBlocks + Host auth/commerce platform seams; zero business authority.

## Consumers

- `Program.cs`: `app.UseMiddleware<RequestObservabilityEnrichmentMiddleware>()` exactly once (after SessionAuthenticationMiddleware).
- Tests: no dedicated Host/Observability guard; OfferArchitectureGuard asserts Program contains the type name; foundation tests cover ObservabilityLogScope keys.

## Protected certifications preserved

HOST_MESSAGING / HEALTH / MULTITENANCY / ERRORS / SECURITY / ADMIN — untouched (docs-only analyze).
