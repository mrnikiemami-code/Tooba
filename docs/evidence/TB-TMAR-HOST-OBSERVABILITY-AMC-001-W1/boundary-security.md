# boundary-security — TB-TMAR-HOST-OBSERVABILITY-AMC-001-W1

| Boundary | State |
|---|---|
| Authentication | consume `CurrentAuthenticatedSession` only; no authenticate/authorize |
| Tenancy | consume `ICurrentCommerceContext` only; no resolve/mutate |
| Contracts | BuildingBlocks Observability + Host session/commerce only |
| Foreign Application | **ZERO** |
| Foreign Infrastructure | **ZERO** |
| Foreign Domain | **ZERO** |
| Foreign DbContext | **ZERO** |
| Business authority | **ZERO** |
| Exception catch/swallow | **ZERO** (`_next` propagates) |
| Custom ActivitySource / Meter | **ZERO** |
| Hard-coded user-facing text | **ZERO** |
| QueryString / RawTarget / full URL / headers logged | **ZERO** |

Protected certifications preserved: Messaging / Health / MultiTenancy / Errors / Security / Admin.
