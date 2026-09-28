# TB-TMAR-AUTHORIZATION-POSTCERT-CLEANUP-001 — Validation

## 1. Focused builds

| Project | Result |
| ------- | ------ |
| `Tooba.AccessControl.Contracts` | Build succeeded, 0 errors |
| `Tooba.AccessControl.Infrastructure` | Build succeeded, 0 errors |
| `Tooba.Host` | Build succeeded, 0 errors |
| `Tooba.Host.Tests` | Build succeeded, 0 errors |

## 2. Focused tests

| Filter | Result |
| ------ | ------ |
| `HostReadinessBoundaryGuardTests` (new readiness boundary guard + behavior parity) | PASS |
| `HostAuthorizationEvacuationGuardTests` | PASS |
| `AuthorizationFoundationTests` (incl. new `AppliedVersion_means_successfully_applied_only`) | PASS |
| `TmarCompleteReferenceStructureGateTests` (AccessControl structure/manifest guard) | PASS |
| Total | **30 / 30 PASS** |

Command:

```text
dotnet test src/backend/Host/Tooba.Host.Tests --filter "FullyQualifiedName~HostReadinessBoundaryGuardTests|FullyQualifiedName~HostAuthorizationEvacuationGuardTests|FullyQualifiedName~AuthorizationFoundationTests|FullyQualifiedName~TmarCompleteReferenceStructureGateTests"
```

## 3. Boundary verification

```text
Host sources -> Tooba.AccessControl.Infrastructure.Authorization / SpiceDbAuthorizationOptions / SpiceDbHealthProbe = ZERO
Host readiness -> Tooba.AccessControl.Contracts.Readiness.IAuthorizationReadinessProbe                       = YES
AccessControl.Contracts csproj -> foreign Application/Infrastructure/Domain                                  = ZERO
readiness result/label secret leakage                                                                        = ZERO
```

## 4. Pre-existing failures (parity with `main`, not introduced)

| Test | On `main` | On this change | Note |
| ---- | --------- | -------------- | ---- |
| `HostHealthEndpointTests` (both cases) | FAIL `duplicate_error_descriptor:reservation.policy.initial.invalid` | FAIL identical | Verified by stashing the change and re-running on clean `main`; unrelated to Authorization readiness |
| `PaymentArchitectureGuardTests.Payment_endpoints_cqrs_and_host_ownership_are_enforced` | FAIL | FAIL (unchanged) | pre-existing Host-evacuation debt |

## 5. Repair iterations

`MAX_REPAIR_ITERATIONS = 1`. One repair iteration used: the readiness boundary guard initially
asserted `DoesNotContain("Token")`, which also matched the non-secret preserved label
`spicedb-token-missing`; the assertion was narrowed to the real secret surface (`string Token`,
`string Endpoint`) and the guard then passed. No ambiguous second failure.

## 6. Guard additions

- `src/backend/Host/Tooba.Host.Tests/Architecture/HostReadinessBoundaryGuardTests.cs`
  - Host readiness source has ZERO Authorization infrastructure coupling.
  - The readiness seam lives in `AccessControl.Contracts` with no foreign layer/secret surface.
  - Mode/pre-check semantics parity (`Disabled`, `InMemory`, endpoint-missing, token-missing).
  - Probe-disabled skips remote probe and stays ready.
  - Probe-enabled reports `spicedb-unreachable`.
  - No secret/endpoint in result labels.
  - `Program.cs` has no Authorization infrastructure import.
- `AuthorizationFoundationTests.AppliedVersion_means_successfully_applied_only`
  - no-op request → `null`; failing real write → `null`.
