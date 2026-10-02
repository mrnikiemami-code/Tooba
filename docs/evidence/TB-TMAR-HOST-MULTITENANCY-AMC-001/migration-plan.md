# migration-plan — TB-TMAR-HOST-MULTITENANCY-AMC-001

## Recommended wave count: 2

### W1 — Structure + namespace (≈12–15 min)

Files:

- Split `TenantResolutionMiddleware.cs` → `HttpCommerceContextAccessor.cs` + `TenantResolutionMiddleware.cs`
- Namespace → exact `Tooba.Host.MultiTenancy`
- Update Program usings/registrations as needed
- Keep RequestServices scoped assigner bridge (document as lifetime-required) OR introduce explicit factory only if zero behavior risk
- Update Errors guards path reads if filenames change
- Focused tests: TenantResolution* + HostErrors* guards + HostNormalizerTests

Preserve: resolution HTTP matrix, fail-closed, StoreCommerce assign pairing, DI lifetimes.

### W2-CERT — Certify Host/MultiTenancy (≈10–12 min)

- Durable structure guard (exact files, namespace, dispositions)
- Re-verify fail-closed + presentation + deps ZERO foreign layers
- SoT: HOST_MULTITENANCY_AMC_CERTIFIED (or Architect-chosen label)
- No production redesign beyond W1

## Protected

HOST_ERRORS_AMC_CERTIFIED, HOST_SECURITY_AMC_CERTIFIED, HOST_ADMIN_FULLY_CERTIFIED, frontend frozen, Checkout paused.

## Out of scope

Module recovery; Configuration folder relocation of ControlPlaneRegistry; Persistence redesign of DatabaseConnectionResolver; expanding SkipPrefixes.
