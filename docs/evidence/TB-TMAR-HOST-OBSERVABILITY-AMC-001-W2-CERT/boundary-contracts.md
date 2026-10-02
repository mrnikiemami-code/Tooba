# boundary-contracts — TB-TMAR-HOST-OBSERVABILITY-AMC-001-W2-CERT

| Boundary | State |
|---|---|
| Ownership | GLOBAL_HOST_OBSERVABILITY_PLATFORM_CERTIFIED (Host-owned; not BuildingBlocks) |
| Authentication | consume `CurrentAuthenticatedSession` only |
| Tenancy | consume `ICurrentCommerceContext` only; no mutate/resolve |
| Correlation | CANONICAL_CERTIFIED (`ICorrelationIdProvider` + Items fallback + Ensure) |
| StoreId | TENANTID_LOGGING_MIRROR_CERTIFIED (logging dimension only) |
| Contracts | BuildingBlocks Observability + Host session/commerce |
| Foreign Application / Infrastructure / Domain / DbContext | ZERO |
| Business authority | ZERO |
| Active consumer | Program single registration |
| Exception propagation | no catch; scope disposes |
| Logger | BeginScope only |
