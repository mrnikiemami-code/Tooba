# migration-plan — TB-TMAR-HOST-ERRORS-AMC-001

## Recommended waves (2)

### W1 — ~12–15 min (hard ≤20)

1. Rehome `TenantResolutionMiddleware.WriteProblemAsync` to canonical `IExceptionPresentationService.WriteAsync` (or equivalent SafeErrorMapper+ApiResponseFactory write) preserving FailClosed 404 `platform.resolution.failed` behavior.
2. Delete `PlatformExceptionMapper.cs` (`PlatformExceptionMapper` + `MappedPlatformError`).
3. Rename `ToobaExceptionHandler` namespace to exact `Tooba.Host.Errors`; update Program usings/registration as needed.
4. Remove/retarget `PlatformExceptionMapperTests`; keep `ErrorContractTests` integration.
5. Update Offer path/namespace guard if it asserts namespace text.
6. Focused Host/Host.Tests validation only.

**Do not** redesign ExceptionPresentationService / ApiResponseFactory / catalogs.

### W2 — CERT (~10–12 min)

Certify Host/Errors as thin global exception boundary (`ToobaExceptionHandler` only, count 1 after delete), durable guard, SoT labels. Production repair expected NONE if W1 clean.

## Rejected alternate

Blind delete of mapper without MultiTenancy rehome → **BLOCKED** (would break tenant fail-closed ProblemDetails write).

## Out of scope

MultiTenancy FailClosed hard-coded `"Not Found"` title; BuildingBlocks redesign; reservation.policy catalog debt; Security/Admin reopen.
