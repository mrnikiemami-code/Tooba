# migration-plan — TB-TMAR-HOST-HEALTH-AMC-001

## Recommended wave count: 2

### W1 — Structure + DI + disclosure (~12–15 min)

- Namespace → exact `Tooba.Host.Health` (both files)
- Remove `IServiceProvider`; inject `IBusControl?` **or** introduce thin `IHostMessagingReadiness` in Host/Messaging returning safe labels only
- Sanitize disclosure: replace `missing-reference:{reference}` with generic `missing-reference` (or hashed/opaque); omit or redact `messaging-schema` from public JSON
- Optionally Active-only tenant connection refs for readiness (document choice vs startup collect)
- Optional try/catch → not-ready labels for probe/bus throws
- Add `HostHealthAmcW1GuardTests`
- Focused: HostReadinessBoundaryGuardTests + new W1 guard + smoke readiness tests

Preserve: 4 routes, liveness semantics, CORS on live aliases, Results.Json operational shape, AccessControl Contracts seam.

### W2-CERT (~10–12 min)

- Durable cert guard; SoT `HOST_HEALTH_AMC_CERTIFIED`
- No redesign beyond W1

## Protected

HOST_MULTITENANCY / ERRORS / SECURITY / ADMIN certifications; frontend frozen; Checkout paused.
