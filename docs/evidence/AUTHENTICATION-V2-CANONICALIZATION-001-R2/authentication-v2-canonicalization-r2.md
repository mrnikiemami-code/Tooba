# AUTHENTICATION-V2-CANONICALIZATION-001-R2 — Touched Identity Infrastructure Surface Repair

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: `tooba-main`
Scope: only the Identity.Infrastructure surface touched by AUTHENTICATION-V2-CANONICALIZATION-001/-R1.
Not started: independent Identity recovery, CustomerProfile recovery, next Host folder, general Identity redesign.

Predecessor commit: `1befd8c7c95d6727797847ced0236119072d52f2`

R1 was accepted for: canonical `ApiResponseFactory` path; Host -> Identity Contracts-only boundary; removal of
direct Host `Identity.Application`/`Domain` coupling. R1 was **not** redone here.

## 1. IdentityServices.cs no longer a root multi-responsibility dump

`src/backend/Modules/Identity/Tooba.Identity.Infrastructure/IdentityServices.cs` (456 LOC, 9 unrelated types)
**deleted**. Its existing types were split into cohesive capability folders/files, namespaces aligned 1:1 with
their folder:

| Responsibility | Final location | Namespace |
| --- | --- | --- |
| Authentication | `Authentication/IdentityAuthenticationService.cs` | `...Infrastructure.Authentication` |
| PasswordHashing | `PasswordHashing/AspNetPasswordHashingService.cs` | `...Infrastructure.PasswordHashing` |
| Otp | `Otp/InMemoryOtpChallengeService.cs`, `Otp/CapturingOtpSender.cs`, `Otp/IdentityOtpLoginService.cs`, `Otp/OtpDeliveryOptions.cs`, `Otp/OtpDeliveryInstrumentation.cs`, `Otp/OtpDeliveryProviderSender.cs`, `Otp/WebhookOtpDeliveryProvider.cs`, `Otp/FailClosedOtpDeliveryProvider.cs`, `Otp/CapturingOtpDeliveryProvider.cs` | `...Infrastructure.Otp` |
| Sessions | `Sessions/IdentityLifecycleService.cs` | `...Infrastructure.Sessions` |
| Contacts | `Contacts/EfIdentityContactLookup.cs` | `...Infrastructure.Contacts` |
| Adapters | `Adapters/ActorContactLookupAdapter.cs`, `Adapters/ActorIdentifierResolverAdapter.cs` | `...Infrastructure.Adapters` |
| ExternalIdentity | `ExternalIdentity/EfExternalIdentityDirectory.cs` | `...Infrastructure.ExternalIdentity` |
| Mfa | `Mfa/EfMfaEnrollmentStore.cs` | `...Infrastructure.Mfa` |
| SecurityEvents | `SecurityEvents/InMemoryIdentitySecurityEventSink.cs` | `...Infrastructure.SecurityEvents` |

No type was redesigned. `IdentityOtpLoginService` / `OtpDeliveryProviderSender` / the OTP delivery providers were
grouped under `Otp` because every one of them is an OTP capability; the classification is stated explicitly so an
Architect can re-classify without archaeology.

## 2. Obsolete duplicate/legacy types removed

`IdentityDuplicateIdentifierException` (in the deleted root dump) had **no valid consumer** after the Contracts
fault migration. Deleted.

The only duplicate-identifier fault is now `Tooba.Identity.Contracts.Problems.IdentityDuplicateIdentifierFault`,
which is the only type the Host boundary catches (`AuthenticationHttpBoundary.RegisterAsync`).

## 3. Hard-coded user-facing text removed from the touched production surface

`IdentityLifecycleService` and `IdentityAuthenticationService` previously threw exceptions carrying Persian
user-facing sentences. They now throw with canonical machine codes:

- identifier-not-owned -> `InvalidOperationException(IdentityErrorCodes.ValidationFailed)`
- wrong current password -> `InvalidOperationException(IdentityErrorCodes.PasswordChangeFailed)`
- password policy failures -> `ArgumentException(IdentityErrorCodes.ValidationFailed, nameof(password))`

No second localization system was created; the canonical `identity.*` code + resource/catalog mechanism is reused.
`Identity.Domain` `LoginIdentifierNormalizer` argument messages were deliberately **not** changed: they are internal
developer/diagnostic arguments outside the R1/R2-touched surface and on a pre-existing frozen path; no agent-visible
surface could confirm text or safety impact, so no speculative broad change was made.

## 4. Exact path <-> namespace alignment

Every folder in `Tooba.Identity.Infrastructure` maps 1:1 to its namespace
(`Tooba.Identity.Infrastructure.<Folder>`), except `Persistence/Migrations`, which retains the EF-generated
block-scoped namespace `Tooba.Identity.Infrastructure.Persistence.Migrations`. Those files are generated code,
untouched by R1/R2 (`git log` shows last change = the original `3f0c6d1c` migration commit), and rewriting their
namespace would add merge risk to generated artifacts. Schema/migrations unchanged.

## 5. Root retention

Root now contains only:

- `IdentityModule.cs` (module composition root),
- `IdentityOutboxRegistration.cs` (outbox module registration descriptor).

## 6. Re-read verification of every production file changed by R1/R2

| Check | Result |
| --- | --- |
| cohesive responsibility / correct folder | OK (capability folders above) |
| exact namespace | OK (1:1 with folder; EF migrations exception documented) |
| no root dump | OK (`IdentityServices.cs` deleted) |
| no obsolete duplicate type | OK (`IdentityDuplicateIdentifierException` deleted; 1 declaration left = guard assertion) |
| no hard-coded user-facing localized text | OK (touched surface clean) |
| no foreign Application/Infrastructure/Domain leakage | OK (zero `Tooba.Identity.Application`/`Domain` in Host production) |
| no parallel canonical mechanism | OK (`Parallel-Problem-Pipeline = ZERO` from R1 retained) |

## 7. Preservation

- 13 auth routes, HTTP methods and semantics: unchanged.
- `identity.*` machine codes: unchanged.
- auth/session behavior, Bearer + `tooba_session`, OTP/password/reset, tenant-spoof, throttle: unchanged.
- telemetry event names and correlation: unchanged.
- schema/migrations: unchanged.
- `Behavior-Change = NONE`.

## 8. Tests (evidence)

- Full solution build: only the pre-existing `Tooba.Order.Tests.OrderSellerPanelArchitectureGuardTests`
  missing `Tooba.AccessControl` reference errors remain.
- Focused suite (guards + Identity/auth/OTP/checkout/boundary/storefront/customer-profile):
  **62 passed / 10 skipped (Docker unavailable) / 0 failed**.
- One deterministic local repair was required: `CheckoutIdentityContractTests.Development_otp_fixture_is_environment_gated`
  read `IdentityLifecycleService.cs` by path; the path was updated to `Sessions/IdentityLifecycleService.cs`.
  Assertions were **not** weakened. A path-based `.csproj` assertion in `IdentityFoundationTests` still scans the
  whole Identity tree and passes.

Pre-existing failures not caused by this task (unchanged): `ErrorContractTests`,
`TmarSourceSizeAndInfraAppTests`, `Tooba.Order.Tests.OrderSellerPanelArchitectureGuardTests`.

## 9. Final disposition

```text
Cross-Module-Coupling-State = LEGAL_CONTRACTS_ONLY
Cross-Module-Join-State = NONE
Identity-Business-Authority-In-Host = ZERO
Identity-Persistence-Authority-In-Host = ZERO
Behavior-Change = NONE
Parallel-Problem-Pipeline = ZERO
Root-Dump-State = ZERO
Obsolete-Duplicate-Type-State = ZERO
Hard-Coded-User-Facing-Text-State = ZERO_IN_TOUCHED_SURFACE
Path-Namespace-Alignment = EXACT (EF-generated migrations excepted, documented)
```

Authentication is the **current Host-folder checkpoint**. The next Host folder has **not** been started.
