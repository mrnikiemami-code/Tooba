# correlation-context — TB-TMAR-HOST-OBSERVABILITY-AMC-001

## Correlation authority

Canonical SSOT: `ICorrelationIdProvider` + `CorrelationIdContext` set by `CorrelationIdMiddleware` (BuildingBlocks).

HttpContext.Items[`CorrelationIdMiddleware.HttpContextItemKey`] is documented as optional mirror, not SSOT.

## Enrichment middleware resolution

```text
correlationIdProvider.GetCorrelationId()
  ?? Items[HttpContextItemKey]
  ?? correlationIdProvider.EnsureCorrelationId()
```

| Concern | Classification |
|---|---|
| Competing CorrelationId creation | **DEFENSIVE_FALLBACK_NOT_PRIMARY_AUTHORITY** |
| Items fallback | Defensive mirror read; duplicates foundation semantics only if provider empty after Correlation middleware |
| EnsureCorrelationId | Last-resort; should be rare if pipeline order preserved |
| State | **ACCEPTABLE_DEFENSIVE_RECOVERY** (not architectural redesign debt for Analyze) |

## Commerce / store / actor

| Field | Source | Notes |
|---|---|---|
| tenantId | `ICurrentCommerceContext.Current?.Tenant?.TenantId.Value` | Neutral BuildingBlocks commerce seam; Marketplace Tenant-null → null |
| storeId | `= tenantId` | Logging dimension only; comment: Single-Store durable store identity equals TenantId when no separate StoreId; Marketplace with null tenant → null storeId; ObservabilityLogScope defines StoreId as independent optional key |
| actorId | `CurrentAuthenticatedSession.UserId` as Guid `"N"` | Host auth platform dependency acceptable; no username/email/phone/token |

StoreId semantics for Marketplace: **LOGGING_ONLY_NOT_BUSINESS_AUTHORITY**; equating storeId to tenantId is Single-Store-oriented logging convenience and must not be treated as Marketplace seller/store business identity.
