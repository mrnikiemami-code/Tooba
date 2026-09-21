# Tax audit — TB-TMAR-REFBATCH-TP-001

Six projects remain: Domain, Application, Contracts, Infrastructure, Endpoints, Tests.

| Check | Result |
| --- | --- |
| Namespaces match ownership | Repaired. `TaxOutcome` moved from namespace `Tooba.Tax.Domain` in the Contracts assembly to `Tooba.Tax.Contracts`. |
| TypeForwardedTo | Removed. |
| Domain → Contracts | Removed. |
| Domain HTTP / persistence / Host | None. |
| Foreign Domain implementation refs | None. |
| Time / id | `TaxDirectory` uses `IClock` and `IIdGenerator`. Aggregates receive ids. No `DateTimeOffset.UtcNow`, `Guid.NewGuid`, or `UuidV7.New` in production. |
| MediatR | Not added. Tax has no module-owned HTTP use case. `MapTaxModule` is a composition point only. |
| Application DbContext / Host / foreign Application | None. |
| Contracts leakage | `ITaxCalculator`, `ITaxQueryGateway`, `ITaxSchemaMigrator` are ports. |
| Infrastructure ownership | `TaxDbContext` stays in Tax.Infrastructure. |
| Cross-module SQL / FK | None. Offer id is a value, not an FK. |
| Endpoints | Thin. No try/catch, Accept-Language, ProblemDetails, or bilingual switch. |
| Host aggregate mutation / DbContext | Production Host no longer names `TaxDbContext`. Reads and seeds use `ITaxQueryGateway`. Schema migrate uses `ITaxSchemaMigrator`. |
| HTTP semantic errors | No Tax HTTP route. Calculation failures return `TaxOutcome`, not exceptions. No ceremonial resx. |
| Tracing | No cross-module call inside Tax. No module-call tracer added. |

Persian XML documentation remains on older types. It is not a user-facing error surface. Exception strings that were Persian were replaced with stable English codes.
