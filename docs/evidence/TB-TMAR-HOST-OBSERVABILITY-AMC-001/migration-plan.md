# migration-plan — TB-TMAR-HOST-OBSERVABILITY-AMC-001

## Recommended shape: 2 waves

### W1 — Namespace + bounded ClientIp hygiene (~12–15 min)

- Change namespace to exact `Tooba.Host.Observability`
- Program `using Tooba.Host.Observability;`
- Align ClientIp with key contract: omit raw ClientIp when TrustedProxies not configured (or only populate after forwarded-headers path); do not expand to hashing/redaction redesign
- Preserve storeId=tenantId logging convenience pending Architect Marketplace store-identity decision (document only)
- Add `HostObservabilityAmcW1GuardTests`
- Focused Host tests

### W2-CERT — Certify Host/Observability platform boundary (~10–12 min)

- Labels: `HOST_OBSERVABILITY_AMC_CERTIFIED` / `HOST_OBSERVABILITY_PLATFORM_BOUNDARY_CERTIFIED`
- Durable cert guard
- No production redesign

## Alternatives rejected for now

- Direct CERT-only: blocked by path↔namespace violation + ClientIp trusted-proxy contract mismatch
- Move to BuildingBlocks: rejected — middleware is Host pipeline composition after Host Tenant/Session seams
